
using UnityEngine;

[CreateAssetMenu(menuName = "UI/Theme", fileName = "UiTheme")]
public class UiTheme : ScriptableObject
{
    [Header("Colors")]
    public Color background = new Color32(23, 54, 81, 255);         // #173651
    public Color titleText = new Color32(230, 240, 255, 255);       // #E6F0FF
    public Color inputBg = new Color32(15, 31, 51, 255);          // #0F1F33
    public Color inputOutline = new Color32(31, 59, 89, 255);       // #1F3B59
    public Color inputText = new Color32(241, 245, 249, 255);       // #F1F5F9
    public Color placeholder = new Color32(148, 163, 184, 255);     // #94A3B8

    public Color primary = new Color32(37, 99, 235, 255);         // #2563EB
    public Color primaryHighlighted = new Color32(30, 84, 204, 255);// #1E54CC
    public Color primaryPressed = new Color32(39, 91, 179, 255);// #275BB3

    public Color ghostText = new Color32(220, 227, 240, 255);       // #DCE3F0
    public Color ghostOutline = new Color32(59, 130, 246, 255);     // #3B82F6

    [Header("Layout")]
    public int formWidth = 440;
    public int inputMinHeight = 56;
    public int buttonMinHeight = 44;

    [Header("Sprites")]
    public Sprite roundedSprite; // optional 9-slice rounded sprite
}
