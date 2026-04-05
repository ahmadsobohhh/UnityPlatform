using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Drives row background color on hover/press while keeping Button for click.</summary>
public class StudentClassRowButtonColors : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    Image _img;
    Color _normal, _hover, _pressed;
    bool _over;
    bool _held;

    public void Bind(Image img, Color normal, Color hover, Color pressed)
    {
        _img = img;
        _normal = normal;
        _hover = hover;
        _pressed = pressed;
        Apply();
    }

    void OnDisable()
    {
        _held = false;
        _over = false;
        if (_img != null)
            _img.color = _normal;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _over = true;
        Apply();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _over = false;
        Apply();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;
        _held = true;
        Apply();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _held = false;
        Apply();
    }

    void Apply()
    {
        if (_img == null) return;
        if (_held)
            _img.color = _pressed;
        else if (_over)
            _img.color = _hover;
        else
            _img.color = _normal;
    }
}
