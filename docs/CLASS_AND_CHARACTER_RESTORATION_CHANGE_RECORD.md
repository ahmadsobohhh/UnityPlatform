# Classes and Character Selection — Change and Restoration Record

## Why this file exists

This record explains the regression visible on the Classes page, exactly how this
branch caused it, and what has been restored. It is intentionally written in plain
language so the project history is understandable without reading Unity YAML files.

Audit baseline: `main...feature/celestial-clock-quest`.

The original Celestial Clock implementation consisted of three commits:

- `eb57689` — quest vertical slice, imported art, class/lobby integration, and
  Firebase scaffold.
- `817efdb` — local Windows preview builder and editor preview command.
- `75a3a73` — preview-only missing-scene guard.

The original implementation changed 266 paths against `main` (257 additions and
9 modifications). The restoration below removes only the UI changes that altered
existing product pages; the standalone quest, its assets, and preview tooling remain.

## The visible regression

### Harbour image and dark/opaque legacy pages

**Added file:** `Assets/Scripts/Frontend/PremiumFrontendPolish.cs`

This script ran automatically after every scene load. It targeted all of these
existing pages:

- `WelcomePage`
- `Login`
- `StudentAvatarSelect`
- `StudentHub`
- `ClassroomScene`
- `TeacherClassSelect`
- `TeacherClass`
- `StudentProfile`
- `StudentJoinClassWCode`

For each page, it added a `PremiumHarbourBackdrop` to the first Canvas. That backdrop
loaded `Assets/Resources/Frontend/pirate-academy-harbour.png` at 88% opacity, then
added a navy veil at 44% opacity and a low harbour-glow layer. It also changed button
outlines and label/text colours and shadows at runtime.

This was the source of the new harbour picture in the screenshot. It also explains
why character selection changed even though no character-selection scene or character
selection script was edited.

**Restoration completed:**

- Removed `PremiumFrontendPolish.cs` and its Unity metadata.
- Removed the harbour image resource and its metadata.
- Removed the now-empty frontend folder metadata.

`StudentAvatarSelect.unity`, `StudentCharacterSelect.cs`, and
`CharacterCreation.cs` had no branch diff versus `main`; after the global script was
removed, character selection returns to its authored appearance and behaviour.

### Added quest status/card on the Classes pages

The screenshot text `Quest status could not be loaded. Please try again.` came from
the added `StudentQuestLobby` component after its Firestore assignment read failed.

The initial implementation added the following legacy-page UI:

- **StudentHub:** a bottom-right `StartPirateQuestButton`, Canvas `QuestLauncher`,
  Canvas `StudentQuestLobby`, and the runtime status label.
- **ClassroomScene:** Canvas `QuestLauncher` and Canvas `StudentQuestLobby`; the
  component built a lower-right quest card at runtime.
- **TeacherClass:** Canvas `TeacherQuestLobby`; the component built a lower-right
  unlock/lock control at runtime and changed serialized class-row presentation values.
- **QuestLobbyAutoSetup:** an automatic scene-load script which re-added those
  components if a Canvas did not already have them.

**Restoration completed:**

- Restored `StudentHub.unity` exactly to its `main` version.
- Restored `ClassroomScene.unity` exactly to its `main` version.
- Restored `TeacherClass.unity` exactly to its `main` version.
- Removed `QuestLobbyAutoSetup.cs` so no legacy page can gain quest UI at runtime.
- Removed `QuestLobbyControllers.cs`, which created the student status/card and
  teacher unlock panel.
- Removed `SetupPirateQuestIntegration.cs`, the editor command that could put those
  controls back into legacy pages.
- Changed `SetupCelestialClockQuest.cs` so its setup command creates only the
  standalone quest scene and build-setting entry. It no longer opens, edits, or adds
  components to Student Hub or Classroom scenes.

## Opacity failure-path repair

`StudentClassLoader` already hid the Class overlay before its Firebase class query.
Its error/no-user path did not reveal that overlay again, which could leave the
original class UI transparent after an offline, permission, or authentication error.

**Change made during restoration:**

- Added `ShowClassesAfterLoadFailure()` in
  `Assets/Scripts/Student/StudentClassLoader.cs`.
- The no-user and exception paths now re-enable the existing authored Classes UI and
  show its existing empty-state message instead of leaving the canvas faded out.

This is a resilience fix, not a redesign: it does not add a background, move layout,
or restyle the page.

## Changes intentionally retained

The following remain in the branch because they are isolated from existing classes and
character-selection layouts:

- `Assets/Scenes/Gameplay/CelestialClockQuest.unity` and the Celestial Clock runtime:
  the standalone pirate learning quest, ship deck, captain, four math challenges,
  journal, progress, and completion flow.
- `Assets/Content/Quests/CelestialClockQuest.asset` and the quest framework under
  `Assets/Scripts/QuestFramework/`.
- Imported art under `Assets/Gameplay/Pirate/` and `Assets/Gameplay/ColonialShip/`.
- Preview tools: `BuildCelestialClockPreview.cs` and the editor preview menu command.
- The preview-only `SceneTransition` guard, which prevents a one-scene standalone
  preview from trying to load an omitted hub scene.
- Firebase Functions, rules, indexes, and seed files under `firebase/`. These are a
  scaffold only; they have not been deployed or connected to the restored legacy UI.
- Class-data reliability changes that do not restyle screens:
  normalized class-code/membership data in `JointClassManager.cs`, class-context
  synchronization in `StudentClassLoader.cs`, and normalized teacher class metadata
  in `TeacherClassManager.cs`.

The quest can be opened directly from
`Assets/Scenes/Gameplay/CelestialClockQuest.unity` or with
**Tools > Imagine Quest > Preview Celestial Clock Quest**. It no longer appears on or
changes the existing Classes or character-selection pages.

## Verification performed

After restoration, these three legacy scenes are byte-for-byte equivalent in content
to their `main` versions (apart from working-tree line-ending normalization):

```text
Assets/Scenes/StudentPages/StudentHub.unity
Assets/Scenes/StudentPages/ClassroomScene.unity
Assets/Scenes/TeacherPages/TeacherClass.unity
```

You can independently verify the scene restoration with:

```powershell
git diff --exit-code main -- Assets/Scenes/StudentPages/StudentHub.unity
git diff --exit-code main -- Assets/Scenes/StudentPages/ClassroomScene.unity
git diff --exit-code main -- Assets/Scenes/TeacherPages/TeacherClass.unity
```

For the full file-by-file history of the original implementation, run:

```powershell
git diff --name-status main...feature/celestial-clock-quest
git log --oneline main..feature/celestial-clock-quest
```

## Unrelated local files preserved

This restoration does not stage, delete, or alter the pre-existing local URP/font/
project-settings changes or `Vision Document.docx`. They remain outside the feature
branch work.
