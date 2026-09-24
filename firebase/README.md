# Imagine Quest Firebase foundation

This directory is a **deployment scaffold only**. Nothing in it has been deployed,
no Firebase project has been selected, and the Unity client has not been switched to
call these endpoints yet.

It establishes the backend boundary needed for real classes and teacher-assigned
quests: the client can request an enrollment or submit an answer, while the server
owns membership writes, assignment authority, outcomes, XP balances, and immutable
reward receipts.

## What is included

| File | Purpose |
| --- | --- |
| `firebase.json` | Functions, Firestore rules/indexes, and local Emulator Suite configuration. |
| `firestore.rules` | Target access policy with carefully labeled compatibility routes for the current Unity app. |
| `firestore.indexes.json` | Teacher-dashboard/event query indexes for the new assignment model. |
| `functions/src/index.ts` | Callable functions for enrollment, assignments, and verified quest events. |

The functions use Node 20 and second-generation Firebase callable functions in
`northamerica-northeast1`. Keep the Unity Functions client configured to that same
region when it is added.

## Callable API

All calls require Firebase Authentication. App Check is deliberately not enforced in
this first scaffold because the Unity project does not yet configure an App Check
provider. Enable it as part of the client migration before a public release.

### `joinClassByCode`

```json
{ "code": "AB12CD" }
```

Only a profile whose role is `student` can join. The function normalizes the code,
checks that it identifies exactly one class, then atomically writes both sides of the
relationship:

```text
classes/{classId}/members/{uid}
users/{uid}/classes/{classId}
```

It supports the current `code` field as a temporary fallback and prefers the newer
`codeNormalized` field.

### `upsertQuestAssignment`

```json
{
  "classId": "class-id",
  "assignmentId": "celestial-clock-01",
  "questDefinitionId": "celestial-clock",
  "contentVersion": "v1",
  "title": "The Riddle of the Celestial Clock",
  "state": "open",
  "portalPlan": ["teaching", "practice", "finale"],
  "targetConcepts": ["fractions", "angles", "one-step-equations"],
  "rewardsPolicy": { "challengeXp": 25, "completionXp": 100 },
  "releaseAtMs": null,
  "dueAtMs": null
}
```

Only the `ownerUid` of the class can call it. The function requires the private
quest definition/version to exist before an assignment can reference it. XP values
are bounded server-side (50 per challenge, 200 for completion).

### `recordQuestEvent`

For a submitted answer:

```json
{
  "classId": "class-id",
  "assignmentId": "celestial-clock-01",
  "runId": "run-2026-09-23-a",
  "eventId": "event-001",
  "eventType": "challenge-submitted",
  "challengeId": "sail-fraction-challenge",
  "response": "3/4"
}
```

For final completion:

```json
{
  "classId": "class-id",
  "assignmentId": "celestial-clock-01",
  "runId": "run-2026-09-23-a",
  "eventId": "event-004",
  "eventType": "quest-completed"
}
```

Each `eventId` is idempotent within an assignment. The server validates a submitted
answer against the private definition, awards each challenge only once, and allows
completion only after every `requiredChallengeId` is complete. Wallet XP and a
ledger receipt are written in the same transaction, so a retry cannot award twice.
The raw student response is intentionally not stored.

## Private quest-definition shape

The server expects an Admin-authored document like this. Do **not** grant Unity
clients direct read access to this path; it contains answer keys.

```text
questDefinitions/celestial-clock/versions/v1
  requiredChallengeIds: [
    "sail-fraction-challenge",
    "compass-angle-challenge",
    "star-equation-challenge",
    "celestial-clock-challenge"
  ]
  challenges:
    sail-fraction-challenge:
      acceptedAnswers: ["3/4", "0.75"]
    compass-angle-challenge:
      acceptedAnswers: ["east"]
    star-equation-challenge:
      acceptedAnswers: [12, "12"]
    celestial-clock-challenge:
      acceptedAnswers: [90, "90", "90°"]
```

The matching Admin-only seed template is at
`firebase/seed/celestial-clock-v1.private.example.json`. Do not move its answer
keys into a client-readable Firestore collection.

If dynamic client-authored quest text is needed later, publish a separate sanitized
manifest with no answer fields. Never use the private definition as a client asset.

Authoritative writes land at:

```text
classes/{classId}/assignments/{assignmentId}
users/{uid}/classProgress/{classId}/assignments/{assignmentId}
users/{uid}/classProgress/{classId}/assignments/{assignmentId}/runs/{runId}
users/{uid}/classProgress/{classId}/assignments/{assignmentId}/events/{eventId}
wallets/{uid}
walletLedger/{uid}_{classId}_{assignmentId}_{eventId}
questEventAudit/{uid}_{classId}_{assignmentId}_{eventId}
```

## Local validation before any deployment

From this directory, install and compile the functions:

```powershell
cd C:\Users\Ahmad Soboh\Desktop\Projects\Capstone\UnityPlatform\firebase\functions
npm install
npm run build
cd ..
firebase emulators:start
```

Use an Emulator Suite project alias or select a Firebase project only when the team
is ready. A deploy is intentionally a separate, explicit operation:

```powershell
firebase deploy --only functions,firestore:rules,firestore:indexes
```

Do not run that command until the staged migration below is complete and someone has
reviewed the rules against the real class roster data.

## Transitional compatibility and migration order

The current Unity project creates profiles, classes, membership documents,
`questAssignments`, and `users/{uid}/questProgress` directly from the client. The
rules retain only the minimum compatibility surface for those calls so applying the
rules during development does not immediately strand the existing screens.

Two important limits follow from that decision:

1. A direct client-created `users/{uid}/questProgress` value is **display-only**.
   Its `earnedXp` must never affect a wallet, grade, reward, or teacher report.
2. Teacher self-registration is still a product-flow choice in the Unity client.
   A real release should provision teachers through an approved Admin SDK/custom
   claim workflow rather than trust a client-selected `role` value.

Recommended sequence:

1. Add the Firebase Functions Unity SDK and invoke `joinClassByCode` instead of the
   direct class-code query and batch write.
2. Move the teacher quest control to `upsertQuestAssignment` and read the new
   `/assignments` collection.
3. Send answer/completion events through `recordQuestEvent`; display the returned
   server XP rather than calculating wallet rewards locally.
4. Backfill old classes with `codeNormalized` and old memberships with
   `role: "student"` plus display names.
5. Tighten the rules: deny `classes` list queries, deny direct `/members` and
   `/users/{uid}/classes` creation, retire `/questAssignments`, and finally retire
   legacy `/questProgress` writes.
6. Configure Firebase App Check for Unity, then set callable `enforceAppCheck` to
   `true` for the release build.

The scaffold intentionally does not model Hearts, real-world prizes, payments, or
other high-risk rewards. Keep those out of the client and add policy, age/privacy,
and abuse-review work before introducing them.
