// Script: SceneTransition
// Path: Assets/Scripts/Shared/SceneTransition.cs
// Purpose: Performs animated scene changes and transition timing control.

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }

    [SerializeField] private float defaultFadeTime = 0.5f;

    private Canvas canvas;
    private Image overlay;
    private bool busy;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
    }

    private void BuildOverlay()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        gameObject.AddComponent<GraphicRaycaster>();

        var go = new GameObject("FadeOverlay");
        go.transform.SetParent(transform, false);
        overlay = go.AddComponent<Image>();
        overlay.color = new Color(0, 0, 0, 0);
        overlay.raycastTarget = false;

        var rect = overlay.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void EnsureInstance()
    {
        if (Instance != null) return;
        var go = new GameObject("SceneTransition");
        go.AddComponent<SceneTransition>();
    }

    public static void LoadScene(string sceneName, float fadeTime = -1f)
    {
        EnsureInstance();
        if (Instance.busy) return;
        float t = fadeTime < 0 ? Instance.defaultFadeTime : fadeTime;
        Instance.StartCoroutine(Instance.DoTransition(sceneName, t));
    }

    private IEnumerator DoTransition(string sceneName, float fadeTime)
    {
        busy = true;
        overlay.raycastTarget = true;

        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            overlay.color = new Color(0, 0, 0, Mathf.Clamp01(elapsed / fadeTime));
            yield return null;
        }
        overlay.color = Color.black;

        SceneManager.LoadScene(sceneName);
        yield return null;

        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            overlay.color = new Color(0, 0, 0, 1f - Mathf.Clamp01(elapsed / fadeTime));
            yield return null;
        }
        overlay.color = new Color(0, 0, 0, 0);
        overlay.raycastTarget = false;
        busy = false;
    }
}


