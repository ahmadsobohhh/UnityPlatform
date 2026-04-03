using System.Collections;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private CanvasGroup mainMenuGroup;
    [SerializeField] private CanvasGroup loginGroup;
    [SerializeField] private CanvasGroup registerGroup;
    [SerializeField] private float transitionDuration = 0.4f;

    private CanvasGroup currentPanel;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ShowImmediate(mainMenuGroup);
        HideImmediate(loginGroup);
        HideImmediate(registerGroup);
        currentPanel = mainMenuGroup;
    }

    public void ShowMainMenu() => TransitionTo(mainMenuGroup);
    public void ShowLogin() => TransitionTo(loginGroup);
    public void ShowRegister() => TransitionTo(registerGroup);

    private void TransitionTo(CanvasGroup target)
    {
        if (target == currentPanel) return;
        StartCoroutine(CrossFade(currentPanel, target));
    }

    private IEnumerator CrossFade(CanvasGroup from, CanvasGroup to)
    {
        if (from != null)
        {
            float t = 0f;
            while (t < transitionDuration)
            {
                t += Time.unscaledDeltaTime;
                from.alpha = Mathf.Lerp(1f, 0f, t / transitionDuration);
                yield return null;
            }
            HideImmediate(from);
        }

        if (to != null)
        {
            to.gameObject.SetActive(true);
            to.alpha = 0f;
            to.interactable = true;
            to.blocksRaycasts = true;

            float t = 0f;
            while (t < transitionDuration)
            {
                t += Time.unscaledDeltaTime;
                to.alpha = Mathf.Lerp(0f, 1f, t / transitionDuration);
                yield return null;
            }
            to.alpha = 1f;
        }

        currentPanel = to;
    }

    private void ShowImmediate(CanvasGroup group)
    {
        if (group == null) return;
        group.gameObject.SetActive(true);
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    private void HideImmediate(CanvasGroup group)
    {
        if (group == null) return;
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        group.gameObject.SetActive(false);
    }
}
