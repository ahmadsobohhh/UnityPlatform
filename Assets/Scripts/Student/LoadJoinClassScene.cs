using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadJoinClassScene : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject joinPopup;
    [SerializeField] private float popupFadeDuration = 0.18f;
    [SerializeField] private string fallbackSceneName = "StudentJoinClassWCode";

    private CanvasGroup popupGroup;
    private RectTransform popupRect;

    private void Awake()
    {
        if (joinPopup == null)
        {
            var found = GameObject.Find("JoinGUI");
            if (found != null) joinPopup = found;
        }

        if (joinPopup != null)
        {
            popupGroup = joinPopup.GetComponent<CanvasGroup>();
            if (popupGroup == null)
                popupGroup = joinPopup.AddComponent<CanvasGroup>();

            popupRect = joinPopup.GetComponent<RectTransform>();
            SetPopupVisibleImmediate(false);
        }
    }

    public void GoToJoinClass()
    {
        if (joinPopup == null)
        {
            SceneManager.LoadScene(fallbackSceneName);
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

        SceneManager.LoadScene(fallbackSceneName);
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