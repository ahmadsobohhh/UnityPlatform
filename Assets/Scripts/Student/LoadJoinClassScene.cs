using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LoadJoinClassScene : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject joinPopup;
    [SerializeField] private float popupFadeDuration = 0.18f;

    [Header("Theme")]
    [SerializeField] private Color panelColor = new Color(0.03f, 0.04f, 0.06f, 0.78f);
    [SerializeField] private Color titleColor = new Color(0.93f, 0.82f, 0.57f, 1f);
    [SerializeField] private Color inputBackgroundColor = new Color(0.75f, 0.75f, 0.75f, 0.9f);
    [SerializeField] private Color inputTextColor = new Color(0.16f, 0.16f, 0.16f, 1f);
    [SerializeField] private Color inputPlaceholderColor = new Color(0.52f, 0.52f, 0.52f, 0.85f);
    [SerializeField] private Color joinButtonColor = new Color(0.43f, 0.31f, 0.12f, 0.96f);
    [SerializeField] private Color joinButtonTextColor = new Color(0.95f, 0.95f, 0.95f, 1f);
    [SerializeField] private float titleFontSize = 42f;
    [SerializeField] private float inputFontSize = 26f;
    [SerializeField] private float placeholderFontSize = 24f;

    private CanvasGroup popupGroup;
    private RectTransform popupRect;

    private void Awake()
    {
        ResolveJoinPopupReference();

        if (joinPopup != null)
        {
            popupGroup = joinPopup.GetComponent<CanvasGroup>();
            if (popupGroup == null)
                popupGroup = joinPopup.AddComponent<CanvasGroup>();

            popupRect = joinPopup.GetComponent<RectTransform>();
            ApplyPopupTheme();
            SetPopupVisibleImmediate(false);
        }
    }

    public void GoToJoinClass()
    {
        if (joinPopup == null)
            ResolveJoinPopupReference();

        if (joinPopup == null)
        {
            Debug.LogWarning("[LoadJoinClassScene] JoinGUI popup was not found in StudentHub.");
            return;
        }

        if (!joinPopup.activeSelf || (popupGroup != null && popupGroup.alpha < 0.9f))
        {
            OpenJoinClassPopup();
            return;
        }

        var joinManager = FindFirstObjectByType<JointClassManager>();
        if (joinManager != null)
        {
            joinManager.JoinClassByCode();
            return;
        }

        Debug.LogWarning("[LoadJoinClassScene] JointClassManager not found; cannot submit join code.");
    }

    private void ResolveJoinPopupReference()
    {
        if (joinPopup != null)
            return;

        // JoinGUI starts inactive, so standard GameObject.Find cannot see it.
        var directChild = transform.Find("JoinGUI");
        if (directChild != null)
        {
            joinPopup = directChild.gameObject;
            return;
        }

        var activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
            return;

        foreach (var root in activeScene.GetRootGameObjects())
        {
            var allChildren = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in allChildren)
            {
                if (t != null && t.name == "JoinGUI")
                {
                    joinPopup = t.gameObject;
                    return;
                }
            }
        }
    }

    private void ApplyPopupTheme()
    {
        if (joinPopup == null) return;

        var panelImage = joinPopup.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = panelColor;
            panelImage.raycastTarget = true;
        }

        TMP_InputField input = null;
        Button joinBtn = null;
        TMP_Text title = null;
        TMP_Text closeX = null;

        foreach (var tmp in joinPopup.GetComponentsInChildren<TMP_Text>(true))
        {
            if (tmp == null) continue;
            string lower = (tmp.text ?? "").Trim().ToLowerInvariant();

            if (title == null && lower.Contains("join") && lower.Contains("class"))
                title = tmp;
            else if (closeX == null && lower == "x")
                closeX = tmp;
        }

        foreach (var field in joinPopup.GetComponentsInChildren<TMP_InputField>(true))
        {
            if (field != null && field.gameObject.name == "codeInput")
            {
                input = field;
                break;
            }
        }

        foreach (var btn in joinPopup.GetComponentsInChildren<Button>(true))
        {
            if (btn == null) continue;
            if (btn.gameObject.name == "JoinClassBtn")
                joinBtn = btn;
        }

        if (title != null)
        {
            title.text = "Join a Class";
            title.color = titleColor;
            title.fontSize = titleFontSize;
            title.fontStyle = FontStyles.Normal;
            title.alignment = TextAlignmentOptions.Center;
        }

        if (closeX != null)
        {
            closeX.color = titleColor;
            closeX.fontSize = Mathf.Max(closeX.fontSize, 34f);
        }

        if (input != null)
        {
            var bg = input.targetGraphic as Image;
            if (bg != null)
                bg.color = inputBackgroundColor;

            if (input.textComponent != null)
            {
                input.textComponent.color = inputTextColor;
                input.textComponent.fontSize = inputFontSize;
                input.textComponent.fontStyle = FontStyles.Normal;
            }

            var ph = input.placeholder as TMP_Text;
            if (ph != null)
            {
                ph.text = "Enter class code...";
                ph.color = inputPlaceholderColor;
                ph.fontSize = placeholderFontSize;
                ph.fontStyle = FontStyles.Italic;
            }
        }

        if (joinBtn != null)
        {
            var joinImg = joinBtn.targetGraphic as Image;
            if (joinImg != null)
                joinImg.color = joinButtonColor;

            var label = joinBtn.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = "Join";
                label.color = joinButtonTextColor;
                label.fontSize = Mathf.Max(label.fontSize, 40f);
            }
        }
    }

    public void OpenJoinClassPopup()
    {
        if (joinPopup == null) return;
        StopAllCoroutines();
        StartCoroutine(FadePopup(true));
    }

    public void CloseJoinClassPopup()
    {
        if (joinPopup == null) return;
        StopAllCoroutines();
        StartCoroutine(FadePopup(false));
    }

    private System.Collections.IEnumerator FadePopup(bool show)
    {
        joinPopup.SetActive(true);

        float start = popupGroup != null ? popupGroup.alpha : (show ? 0f : 1f);
        float end = show ? 1f : 0f;

        Vector3 baseScale = Vector3.one;
        if (popupRect != null && popupRect.localScale != Vector3.zero)
            baseScale = popupRect.localScale;

        float t = 0f;
        while (t < popupFadeDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / Mathf.Max(0.01f, popupFadeDuration));
            float eased = 1f - Mathf.Pow(1f - p, 3f);
            float a = Mathf.Lerp(start, end, eased);

            if (popupGroup != null)
            {
                popupGroup.alpha = a;
                popupGroup.interactable = show && a > 0.95f;
                popupGroup.blocksRaycasts = show && a > 0.95f;
            }

            if (popupRect != null)
            {
                float scale = show
                    ? Mathf.Lerp(0.95f, 1f, eased)
                    : Mathf.Lerp(1f, 0.95f, eased);
                popupRect.localScale = baseScale * scale;
            }

            yield return null;
        }

        if (popupGroup != null)
        {
            popupGroup.alpha = end;
            popupGroup.interactable = show;
            popupGroup.blocksRaycasts = show;
        }

        if (popupRect != null)
            popupRect.localScale = baseScale;

        if (!show)
            joinPopup.SetActive(false);
    }

    private void SetPopupVisibleImmediate(bool visible)
    {
        if (joinPopup == null) return;

        joinPopup.SetActive(visible);
        if (popupGroup != null)
        {
            popupGroup.alpha = visible ? 1f : 0f;
            popupGroup.interactable = visible;
            popupGroup.blocksRaycasts = visible;
        }

        if (popupRect != null)
            popupRect.localScale = Vector3.one;
    }
}