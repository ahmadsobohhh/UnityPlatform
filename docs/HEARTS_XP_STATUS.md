# Hearts, XP and the personal recap

## Release status: deployment intentionally pending

This implementation is on `abdubranch3`. It is not deployed to production.
The selected Firebase project is `capstone-d51c2`, whose billing was disabled when
checked on November 10, 2026. No billing account, paid plan, or Cloud Functions API
has been enabled by this work. Production rules, assignments and balances have
not been changed. Backend test accounts and writes exist only in the Emulator Suite.

`Assets/Resources/StudentCloud.json` deliberately has `enabled: false`.
The new callable client rejects requests before obtaining credentials or making a
network request when this gate is off. The workshop outbox also remains paused.
While the gate is off, a separate device-only profile provides 50 starting Hearts,
0 starting XP, persistent awards and the personal recap. The HUD labels this **LOCAL
XP** and the recap shows **Local progress • saved**. Existing authentication and class
features still use their existing Firebase paths; this feature adds no cloud writes.

Local profiles are stored beneath `Application.persistentDataPath/LocalStudentStatus`,
separately for each signed-in UID (or `local-guest` when opening a scene directly).
Atomic saves retain the previous version as `.bak`; malformed saves are preserved,
not replaced with fresh balances. Local values are never uploaded as a Firebase
wallet. Enabling the cloud gate later switches to the independently verified wallet.
Cloud-off workshop checkpoints use `local-practice`, so their pending events cannot
silently become a future assigned-workshop reward outbox.

Do not turn on this gate or deploy the backend until the project owner explicitly
authorizes the required Firebase billing setup. Signing into the CLI alone does
not enable billing. The code is ready to test locally without a paid Firebase plan.

## Player experience

- A compact heart icon/count and saved XP total appear on the ship and in the workshop.
- Open **Journal > Personal Recap** to see classroom Hearts, total XP, XP earned this
  visit, recent activity and connection state. The hub also has a **Recap** button.
- Hearts are classroom standing, not combat health. Quiz mistakes do not deduct them.
- The recap pauses workshop interaction and closes back to the existing journal.
  Escape closes the recap before the journal. No new settings page is required.
- Saved XP gains produce a short notification. In local mode each correct workshop
  craft awards 25 XP once, and delivery awards 100 XP once (175 total per assignment).
  Existing completed local crafts are credited once when their checkpoint opens.
  Ship seal answers and quest completion also record deduplicated local rewards.
  Wrong answers award no XP and never deduct Hearts. Replays/reloads cannot farm XP.
  These device-only awards never alter the authoritative Firebase total.
- A disconnected subscription shows its last saved totals with an offline label;
  a missing/invalid snapshot shows unavailable values. Reconnect restarts subscriptions.
- Listeners follow the authenticated UID and clear on account changes. Totals and
  history survive scene changes, while UI subscribers unsubscribe when destroyed.
- The recap has been checked in a Windows test player at 1600x900, 1280x720 and
  1024x768. Test screenshots use clearly isolated fixture data, not production accounts.

## Teacher experience

Open a class, then **Hearts & Progress**.

1. Choose a student from the paginated class roster.
2. Review the saved Hearts/XP, select -5, -1, +1 or +5 Hearts, and enter a reason.
3. Check the before/after preview and save. A retry reuses the same request ID.
4. A change reaching zero explicitly explains the reset and requires a second
   **Confirm Fresh Start** action. Heart restoration does not automatically refund XP.
5. **Assign Workshop** opens The Aurora's Repair for enrolled students; **Close
   Workshop** prevents new submissions while preserving progress and retry receipts.

With the cloud gate off, the existing class-owner and roster reads still establish
the teacher's class context. Heart adjustments update only that student's profile
on this PC, with reasons, deduplicated retries and the same reset confirmation.
The UI explicitly labels local XP; it cannot change standing on another device.
Assignment controls remain disabled and the ship/video shortcut opens local practice.
The assignment workflow described above becomes available after authorized deployment.

Students use the ship chart's assigned-workshop entry. It verifies the assignment
and establishes the correct quest/class/assignment context before loading the room.
The video panel's assigned-quest shortcut uses that same path.

## Authoritative backend

All functions run in `northamerica-northeast1`, on Node 22, with Firebase Authentication.
Students cannot write their own wallet, standing, activity or reward receipts.

| Callable | Responsibility |
| --- | --- |
| `ensureStudentStatus` | Create missing wallet/standing documents for the signed-in student; preserve existing values including zero. |
| `getClassStudentStatus` | Return one enrolled student's totals only to the owner of that class. |
| `adjustStudentHearts` | Verify class ownership and membership, validate reason/bounds, save a deduplicated adjustment and activity entry atomically. |
| `setWorkshopAssignment` | Allow the class owner to open/close the workshop. |
| `blacksmithQuest` | Validate ordered workshop events against server-side answers, restore saved progress, and award each craft/delivery once per assignment. |
| `recordQuestEvent` | Existing general quest reward path; now also respects zero Hearts and writes recap activity. |

The current workshop rates reuse the project's existing 25 XP per correct craft and
100 XP on delivery, for 175 XP total. Repeated answers, new event IDs for the same
completed craft, reloading and repeated completion cannot multiply that reward.
They are initial project rates, not a new client-approved balancing specification.
No leveling curve or Power costs are invented by this change.

Storage:

- `wallets/{uid}`: XP, existing level/currencies, reward epoch and last mutation ID.
- `studentStanding/{uid}`: Hearts (0..50), Power epoch, last Heart adjustment ID.
- `studentActivity/{uid}/entries/{receipt}`: the student's read-only recap history.
- `heartAdjustments/{receipt}`: server/admin-only audit and before-change snapshots.
- `users/{uid}/classProgress/{classId}/assignments/{assignmentId}`: server-verified workshop masks, completion and assignment XP.
- `walletLedger/{receipt}`: authoritative XP receipts.

At zero Hearts, the same transaction resets XP/Crystals/Gold, sets level to 1 and
advances the reward and Power epochs. `powersRevoked` records the consequence.
The existing Powers UI is still a preview; a future activation backend must validate
the grant's Power epoch and positive Hearts before accepting it. New rewards are
blocked at zero Hearts until the teacher restores standing. Old completed crafts
cannot be replayed to refill a reset wallet.

Workshop events retain stable IDs in the local checkpoint until acknowledged.
Assigned workshops retry pending events every 15 seconds when the gate is enabled.
Delivery waits briefly for confirmation before returning to the ship; unacknowledged
events remain on disk rather than pretending to have earned wallet XP.

## Local verification

Install the locked dependencies in `firebase/functions` with `npm ci` and compile
with `npm run build`. Use Node 22. Then, from `firebase`, run:

```powershell
firebase emulators:exec --project demo-imaginequest-status --only auth,firestore,functions "npm --prefix functions run test:integration"
```

The test script refuses to run without both Auth/Firestore emulators and a `demo-`
project ID. It covers authentication, class ownership/membership, balance bounds,
wrong answers, ordered completion, concurrent retries, reward replay prevention,
closed assignments, zero-Hearts resets, existing quest reward compatibility,
private balance rules, and preview/apply/stale-data protection for rollback.

Unity checks:

- EditMode: `LocalStudentStatusStoreTests`, `StudentLiveStatusTests`, `StudentDashboardTests`, `BlacksmithWorkshopTests`.
- PlayMode: `StudentLocalProgressTests`, `StudentStatusViewTests` plus the existing workshop/portal fixtures.
- Windows player: `StudentStatusViewTests`, including all three display sizes.

The local-mode follow-up passed 35 EditMode checks and 10 PlayMode checks on
November 10, 2026. The workshop fixture exercises the real answer controls, HUD,
recap, standing changes, duplicate prevention, account isolation and service restart.
Its save files and screenshots use isolated test profiles, not the student's progress.

Set `IMAGINEQUEST_OFFLINE_TESTS=1` in the test process so no persisted sign-in can
contact the live economy. Set `IMAGINEQUEST_TEST_OUTPUT` to retain UI screenshots.
This flag is test isolation, not a substitute for Firebase authorization.

## Reversibility and future deployment

The deployed rules were downloaded before any work on the live service. Their exact
pre-change source is versioned at `firebase/rollback/firestore-before-hearts-xp.rules`.
The existing rules outside this feature are preserved. Reverting this feature must
not bulk-delete wallets or earned quest progress.

For a future authorized release:

1. Review and back up the project's current rules and current affected data again;
   the saved baseline is dated November 10 and may become stale.
2. Obtain explicit authorization for billing/Blaze. Do not enable it automatically.
3. Deploy only this feature's functions and the reviewed Firestore rules to the
   explicit project `capstone-d51c2`. Do not deploy unrelated functions or indexes.
4. Verify with dedicated test teacher/student accounts, then enable `StudentCloud.json`
   and build the client. Never activate the client gate before its backend is available.

To roll back a release, first disable the client gate and rebuild the client, then
restore the **current pre-release** rules and previous function revisions. Newly
created functions may be removed individually after checking that no other feature
uses them. Never reset a database as a code rollback. Keep receipts/history for audit.

### Reversing a particular Heart adjustment

Each new adjustment saves full prior wallet/standing snapshots in its private receipt.
An Admin SDK helper is provided; it is deliberately **not** an internet-callable function.
It requires the administrator's own Application Default Credentials. From
`firebase/functions`, after building:

```powershell
# Preview only. No writes.
node scripts/rollback-heart-adjustment.cjs --project capstone-d51c2 --receipt <64-character-receipt-id>
# Explicitly apply only after reviewing the preview.
node scripts/rollback-heart-adjustment.cjs --project capstone-d51c2 --receipt <64-character-receipt-id> --apply
```

It restores the saved standing and, when the original operation reset/created the
wallet, the saved wallet as well. It also appends a visible reversal entry and marks
the original receipt reversed. Retries are idempotent. It refuses to overwrite a
later standing change or a later wallet mutation. Such cases require individual
administrator review, not a forced bulk restore. No production rollback was run.
