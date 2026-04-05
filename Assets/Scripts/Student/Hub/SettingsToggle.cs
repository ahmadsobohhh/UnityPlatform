using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

            EnsureEffectsSliderExists();
        }
    }

    private void EnsureEffectsSliderExists()
    {
        if (settingsPanel == null) return;

        if (FindSliderByName("EffectsSlider") != null)
            return;

        var musicSlider = FindSliderByName("MusicSlider");
        if (musicSlider == null || musicSlider.transform.parent == null)
            return;

        var musicRow = musicSlider.transform.parent.gameObject;
        var effectsRow = Instantiate(musicRow, musicRow.transform.parent);
        effectsRow.name = "EffectsSliderRow";
        effectsRow.transform.SetSiblingIndex(musicRow.transform.GetSiblingIndex() + 1);
        PositionEffectsRow(musicRow, effectsRow);

        foreach (var slider in effectsRow.GetComponentsInChildren<Slider>(true))
        {
            slider.gameObject.name = "EffectsSlider";
            slider.minValue = 0f;
            slider.maxValue = 1f;
            float defaultValue = AudioManager.Instance != null ? AudioManager.Instance.EffectsVolume : PlayerPrefs.GetFloat("EffectsVolume", 0.5f);
            slider.SetValueWithoutNotify(defaultValue);
            break;
        }

        foreach (var text in effectsRow.GetComponentsInChildren<TMP_Text>(true))
        {
            if (!string.IsNullOrEmpty(text.text) && text.text.ToLower().Contains("music"))
            {
                text.text = "Effects Volume";
                break;
            }
        }
    }

    private void PositionEffectsRow(GameObject musicRow, GameObject effectsRow)
    {
        if (musicRow == null || effectsRow == null)
            return;

        var parentLayout = musicRow.transform.parent.GetComponent<LayoutGroup>();
        if (parentLayout != null)
            return;

        var musicRt = musicRow.GetComponent<RectTransform>();
        var effectsRt = effectsRow.GetComponent<RectTransform>();
        if (musicRt == null || effectsRt == null)
            return;

        float spacing = 14f;
        float rowHeight = musicRt.sizeDelta.y > 0f ? musicRt.sizeDelta.y : 50f;
        effectsRt.anchoredPosition = new Vector2(musicRt.anchoredPosition.x, musicRt.anchoredPosition.y - rowHeight - spacing);

        var parentRt = musicRow.transform.parent as RectTransform;
        if (parentRt != null)
        {
            float minHeightNeeded = Mathf.Abs(effectsRt.anchoredPosition.y) + rowHeight + 16f;
            if (parentRt.sizeDelta.y < minHeightNeeded)
                parentRt.sizeDelta = new Vector2(parentRt.sizeDelta.x, minHeightNeeded);
        }
    }

    private Slider FindSliderByName(string sliderName)
    {
        foreach (var slider in settingsPanel.GetComponentsInChildren<Slider>(true))
        {
            if (slider.gameObject.name == sliderName)
                return slider;
        }

        return null;
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
