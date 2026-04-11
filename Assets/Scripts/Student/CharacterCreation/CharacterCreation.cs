// Script: CharacterCreation
// Path: Assets/Scripts/Student/CharacterCreation/CharacterCreation.cs
// Purpose: Drives student character creation selections and persistence.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;

public class CharacterCreation : MonoBehaviour
{
    [System.Serializable]
    public class CharacterOption
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite sprite;
    }

    [Header("Character Data")]
    [SerializeField] private CharacterOption[] characters;

    [Header("UI References")]
    [SerializeField] private Image characterPreview;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button leftArrow;
    [SerializeField] private Button rightArrow;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Image[] colorSwatches;
    [SerializeField] private Image selectionRing;
    [SerializeField] private CanvasGroup uiGroup;

    private int currentIndex;
    private int currentColorIndex;
    private bool transitioning;

    private static readonly Color[] TintColors =
    {
        new Color(1f, 1f, 1f, 1f),
        new Color(1f, 0.9f, 0.75f, 1f),
        new Color(0.75f, 0.9f, 1f, 1f),
        new Color(1f, 0.78f, 0.78f, 1f),
        new Color(0.78f, 1f, 0.82f, 1f),
        new Color(0.92f, 0.78f, 1f, 1f),
    };

    public static readonly Color[] SwatchDisplayColors =
    {
        Color.white,
        new Color(0.95f, 0.75f, 0.3f, 1f),
        new Color(0.3f, 0.7f, 1f, 1f),
        new Color(0.9f, 0.25f, 0.25f, 1f),
        new Color(0.25f, 0.85f, 0.4f, 1f),
        new Color(0.7f, 0.3f, 0.95f, 1f),
    };

    private void Start()
    {
        currentIndex = 0;
        currentColorIndex = 0;

        if (leftArrow != null) leftArrow.onClick.AddListener(PreviousCharacter);
        if (rightArrow != null) rightArrow.onClick.AddListener(NextCharacter);
        if (confirmButton != null) confirmButton.onClick.AddListener(ConfirmSelection);

        for (int i = 0; i < colorSwatches.Length && i < TintColors.Length; i++)
        {
            int idx = i;
            var btn = colorSwatches[i].GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => SelectColor(idx));
        }

        UpdateDisplay();
        UpdateSelectionRing(currentColorIndex);
        StartCoroutine(EntranceAnimation());
    }

    private IEnumerator EntranceAnimation()
    {
        if (uiGroup == null) yield break;

        uiGroup.alpha = 0f;
        if (characterPreview != null)
            characterPreview.transform.localScale = Vector3.zero;

        yield return new WaitForSeconds(0.3f);

        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / 0.6f);
            float ease = 1f - Mathf.Pow(1f - p, 3f);
            uiGroup.alpha = ease;
            if (characterPreview != null)
                characterPreview.transform.localScale = Vector3.one * ease;
            yield return null;
        }

        uiGroup.alpha = 1f;
        if (characterPreview != null)
            characterPreview.transform.localScale = Vector3.one;
    }

    public void PreviousCharacter()
    {
        if (!transitioning && characters.Length > 0)
            StartCoroutine(SwitchCharacter(-1));
    }

    public void NextCharacter()
    {
        if (!transitioning && characters.Length > 0)
            StartCoroutine(SwitchCharacter(1));
    }

    private IEnumerator SwitchCharacter(int direction)
    {
        transitioning = true;
        var preview = characterPreview.transform;

        float t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(1f, 0.7f, t / 0.12f);
            preview.localScale = new Vector3(s, s, 1f);
            characterPreview.color = new Color(
                characterPreview.color.r,
                characterPreview.color.g,
                characterPreview.color.b,
                Mathf.Lerp(1f, 0f, t / 0.12f));
            yield return null;
        }

        currentIndex = (currentIndex + direction + characters.Length) % characters.Length;
        UpdateDisplay();

        t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            float p = t / 0.2f;
            float overshoot = 1f + 0.08f * Mathf.Sin(p * Mathf.PI);
            preview.localScale = new Vector3(
                Mathf.Lerp(0.7f, 1f, p) * overshoot,
                Mathf.Lerp(0.7f, 1f, p) * overshoot, 1f);
            Color c = characterPreview.color;
            characterPreview.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0f, 1f, p));
            yield return null;
        }

        preview.localScale = Vector3.one;
        Color final_ = TintColors[currentColorIndex];
        characterPreview.color = final_;
        transitioning = false;
    }

    public void SelectColor(int idx)
    {
        if (idx < 0 || idx >= TintColors.Length) return;
        currentColorIndex = idx;
        characterPreview.color = TintColors[idx];
        UpdateSelectionRing(idx);
    }

    private void UpdateSelectionRing(int idx)
    {
        if (selectionRing == null || idx >= colorSwatches.Length) return;
        selectionRing.transform.position = colorSwatches[idx].transform.position;
    }

    private void UpdateDisplay()
    {
        if (characters == null || characters.Length == 0) return;

        var c = characters[currentIndex];
        if (characterPreview != null)
        {
            characterPreview.sprite = c.sprite;
            characterPreview.color = TintColors[currentColorIndex];
            characterPreview.preserveAspect = true;
        }
        if (nameText != null) nameText.text = c.displayName;
        if (descriptionText != null) descriptionText.text = c.description;
    }

    public async void ConfirmSelection()
    {
        if (characters == null || characters.Length == 0) return;

        confirmButton.interactable = false;
        var selected = characters[currentIndex];

        try
        {
            var auth = FirebaseAuth.DefaultInstance;
            var db = FirebaseFirestore.DefaultInstance;
            var user = auth.CurrentUser;

            if (user == null) { Debug.LogError("No signed-in user."); return; }

            var updates = new Dictionary<string, object>
            {
                { "avatarId", selected.id },
                { "characterClass", selected.displayName },
                { "characterColor", ColorUtility.ToHtmlStringRGB(TintColors[currentColorIndex]) },
                { "avatarChosen", true },
                { "createdAt", Timestamp.GetCurrentTimestamp() }
            };

            await db.Collection("users").Document(user.UserId).UpdateAsync(updates);
            Debug.Log($"Character created: {selected.displayName}");

            SceneTransition.LoadScene("StudentHub");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to save character: " + e);
            confirmButton.interactable = true;
        }
    }
}


