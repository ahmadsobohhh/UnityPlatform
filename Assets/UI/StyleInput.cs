
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class StyleInput : MonoBehaviour
{
    public UiTheme theme;
    void OnEnable() => Apply();
    void OnValidate() => Apply();

    void Apply()
    {
        if (!theme) return;
        var img = GetComponent<Image>();
        if (img) { img.color = theme.inputBg; if (theme.roundedSprite) { img.sprite = theme.roundedSprite; img.type = Image.Type.Sliced; } }
        var le = GetComponent<LayoutElement>();
        if (!le) le = gameObject.AddComponent<LayoutElement>();
        le.minHeight = theme.inputMinHeight; le.preferredWidth = theme.formWidth;

        var outline = GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();
        outline.effectColor = theme.inputOutline; outline.effectDistance = new Vector2(1, -1);

        var tmp = GetComponentInChildren<TMP_Text>();
        if (tmp) tmp.color = theme.inputText;
        var placeholder = transform.Find("Text Area/Placeholder")?.GetComponent<TMP_Text>();
        if (placeholder) placeholder.color = theme.placeholder;
        var caret = GetComponentInChildren<TMP_InputField>();
        if (caret) caret.caretColor = Color.white;
    }
}
