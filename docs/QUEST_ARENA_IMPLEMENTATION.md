# Quest Arena Implementation

## Purpose

`QuestArena` is the playable third-person home of ImagineQuest. The arena is the Aurora pirate ship. Students enter it from an assigned class, explore the deck as their selected pirate, and choose a quest at the chart table or helm. The arena itself has no active lesson, clock stations, seal counters, XP counters, or Current Course panel.

The Celestial Clock remains the first complete quest, but it is no longer treated as the name of the entire game space.

## Current playable flow

1. A student opens a class and clicks **Enter Arena**. A selected quest is not required to explore the ship.
2. `QuestLauncher` creates a `QuestSession` containing the class, assignment, and quest identifiers.
3. Legacy buttons that name `PirateQuest` or `CelestialClockQuest` are redirected to `QuestArena`.
4. The selected pirate spawns on the Aurora in third person.
5. The student explores the ship and interacts with crew, chart table, helm, telescope, reward chest, and bell.
6. The chart table and helm open **Choose a Quest**.
7. The chart reads the teacher's assignment from Firestore. **Play Assigned Quest** checks it again before entering the existing lesson scene. Deselected quests cannot be launched through the chart. Currently only the existing assignment has playable content; additional quests have not been added.
8. Videos, teaching notes, challenges, seals, and saved progress belong to that selected quest, not to the arena.
9. The quest's menu and completion screen return to `QuestArena` while preserving the class and assignment context.
10. The arena's menu returns to the selected class.

## Controls

| Input | Action |
| --- | --- |
| `WASD` | Walk relative to the camera |
| `Shift` | Sprint |
| `Space` | Jump |
| Mouse | Orbit the third-person camera |
| Mouse wheel | Zoom |
| Hold either `Alt` | Temporarily reveal interaction outlines |
| `E` | Use the nearest highlighted interaction |
| `J` | Open the navigator journal |
| `Esc` | Close an overlay or open the pause menu |

## Ship interactions

- **Captain slot:** Empty in the arena until a replacement skin is chosen. The existing lesson's captain and dialogue remain.
- **Quest chart:** Opens quest selection.
- **Helm:** Opens quest selection.
- **Telescope:** Opens lookout information.
- **Reward chest:** Explains the reward area; no active-course seal or XP counters appear in the arena.
- **Ship bell:** Provides a simple environmental interaction and feedback.
- **Crew:** Four idle, grounded crew members with different outfits: Luma the navigator, Rook the boatswain, Pip the lookout, and Sage the quartermaster. Talking opens a crew information panel, not a full branching dialogue system.
- **Learning fixtures:** Exist only inside the selected quest scene.

All interactions implement `IQuestArenaInteractable` and register with `QuestArenaInteractionRegistry`. This gives the player one consistent targeting and prompt system instead of putting interaction-specific code into the controller.

Interactable fixtures have thin colored edge shells: gold for quest selection and cyan for ship/crew interactions. These are hidden unless Alt is held; releasing Alt or opening an overlay hides them again. The nearest target brightens from a 1.3-pixel outline to 2.1 pixels while revealed. Outlines respect scene depth, do not add collision, and do not show through the ship.

## Locomotion tuning

- Normal movement is intentionally slower than the original prototype.
- Acceleration and braking are gradual, so the pirate carries weight instead of instantly reaching full speed.
- Sprint is a moderate increase rather than an arcade-speed multiplier.
- Turning uses damped rotation and no longer snaps toward input.
- Gravity and jump height are tuned for the scale of the ship.
- The evaluated active skinned mesh vertices determine the pirate's foot height. Inactive outfit variants and loose animation bounds do not determine that offset. The humanoid model uses the original fixed scale of 0.52 (approximately one metre tall); a 1.08-metre capsule fits the pirate and the scaled ship. BakeMesh compensates renderer scale before TransformPoint converts vertices to world space, preventing double scaling. Foot alignment only translates the model and never enlarges it.
- The authored ship floor, hull, steps, stairs, and major solid props receive mesh collision. The flat invisible deck was removed because it floated above lower ship surfaces.
- Gravity remains active while menus are open. Descending steps maintain contact within the controller's step height; falling below the ship restores the player to its spawn area.
- The camera starts 1.25 metres behind the pirate, with a 0.85–2.0 metre zoom range and 0.3 metre wheel steps. Camera collision ignores the player capsule.

The arena now reuses the existing older repository's humanoid `Idle.fbx`, `Walk.anim`, and `RunForward.fbx` in `Assets/Gameplay/Pirate/Locomotion`. `PirateLocomotion.controller` blends those three distinct clips using actual capsule velocity. Ordinary walking moves at 1.4 units/second, and holding Shift accelerates to 2.45 units/second while blending to the run clip. A blocked player blends back to idle. The existing `Pirate.FBX` humanoid avatar retargets the clips to every selectable pirate.

Jumping adds retargeted `Jump_Up`, `FallingLoop`, and `Jump_Down` clips, driven by actual grounded status and vertical velocity. Space has a 0.12-second input buffer and 0.10-second coyote window. Holding Space gives a roughly 0.42-metre jump; releasing early shortens it. Falling gravity is stronger than rising gravity, ceiling contact cancels upward velocity, and air steering is reduced. Landing transitions back to the walk/run blend rather than stretching a walking clip over the jump.

Crew use the same controller at Speed 0, which plays the idle animation. The arena captain is currently removed; the original male captain remains in the separate lesson scene.

Validated in an isolated Unity 6000.3.6f1 editor run: all five pirate variants are approximately 0.957–0.974 metres tall at the fixed 0.52 scale, all three locomotion clips resolve and retarget, walking/running produce different leg poses, and idle foot displacement stays below a millimetre. The captain's spawn ray hits the actual furnished ship deck at approximately y=0.966. The skybox and water materials import and their shaders compile. The runtime assembly also compiles successfully. This asset/geometry validation does not replace an interactive visual Play Mode review of the entire scene.

## Living ship and sea

The arena uses `Colonial Ship.prefab`, the furnished variant with barrels, crates, cannons, sandbags, lanterns, and other cargo. Major solid props receive collision. The old lesson scene retains its previous ship layout.

`AuroraSky.mat` supplies a dusk skybox with a warm horizon, stars, and subtle moving cloud patterns. The player camera renders the skybox. `AuroraWater.mat` supplies moving waves, ripples, reflected sky colors, and moonlight glints. The ocean extends to the horizon. Distant rock silhouettes, circling gulls, gently moving sails, and lantern flicker provide environmental motion without moving the ship's collision surfaces.

The added chart table has legs and parchment, the telescope and bell have supporting fixtures, the helm interaction uses the ship's existing wheel, and generated deck fixtures sample the actual deck height. This replaces the earlier floating props at hard-coded heights.

`ArenaDeckPlacement` checks the chart's whole footprint for level deck and solid obstructions. Crew positions use the same clearance check; stairs and cargo are not valid spawn locations. NPC capsules compensate for the visual model's 0.52 scale so their collision remains approximately one metre tall.

The existing bundled pirate music plays quietly in a single looping AudioManager source. The music slider still controls it. Quest videos duck the music to 18% of its current level and restore it afterward; returning to a non-gameplay scene restores the manager's normal background track when configured.

## Quest video slots

Select the runtime object in `Assets/Scenes/Gameplay/CelestialClockQuest.unity`. Its bootstrap exposes these URLs for the selected quest:

- Opening Video URL
- Teaching Video URL (fallback)
- Completion Video URL
- Sail Fraction Video URL
- Compass Angle Video URL
- Star Equation Video URL
- Celestial Clock Video URL

The four station-specific fields are where the final educational videos should be placed. A missing station URL falls back to the general configured video.

## Character selection

Character creation and login cache the selected avatar and tint under the signed-in account ID. The arena also reads the saved Firestore profile, so existing accounts do not need to recreate their character. A different account cannot inherit the previous student's cached appearance. Missing profiles use a blue navigator pirate with a normal pirate hat.

`PirateAvatarStyler` adapts all eight existing pixel portraits to the modular 3D pirate rig. These are ship-themed adaptations, not new external character models:

| Signup avatar | Arena appearance |
| --- | --- |
| Hero | Blue outfit, red belt accents, original pirate hat |
| Mage | Blue cloak and clothing, gold accents |
| Knight | Silver alternate clothing and original skull hat |
| Bandit | Brown outfit, dark cloak, original pirate hat |
| Monk | Saffron clothing, green accents, no hat |
| Archer | Green cloak and clothing, brown accents, original pirate hat |
| Samurai | Red alternate clothing, gold accents, original skull hat |
| Scholar | Purple cloak and clothing, cream accents |

The selected personal tint subtly modifies the outfit without recoloring exposed skin. Only original modular meshes are used. Applying a saved appearance does not rescale the pirate.

## Corrective pass: landing, clean characters, Alt, and dummy quest

The added primitive hats, armor spheres, scarf blocks, quiver cylinders, and spectacles were removed from runtime skin creation because they appeared as stray shapes on the characters. The eight palette-based outfits, default skin, and crew remain. Both cloak variants are no longer enabled simultaneously. Only the previous pass's named prototype attachments are removed during appearance refresh; original pirate meshes and source prefabs are preserved.

Falling was tested against the furnished ship: the CharacterController stopped on the actual deck at 20, 30, and 60 fps. The visible penetration came from retargeted **Landing** animation vertices, measured at about 0.27 metres below the support root. `PirateVisualGrounding` resets the previous visual correction, evaluates the active skinned meshes, and raises only the visible model enough to keep it above the capsule's real support surface. It releases that correction when airborne. The capsule, deck height, model scale, and jump trajectory are unchanged. Downward velocity is also reset when a move reports ground contact.

Alt reveal uses either left or right Alt. Outlines are initially hidden and only revealed while player input is enabled and Alt is held. Proximity still determines the E prompt, so Alt is a discovery aid rather than a requirement for interaction.

### First Voyage — Dummy Quest

The chart has a clearly labeled, always-available **Play Dummy Quest** entry separate from the teacher-assigned lesson. It is a video-only demo with no XP, rewards, Firestore writes, or scene transition. This does not bypass the real assignment's permission gate.

Play closes the chart and reuses `UrlVideoSequencePlayer`, its existing YouTube support, retry, pause, seek, volume, and exit controls. The world-space projection is anchored 1.1 metres above the tabletop object's centre, leaving the bottom of the 1.8-metre panel above the table. The camera temporarily faces the projection from 2.6 metres away. Completion or Exit restores normal third-person exploration and music volume. The projection remains in the ship, rather than opening another fullscreen quest scene.

The existing placeholder video URL is serialized as **Dummy Quest Video URL** on the QuestArena bootstrap. Replace that URL with the first real quest video in Unity's Inspector when ready.

### Corrective-pass verification

- Runtime assembly compiles successfully.
- Isolated Unity tests reproduced the original landing penetration, then verified all five pirate rigs stayed at or above the support root throughout landing. Visual correction did not move the support root and released when airborne.
- Twelve furnished-ship fall checks (four clear deck positions × 20/30/60 fps) stopped on the real deck.
- All nine outfit identities were checked to create no primitive MeshRenderer accessories.
- Outline tests checked hidden-by-default, revealed, and hidden-again states.
- Log: `Logs/arena-landing-and-alt-final.log`. The video wiring compiles; actual network video playback and the full chart-to-projection interaction still need a live Play Mode check.

## Teacher quest selection

Inside a teacher's class view, the existing assignment card now reads **Select Class Quests** and offers **Select Quest / Deselect Quest**. The request's reference to selecting/deselecting "classes" is interpreted as selecting quests within the class, pending clarification. There is currently one implemented quest. This change preserves class creation, student lists, authentication, and assignment/progress documents; it does not delete classes.

The existing ownership check and Firestore rules still govern assignment writes. The arena checks `classes/{classId}/questAssignments/pirate-voyage` from the server and requires `isUnlocked: true` and the supported quest ID. A connection/permission failure offers Refresh and leaves play disabled. Only a signed-out direct Unity Editor scene preview bypasses class selection, and it is explicitly labeled LOCAL EDITOR PREVIEW.

## Skin flicker, female captain, and helm placement

Historical notes: the female captain described below was subsequently removed at the user's request. See the removal section for the current state.

The master pirate prefab contains a full body alongside alternative torso, head, legs, pants, and clothing parts. The earlier outfit code selected clothes but left the full body and other alternatives active, so multiple almost-identical surfaces fought for the same depth pixels. This caused the grey, patchy flicker shown in the user's screenshot.

`PirateAvatarStyler` now disables the overlapping full body, cutout Head alternative, bare legs, pants, and body-cloth detail. The selected outfit keeps **Pirate_Torso**, which contains the chest, head, and arms, plus jaw/eyes, one upper-clothing variant, one trousers mesh, and one boots mesh. Torso is skin geometry and is not tinted like clothing. All eight portrait-inspired palettes and the default remain; there are no added primitive accessories. Original source prefabs and meshes were not deleted or rewritten.

The male player and four male crew still share the original pirate rig/base character. They have different colours, hats, cloaks, and upper-clothing variants; these should not be represented as nine independently modeled people. Captain Esme is now a genuinely separate female model: Anne from Quaternius's [Pirate Kit](https://quaternius.com/packs/piratekit.html), licensed CC0. The imported model, atlas, humanoid avatar, material, and source/license notes are under `Assets/Gameplay/FemalePirate/`. Her prefab is under `Assets/Resources/QuestArena/`. No student avatar selection or account data was changed to introduce her.

The animation setup preserves a character's own valid humanoid avatar instead of replacing the female rig with the male one. Esme is authored at approximately one metre tall, receives the same idle controller, and has a correctly scaled blocking capsule. The actual helm is at approximately `(0, 2.65, -5.93)` on the furnished ship. Her verified clear deck position is approximately `(1.5, 2.01, -6.53)`, beside the helm rather than beside the chart.

Verification: runtime assembly build passes; all outfit identities were checked to exclude overlapping alternative body parts; previous landing/collision/outline checks still pass; helm-side clearance was tested against the furnished ship; the female avatar is valid and humanoid, stands about 1.001 metres tall, has stable idle feet, and responds to distinct walk/run poses. An isolated rendered character preview was inspected to catch T-pose/culling problems. Test logs: `Logs/arena-skins-and-female-final.log` and `Logs/female-captain-final-check.log`. These are asset/geometry checks, not a claim of a complete live classroom session review.

This earlier pass was held locally pending the user's review.

### Follow-up: missing chest and captain discovery

The first flicker fix incorrectly disabled Torso and kept Head. Head is the chest-cutout alternative used by some clothing configurations; it is not a complete upper body. The user's follow-up screenshot exposed this omission. The corrected selection keeps Torso and hides Head/full Body, restoring the chest without drawing duplicate head/arm surfaces. Skin geometry is excluded from outfit tinting.

The female prefab was wired in the scene file, but an already-open Unity scene can retain an empty newly-added serialized field. The captain factory now resolves `Resources.Load<GameObject>("QuestArena/FemalePirate")` when that field is empty. Moving the existing prefab into Resources preserves its GUID, so scene references still work and the model is also available in builds. The factory logs which model it spawned and its deck position. Captain Esme is on the raised rear deck next to the wheel, not among the four crew on the main deck.

Validation now includes nine ray probes across the chest for every outfit, front-view rendered previews of all nine styles, and a spawn harness using the captain factory's current code with its Female Captain Prefab field deliberately null. The spawn check verifies the actual female mesh, retained humanoid avatar, approximately one-metre blocking capsule, and placement within two metres of the real helm. No original model or class-management feature was deleted. Stop Play Mode and start it again to rebuild the current runtime characters.

Logs: `Logs/chest-coverage-check.log`, `Logs/captain-resource-spawn-check.log`, and `Logs/character-correction-final.log`. Outfit preview: `Logs/chest-outfits-preview.png` (isolated art preview, not a live classroom screenshot).

## Latest pass: exact change map (2026-10-12)

- `CelestialClockQuestBootstrap.cs`: Relocated the chart, spawned four crew, connected interaction outlines and music, fetched saved student appearance, added fresh assignment checks to the quest picker, and improved the jump controller. NPC collision dimensions now compensate for model scale.
- `ArenaDeckPlacement.cs`: Added whole-footprint deck-height and obstruction checks. The furnished ship's verified chart location is `(1, 0.981, 3)`; crew positions are `(1, 0.981, -1.5)`, `(-1, 0.981, -1.5)`, `(-1, 0.981, 3.5)`, and `(0, 1.425, 7)` (rounded heights).
- `QuestArenaOutline.cs`, `QuestItemOutline.shader`, and its material: Added depth-tested, thin interaction edges and stronger nearest-target feedback.
- `PirateAvatarStyler.cs`: Added the default navigator and eight portrait-matched modular outfits. Prototype accessories were subsequently removed; the corrected torso selection restores the chest without overlapping meshes.
- `StudentAvatarIdentity.cs`, `AuthManager.cs`, `CharacterCreation.cs`, `StudentAvatarSelect.cs`, and `StudentSignOut.cs`: Connected saved avatar identity to account-scoped appearance caching and sign-out cleanup.
- `AudioManager.cs`: Added single-source looping track selection, gameplay volume scaling, video ducking, and normal-track restoration outside gameplay.
- `QuestLobbyControllers.cs`: Made the student class card an arena entry rather than an assignment gate, and relabeled the teacher's existing quest toggle as Select/Deselect Quest. No class deletion controls were added.
- `Pirate/Locomotion/Jumps.fbx`, `FallingLoop.fbx`, and `PirateLocomotion.controller`: Added takeoff, falling, and landing states using the existing project's retargetable clips.
- `QuestArena.unity` and `CelestialClockQuest.unity`: Assigned the outline material and existing pirate music clip.

No existing scene, avatar portrait, class-management system, lesson, or progress feature was deleted by this pass. It uses bundled models, clips, and music; no external asset purchase or download is required.

## Female captain removal (2026-10-12)

At the user's request, the current female captain is removed from the ship arena, leaving that slot empty for a future skin. `CreateCaptain` skips NPC creation in arena mode but still builds the chart, helm, telescope, chest, bell, and four existing crew. It does not substitute a male captain in the arena. The separate Celestial Clock lesson retains its original male captain and dialogue.

Removed the female prefab field, Resources fallback, and both scene references. The unused female model, textures, material, avatar, prefab, and their Unity metadata were moved out of `Assets` into the local ignored `Logs/RemovedFemalePirate/` archive, so they remain recoverable but are not shipped or pushed. Player skins, restored chest meshes, classroom flows, and all other arena improvements remain.

The push includes the accumulated ship-arena implementation and this removal on `feature/celestial-clock-quest`. Unrelated editor-generated font/render settings, general project settings, and `Vision Document.docx` are excluded.

Removal verification: runtime build passed with zero errors. The isolated Unity checks passed for the empty arena captain slot, continued ship-interaction initialization, preserved male lesson captain, all nine chest/outfit identities, animation transitions, chart and crew placement, outlines, and sky/water shaders. The captain factory tested is identical to the production method. Log: `Logs/arena-captain-removal-final.log`. This is automated validation, not a live classroom/video-playback review.

### Verification

- Runtime assembly build passed with zero errors.
- Isolated Unity 6000.3.6f1 validation passed for all nine skin identities, unchanged character scale, walk/run pose differences, takeoff → falling → landing → locomotion transitions, clear chart placement, all four unobstructed crew positions, outline creation, shader compilation, and music reference resolution (229.08-second clip).
- Validation output: `Logs/arena-art-check-complete.log` (local ignored test artifact).
- Full interactive visual review and signed-in teacher → select/deselect → student → arena → quest verification remain manual checks. No live Firebase assignment was modified during automated validation.

## Scene compatibility

- New gameplay scene: `Assets/Scenes/Gameplay/QuestArena.unity`
- Existing lesson scene retained: `Assets/Scenes/Gameplay/CelestialClockQuest.unity`
- Both are currently included in build settings.
- Old serialized launchers continue working because `QuestLauncher` translates old scene names to `QuestArena`.

`arenaOnly` is enabled in the arena scene and disabled in the existing lesson scene. The arena's quest choice loads the lesson directly; old class launch buttons still enter the arena through `QuestLauncher`.

## Main runtime components

- `QuestLauncher`: Enters the arena with class and assignment context.
- `QuestSession`: Carries that context through the scene transition.
- `CelestialClockQuestBootstrap`: Builds the current Aurora vertical slice, UI, Celestial Clock content, and persistence bridge.
- `CelestialClockPlayerController`: Third-person movement, orbit camera, animation parameters, and interaction targeting.
- `QuestArenaInteractionRegistry`: Maintains available interactions and resolves the nearest usable target.
- `CelestialClockStation`: A video-led educational interaction that can be locked and completed.
- `QuestArenaShipInteractable`: Reusable endpoint for non-lesson ship fixtures.
- `QuestProgressRepository`: Saves restored nodes, provisional XP, and completion state.

## Next implementation passes

### Art pass

- Replace primitive chart, helm, telescope, and bell stand-ins with final prefabs.
- Add authored collision around stairs, raised decks, cabin entry, mast, and railings.
- Add camera collision layers so decorative particles never obstruct camera movement.
- Idle, walk, run, takeoff, falling, and landing are connected. Add dedicated interaction clips in a later content pass.

### Interaction pass

- Add a real dialogue presenter for the captain.
- Replace the lightweight edge-shell outline with an authored art treatment if required.
- Add chest, bell, helm, and hatch animations with sound.
- Add a close telescope camera and return transition.
- Replace the current informational helm panel with destination selection when a second quest is ready.

### Quest-authoring pass

- Move video URLs and station layout out of the bootstrap and into quest data assets.
- Create a quest catalog containing title, icon, subject, scene/ship placement, prerequisite, assignment status, videos, objectives, and rewards.
- Convert each learning fixture into an authored prefab.
- Split the Celestial Clock controller and UI out of the arena bootstrap once the vertical slice is approved.

### Persistence pass

- Load completed node IDs when entering the arena and restore visual state.
- Expand profile skin data if future characters require additional rigs.
- Store ship rewards and quest unlocks per student.
- Show precise challenge evidence in the teacher view.
- Add offline/retry UI for failed progress writes.

## Acceptance checklist for this vertical slice

- The class launch route enters `QuestArena`.
- A visible pirate is spawned and the camera is third person.
- Walking, sprinting, jumping, orbiting, zooming, and camera collision work.
- UI and video overlays disable movement and release the cursor.
- All ship fixtures show the same `E` interaction prompt style.
- The arena contains no clock stations, active course, seal log, or lesson HUD.
- The chart opens quest selection; playing the selected quest loads its separate scene.
- Inside the selected quest, every station launches its configured video slot and correct challenges unlock the finale.
- Progress saving still receives the original class and assignment identifiers.
- Leaving a quest returns to the ship without clearing assignment context. Leaving the arena clears the quest session and returns to `ClassroomScene`.
