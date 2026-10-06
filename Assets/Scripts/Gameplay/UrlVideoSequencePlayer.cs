using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;

namespace ImagineQuest.Gameplay
{
    /// <summary>Skippable video on a square world-space panel. Empty/failed streams return to the quest.</summary>
    public sealed class UrlVideoSequencePlayer : MonoBehaviour
    {
        [SerializeField] private RawImage display;
        [SerializeField] private CanvasGroup container;
        [SerializeField] private VideoPlayer player;
        private TMP_Text status;
        private RenderTexture texture;
        private Action continuation;
        private bool playing;
        private bool failed;
        private bool ended;
        private Canvas worldCanvas;
        private AspectRatioFitter videoAspect;
        private bool paused;
        private float volume = 0.8f;
        private bool muted;
        private Slider timeline;
        private TMP_Text timeLabel;
        private TMP_Text pauseLabel;
        private TMP_Text muteLabel;
        private QuestYouTubePlayer youtube;
        private string currentUrl;
        private Action exitAction;
        private Vector3 anchorPosition;
        public Vector3? WorldAnchor { get; set; }
        public bool IsPlaying => playing;

        public void Play(string url, Action completed) => Play(url, completed, completed);

        public void Play(string url, Action completed, Action exited)
        {
            if (playing) return;
            if (string.IsNullOrWhiteSpace(url))
            {
                completed?.Invoke();
                return;
            }
            EnsureOverlay();
            currentUrl = url;
            exitAction = exited;
            anchorPosition = WorldAnchor ?? (Camera.main == null ? Vector3.zero : Camera.main.transform.position + Camera.main.transform.forward * 2.5f);
            PositionPanel();
            playing = true;
            failed = ended = false;
            continuation = completed;
            container.alpha = 1;
            container.blocksRaycasts = container.interactable = true;
            if (status != null) status.text = "Loading video...";
            paused = false;
            if (pauseLabel != null) pauseLabel.text = "PAUSE";
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                (uri.Host == "youtube.com" || uri.Host.EndsWith(".youtube.com") || uri.Host == "youtu.be"))
            {
                string id = uri.Host == "youtu.be" ? uri.AbsolutePath.Trim('/') : null;
                foreach (var part in uri.Query.TrimStart('?').Split('&'))
                    if (part.StartsWith("v=")) id = Uri.UnescapeDataString(part.Substring(2));
                if (id == null || !System.Text.RegularExpressions.Regex.IsMatch(id, "^[a-zA-Z0-9_-]{11}$"))
                { status.text = "Invalid YouTube video link. Retry or exit."; return; }
                if (youtube == null) youtube = display.gameObject.AddComponent<QuestYouTubePlayer>();
                display.raycastTarget = true;
                youtube.Completed = () => Finish(true);
                youtube.Error = message => { status.text = message; };
                youtube.Open(id, display);
                return;
            }
            StartCoroutine(PlayRoutine(url));
        }

        public void Skip()
        {
            if (!playing) return;
            StopAllCoroutines();
            var onExit = exitAction;
            Finish(false);
            onExit?.Invoke();
        }

        private IEnumerator PlayRoutine(string url)
        {
            if (player == null) player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.isLooping = false;
            player.source = VideoSource.Url;
            player.url = url;
            player.waitForFirstFrame = true;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.audioOutputMode = VideoAudioOutputMode.Direct;
            texture = new RenderTexture(1280, 720, 0);
            texture.Create();
            player.targetTexture = texture;
            display.texture = texture;
            player.errorReceived += OnError;
            player.loopPointReached += OnEnded;
            player.Prepare();
            float remaining = 15f;
            while (!player.isPrepared && !failed && remaining > 0)
            {
                remaining -= Time.unscaledDeltaTime;
                yield return null;
            }
            if (!failed && player.isPrepared)
            {
                if (videoAspect != null && player.height > 0)
                    videoAspect.aspectRatio = (float)player.width / player.height;
                if (status != null) status.text = "";
                player.Play();
                ApplyVolume();
                // Wait for the end event, not isPlaying: playback may start asynchronously.
                double lastTime = -1;
                float stalled = 0;
                while (!ended && !failed)
                {
                    if (paused) { stalled = 0; yield return null; continue; }
                    if (player.time > lastTime)
                    {
                        lastTime = player.time;
                        stalled = 0;
                    }
                    else stalled += Time.unscaledDeltaTime;
                    if (stalled >= 20f) { failed = true; break; }
                    yield return null;
                }
            }
            else failed = true;
            if (failed)
            {
                if (status != null) status.text = "Video unavailable. Retry or exit.\nThe lesson has not started.";
                yield break;
            }
            Finish(true);
        }

        private void OnError(VideoPlayer source, string message)
        {
            failed = true;
            Debug.LogWarning("[Quest Video] Playback failed; waiting for retry or exit. " + message);
        }
        private void OnEnded(VideoPlayer source) => ended = true;

        private void Finish(bool resume)
        {
            if (youtube != null) youtube.Close();
            if (player != null)
            {
                player.errorReceived -= OnError;
                player.loopPointReached -= OnEnded;
                player.Stop();
                player.targetTexture = null;
            }
            if (display != null) display.texture = null;
            if (texture != null) { texture.Release(); Destroy(texture); texture = null; }
            if (container != null)
            {
                container.alpha = 0;
                container.blocksRaycasts = container.interactable = false;
            }
            playing = false;
            var callback = continuation;
            continuation = null;
            exitAction = null;
            if (resume) callback?.Invoke();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            Finish(false);
        }

        private void EnsureOverlay()
        {
            if (display != null && container != null) return;
            var root = new GameObject("Video Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            worldCanvas = canvas;
            var panelRect = root.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(800, 800);
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 2;
            container = root.GetComponent<CanvasGroup>();
            var background = Rect("Background", root.transform, Vector2.zero, Vector2.one);
            background.gameObject.AddComponent<Image>().color = new Color(0.035f, 0.055f, 0.08f, 0.98f);
            var border = background.gameObject.AddComponent<Outline>();
            border.effectColor = new Color(0.85f, 0.65f, 0.28f);
            border.effectDistance = new Vector2(4, -4);
            Label("Title", root.transform, new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.98f)).text = "CAPTAIN'S PROJECTION";
            var viewport = Rect("Video Area", root.transform, new Vector2(0.04f, 0.32f), new Vector2(0.96f, 0.88f));
            viewport.gameObject.AddComponent<Image>().color = Color.black;
            var picture = Rect("Video", viewport, Vector2.zero, Vector2.one);
            display = picture.gameObject.AddComponent<RawImage>();
            display.raycastTarget = false;
            var aspect = picture.gameObject.AddComponent<AspectRatioFitter>();
            videoAspect = aspect;
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = 16f / 9f;
            status = Label("Status", root.transform, new Vector2(0.08f, 0.4f), new Vector2(0.92f, 0.72f));
            timeline = MakeSlider("Seek", root.transform, new Vector2(0.06f, 0.26f), new Vector2(0.94f, 0.30f));
            timeline.onValueChanged.AddListener(value =>
            {
                if (youtube != null && youtube.Ready) { youtube.Seek(value * youtube.Duration); return; }
                if (player != null && player.isPrepared && player.canSetTime)
                    player.time = value * player.length;
            });
            timeLabel = Label("Time", root.transform, new Vector2(0.5f, 0.19f), new Vector2(0.94f, 0.25f));
            pauseLabel = Control("Pause", root.transform, 0.06f, 0.27f, TogglePause);
            pauseLabel.text = "PAUSE";
            muteLabel = Control("Mute", root.transform, 0.3f, 0.49f, () => { muted = !muted; ApplyVolume(); });
            muteLabel.text = "MUTE";
            Label("Volume label", root.transform, new Vector2(0.06f, 0.12f), new Vector2(0.3f, 0.18f)).text = "VOLUME";
            var volumeSlider = MakeSlider("Volume", root.transform, new Vector2(0.32f, 0.13f), new Vector2(0.94f, 0.17f));
            volumeSlider.value = volume;
            volumeSlider.onValueChanged.AddListener(value => { volume = value; ApplyVolume(); });
            Label("Input hint", root.transform, new Vector2(0.03f, 0.01f), new Vector2(0.72f, 0.1f)).text = "WASD move | Tab: look / controls";
            var skip = Rect("Exit Video", root.transform, new Vector2(0.75f, 0.025f), new Vector2(0.96f, 0.1f));
            skip.gameObject.AddComponent<Image>().color = new Color(0.35f, 0.23f, 0.09f);
            var button = skip.gameObject.AddComponent<Button>();
            button.onClick.AddListener(Skip);
            Label("Label", skip, Vector2.zero, Vector2.one).text = "EXIT";
            var retry = Control("Retry", root.transform, 0.52f, 0.72f, () =>
            {
                var complete = continuation; var exit = exitAction; var url = currentUrl;
                StopAllCoroutines(); Finish(false); Play(url, complete, exit);
            });
            retry.text = "RETRY";
            timeLabel.rectTransform.anchorMin = new Vector2(0.73f, 0.19f);
            timeLabel.fontSize = 20;
        }

        private void Update()
        {
            if (!playing) return;
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                bool look = Cursor.lockState != CursorLockMode.Locked;
                Cursor.lockState = look ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !look;
            }
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Skip();
            if (youtube != null && youtube.Ready)
            {
                if (status.text == "Loading video...") { status.text = ""; ApplyVolume(); }
                timeline.interactable = youtube.Duration > 0;
                if (youtube.Duration > 0) timeline.SetValueWithoutNotify((float)(youtube.CurrentTime / youtube.Duration));
                timeLabel.text = FormatTime(youtube.CurrentTime) + " / " + FormatTime(youtube.Duration);
                pauseLabel.text = youtube.Paused ? "PLAY" : "PAUSE";
                return;
            }
            bool ready = player != null && player.isPrepared;
            if (timeline != null)
            {
                timeline.interactable = ready && player.canSetTime && player.length > 0;
                if (ready && player.length > 0) timeline.SetValueWithoutNotify((float)(player.time / player.length));
            }
            if (timeLabel != null) timeLabel.text = ready
                ? FormatTime(player.time) + " / " + FormatTime(player.length) : "--:-- / --:--";
        }

        private void LateUpdate() { if (playing) PositionPanel(); }
        private static string FormatTime(double seconds) => TimeSpan.FromSeconds(Math.Max(0, seconds)).ToString(@"mm\:ss");
        private void TogglePause()
        {
            if (youtube != null && youtube.Ready) { youtube.TogglePause(); return; }
            if (player == null || !player.isPrepared) return;
            paused = !paused;
            if (paused) player.Pause(); else player.Play();
            pauseLabel.text = paused ? "PLAY" : "PAUSE";
        }
        private void ApplyVolume()
        {
            if (muteLabel != null) muteLabel.text = muted ? "UNMUTE" : "MUTE";
            if (youtube != null && youtube.Ready) { youtube.Volume(volume, muted); return; }
            if (player == null || !player.isPrepared) return;
            for (ushort track = 0; track < player.audioTrackCount; track++)
            {
                player.SetDirectAudioVolume(track, volume);
                player.SetDirectAudioMute(track, muted);
            }
        }
        private static TMP_Text Control(string name, Transform parent, float left, float right, UnityEngine.Events.UnityAction action)
        {
            var rect = Rect(name, parent, new Vector2(left, 0.19f), new Vector2(right, 0.25f));
            rect.gameObject.AddComponent<Image>().color = new Color(0.35f, 0.23f, 0.09f);
            rect.gameObject.AddComponent<Button>().onClick.AddListener(action);
            return Label("Label", rect, Vector2.zero, Vector2.one);
        }
        private static Slider MakeSlider(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var rect = Rect(name, parent, min, max);
            rect.gameObject.AddComponent<Image>().color = Color.gray;
            var handle = Rect("Handle", rect, Vector2.zero, Vector2.one);
            var graphic = handle.gameObject.AddComponent<Image>();
            graphic.color = new Color(0.9f, 0.72f, 0.3f);
            handle.sizeDelta = new Vector2(18, 0);
            var slider = rect.gameObject.AddComponent<Slider>();
            slider.handleRect = handle;
            slider.targetGraphic = graphic;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            return slider;
        }

        private void PositionPanel()
        {
            if (worldCanvas == null) return;
            var camera = Camera.main;
            if (camera == null) return;
            worldCanvas.worldCamera = camera;
            // Fixed world position above the seal; only rotation follows the viewer.
            var awayFromViewer = anchorPosition - camera.transform.position;
            worldCanvas.transform.position = anchorPosition;
            if (awayFromViewer.sqrMagnitude > 0.001f)
                worldCanvas.transform.rotation = Quaternion.LookRotation(awayFromViewer, Vector3.up);
            worldCanvas.transform.localScale = Vector3.one * (1.8f / 800f);
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
        private static TMP_Text Label(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var text = Rect(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = 28;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }
    }
}
