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
    [SerializeField] private Color panelColor = new Color(0.02f, 0.02f, 0.05f, 0.92f);
    [SerializeField] private Color titleColor = new Color(0.95f, 0.90f, 0.78f, 1f);
    [SerializeField] private Color inputBackgroundColor = new Color(0.08f, 0.06f, 0.04f, 0.9f);
    [SerializeField] private Color inputTextColor = new Color(0.95f, 0.92f, 0.85f, 1f);
    [SerializeField] private Color inputPlaceholderColor = new Color(0.6f, 0.55f, 0.45f, 0.7f);
    [SerializeField] private Color joinButtonColor = new Color(0.28f, 0.22f, 0.10f, 0.85f);
    [SerializeField] private Color joinButtonTextColor = new Color(0.95f, 0.90f, 0.78f, 1f);
    [SerializeField] private float titleFontSize = 56f;
    [SerializeField] private float inputFontSize = 34f;
    [SerializeField] private float placeholderFontSize = 30f;
    [SerializeField] private float joinButtonFontSize = 38f;

    private CanvasGroup popupGroup;
    private RectTransform popupRect;
    private JointClassManager joinManager;

    private void Awake()
    {
        ResolveJoinPopupReference();

        if (joinPopup != null)
        {
            popupGroup = joinPopup.GetComponent<CanvasGroup>();
            if (popupGroup == null)
                popupGroup = joinPopup.AddComponent<CanvasGroup>();

            popupRect = joinPopup.GetComponent<RectTransform>();

            EnsureJoinManager();
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

        if (joinManager == null)
            EnsureJoinManager();

        if (joinManager != null)
        {
            joinManager.JoinClassByCode();
            return;
        }

        Debug.LogWarning("[LoadJoinClassScene] JointClassManager could not be created.");
    }

    private void EnsureJoinManager()
    {
        joinManager = FindFirstObjectByType<JointClassManager>();
        if (joinManager != null) return;

        if (joinPopup == null) return;

        joinManager = joinPopup.AddComponent<JointClassManager>();
        joinManager.sceneToLoad = "StudentHub";

        var codeField = joinPopup.GetComponentInChildren<TMP_InputField>(true);
        if (codeField != null)
            joinManager.codeInput = codeField;
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

        // Force warm fantasy theme to match StudentHub regardless of serialized values
        Color warmPanel = new Color(0.02f, 0.02f, 0.05f, 0.92f);
        Color warmGold = new Color(0.95f, 0.90f, 0.78f, 1f);
        Color warmInputBg = new Color(0.08f, 0.06f, 0.04f, 0.9f);
        Color warmInputText = new Color(0.95f, 0.92f, 0.85f, 1f);
        Color warmPlaceholder = new Color(0.6f, 0.55f, 0.45f, 0.7f);
        Color warmBtnBg = new Color(0.28f, 0.22f, 0.10f, 0.85f);
        Color warmBtnText = new Color(0.95f, 0.90f, 0.78f, 1f);
        Color warmOutline = new Color(0.5f, 0.4f, 0.2f, 0.4f);

        var panelImage = joinPopup.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.sprite = null;
            panelImage.type = Image.Type.Simple;
            panelImage.color = warmPanel;
            panelImage.raycastTarget = true;
        }

        var panelOutline = joinPopup.GetComponent<Outline>();
        if (panelOutline == null) panelOutline = joinPopup.AddComponent<Outline>();
        panelOutline.effectColor = warmOutline;
        panelOutline.effectDistance = new Vector2(2, -2);

        TMP_InputField input = null;
        Button joinBtn = null;
        TMP_Text title = null;
        TMP_Text closeX = null;

        foreach (var field in joinPopup.GetComponentsInChildren<TMP_InputField>(true))
        {
            if (field != null && field.gameObject.name == "codeInput")
            { input = field; break; }
        }

        foreach (var btn in joinPopup.GetComponentsInChildren<Button>(true))
        {
            if (btn != null && btn.gameObject.name == "joinBtn")
            { joinBtn = btn; break; }
        }

        foreach (var tmp in joinPopup.GetComponentsInChildren<TMP_Text>(true))
        {
            if (tmp == null) continue;

            if (joinBtn != null && tmp.transform.IsChildOf(joinBtn.transform))
                continue;
            if (input != null && tmp.transform.IsChildOf(input.transform))
                continue;

            string lower = (tmp.text ?? "").Trim().ToLowerInvariant();

            if (title == null && lower.Length > 1 && lower != "x")
                title = tmp;
            else if (closeX == null && lower == "x")
                closeX = tmp;
        }

        if (title != null)
        {
            title.text = "Join a Class";
            title.color = warmGold;
            title.fontSize = 56;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.enableVertexGradient = true;
            title.colorGradient = new VertexGradient(
                new Color(1f, 0.97f, 0.88f),
                new Color(1f, 0.97f, 0.88f),
                new Color(0.72f, 0.60f, 0.40f),
                new Color(0.72f, 0.60f, 0.40f));
        }

        if (closeX != null)
        {
            closeX.color = new Color(0.80f, 0.70f, 0.50f, 0.8f);
            closeX.fontSize = Mathf.Max(closeX.fontSize, 40f);
        }

        if (input != null)
        {
            var bg = input.targetGraphic as Image;
            if (bg != null)
                bg.color = warmInputBg;

            var inputOutline = input.GetComponent<Outline>();
            if (inputOutline == null) inputOutline = input.gameObject.AddComponent<Outline>();
            inputOutline.effectColor = warmOutline;
            inputOutline.effectDistance = new Vector2(1, -1);

            if (input.textComponent != null)
            {
                input.textComponent.color = warmInputText;
                input.textComponent.fontSize = 34;
                input.textComponent.fontStyle = FontStyles.Bold;
            }

            var ph = input.placeholder as TMP_Text;
            if (ph != null)
            {
                ph.text = "Enter class code";
                ph.color = warmPlaceholder;
                ph.fontSize = 30;
                ph.fontStyle = FontStyles.Italic;
            }
        }

        if (joinBtn != null)
        {
            var joinImg = joinBtn.targetGraphic as Image;
            if (joinImg != null)
            {
                joinImg.sprite = null;
                joinImg.color = new Color(0.02f, 0.02f, 0.04f, 0.82f);
            }

            joinBtn.transition = Selectable.Transition.None;

            var btnOutline = joinBtn.GetComponent<Outline>();
            if (btnOutline == null) btnOutline = joinBtn.gameObject.AddComponent<Outline>();
            btnOutline.effectColor = warmOutline;
            btnOutline.effectDistance = new Vector2(1.5f, -1.5f);

            if (joinBtn.GetComponent<ButtonHoverEffect>() == null)
                joinBtn.gameObject.AddComponent<ButtonHoverEffect>();

            var label = joinBtn.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = "Join";
                label.color = warmGold;
                label.fontSize = 40;
                label.fontStyle = FontStyles.Bold;
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