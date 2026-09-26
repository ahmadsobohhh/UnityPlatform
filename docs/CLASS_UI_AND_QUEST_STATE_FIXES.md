# Class UI and Quest-State Fixes

Date: 2026-09-26
Branch: `feature/celestial-clock-quest`

This document records the changes made in response to the screenshots showing oversized Join/Create dialogs, the old teacher parchment screen, overlapping quest controls, repeated quest errors, and a student quest warning surviving sign-out.

## What was actually broken

### The warning survived sign-out

`QuestLobbyAutoSetup` used `FindFirstObjectByType<Canvas>()`. The project has a `SceneTransition` Canvas marked `DontDestroyOnLoad`, so that persistent Canvas could be selected instead of the Canvas belonging to `StudentHub`.

The student quest controller was then attached to the permanent transition object. It survived scene changes and account changes, which is why the student warning could appear at the same time as the teacher quest panel.

### The backend read is being rejected

The Unity Editor log contains:

```text
Listen for query at classes/{classId}/questAssignments/pirate-voyage failed:
Missing or insufficient permissions.
```

A missing quest document is not an error in Firestore. It returns a successful snapshot with `Exists == false`. The yellow error therefore came from deployed Firestore permissions, not from a teacher simply having no assignment yet.

The repository already contains the intended transition rule in `firebase/firestore.rules`, but those local rules still need to be published to the Firebase project. The Firebase CLI session on this computer is expired, so deployment was not performed automatically.

After re-authenticating, publish the rules from the Unity project's `firebase` folder:

```powershell
firebase login --reauth
Set-Location firebase
firebase deploy --only firestore:rules --project capstone-d51c2
```

Review the rules before deploying them to any production project.

## UI changes

### Student Join Class dialog

Changed `Assets/Scripts/Student/LoadJoinClassScene.cs`:

- Replaced the stretch-based panel layout with a centered `560 x 360` dialog.
- Reduced the title, input, and button footprint.
- Added short instructions and an in-dialog feedback line.
- Kept the existing Student Hub background visible.
- Kept the close button and fade animation.

Changed `Assets/Scripts/Student/JoinClassManager/JointClassManager.cs`:

- Connected join validation and network errors to the new visible feedback line.

### Teacher Create Class dialog

Changed `Assets/Scripts/Teacher/ClassSelect/TeacherClassManager.cs`:

- Replaced the half-screen stretch layout with a centered `660 x 360` dialog.
- Repositioned the title, field label, input, and action row.
- Restyled Create and Cancel to the shared brown/gold game theme.
- Preserved the existing create-class data logic.

### Teacher class view

Changed `Assets/Scenes/TeacherPages/TeacherClass.unity` and `TeacherClassManager.cs`:

- Replaced the narrow blue/parchment presentation with the existing Student Hub pirate-room background.
- Switched the Canvas to the project's `1920 x 1080` responsive scaling setup.
- Added a centered command-deck card with a class heading, join code, and crew roster.
- Removed the non-functional runtime `Edit List` clone.
- Moved Back and Sign Out into stable bottom-corner positions.
- Integrated the teacher quest control inside the command deck instead of floating it over other controls.

### Student class view

Changed `Assets/Scripts/Student/Classroom/ClassroomManager.cs`:

- Tightened the main class card and its typography.
- Converted the body to a two-column layout: roster on the left and assigned quest on the right.
- Reduced oversized student rows and type.
- Kept the same pirate-room background and navigation.

### Your Classes page

- Removed the bottom-right **Begin Celestial Clock** button from `StudentHub`.
- Quest launch is now available only after the student opens a class.

Changed `Assets/Scripts/Gameplay/QuestLobbyControllers.cs`:

- Removed the free-floating yellow status line and quest-launch button from Student Hub.
- Replaced blue floating quest panels with contained brown/gold quest cards.
- Added separate teacher and student copy appropriate to each role.
- Treats a missing assignment as a normal locked state.
- Keeps real Firestore failures in the Unity Console while presenting a contained, readable message.
- Ignores async results if the signed-in account changes before a request completes.

## Session and sign-out changes

Changed `Assets/Scripts/Gameplay/QuestLobbyAutoSetup.cs`:

- Resolves the Canvas only from the newly loaded scene.
- Removes any stale quest-lobby components that were attached to a persistent object.

Changed `Assets/Scripts/Teacher/ClassSelect/ClassSelection.cs` and all student/teacher sign-out handlers:

- Clears static class selection.
- Clears selected/joined class `PlayerPrefs` keys.
- Clears the persistent quest session automatically when Firebase signs out.

## Verification performed

- `git diff --check`: passed.
- Full `GameRuntime` C# compile: passed with zero errors.
- The existing warnings are TextMesh Pro deprecation/unused serialized-field warnings and do not block Unity.
- Live Firebase deployment was not attempted because `firebase projects:list` returned an expired OAuth/401 authentication error.

## Manual Unity check

1. Stop Play mode so Unity imports the updated scripts.
2. Open `Assets/Scenes/StudentPages/StudentHub.unity` and press Play.
3. Click **Join Class** and confirm the compact dialog remains centered.
4. Sign in as a teacher, open **TeacherClassSelect**, and confirm the compact Create Class dialog.
5. Open a class and confirm the command-deck view has one integrated quest card.
6. Sign out, sign in as a student, open the same class, and confirm no teacher/student warning survives the account change.
7. After publishing Firestore rules, unlock the quest as the teacher and confirm the student button changes to **BEGIN CELESTIAL CLOCK**.
