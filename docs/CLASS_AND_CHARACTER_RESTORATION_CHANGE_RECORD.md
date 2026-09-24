# Classes and Character Selection — UI Fix Record

## Scope of this fix

This document records the narrow UI fix made after the Classes page and character
selection screen were visually covered by a new harbour image.

The teacher/student class workflow, quest assignment controls, and standalone quest
remain in place. This fix does **not** remove those features.

## Root cause

The visual regression came from one runtime-only script added in the Celestial Clock
work:

```text
Assets/Scripts/Frontend/PremiumFrontendPolish.cs
```

It automatically ran after every scene load and targeted these existing screens:

- Welcome Page and Login
- Student Avatar Select / character selection
- Student Hub / Your Classes
- Classroom Scene
- Teacher Class Select and Teacher Class
- Student Profile and Join Class

For each target Canvas, it added a full-screen `PremiumHarbourBackdrop`, loaded the
harbour artwork at 88% opacity, then added a 44% navy veil. It also changed button
outlines plus text colours and shadows. That is why the existing Classes and character
selection layouts became faint and appeared behind the new picture.

## Changes made now

### Removed

- `Assets/Scripts/Frontend/PremiumFrontendPolish.cs`
- Its Unity metadata file

Because this was the only automatic visual layer, removing it restores the authored
look of the existing pages. `StudentAvatarSelect.unity`, `StudentCharacterSelect.cs`,
and `CharacterCreation.cs` were never changed by the Celestial Clock branch.

### Kept and restored

The following functionality remains deliberately intact:

- Student Hub and Classroom quest entry controls.
- Teacher class quest unlock/lock control.
- `QuestLobbyAutoSetup`, `QuestLobbyControllers`, and `QuestLauncher`.
- The pirate harbour image resource. It is retained as an asset but is no longer
  loaded automatically by any legacy screen.
- `SetupPirateQuestIntegration` and `SetupCelestialClockQuest` editor tools.
- The standalone Celestial Clock game, its pirate/ship art, quest framework, Firebase
  scaffold, and local preview builder.

### Reliability fix retained

`Assets/Scripts/Student/StudentClassLoader.cs` now reveals the existing Classes UI
when its authentication or Firebase class query fails. Before this change, a failed
load could leave the original content transparent. This fix does not add artwork,
change layout, or restyle the page.

## Explicitly not changed by this UI fix

- The existing character-selection scene hierarchy and character-selection scripts.
- Teacher/student class creation and joining logic.
- The quest scene: `Assets/Scenes/Gameplay/CelestialClockQuest.unity`.
- Any user-owned URP/font/project-settings changes or `Vision Document.docx`.

## Verify the current behavior

1. Exit Play mode and let Unity reload scripts.
2. Open `Assets/Scenes/StudentPages/StudentHub.unity` and press Play.
3. The normal Classes UI and character selection should retain their original look;
   the full-screen harbour image and dark veil should not appear.
4. Existing teacher/student quest controls remain available as before.

For the complete branch history, use:

```powershell
git log --oneline main..feature/celestial-clock-quest
git diff --name-status main...feature/celestial-clock-quest
```
