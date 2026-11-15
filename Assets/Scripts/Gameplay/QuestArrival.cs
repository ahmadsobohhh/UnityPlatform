using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ImagineQuest.Gameplay
{
    internal static class QuestArrival
    {
        public static IEnumerator Play(Transform owner, string title, string subtitle, Action<float> pose, bool alreadyRevealed = false)
        {
            var root = new GameObject(title + " arrival", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            root.transform.SetParent(owner, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 990;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = .5f;
            var group = root.GetComponent<CanvasGroup>();
            group.interactable = group.blocksRaycasts = false;

            var black = CelestialClockQuestBootstrap.CreateFullOverlay("Reveal", root.transform, Color.black).GetComponent<Image>();
            black.raycastTarget = false;
            var top = Strip("Top matte", root.transform, true);
            var bottom = Strip("Bottom matte", root.transform, false);
            var centre = new Vector2(.5f, .5f);
            var text = CelestialClockQuestBootstrap.CreateText("Destination", root.transform, centre, centre,
                new Vector2(0, 12), new Vector2(820, 76), 53, TextAlignmentOptions.Center, new Color(1, .92f, .73f));
            text.text = title;
            text.fontStyle = FontStyles.Bold;
            text.characterSpacing = 9;
            var sub = CelestialClockQuestBootstrap.CreateText("Heading", root.transform, centre, centre,
                new Vector2(0, -64), new Vector2(640, 34), 20, TextAlignmentOptions.Center, new Color(.95f, .72f, .39f));
            sub.text = subtitle;
            sub.characterSpacing = 6;
            var rule = new GameObject("Brass line", typeof(RectTransform), typeof(Image));
            rule.transform.SetParent(root.transform, false);
            var line = rule.GetComponent<RectTransform>();
            line.anchorMin = line.anchorMax = centre;
            line.anchoredPosition = new Vector2(0, -37);
            rule.GetComponent<Image>().color = new Color(.83f, .57f, .28f);
            rule.GetComponent<Image>().raycastTarget = false;

            const float duration = 2.25f;
            float elapsed = 0;
            // The canvas is parented to the destination. Scene unload cleans it
            // up without invoking a stale camera callback during teardown.
            while (elapsed < duration && owner != null)
            {
                float t = elapsed / duration;
                float reveal = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.04f, .50f, t));
                float fade = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.64f, 1, t));
                black.color = new Color(.012f, .010f, .009f, (alreadyRevealed ? .24f : 1f) * (1 - reveal));
                group.alpha = 1 - fade;
                text.alpha = sub.alpha = Mathf.SmoothStep(0, 1, t * 5);
                text.rectTransform.anchoredPosition = new Vector2(0, Mathf.Lerp(1, 18, reveal));
                line.sizeDelta = new Vector2(Mathf.Lerp(0, 274, reveal), 2);
                top.sizeDelta = bottom.sizeDelta = new Vector2(0, 65 * (1 - fade));
                pose?.Invoke(Mathf.SmoothStep(0, 1, t));
                yield return null;
                var key = Keyboard.current;
                if (key != null && (key.escapeKey.wasPressedThisFrame || key.spaceKey.wasPressedThisFrame)) break;
                elapsed += Time.unscaledDeltaTime;
            }
            if (owner != null) pose?.Invoke(1f);
            if (root != null) UnityEngine.Object.Destroy(root);
            yield return null;
        }

        private static RectTransform Strip(string name, Transform owner, bool top)
        {
            var strip = new GameObject(name, typeof(RectTransform), typeof(Image));
            strip.transform.SetParent(owner, false);
            var rect = strip.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, top ? 1 : 0);
            rect.anchorMax = new Vector2(1, top ? 1 : 0);
            rect.pivot = new Vector2(.5f, top ? 1 : 0);
            var image = strip.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = Color.black;
            return rect;
        }
    }
}
