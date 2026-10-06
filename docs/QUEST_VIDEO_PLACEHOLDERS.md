# Quest video panel

Open CelestialClockQuest and select Celestial Clock Quest Runtime. Opening, Teaching, and Completion Video Url fields select each clip; clear a field to skip it. Defaults now reference https://www.youtube.com/watch?v=zQ8caaUxIvY.

YouTube playback is NOT integrated yet. The current native VideoPlayer supports direct media, not YouTube watch pages. YouTube URLs show a browser-required message and an Exit button instead of being submitted to the native decoder.

The square panel follows the camera in LateUpdate. WASD movement remains enabled. Tab switches between mouse-look and pointer controls. Exit (or Escape) closes the panel and continues the quest. Native media controls provide play/pause, seeking, elapsed/total time, volume, and mute. Pausing does not trigger the stall timeout.

Browser investigation:
- WebViewToolkit: MIT, Windows x64, DX11/12, project claims Unity 6000.3 testing, renders to a texture with JavaScript messaging. Candidate for a Windows-only integration; not yet installed or tested here. https://github.com/cantetfelix/WebViewToolkit
- Vuplex 3D WebView: commercial, browser-to-texture implementation with documented YouTube support. Requires the appropriate platform package/license. https://developer.vuplex.com/webview/overview

Verification: GameRuntime compiler check passed with existing warnings. In-editor input, rendering, and streaming need a playthrough. YouTube embedding remains pending browser integration; these controls do not currently play the requested YouTube clip.
