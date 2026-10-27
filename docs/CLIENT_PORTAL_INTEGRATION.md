# Client portal and main integration

This branch includes main's workshop quest, dashboard, login fonts, and ship arena.
The workshop now uses the client's animated vine-and-flower portal rather than a
procedural approximation of the artwork.

## Student flow

1. Read the anvil, workbench, and forge lessons and answer each question correctly.
2. The Forge Stone appears in the clear center of the workshop. Repeated answers
   cannot unlock it early or create duplicates.
3. Walk into the stone. Contact between the player's body and stone volume saves
   portal activation, consumes the stone, and grows the client portal by the door.
   No interaction key is needed. Menus, lessons, and loss of game focus block this.
4. Walk into the open portal to return to `QuestArena` with the repair kit. The
   transition saves completion once. E near the portal can also review the kit.
5. Reopening the workshop restores crafts and portal activation. Journal exit
   remains available before completion and preserves partial progress.

This integrates with the existing class/student/assignment-scoped device checkpoint.
Portal activation is local presentation state and does not mint currency or add a
new backend action. Existing cloud verification remains independent. The repository
still does not supply the `blacksmithQuest` server function; do not describe local
completion as verified wallet XP.

## Client media

- `Portal 1.mp4` is a 23-second recording demonstrating the desired interaction.
  It is reference footage, not a texture to show inside the game.
- `Portail 1 - contour noir.mov` supplies the 5.04-second portal animation. It is
  1116 x 1856, 24 fps, QuickTime Animation / ARGB with an existing alpha channel.
  The original is about 623 MB and remains outside this repository.
- `Assets/Resources/Workshop/ClientPortal.mp4` is a roughly 4.8 MB H.264 derivative.
  RGB and the original alpha mask are packed side by side, avoiding dependence on
  native alpha-video codec support on Windows. A 32-pixel white border at the top
  of the supplied source is cropped; the artwork itself is retained. Each half is
  558 x 912, so the packed video is 1116 x 912. BT.709 colour metadata is explicit
  so Windows Media Foundation does not have to guess its colour primaries.
- `ClientPortalPoster.png` contains its first packed frame. The actual client
  artwork appears while the decoder prepares, and remains visible if playback
  fails. A warning is reported on failure; this fallback is not counted as passing
  the animation test.
- `EmeraldPortal.shader` samples RGB on the left and alpha on the right. It is a
  double-sided URP transparent shader. It preserves dark leaves and wood instead
  of treating all black/dark pixels as transparent. The world remains visible
  outside the client's alpha outline; there is no rectangular black backing.
- The short effect is silent and loops. This local effect is separate from the
  URL-based story/lesson video player.

Reproduce the derivative with FFmpeg (using the original supplied MOV):

```text
ffmpeg -i "Portail 1 - contour noir.mov" -filter_complex "[0:v]crop=1116:1824:0:32,split[c][a];[c]scale=558:912,format=rgb24[color];[a]alphaextract,scale=558:912,format=rgb24[alpha];[color][alpha]hstack,scale=out_color_matrix=bt709,format=yuv420p[v]" -map "[v]" -an -c:v libx264 -crf 18 -color_primaries bt709 -color_trc bt709 -colorspace bt709 -color_range tv -bsf:v "h264_metadata=colour_primaries=1:transfer_characteristics=1:matrix_coefficients=1:video_full_range_flag=0" -movflags +faststart ClientPortal.mp4
ffmpeg -i ClientPortal.mp4 -frames:v 1 ClientPortalPoster.png
```

## Checks

- EditMode: `BlacksmithWorkshopTests`, `StudentDashboardTests`.
- PlayMode: `BlacksmithWorkshopPlayTests`, `WorkshopForgeRewardTests`,
  `WorkshopPortalFlowTests`.
- The media test requires actual decoded, advancing video frames. It renders the
  portal over magenta and checks transparency outside the silhouette.
- The flow test completes lessons, checks a paused stone cannot trigger, touches
  the stone, reloads its saved activation, walks into the portal, verifies the real
  ship scene loads, and checks the saved completion without awarding local XP.
- Set `IMAGINEQUEST_TEST_OUTPUT` to a folder before running tests to retain portal
  screenshots outside Unity's temporary directory.
- `Imagine Quest > Blacksmith > Run Workshop Play Checks` runs all three workshop
  test fixtures. `Play Workshop` opens the scene directly for local practice.
- `BlacksmithWorkshopTools.BuildPortalPreview` builds the workshop and ship for
  Windows. Set `IMAGINEQUEST_BUILD_OUTPUT` to the desired executable path. This
  does not alter the project's normal build-scene list.

Main's former decorative blue portal is replaced, so the room has one return
portal system. Zeenat's old field-reflection tests have been updated to exercise
the integrated quest rather than the removed standalone question implementation.
