import * as admin from "firebase-admin";
import {FieldValue} from "firebase-admin/firestore";
import {createHash} from "node:crypto";
import {HttpsError, onCall} from "firebase-functions/v2/https";

const options = {region: "northamerica-northeast1", maxInstances: 3};
const db = () => admin.firestore();
type Data = Record<string, unknown>;
const STARTING_HEARTS = 50;
const WORKSHOP = "blacksmith-workshop";

function uid(request: {auth?: {uid: string} | null}): string {
  if (!request.auth) throw new HttpsError("unauthenticated", "Sign in to continue.");
  return request.auth.uid;
}
function text(data: Data, key: string, max = 96): string {
  if (data === null || typeof data !== "object" || Array.isArray(data))
    throw new HttpsError("invalid-argument", "Request data must be an object.");
  const value = data[key];
  if (typeof value !== "string" || !value.trim() || value.trim().length > max)
    throw new HttpsError("invalid-argument", `A valid ${key} is required.`);
  return value.trim();
}
function id(data: Data, key: string): string {
  const value = text(data, key, 128);
  if (!/^[A-Za-z0-9_-]+$/.test(value)) throw new HttpsError("invalid-argument", `Invalid ${key}.`);
  return value;
}
function number(value: unknown, fallback = 0): number {
  if (value === undefined) return fallback;
  if (typeof value !== "number" || !Number.isSafeInteger(value) || value < 0)
    throw new HttpsError("failed-precondition", "Student status needs administrator review.");
  return value;
}
function receipt(...parts: string[]): string {
  return createHash("sha256").update(JSON.stringify(parts)).digest("hex");
}
const now = () => FieldValue.serverTimestamp();

export const ensureStudentStatus = onCall(options, async (request) => {
  const student = uid(request);
  return db().runTransaction(async (tx) => {
    const profileRef = db().doc(`users/${student}`);
    const walletRef = db().doc(`wallets/${student}`);
    const standingRef = db().doc(`studentStanding/${student}`);
    const [profile, wallet, standing] = await tx.getAll(profileRef, walletRef, standingRef);
    if (profile.data()?.role !== "student") throw new HttpsError("permission-denied", "A student account is required.");
    // Never overwrite an existing balance, including zero Hearts.
    if (!wallet.exists) tx.create(walletRef, {xp: 0, level: 1, crystals: 0, gold: 0, epoch: 0, updatedAt: now()});
    if (!standing.exists) tx.create(standingRef, {hearts: STARTING_HEARTS, powerEpoch: 0, updatedAt: now()});
    return {initialized: true};
  });
});

export const adjustStudentHearts = onCall(options, async (request) => {
  const teacher = uid(request), input = request.data as Data;
  const classId = id(input, "classId"), student = id(input, "studentId"), eventId = id(input, "eventId");
  const reason = text(input, "reason", 140), delta = input.delta;
  if (typeof delta !== "number" || !Number.isInteger(delta) || delta === 0 || Math.abs(delta) > 50)
    throw new HttpsError("invalid-argument", "Choose a Heart adjustment between -50 and 50.");
  const eventKey = receipt(teacher, classId, student, eventId);
  return db().runTransaction(async (tx) => {
    const classRef = db().doc(`classes/${classId}`), memberRef = classRef.collection("members").doc(student);
    const profileRef = db().doc(`users/${student}`), standingRef = db().doc(`studentStanding/${student}`);
    const walletRef = db().doc(`wallets/${student}`), eventRef = db().doc(`heartAdjustments/${eventKey}`);
    const [room, member, profile, standing, wallet, prior] = await tx.getAll(classRef, memberRef, profileRef, standingRef, walletRef, eventRef);
    if (room.data()?.ownerUid !== teacher || !member.exists || profile.data()?.role !== "student")
      throw new HttpsError("permission-denied", "Only this student's class teacher can change Hearts.");
    if (prior.exists) {
      if (prior.data()?.rolledBackAt)
        throw new HttpsError("failed-precondition", "This adjustment was reversed. Refresh before making another change.");
      if (prior.data()?.delta !== delta || prior.data()?.reason !== reason)
        throw new HttpsError("already-exists", "That request was already used for a different adjustment.");
      return {hearts: prior.data()?.hearts, reset: prior.data()?.reset, alreadyProcessed: true};
    }
    const before = number(standing.data()?.hearts, STARTING_HEARTS), hearts = before + delta;
    if (before > STARTING_HEARTS || hearts < 0 || hearts > STARTING_HEARTS)
      throw new HttpsError("out-of-range", "Hearts must stay between 0 and 50. Refresh and choose a smaller change.");
    const reset = before > 0 && hearts === 0;
    const epoch = number(wallet.data()?.epoch);
    tx.set(standingRef, {hearts, updatedAt: now(), lastAdjustmentId: eventKey,
      ...(reset ? {powersRevoked: true, powerEpoch: FieldValue.increment(1)} : {}),
    }, {merge: true});
    if (reset) {
      // One atomic consequence. Old Power grants are invalid after the epoch changes.
      tx.set(walletRef, {xp: 0, crystals: 0, gold: 0, level: 1, epoch: epoch + 1, lastMutationId: eventKey, updatedAt: now()}, {merge: true});
    } else if (!wallet.exists) {
      tx.create(walletRef, {xp: 0, crystals: 0, gold: 0, level: 1, epoch: 0, lastMutationId: eventKey, updatedAt: now()});
    }
    tx.create(eventRef, {teacherUid: teacher, uid: student, classId, delta, reason, before, hearts, reset,
      beforeStanding: standing.exists ? standing.data() : null, beforeWallet: wallet.exists ? wallet.data() : null,
      restoreWallet: reset || !wallet.exists, occurredAt: now()});
    tx.create(db().doc(`studentActivity/${student}/entries/${eventKey}`), {
      kind: reset ? "standing-reset" : "hearts", title: reset ? "A fresh start" : delta > 0 ? "Hearts restored" : "Classroom standing updated",
      detail: reason, deltaHearts: delta, deltaXp: reset ? -number(wallet.data()?.xp) : 0, occurredAt: now(),
    });
    return {hearts, reset, alreadyProcessed: false};
  });
});

export const getClassStudentStatus = onCall(options, async (request) => {
  const teacher = uid(request), input = request.data as Data;
  const classId = id(input, "classId"), student = id(input, "studentId");
  return db().runTransaction(async (tx) => {
    const [room, member, profile, standing, wallet] = await tx.getAll(
      db().doc(`classes/${classId}`), db().doc(`classes/${classId}/members/${student}`),
      db().doc(`users/${student}`), db().doc(`studentStanding/${student}`), db().doc(`wallets/${student}`));
    if (room.data()?.ownerUid !== teacher || !member.exists || profile.data()?.role !== "student")
      throw new HttpsError("permission-denied", "Only the class teacher can view this student's standing.");
    return {hearts: number(standing.data()?.hearts, STARTING_HEARTS), xp: number(wallet.data()?.xp)};
  });
});

export const setWorkshopAssignment = onCall(options, async (request) => {
  const teacher = uid(request), input = request.data as Data, classId = id(input, "classId");
  if (typeof input.isOpen !== "boolean") throw new HttpsError("invalid-argument", "Choose whether to assign the workshop.");
  return db().runTransaction(async (tx) => {
    const room = await tx.get(db().doc(`classes/${classId}`));
    if (room.data()?.ownerUid !== teacher) throw new HttpsError("permission-denied", "Only the class teacher can assign this quest.");
    tx.set(db().doc(`classes/${classId}/assignments/${WORKSHOP}`), {
      questDefinitionId: WORKSHOP, contentVersion: "1", title: "The Aurora's Repair",
      state: input.isOpen ? "open" : "closed", rewardsPolicy: {challengeXp: 25, completionXp: 100},
      updatedAt: now(), updatedBy: teacher,
    }, {merge: true});
    return {isOpen: input.isOpen};
  });
});

export const blacksmithQuest = onCall(options, async (request) => {
  const student = uid(request), input = request.data as Data;
  const classId = id(input, "classId"), assignmentId = id(input, "assignmentId");
  const action = text(input, "action"), eventId = action === "load" ? "load" : id(input, "eventId");
  if (!["load", "lesson", "answer", "complete"].includes(action) || input.contentVersion !== "1")
    throw new HttpsError("invalid-argument", "This workshop version is unavailable.");
  const station = input.station ?? -1, choice = input.choice ?? -1;
  if ((action === "lesson" || action === "answer") &&
      (typeof station !== "number" || !Number.isInteger(station) || station < 0 || station > 2))
    throw new HttpsError("invalid-argument", "Choose a valid craft.");
  if (action === "answer" && (typeof choice !== "number" || !Number.isInteger(choice) || choice < 0 || choice > 2))
    throw new HttpsError("invalid-argument", "Choose a valid answer.");
  return db().runTransaction(async (tx) => {
    const roomRef = db().doc(`classes/${classId}`), assignmentRef = roomRef.collection("assignments").doc(assignmentId);
    const progressRef = db().doc(`users/${student}/classProgress/${classId}/assignments/${assignmentId}`);
    const eventRef = progressRef.collection("events").doc(eventId);
    const walletRef = db().doc(`wallets/${student}`), standingRef = db().doc(`studentStanding/${student}`);
    const [room, member, profile, assignment, progress, prior, wallet, standing] = await tx.getAll(
      roomRef, roomRef.collection("members").doc(student), db().doc(`users/${student}`),
      assignmentRef, progressRef, eventRef, walletRef, standingRef);
    if (!room.exists || !member.exists || profile.data()?.role !== "student")
      throw new HttpsError("permission-denied", "Join this class before saving workshop progress.");
    if (assignment.data()?.questDefinitionId !== WORKSHOP || assignment.data()?.contentVersion !== "1")
      throw new HttpsError("failed-precondition", "Ask your teacher to assign the workshop to this class.");
    const state = progress.data() ?? {};
    let lessonMask = number(state.lessonMask) & 7, completedMask = number(state.completedMask) & 7;
    let turnedIn = state.turnedIn === true, verifiedXp = number(state.verifiedXp), awardedXp = 0;
    if (prior.exists && action !== "load") {
      if (prior.data()?.action !== action || prior.data()?.station !== station || prior.data()?.choice !== choice)
        throw new HttpsError("already-exists", "That event ID was already used.");
      return {lessonMask, completedMask, turnedIn, verifiedXp, awardedXp: 0};
    }
    if (action !== "load") {
      if (assignment.data()?.state !== "open") throw new HttpsError("failed-precondition", "The teacher has closed this workshop.");
      if (number(standing.data()?.hearts, STARTING_HEARTS) === 0)
        throw new HttpsError("failed-precondition", "Ask your teacher to restore Hearts before earning more XP.");
      if (action === "lesson") lessonMask |= 1 << (station as number);
      if (action === "answer") {
        const bit = 1 << (station as number);
        if (!(lessonMask & bit)) throw new HttpsError("failed-precondition", "Read the craft note before answering.");
        // Private server validation; the client never supplies correctness or an XP amount.
        if (choice === station && !(completedMask & bit)) { completedMask |= bit; awardedXp = 25; }
      }
      if (action === "complete") {
        if (completedMask !== 7) throw new HttpsError("failed-precondition", "Complete all three crafts first.");
        if (!turnedIn) { turnedIn = true; awardedXp = 100; }
      }
      const balance = number(wallet.data()?.xp) + awardedXp;
      if (!Number.isSafeInteger(balance)) throw new HttpsError("out-of-range", "XP balance needs administrator review.");
      verifiedXp += awardedXp;
      tx.set(progressRef, {questDefinitionId: WORKSHOP, contentVersion: "1", lessonMask, completedMask, turnedIn, verifiedXp,
        completed: turnedIn, updatedAt: now()}, {merge: true});
      tx.create(eventRef, {action, station, choice, awardedXp, occurredAt: now()});
      if (!standing.exists) tx.create(standingRef, {hearts: STARTING_HEARTS, powerEpoch: 0, updatedAt: now()});
      if (awardedXp > 0) {
        const key = receipt(student, classId, assignmentId, eventId);
        tx.set(walletRef, {xp: balance, ...(!wallet.exists ? {level: 1, crystals: 0, gold: 0, epoch: 0} : {}), lastMutationId: key, updatedAt: now()}, {merge: true});
        tx.create(db().doc(`walletLedger/${key}`), {uid: student, classId, assignmentId, eventId, amount: awardedXp, rewardType: "xp", serverRecordedAt: now()});
        tx.create(db().doc(`studentActivity/${student}/entries/${key}`), {
          kind: "xp", title: action === "complete" ? "Repair kit delivered" : ["Anvil craft mastered", "Fasteners counted", "Forge craft mastered"][station as number],
          detail: "The Aurora's Repair", deltaXp: awardedXp, deltaHearts: 0, occurredAt: now(),
        });
      }
    }
    return {lessonMask, completedMask, turnedIn, verifiedXp, awardedXp};
  });
});
