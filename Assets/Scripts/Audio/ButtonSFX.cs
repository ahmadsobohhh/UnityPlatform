// Script: ButtonSFX
// Path: Assets/Scripts/Audio/ButtonSFX.cs
// Purpose: Plays hover and click sound feedback for UI buttons.

using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(UnityEngine.UI.Button))]
public class ButtonSFX : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayHover();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayClick();
    }
}


