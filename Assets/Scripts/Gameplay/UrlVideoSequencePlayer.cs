using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace ImagineQuest.Gameplay
{
    /// <summary>
    /// Plays a directly streamable HTTPS media URL and always returns control to Unity.
    /// Empty or unavailable URLs fall back to the quest briefing rather than blocking play.
    /// </summary>
    public sealed class UrlVideoSequencePlayer : MonoBehaviour
    {
        [SerializeField] private RawImage display;
        [SerializeField] private CanvasGroup container;
        [SerializeField] private VideoPlayer player;

        public void Play(string url, Action completed)
        {
            StartCoroutine(PlayRoutine(url, completed));
        }

        private IEnumerator PlayRoutine(string url, Action completed)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                completed?.Invoke();
                yield break;
            }

            if (player == null)
                player = gameObject.AddComponent<VideoPlayer>();
            if (container == null)
                container = GetComponent<CanvasGroup>();

            player.source = VideoSource.Url;
            player.url = url;
            player.playOnAwake = false;
            player.waitForFirstFrame = true;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.audioOutputMode = VideoAudioOutputMode.Direct;

            var texture = new RenderTexture(1920, 1080, 0);
            player.targetTexture = texture;
            if (display != null)
                display.texture = texture;

            bool failed = false;
            player.errorReceived += (_, __) => failed = true;
            if (container != null)
            {
                container.alpha = 1f;
                container.blocksRaycasts = true;
            }

            player.Prepare();
            float timeout = 15f;
            while (!player.isPrepared && !failed && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (!player.isPrepared || failed)
            {
                Debug.LogWarning("[Video] Quest video could not be prepared: " + url);
            }
            else
            {
                player.Play();
                while (player.isPlaying && !failed)
                    yield return null;
            }

            player.Stop();
            player.targetTexture = null;
            Destroy(texture);
            if (container != null)
            {
                container.alpha = 0f;
                container.blocksRaycasts = false;
            }
            completed?.Invoke();
        }
    }
}
