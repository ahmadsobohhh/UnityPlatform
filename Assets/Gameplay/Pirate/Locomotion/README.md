# Pirate locomotion

Idle.fbx, Walk.anim and RunForward.fbx are reused from the existing capstone-project repository, Assets/Core/UserAvatar/FinalCharacterController/Animation. Their existing asset identifiers were preserved. No new asset-store download is required.

PirateLocomotion blends three distinct humanoid clips: idle at Speed 0, walk at Speed 1, run at Speed 2. The pirate's existing humanoid avatar (Pirate.FBX) retargets those clips. Movement drives the blend using actual capsule velocity, so walking against an obstacle does not keep playing a full-speed walk. Holding Shift increases movement speed and selects the running blend.

Crew members use Speed 0 continuously for a breathing idle. The arena captain slot is currently empty pending a replacement skin. The old Walk@Test animation controller is retained for legacy art demos, but is no longer used by arena characters.
