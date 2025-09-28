
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum ButtonStyle { Primary, Ghost }

[ExecuteAlways]
public class StyleButton : MonoBehaviour
{
    public UiTheme theme;
    public ButtonStyle style = ButtonStyle.Primary;

    void OnEnable() => Apply();
    void OnValidate() => Apply();

    void Apply()
    {
        if (!theme) return;
        var img = GetComponent<Image>();
        var btn = GetComponent<Button>();
        var le = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
        var txt = GetComponentInChildren<TMP_Text>();

        le.minHeight = theme.buttonMinHeight; le.preferredWidth = Mathf.RoundToInt(theme.formWidth * 0.72f); // ~320

        if (style == ButtonStyle.Primary)
        {
            if (img) { img.color = theme.primary; if (theme.roundedSprite) { img.sprite = theme.roundedSprite; img.type = Image.Type.Sliced; } }
            if (btn)
            {
                var cb = btn.colors;
                cb.normalColor = theme.primary;
                cb.highlightedColor = theme.primaryHighlighted;
                cb.pressedColor = theme.primaryPressed;
                cb.disabledColor = new Color(1, 1, 1, 0.35f);
                btn.colors = cb;
            }
            if (txt) txt.color = Color.white;
        }
        else
        { // Ghost
            if (img) { img.color = new Color(1, 1, 1, 0); if (theme.roundedSprite) { img.sprite = theme.roundedSprite; img.type = Image.Type.Sliced; } }
            var outline = GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();
            outline.effectColor = theme.ghostOutline; outline.effectDistance = new Vector2(1, -1);
            if (txt) txt.color = theme.ghostText;
        }
    }
}
