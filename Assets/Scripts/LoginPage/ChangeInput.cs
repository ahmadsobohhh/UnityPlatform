// Script: ChangeInput
// Path: Assets/Scripts/LoginPage/ChangeInput.cs
// Purpose: Switches selected input fields and keyboard navigation behavior.

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ChangeInput : MonoBehaviour
{
    [SerializeField] private Button submitButton;

    private TMP_InputField[] fields;
    private bool wasFocused;

    private void OnEnable()
    {
        fields = GetComponentsInChildren<TMP_InputField>(true);

        if (fields.Length > 0)
            fields[0].Select();
    }

    private void Update()
    {
        if (fields == null || fields.Length == 0) return;

        int current = GetCurrentIndex();

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                int prev = current <= 0 ? fields.Length - 1 : current - 1;
                fields[prev].Select();
            }
            else
            {
                int next = current >= fields.Length - 1 ? 0 : current + 1;
                fields[next].Select();
            }
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (submitButton != null)
                submitButton.onClick.Invoke();
        }
    }

    private int GetCurrentIndex()
    {
        var selected = EventSystem.current?.currentSelectedGameObject;
        if (selected == null) return -1;

        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i].gameObject == selected)
                return i;
        }
        return -1;
    }
}


