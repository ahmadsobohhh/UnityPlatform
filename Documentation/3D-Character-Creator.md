# 3D character creator

## Open and use

New student onboarding opens `StudentAvatarSelect`. Existing students can use **Profile → Edit Appearance**. For a direct Unity preview, select **Imagine Quest → Character → Play Creator**.

Choose Boy/Girl, one of six skin tones, four hairstyles, and six hair colours. Drag the model or use Turn Left/Right; scroll to zoom, use Walk/Idle to inspect animation, and Reset View to restore framing. Each model always wears its basic deckhand uniform. Confirming saves the selected appearance; reopening restores those choices. Profile and dashboard show a live 3D headshot. The ship uses the same prefab, mesh, hair and material settings with the existing movement controller.

## Persistence and compatibility

Appearance is a strictly validated versioned string in the existing `users/{uid}.avatarId` field: `pirate-v1:<body>:<skin>:<hair>:<colour>`. All 288 supported combinations round-trip. Missing/legacy/malformed IDs render a clothed default and are upgraded only when the student confirms. The existing `characterColor`, `characterClass`, `avatarChosen`, and `updatedAt` fields are updated together; the creator does not replace the profile document or alter its creation date, rewards, progress, classes, Hearts, or XP.

Authenticated saving uses the project's existing Firebase client and existing profile rules. No backend deployment, new cloud service, security-rule change, billing-plan change, or paid dependency is part of this feature. Guest play saves a separate local appearance. Guest settings are never applied to a signed-in account, and cached signed-in settings are scoped to their owner.

The UI waits for the profile before enabling save. Failed loads offer Retry; failed saves keep the current choices. Saves are single-flight and freeze appearance controls until completion. Account changes and abandoned screens invalidate asynchronous UI responses. A slow write shows a connection message and leaves Back available; it can still finish saving to the original account after leaving the screen.

## Verification

`StudentAppearanceTests` validates every encoded appearance and malformed/legacy fallbacks. `StudentCharacterCreationTests` checks both clothed Humanoid models, all hairstyles, animated walking, complete material slots, independent clothing/skin tint, selection limits, preview controls, portrait isolation/lifecycle, save retries, duplicate clicks, missing profiles, account changes, leaving during a pending save, and appearance/mesh consistency on the ship.

Verified on 15 November 2026: 51 Edit Mode checks passed; all 15 existing workshop, portal, arrival, quest-menu, Hearts/XP and combat-return checks passed; all 7 creator checks passed in the standalone Windows player. Both bodies were rendered at 1600×900, 1280×720 and 1024×768 and inspected visually. Validation artifacts are kept outside Assets so they are not imported or shipped with the game.

`firebase/functions/test/appearance.integration.cjs` also passed against local Auth/Firestore emulators with the existing repository rules. It verifies the creator's exact profile fields plus server timestamp, preservation of creation date/username/role/Hearts/XP/currencies, saved appearance readback, and rejection of another account's reads and writes. It requires `GCLOUD_PROJECT` beginning with `demo-` and both emulator hosts on `127.0.0.1`; it refuses production endpoints. Run it through an isolated Auth/Firestore Emulator Suite with `npm --prefix firebase/functions run test:appearance`. No production profile was written during validation.
