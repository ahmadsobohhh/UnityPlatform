using UnityEngine;
using UnityEngine.UI;

public class SettingsToggle : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;
    private CanvasGroup panelGroup;
    private bool isOpen;
    private float targetAlpha;

    private void Awake()
    {
        if (settingsPanel != null)
        {
            panelGroup = settingsPanel.GetComponent<CanvasGroup>();
            if (panelGroup == null)
                panelGroup = settingsPanel.AddComponent<CanvasGroup>();

            panelGroup.alpha = 0f;
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
            settingsPanel.SetActive(true);
        }
    }

    public void Toggle()
    {
        isOpen = !isOpen;
        targetAlpha = isOpen ? 1f : 0f;

        if (panelGroup != null)
        {
            panelGroup.interactable = isOpen;
            panelGroup.blocksRaycasts = isOpen;
        }
    }

    public void Close()
    {
        isOpen = false;
        targetAlpha = 0f;

        if (panelGroup != null)
        {
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        if (panelGroup == null) return;
        panelGroup.alpha = Mathf.Lerp(panelGroup.alpha, targetAlpha, Time.deltaTime * 10f);
    }
}
