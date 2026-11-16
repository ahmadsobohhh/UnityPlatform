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
