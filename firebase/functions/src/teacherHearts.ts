import * as admin from "firebase-admin";
import {HttpsError, onCall} from "firebase-functions/v2/https";

// The Vision Document starts every student at 50 Hearts.
// Teachers can view the standing of students in their own classes.
const STARTING_HEARTS = 50;
const DOCUMENT_ID = /^[A-Za-z0-9][A-Za-z0-9_-]*$/;

function requiredId(value: unknown, name: string): string {
  if (typeof value !== "string" || value.length === 0 || value.length > 96 ||
      !DOCUMENT_ID.test(value)) {
    throw new HttpsError("invalid-argument", `${name} must be a valid document ID.`);
  }
  return value;
}

/**
 * Read the Hearts for one member of a teacher-owned class.
 * No client-side writes or reward changes occur here.
 */
export const getStudentHeartsForTeacher = onCall(async (request) => {
  const teacherUid = request.auth?.uid;
  if (!teacherUid) {
    throw new HttpsError("unauthenticated", "Sign in to view student Hearts.");
  }
  const input = request.data;
  if (input === null || typeof input !== "object" || Array.isArray(input)) {
    throw new HttpsError("invalid-argument", "A class ID and student ID are required.");
  }
  const classId = requiredId(input.classId, "classId");
  const studentUid = requiredId(input.studentUid, "studentUid");

  const firestore = admin.firestore();
  const classRef = firestore.collection("classes").doc(classId);
  const [classSnapshot, memberSnapshot, standingSnapshot] = await Promise.all([
    classRef.get(),
    classRef.collection("members").doc(studentUid).get(),
    firestore.collection("studentStanding").doc(studentUid).get(),
  ]);

  if (!classSnapshot.exists || classSnapshot.data()?.ownerUid !== teacherUid ||
      !memberSnapshot.exists || memberSnapshot.data()?.role !== "student") {
    throw new HttpsError("permission-denied", "You can view Hearts only for students in your class.");
  }

  const storedHearts = standingSnapshot.data()?.hearts;
  const hearts = storedHearts === undefined ? STARTING_HEARTS : storedHearts;
  if (typeof hearts !== "number" || !Number.isSafeInteger(hearts) ||
      hearts < 0 || hearts > STARTING_HEARTS) {
    throw new HttpsError("failed-precondition", "This student has an invalid Heart balance.");
  }
  return {hearts};
});

type HeartChangeKind = "deduct" | "restore";
const REASONS = ["late", "homework", "disruption", "other", "recovery"] as const;

/**
 * Teacher-only, audited Heart adjustment. The transaction keeps balance, reset
 * and audit together. A requestId makes retries idempotent.
 */
export const adjustStudentHeartsForTeacher = onCall(async (request) => {
  const teacherUid = request.auth?.uid;
  if (!teacherUid) throw new HttpsError("unauthenticated", "Teacher sign-in required.");
  const input = request.data;
  if (!input || typeof input !== "object" || Array.isArray(input)) {
    throw new HttpsError("invalid-argument", "Heart change details are required.");
  }
  const classId = requiredId(input.classId, "classId");
  const studentUid = requiredId(input.studentUid, "studentUid");
  const requestId = requiredId(input.requestId, "requestId");
  const kind: HeartChangeKind = input.kind === "deduct" || input.kind === "restore" ?
    input.kind : (() => { throw new HttpsError("invalid-argument", "Invalid Heart action."); })();
  const amount = input.amount;
  if (typeof amount !== "number" || !Number.isSafeInteger(amount) ||
      amount < 1 || amount > STARTING_HEARTS) {
    throw new HttpsError("invalid-argument", "Amount must be 1 to 50.");
  }
  const reason = input.reason;
  if (typeof reason !== "string" || !REASONS.some((value) => value === reason)) {
    throw new HttpsError("invalid-argument", "Choose a valid reason.");
  }
  if ((kind === "restore") !== (reason === "recovery")) {
    throw new HttpsError("invalid-argument", "Recovery reason must match a restoration.");
  }
  const note = input.note === undefined ? "" : input.note;
  if (typeof note !== "string" || note.length > 200 ||
      (reason === "other" && !note.trim())) {
    throw new HttpsError("invalid-argument", "Provide a short note for other infractions.");
  }

  const db = admin.firestore();
  const classRef = db.collection("classes").doc(classId);
  const memberRef = classRef.collection("members").doc(studentUid);
  const standingRef = db.collection("studentStanding").doc(studentUid);
  const walletRef = db.collection("wallets").doc(studentUid);
  const auditRef = db.collection("heartAdjustments").doc(requestId);

  return db.runTransaction(async (tx) => {
    const [classSnap, memberSnap, standingSnap, walletSnap, auditSnap] = await Promise.all([
      tx.get(classRef), tx.get(memberRef), tx.get(standingRef),
      tx.get(walletRef), tx.get(auditRef),
    ]);
    if (!classSnap.exists || classSnap.data()?.ownerUid !== teacherUid ||
        !memberSnap.exists || memberSnap.data()?.role !== "student") {
      throw new HttpsError("permission-denied", "You may change Hearts only for your class students.");
    }
    if (auditSnap.exists) {
      const prior = auditSnap.data() ?? {};
      if (prior.teacherUid !== teacherUid || prior.studentUid !== studentUid ||
          prior.classId !== classId || prior.kind !== kind ||
          prior.amount !== amount || prior.reason !== reason || prior.note !== note) {
        throw new HttpsError("already-exists", "Request ID was already used for a different change.");
      }
      return {hearts: prior.after, reachedZero: prior.reachedZero === true, alreadyProcessed: true};
    }
    const oldValue = standingSnap.data()?.hearts ?? STARTING_HEARTS;
    if (typeof oldValue !== "number" || !Number.isSafeInteger(oldValue) ||
        oldValue < 0 || oldValue > STARTING_HEARTS) {
      throw new HttpsError("failed-precondition", "Invalid stored Heart balance.");
    }
    const hearts = kind === "deduct" ?
      Math.max(0, oldValue - amount) : Math.min(STARTING_HEARTS, oldValue + amount);
    if (hearts === oldValue) {
      throw new HttpsError("failed-precondition", "The Heart balance cannot change further.");
    }
    const reachedZero = oldValue > 0 && hearts === 0;
    const now = admin.firestore.Timestamp.now();
    tx.set(standingRef, {
      hearts,
      updatedAt: now,
      updatedBy: teacherUid,
      ...(reachedZero ? {powersRevoked: true, zeroHeartsAt: now} : {}),
    }, {merge: true});
    if (reachedZero) {
      // Reset the server-owned economy and known power inventories atomically.
      tx.set(walletRef, {
        xp: 0, crystals: 0, gold: 0, level: 1,
        unlockedPowers: [], activePowers: [], equippedPowers: [],
        updatedAt: now,
      }, {merge: true});
      tx.set(db.collection("walletLedger").doc("heartReset_" + requestId), {
        uid: studentUid, classId, eventType: "heart-zero-reset",
        xpBefore: walletSnap.data()?.xp ?? 0,
        crystalsBefore: walletSnap.data()?.crystals ?? 0,
        goldBefore: walletSnap.data()?.gold ?? 0,
        serverRecordedAt: now,
      });
    }
    tx.set(auditRef, {
      teacherUid, classId, studentUid, requestId, kind, amount, reason, note,
      before: oldValue, after: hearts, reachedZero, createdAt: now,
    });
    return {hearts, reachedZero, alreadyProcessed: false};
  });
});

/** Teacher-only history, newest first; never exposes other classes' records. */
export const getStudentHeartHistoryForTeacher = onCall(async (request) => {
  const teacherUid = request.auth?.uid;
  if (!teacherUid) throw new HttpsError("unauthenticated", "Teacher sign-in required.");
  const input = request.data;
  if (!input || typeof input !== "object" || Array.isArray(input)) {
    throw new HttpsError("invalid-argument", "Class and student IDs required.");
  }
  const classId = requiredId(input.classId, "classId");
  const studentUid = requiredId(input.studentUid, "studentUid");
  const db = admin.firestore();
  const classRef = db.collection("classes").doc(classId);
  const [classSnap, memberSnap] = await Promise.all([
    classRef.get(), classRef.collection("members").doc(studentUid).get(),
  ]);
  if (!classSnap.exists || classSnap.data()?.ownerUid !== teacherUid ||
      !memberSnap.exists || memberSnap.data()?.role !== "student") {
    throw new HttpsError("permission-denied", "Not authorized to view Heart history.");
  }
  // Query by class and student, then sort in memory to avoid a composite-index requirement.
  const entries = await db.collection("heartAdjustments")
    .where("classId", "==", classId).where("studentUid", "==", studentUid)
    .limit(100).get();
  return {entries: entries.docs.map((doc) => {
    const d = doc.data();
    return {kind: d.kind, amount: d.amount, reason: d.reason, note: d.note,
      before: d.before, after: d.after, atMs: d.createdAt?.toMillis?.() ?? 0};
  }).sort((a, b) => b.atMs - a.atMs)};
});
