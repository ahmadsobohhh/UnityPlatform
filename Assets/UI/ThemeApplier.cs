
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class ThemeApplier : MonoBehaviour
{
    public UiTheme theme;
    public Image background;     // assign your Background Image
    public TMP_Text title;       // assign "User Login" text
    public RectTransform form;   // the "Form" container (with VerticalLayoutGroup)

    void OnEnable() => Apply();
    void OnValidate() => Apply();

    public void Apply()
    {
        if (!theme) return;
        if (background) background.color = theme.background;
        if (title) { title.color = theme.titleText; title.fontSize = 40; title.alignment = TextAlignmentOptions.Center; }

        // auto-apply to children that have style components
        foreach (var s in GetComponentsInChildren<StyleInput>(true)) { s.theme = theme; s.OnValidate(); }
        foreach (var s in GetComponentsInChildren<StyleButton>(true)) { s.theme = theme; s.OnValidate(); }

        // form width hint for inputs
        if (form)
        {
            var le = form.GetComponent<LayoutElement>() ?? form.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = theme.formWidth;
        }
    }
}
