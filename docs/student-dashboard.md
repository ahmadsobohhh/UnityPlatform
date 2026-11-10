# Student dashboard

`StudentHub` has a scene-attached `StudentDashboard` presenter. It builds the top
status area at runtime using the existing hub font and eight profile avatar sprites.
The class list, join dialog, profile, settings and sign-out navigation remain available.

## Data contract

The presenter obtains all student values through `IStudentDashboardRepository` /
`StudentDashboardRepository`. Server reads are scoped to the authenticated Firebase UID;
an offline cache miss cannot be mistaken for a new student's initial balance.

| Firestore document | Fields | Owner |
| --- | --- | --- |
| `users/{uid}` | `username` (or `firstName` / `lastName`), `avatarId`, `characterColor` | Existing profile flow |
| `wallets/{uid}` | Integer `level`, `xp`, `crystals`, `gold` | Authoritative reward backend |
| `studentStanding/{uid}` | Integer `hearts` | Teacher-authorized server workflow |

Missing wallet fields default to level 1 and zero currency in the data model.
Missing standing defaults to the vision's starting 50 Hearts. These defaults are
read-only; opening the dashboard never creates, awards, deducts or resets anything.
An existing `hearts: 0` stays zero. Errors and invalid balances show unavailable
values and a Refresh action, rather than fabricated balances. Profile absence is
an error; an unknown avatar uses the student's initial.

The existing `recordQuestEvent` function already increments `wallets/{uid}.xp`.
It does not yet populate levels, Crystals or Gold. Their producers can add those
fields without UI changes. The vision defers XP thresholds to a balancing document,
so this change displays the backend level (default 1) and total XP without inventing
a leveling curve or a progress-to-next-level calculation. Legacy class-member XP
and provisional quest progress are not used as authoritative balances.

Hearts are separate teacher-controlled standing. The Hearts/XP extension provides
the class-owner adjustment UI and its protected backend mutation; quiz mistakes do
not deduct Hearts. The zero-Hearts consequence is applied by a standing adjustment,
never triggered by a dashboard read. See `HEARTS_XP_STATUS.md` for the deployment gate.

Deploy the updated `firebase/firestore.rules` with the project's normal Firebase
deployment workflow before using the new standing path. Without its read rule,
the dashboard reports a load error. No Firebase deployment is performed by this commit.

Identity refreshes on scene enable, authentication changes, return to application
focus and the Refresh button. When enabled, the Hearts/XP extension owns live
wallet/standing listeners across the hub and gameplay, plus a personal recap with
recent activity. Account changes invalidate old responses and listeners.

The release gate currently remains OFF, as requested by the project owner while
deployment/billing are pending. Identity still loads; a separate per-account local
profile starts at 50 Hearts and 0 XP and persists actual local rewards. The hub and
recap explicitly label local progress. Local saves are never uploaded as wallets.
See `HEARTS_XP_STATUS.md` for the full
implementation, deployment state and rollback procedure. The Powers cards remain
previews; this update does not implement Power activation.

## Verification

Run `StudentDashboardTests` in Unity's EditMode Test Runner for defaults, authoritative
source separation, zero Hearts, malformed data, large balances and scene avatar wiring.
Then play StudentHub with a signed-in student and verify:

1. Known profile/avatar and wallet/standing values appear in the header.
2. Missing optional documents show initial values; permission/network failures show
   unavailable values and Refresh recovers after connectivity/access is restored.
3. Set Hearts to 0 through trusted server tooling; refresh and confirm no wallet reset.
4. Update profile/avatar, return to the hub, and confirm the new identity appears.
5. Sign out during a load; old account values must not appear for the next account.
6. At 1920×1080, 1280×720 and 1024×768, check the status area, class heading/list,
   Join Class dialog, profile, settings and sign-out controls for overlap and usability.
