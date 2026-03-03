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

    private void OnEnable() => Apply();
    private void OnValidate() => Apply();

    public void Apply()
    {
        if (!theme) return;

        // Background + title
        if (background) background.color = theme.background;
        if (title)
        {
            title.color = theme.titleText;
            title.fontSize = 40;
            title.alignment = TextAlignmentOptions.Center;
        }

        // Push theme to child stylers
        foreach (var s in GetComponentsInChildren<StyleInput>(true))
        {
            s.theme = theme;
            s.Apply();
        }
        foreach (var s in GetComponentsInChildren<StyleButton>(true))
        {
            s.theme = theme;
            s.Apply();
        }

        // Optional width hint for the form container
        if (form)
        {
            var le = form.GetComponent<LayoutElement>();
            if (!le) le = form.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = theme.formWidth;
        }
    }
}
