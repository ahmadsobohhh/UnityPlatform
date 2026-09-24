import * as admin from "firebase-admin";
import {setGlobalOptions} from "firebase-functions/v2";
import {HttpsError, onCall} from "firebase-functions/v2/https";

admin.initializeApp();

const db = admin.firestore();

// Keep the game services close to the Canadian school audience by default. Change
// this before the first deploy only if the Firebase project is intentionally hosted
// elsewhere; Unity callable clients must use the same region.
setGlobalOptions({region: "northamerica-northeast1", maxInstances: 10});

const MAX_CHALLENGE_XP = 50;
const MAX_COMPLETION_XP = 200;
const MAX_CODE_LENGTH = 32;
const MAX_ID_LENGTH = 96;

type RecordValue = Record<string, unknown>;
type AssignmentState = "draft" | "open" | "closed" | "archived";

interface RewardPolicy {
  challengeXp: number;
  completionXp: number;
}

interface QuestEventResult {
  alreadyProcessed: boolean;
  correct: boolean | null;
  awardedXp: number;
  completed: boolean;
  completedChallengeIds: string[];
}

function hasOwn(value: RecordValue, key: string): boolean {
  return Object.prototype.hasOwnProperty.call(value, key);
}

function record(value: unknown, label: string): RecordValue {
  if (value === null || Array.isArray(value) || typeof value !== "object") {
    throw new HttpsError("invalid-argument", `${label} must be an object.`);
  }

  return value as RecordValue;
}

function requiredString(value: RecordValue, key: string, maxLength = 256): string {
  const candidate = value[key];
  if (typeof candidate !== "string") {
    throw new HttpsError("invalid-argument", `${key} must be a string.`);
  }

  const clean = candidate.trim();
  if (clean.length === 0 || clean.length > maxLength) {
    throw new HttpsError("invalid-argument", `${key} must be between 1 and ${maxLength} characters.`);
  }

  return clean;
}

function optionalString(value: RecordValue, key: string, maxLength = 256): string | undefined {
  if (!hasOwn(value, key) || value[key] === null || value[key] === undefined) {
    return undefined;
  }

  return requiredString(value, key, maxLength);
}

function documentId(value: RecordValue, key: string): string {
  const id = requiredString(value, key, MAX_ID_LENGTH);
  if (!/^[A-Za-z0-9][A-Za-z0-9_-]*$/.test(id)) {
    throw new HttpsError("invalid-argument", `${key} may contain only letters, numbers, underscores, and hyphens.`);
  }

  return id;
}

function optionalDocumentId(value: RecordValue, key: string): string | undefined {
  if (!hasOwn(value, key) || value[key] === null || value[key] === undefined) {
    return undefined;
  }

  return documentId(value, key);
}

function requiredUid(request: {auth?: {uid: string} | null}): string {
  const uid = request.auth?.uid;
  if (!uid) {
    throw new HttpsError("unauthenticated", "Sign in before calling this game service.");
  }

  return uid;
}

function normalizedClassCode(rawCode: string): string {
  const code = rawCode.trim().toUpperCase();
  if (code.length < 4 || code.length > MAX_CODE_LENGTH || !/^[A-Z0-9-]+$/.test(code)) {
    throw new HttpsError("invalid-argument", "Enter a valid class code.");
  }

  return code;
}

function boundedWholeNumber(value: unknown, fallback: number, maximum: number, label: string): number {
  if (value === undefined || value === null) {
    return fallback;
  }

  if (typeof value !== "number" || !Number.isSafeInteger(value) || value < 0 || value > maximum) {
    throw new HttpsError("invalid-argument", `${label} must be a whole number from 0 to ${maximum}.`);
  }

  return value;
}

function rewardPolicyFromInput(value: RecordValue): RewardPolicy {
  const raw = hasOwn(value, "rewardsPolicy") ? record(value.rewardsPolicy, "rewardsPolicy") : {};
  return {
    challengeXp: boundedWholeNumber(raw.challengeXp, 25, MAX_CHALLENGE_XP, "rewardsPolicy.challengeXp"),
    completionXp: boundedWholeNumber(raw.completionXp, 100, MAX_COMPLETION_XP, "rewardsPolicy.completionXp"),
  };
}

function rewardPolicyFromStored(value: unknown): RewardPolicy {
  if (value === null || Array.isArray(value) || typeof value !== "object") {
    return {challengeXp: 0, completionXp: 0};
  }

  const raw = value as RecordValue;
  const challengeXp = typeof raw.challengeXp === "number" && Number.isSafeInteger(raw.challengeXp)
    ? Math.min(Math.max(raw.challengeXp, 0), MAX_CHALLENGE_XP)
    : 0;
  const completionXp = typeof raw.completionXp === "number" && Number.isSafeInteger(raw.completionXp)
    ? Math.min(Math.max(raw.completionXp, 0), MAX_COMPLETION_XP)
    : 0;
  return {challengeXp, completionXp};
}

function stringList(value: unknown, label: string, maximumItems: number, itemLength = 80): string[] {
  if (value === undefined || value === null) {
    return [];
  }

  if (!Array.isArray(value) || value.length > maximumItems) {
    throw new HttpsError("invalid-argument", `${label} must contain no more than ${maximumItems} values.`);
  }

  const unique = new Set<string>();
  for (const item of value) {
    if (typeof item !== "string") {
      throw new HttpsError("invalid-argument", `${label} can contain only strings.`);
    }

    const clean = item.trim();
    if (clean.length === 0 || clean.length > itemLength) {
      throw new HttpsError("invalid-argument", `${label} contains an invalid value.`);
    }

    unique.add(clean);
  }

  return [...unique];
}

function epochMillis(value: unknown, label: string): number | null {
  if (value === undefined || value === null) {
    return null;
  }

  if (typeof value !== "number" || !Number.isSafeInteger(value) || value < 0) {
    throw new HttpsError("invalid-argument", `${label} must be Unix milliseconds or null.`);
  }

  return value;
}

function timestampMillis(value: unknown): number | null {
  if (value && typeof value === "object" && "toMillis" in value &&
    typeof (value as {toMillis?: unknown}).toMillis === "function") {
    return (value as {toMillis: () => number}).toMillis();
  }

  return null;
}

function assignmentState(value: unknown): AssignmentState {
  // `published` appeared in an early design note. Normalize it here so an old
  // management client can be upgraded without accidentally releasing a duplicate
  // state vocabulary alongside the Unity QuestAssignmentRecord's `Open` value.
  if (value === "published") {
    return "open";
  }
  if (value !== "draft" && value !== "open" && value !== "closed" && value !== "archived") {
    throw new HttpsError("invalid-argument", "state must be draft, open, closed, or archived.");
  }

  return value;
}

function storedString(value: RecordValue, key: string): string {
  const result = value[key];
  if (typeof result !== "string" || result.trim().length === 0) {
    throw new HttpsError("failed-precondition", `The assignment is missing ${key}.`);
  }

  return result.trim();
}

function ensureAssignmentIsAvailable(assignment: RecordValue, nowMillis: number): void {
  if (assignment.state !== "open") {
    throw new HttpsError("failed-precondition", "This quest is not currently assigned to students.");
  }

  const releaseAt = timestampMillis(assignment.releaseAt);
  const dueAt = timestampMillis(assignment.dueAt);
  if (releaseAt !== null && releaseAt > nowMillis) {
    throw new HttpsError("failed-precondition", "This quest has not been released yet.");
  }
  if (dueAt !== null && dueAt < nowMillis) {
    throw new HttpsError("failed-precondition", "This quest assignment has closed.");
  }
}

function normalisedAnswer(value: unknown): string | null {
  if (typeof value === "string") {
    return `s:${value.trim().toLocaleLowerCase("en-CA")}`;
  }
  if (typeof value === "number" && Number.isFinite(value)) {
    return `n:${value}`;
  }
  if (typeof value === "boolean") {
    return `b:${value}`;
  }

  return null;
}

function isResponseCorrect(challenge: RecordValue, response: unknown): boolean {
  const submitted = normalisedAnswer(response);
  if (submitted === null) {
    throw new HttpsError("invalid-argument", "response must be a string, number, or boolean.");
  }

  const accepted = Array.isArray(challenge.acceptedAnswers)
    ? challenge.acceptedAnswers
    : hasOwn(challenge, "answer") ? [challenge.answer] : [];
  if (accepted.length === 0) {
    throw new HttpsError("failed-precondition", "The private quest definition has no answer key for this challenge.");
  }

  return accepted.some((answer) => normalisedAnswer(answer) === submitted);
}

function challengeFromDefinition(definition: RecordValue, challengeId: string): RecordValue {
  const challenges = definition.challenges;
  if (challenges === null || Array.isArray(challenges) || typeof challenges !== "object") {
    throw new HttpsError("failed-precondition", "The private quest definition has no challenge map.");
  }

  const challenge = (challenges as RecordValue)[challengeId];
  if (challenge === null || Array.isArray(challenge) || typeof challenge !== "object") {
    throw new HttpsError("not-found", "That challenge is not part of the assigned quest.");
  }

  return challenge as RecordValue;
}

function requiredChallengeIds(definition: RecordValue): string[] {
  const ids = definition.requiredChallengeIds;
  if (!Array.isArray(ids) || ids.length === 0) {
    throw new HttpsError("failed-precondition", "The private quest definition has no requiredChallengeIds.");
  }

  return ids.map((id) => {
    if (typeof id !== "string" || !/^[A-Za-z0-9][A-Za-z0-9_-]*$/.test(id)) {
      throw new HttpsError("failed-precondition", "The private quest definition has an invalid challenge id.");
    }
    return id;
  });
}

function completedIdsFromProgress(value: RecordValue): Set<string> {
  const raw = value.completedChallengeIds;
  if (!Array.isArray(raw)) {
    return new Set<string>();
  }

  return new Set(raw.filter((id): id is string => typeof id === "string"));
}

function dataString(value: RecordValue, key: string, fallback = ""): string {
  return typeof value[key] === "string" ? value[key] as string : fallback;
}

/**
 * Atomically resolves a human-readable class code into the two membership indexes
 * used by the current Unity scenes. It is the secure replacement for client-side
 * class-code queries and batched membership writes.
 */
export const joinClassByCode = onCall(async (request) => {
  const uid = requiredUid(request);
  const input = record(request.data, "data");
  const code = normalizedClassCode(requiredString(input, "code", MAX_CODE_LENGTH));
  const profileRef = db.collection("users").doc(uid);

  let matching = await db.collection("classes").where("codeNormalized", "==", code).limit(2).get();
  // Classes created before codeNormalized was added remain joinable while the data is
  // migrated. This fallback can be removed after all historical classes are updated.
  if (matching.empty) {
    matching = await db.collection("classes").where("code", "==", code).limit(2).get();
  }

  if (matching.empty) {
    throw new HttpsError("not-found", "No class was found for that code.");
  }
  if (matching.size > 1) {
    throw new HttpsError("failed-precondition", "This class code is duplicated. Ask the teacher to create a new class.");
  }

  const classRef = matching.docs[0].ref;
  const result = await db.runTransaction(async (transaction) => {
    const [classSnapshot, profileSnapshot] = await Promise.all([
      transaction.get(classRef),
      transaction.get(profileRef),
    ]);

    if (!classSnapshot.exists) {
      throw new HttpsError("not-found", "This class no longer exists.");
    }
    if (!profileSnapshot.exists || profileSnapshot.data()?.role !== "student") {
      throw new HttpsError("permission-denied", "Only a student profile can join a class.");
    }

    const classData = classSnapshot.data() ?? {};
    const className = dataString(classData, "name", "Unnamed class");
    const classCode = dataString(classData, "code", code);
    const firstName = dataString(profileSnapshot.data() ?? {}, "firstName");
    const lastName = dataString(profileSnapshot.data() ?? {}, "lastName");
    const joinedAt = admin.firestore.Timestamp.now();
    const memberRef = classRef.collection("members").doc(uid);
    const indexRef = profileRef.collection("classes").doc(classRef.id);

    transaction.set(memberRef, {
      uid,
      role: "student",
      firstName,
      lastName,
      joinedAt,
    }, {merge: true});
    transaction.set(indexRef, {
      id: classRef.id,
      name: className,
      code: classCode,
      membershipRole: "student",
      joinedAt,
    }, {merge: true});

    return {classId: classRef.id, name: className, code: classCode};
  });

  return {joined: true, ...result};
});

/**
 * Lets the owner of a class publish a versioned quest assignment. Definitions remain
 * server-only because they can contain answer keys; this function only references an
 * already-authored definition/version.
 */
export const upsertQuestAssignment = onCall(async (request) => {
  const uid = requiredUid(request);
  const input = record(request.data, "data");
  const classId = documentId(input, "classId");
  const questDefinitionId = documentId(input, "questDefinitionId");
  const contentVersion = documentId(input, "contentVersion");
  const title = requiredString(input, "title", 120);
  const state = assignmentState(input.state ?? "draft");
  const portalPlan = stringList(input.portalPlan, "portalPlan", 12);
  const targetConcepts = stringList(input.targetConcepts, "targetConcepts", 20);
  const rewardsPolicy = rewardPolicyFromInput(input);
  const releaseAtMillis = epochMillis(input.releaseAtMs, "releaseAtMs");
  const dueAtMillis = epochMillis(input.dueAtMs, "dueAtMs");

  if (releaseAtMillis !== null && dueAtMillis !== null && dueAtMillis < releaseAtMillis) {
    throw new HttpsError("invalid-argument", "dueAtMs must be after releaseAtMs.");
  }

  const classRef = db.collection("classes").doc(classId);
  const classSnapshot = await classRef.get();
  if (!classSnapshot.exists || classSnapshot.data()?.ownerUid !== uid) {
    throw new HttpsError("permission-denied", "Only this class's teacher can manage assignments.");
  }

  const definitionRef = db.collection("questDefinitions").doc(questDefinitionId)
    .collection("versions").doc(contentVersion);
  if (!(await definitionRef.get()).exists) {
    throw new HttpsError("failed-precondition", "Create the private quest definition/version before assigning it.");
  }

  const assignmentId = optionalDocumentId(input, "assignmentId") ?? classRef.collection("assignments").doc().id;
  const assignmentRef = classRef.collection("assignments").doc(assignmentId);
  const now = admin.firestore.Timestamp.now();
  const existing = await assignmentRef.get();

  await assignmentRef.set({
    assignmentId,
    classId,
    questDefinitionId,
    contentVersion,
    title,
    state,
    portalPlan,
    targetConcepts,
    rewardsPolicy,
    releaseAt: releaseAtMillis === null ? null : admin.firestore.Timestamp.fromMillis(releaseAtMillis),
    dueAt: dueAtMillis === null ? null : admin.firestore.Timestamp.fromMillis(dueAtMillis),
    createdBy: existing.exists ? dataString(existing.data() ?? {}, "createdBy", uid) : uid,
    createdAt: existing.exists ? (existing.data()?.createdAt ?? now) : now,
    updatedBy: uid,
    updatedAt: now,
  }, {merge: true});

  return {assignmentId, state, classId, questDefinitionId, contentVersion};
});

/**
 * Records one idempotent game event. Answer validation happens against a private
 * Firestore definition; only the function can increase XP or write a wallet ledger.
 *
 * challenge-submitted: { classId, assignmentId, runId, eventId, eventType,
 *                        challengeId, response }
 * quest-completed:    { classId, assignmentId, runId, eventId, eventType }
 */
export const recordQuestEvent = onCall(async (request): Promise<QuestEventResult> => {
  const uid = requiredUid(request);
  const input = record(request.data, "data");
  const classId = documentId(input, "classId");
  const assignmentId = documentId(input, "assignmentId");
  const runId = documentId(input, "runId");
  const eventId = documentId(input, "eventId");
  const rawEventType = requiredString(input, "eventType", 32);
  const eventType = rawEventType === "challenge-submitted" || rawEventType === "quest-completed"
    ? rawEventType
    : (() => { throw new HttpsError("invalid-argument", "eventType must be challenge-submitted or quest-completed."); })();
  const challengeId = eventType === "challenge-submitted" ? documentId(input, "challengeId") : null;
  if (eventType === "challenge-submitted" && !hasOwn(input, "response")) {
    throw new HttpsError("invalid-argument", "response is required for challenge-submitted events.");
  }

  const classRef = db.collection("classes").doc(classId);
  const memberRef = classRef.collection("members").doc(uid);
  const assignmentRef = classRef.collection("assignments").doc(assignmentId);
  const profileRef = db.collection("users").doc(uid);
  const progressRef = db.collection("users").doc(uid).collection("classProgress")
    .doc(classId).collection("assignments").doc(assignmentId);
  const eventRef = progressRef.collection("events").doc(eventId);
  const runRef = progressRef.collection("runs").doc(runId);
  const walletRef = db.collection("wallets").doc(uid);
  const uniqueEventId = `${uid}_${classId}_${assignmentId}_${eventId}`;
  const walletLedgerRef = db.collection("walletLedger").doc(uniqueEventId);
  const auditRef = db.collection("questEventAudit").doc(uniqueEventId);

  return db.runTransaction(async (transaction): Promise<QuestEventResult> => {
    const [classSnapshot, memberSnapshot, assignmentSnapshot, profileSnapshot, progressSnapshot, eventSnapshot] = await Promise.all([
      transaction.get(classRef),
      transaction.get(memberRef),
      transaction.get(assignmentRef),
      transaction.get(profileRef),
      transaction.get(progressRef),
      transaction.get(eventRef),
    ]);

    if (!classSnapshot.exists || !memberSnapshot.exists || profileSnapshot.data()?.role !== "student") {
      throw new HttpsError("permission-denied", "Only a student enrolled in this class can record quest progress.");
    }

    // A client can safely retry a timed-out call even after an assignment closes or a
    // content version is retired. The event already exists, so this branch performs
    // no additional scoring or wallet mutation.
    if (eventSnapshot.exists) {
      const prior = eventSnapshot.data() ?? {};
      return {
        alreadyProcessed: true,
        correct: typeof prior.correct === "boolean" ? prior.correct : null,
        awardedXp: typeof prior.awardedXp === "number" ? prior.awardedXp : 0,
        completed: prior.completed === true,
        completedChallengeIds: Array.isArray(prior.completedChallengeIds)
          ? prior.completedChallengeIds.filter((id): id is string => typeof id === "string")
          : [],
      };
    }

    if (!assignmentSnapshot.exists) {
      throw new HttpsError("not-found", "That quest assignment does not exist.");
    }

    const assignment = assignmentSnapshot.data() ?? {};
    ensureAssignmentIsAvailable(assignment, Date.now());
    const questDefinitionId = storedString(assignment, "questDefinitionId");
    const contentVersion = storedString(assignment, "contentVersion");
    const definitionRef = db.collection("questDefinitions").doc(questDefinitionId)
      .collection("versions").doc(contentVersion);
    const definitionSnapshot = await transaction.get(definitionRef);
    if (!definitionSnapshot.exists) {
      throw new HttpsError("failed-precondition", "The assigned quest definition is unavailable.");
    }

    const definition = definitionSnapshot.data() ?? {};
    const progress = progressSnapshot.exists ? progressSnapshot.data() ?? {} : {};
    const completedIds = completedIdsFromProgress(progress);
    const rewards = rewardPolicyFromStored(assignment.rewardsPolicy);
    const wasCompleted = progress.completed === true;
    let correct: boolean | null = null;
    let awardedXp = 0;
    let completed = wasCompleted;

    if (eventType === "challenge-submitted") {
      const challenge = challengeFromDefinition(definition, challengeId!);
      correct = isResponseCorrect(challenge, input.response);
      if (correct && !completedIds.has(challengeId!)) {
        completedIds.add(challengeId!);
        awardedXp = rewards.challengeXp;
      }
    } else {
      const requiredIds = requiredChallengeIds(definition);
      const allRequiredChallengesCompleted = requiredIds.every((id) => completedIds.has(id));
      if (!allRequiredChallengesCompleted) {
        throw new HttpsError("failed-precondition", "Complete every required challenge before finishing this quest.");
      }
      completed = true;
      if (!wasCompleted) {
        awardedXp = rewards.completionXp;
      }
    }

    const sortedCompletedIds = [...completedIds].sort();
    const now = admin.firestore.Timestamp.now();
    const progressUpdate: Record<string, unknown> = {
      classId,
      assignmentId,
      questDefinitionId,
      contentVersion,
      completedChallengeIds: sortedCompletedIds,
      completedChallengeCount: sortedCompletedIds.length,
      completed,
      updatedAt: now,
    };
    if (completed && !wasCompleted) {
      progressUpdate.completedAt = now;
    }
    if (awardedXp > 0) {
      progressUpdate.earnedXp = admin.firestore.FieldValue.increment(awardedXp);
    }

    transaction.set(progressRef, progressUpdate, {merge: true});
    transaction.set(runRef, {
      runId,
      classId,
      assignmentId,
      lastEventId: eventId,
      lastEventType: eventType,
      lastChallengeId: challengeId,
      lastAnswerCorrect: correct,
      completedChallengeIds: sortedCompletedIds,
      completed,
      updatedAt: now,
      ...(awardedXp > 0 ? {earnedXp: admin.firestore.FieldValue.increment(awardedXp)} : {}),
    }, {merge: true});
    transaction.set(eventRef, {
      eventId,
      eventType,
      classId,
      assignmentId,
      runId,
      challengeId,
      correct,
      awardedXp,
      completed,
      completedChallengeIds: sortedCompletedIds,
      serverRecordedAt: now,
    });
    transaction.set(auditRef, {
      uid,
      eventId,
      eventType,
      classId,
      assignmentId,
      runId,
      challengeId,
      correct,
      awardedXp,
      completed,
      serverRecordedAt: now,
    });

    if (awardedXp > 0) {
      transaction.set(walletRef, {
        xp: admin.firestore.FieldValue.increment(awardedXp),
        updatedAt: now,
      }, {merge: true});
      transaction.set(walletLedgerRef, {
        uid,
        classId,
        assignmentId,
        runId,
        eventId,
        rewardType: "xp",
        amount: awardedXp,
        serverRecordedAt: now,
      });
    }

    return {
      alreadyProcessed: false,
      correct,
      awardedXp,
      completed,
      completedChallengeIds: sortedCompletedIds,
    };
  });
});
