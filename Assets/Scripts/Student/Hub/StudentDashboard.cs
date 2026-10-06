using System;
using System.Globalization;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ImagineQuest.Student
{
    /// <summary>Scene-owned presenter. Disabling it invalidates any pending asynchronous response.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class StudentDashboard : MonoBehaviour
    {
        [SerializeField] private Sprite[] avatarSprites;
        [SerializeField] private string[] avatarIds;
        [SerializeField] private TMP_FontAsset font;

        private IStudentDashboardRepository repository;
        private FirebaseAuth auth;
        private RectTransform panel;
        private RectTransform powersOverlay;
        private Button powersButton;
        private TMP_Text nameText, avatarFallback, levelText, crystalsText, goldText, heartsText, statusText;
        private Image avatarImage;
        private Button refreshButton;
        private int requestVersion;
        private bool loading;

        private void Awake()
        {
            BuildUi();
        }

        private void OnEnable() { Refresh(); }

        private void OnDisable()
        {
            requestVersion++;
            loading = false;
            if (powersOverlay != null) powersOverlay.gameObject.SetActive(false);
            if (auth != null) auth.StateChanged -= OnAuthChanged;
        }

        private void OnDestroy()
        {
            if (panel != null) Destroy(panel.gameObject);
            if (powersOverlay != null) Destroy(powersOverlay.gameObject);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused && isActiveAndEnabled && !loading) Refresh();
        }

        private void OnAuthChanged(object sender, EventArgs args) { Refresh(); }

        public async void Refresh()
        {
            if (!isActiveAndEnabled || panel == null) return;
            int version = ++requestVersion;
            loading = true;
            ShowUnavailable("Loading your dashboard...");
            refreshButton.interactable = false;
            try
            {
                var dependencies = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (!IsCurrent(version)) return;
                if (dependencies != DependencyStatus.Available)
                    throw new InvalidOperationException("Firebase dependencies unavailable.");

                if (auth == null) auth = FirebaseAuth.DefaultInstance;
                auth.StateChanged -= OnAuthChanged;
                auth.StateChanged += OnAuthChanged;
                if (repository == null) repository = new StudentDashboardRepository(FirebaseFirestore.DefaultInstance);

                var user = auth.CurrentUser;
                if (user == null)
                {
                    ShowUnavailable("Sign in to see your dashboard.");
                    return;
                }

                string uid = user.UserId;
                var snapshot = await repository.LoadAsync(uid);
                if (!IsCurrent(version) || auth.CurrentUser?.UserId != uid) return;
                Render(snapshot);
            }
            catch (Exception exception)
            {
                if (!IsCurrent(version)) return;
                ShowUnavailable("Could not load your status. Select Refresh to try again.");
                Debug.LogWarning("[StudentDashboard] Status load failed: " + exception.Message);
            }
            finally
            {
                if (IsCurrent(version))
                {
                    loading = false;
                    refreshButton.interactable = true;
                }
            }
        }

        private bool IsCurrent(int version)
        {
            return this != null && isActiveAndEnabled && version == requestVersion;
        }

        private void ShowUnavailable(string message)
        {
            nameText.text = "Student dashboard";
            levelText.text = crystalsText.text = goldText.text = heartsText.text = "—";
            avatarImage.sprite = null;
            avatarImage.enabled = false;
            avatarFallback.text = "?";
            avatarFallback.gameObject.SetActive(true);
            statusText.text = message;
        }

        private void Render(StudentDashboardSnapshot snapshot)
        {
            nameText.text = snapshot.DisplayName;
            levelText.text = "Level " + Format(snapshot.Economy.Level) + "  ·  " + Format(snapshot.Economy.Xp) + " XP";
            crystalsText.text = Format(snapshot.Economy.Crystals);
            goldText.text = Format(snapshot.Economy.Gold);
            heartsText.text = Format(snapshot.Standing.Hearts);
            statusText.text = "Your progress, rewards and classroom standing";

            Sprite sprite = null;
            if (avatarIds != null && avatarSprites != null)
                for (int i = 0; i < Math.Min(avatarIds.Length, avatarSprites.Length); i++)
                    if (avatarIds[i] == snapshot.AvatarId) { sprite = avatarSprites[i]; break; }
            avatarImage.sprite = sprite;
            avatarImage.enabled = sprite != null;
            avatarImage.color = ColorUtility.TryParseHtmlString("#" + snapshot.CharacterColor.TrimStart('#'), out Color tint)
                ? tint : Color.white;
            avatarFallback.gameObject.SetActive(sprite == null);
            avatarFallback.text = StringInfo.GetNextTextElement(snapshot.DisplayName).ToUpperInvariant();
        }

        private static string Format(long value) { return value.ToString("N0", CultureInfo.CurrentCulture); }

        private void BuildUi()
        {
            if (font == null)
            {
                var loader = GetComponent<StudentClassLoader>();
                if (loader != null) font = loader.cardFont;
            }
            panel = Rect("StudentDashboardStatus", transform, new Vector2(0.1f, 0.79f), new Vector2(0.9f, 0.98f));
            var background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.06f, 0.055f, 0.09f, 0.94f);
            background.raycastTarget = false;

            var avatar = Rect("Avatar", panel, new Vector2(0.015f, 0.53f), new Vector2(0.075f, 0.96f));
            avatarImage = avatar.gameObject.AddComponent<Image>();
            avatarImage.preserveAspect = true;
            avatarImage.raycastTarget = false;
            avatarFallback = Text("AvatarFallback", avatar, Vector2.zero, Vector2.one, 36);
            avatarFallback.alignment = TextAlignmentOptions.Center;
            nameText = Text("StudentName", panel, new Vector2(0.09f, 0.70f), new Vector2(0.76f, 0.96f), 32);
            nameText.fontStyle = FontStyles.Bold;
            nameText.richText = false;
            statusText = Text("Status", panel, new Vector2(0.09f, 0.52f), new Vector2(0.65f, 0.70f), 18);
            statusText.color = new Color(0.88f, 0.84f, 0.74f);

            levelText = Stat("Level / XP", "Economy · Progress", 0.015f, 0.375f, new Color(0.84f, 0.73f, 1f));
            crystalsText = Stat("Crystals", "Economy · Power points", 0.385f, 0.58f, new Color(0.55f, 0.88f, 1f));
            goldText = Stat("Gold", "Economy · Coins", 0.59f, 0.775f, new Color(1f, 0.83f, 0.42f));
            heartsText = Stat("Hearts", "Teacher-controlled standing", 0.79f, 0.985f, new Color(1f, 0.65f, 0.68f));

            var refresh = Rect("RefreshStatus", panel, new Vector2(0.85f, 0.62f), new Vector2(0.985f, 0.90f));
            var refreshImage = refresh.gameObject.AddComponent<Image>();
            refreshImage.color = new Color(0.27f, 0.23f, 0.17f);
            refreshButton = refresh.gameObject.AddComponent<Button>();
            refreshButton.targetGraphic = refreshImage;
            refreshButton.onClick.AddListener(Refresh);
            var refreshLabel = Text("Label", refresh, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.95f), 22);
            refreshLabel.text = "Refresh";
            refreshLabel.alignment = TextAlignmentOptions.Center;
            BuildPowersPreview();

            // Keep the existing class list and its generated heading below the status area.
            var content = transform.Find("ContentPanel") as RectTransform;
            if (content != null)
            {
                content.anchorMax = new Vector2(content.anchorMax.x, 0.68f);
                content.offsetMin = content.offsetMax = Vector2.zero;
            }
            // The status panel must not draw over modal dialogs or the scene fade.
            var bottomBar = transform.Find("BottomBar");
            if (bottomBar != null) panel.SetSiblingIndex(bottomBar.GetSiblingIndex());
            if (bottomBar != null) powersOverlay.SetSiblingIndex(bottomBar.GetSiblingIndex());
        }

        // UI-only examples: never infer ownership, costs or eligibility from the wallet.
        // Keep this separate from Render/Refresh so preview data cannot affect student data.
        private void BuildPowersPreview()
        {
            powersButton = PreviewButton("OpenPowers", panel,
                new Vector2(0.67f, 0.62f), new Vector2(0.82f, 0.90f), "Powers");
            powersOverlay = Rect("PowersPreviewOverlay", transform, Vector2.zero, Vector2.one);
            var shade = powersOverlay.gameObject.AddComponent<Image>();
            shade.color = new Color(0f, 0f, 0f, 0.78f);
            shade.raycastTarget = true;
            var window = Rect("PowersWindow", powersOverlay,
                new Vector2(0.12f, 0.16f), new Vector2(0.88f, 0.88f));
            var background = window.gameObject.AddComponent<Image>();
            background.color = new Color(0.06f, 0.055f, 0.09f, 1f);

            var title = Text("Title", window, new Vector2(0.04f, 0.88f), new Vector2(0.74f, 0.97f), 32);
            title.text = "Powers · Preview";
            title.fontStyle = FontStyles.Bold;
            var close = PreviewButton("ClosePowers", window,
                new Vector2(0.79f, 0.88f), new Vector2(0.96f, 0.97f), "Close");
            close.onClick.AddListener(() =>
            {
                powersOverlay.gameObject.SetActive(false);
                powersButton.Select();
            });
            var intro = Text("Explanation", window, new Vector2(0.04f, 0.75f), new Vector2(0.96f, 0.87f), 19);
            intro.enableWordWrapping = true;
            intro.text = "Level up to unlock classroom privileges. Crystals will be needed to activate them. These examples are not your actual powers.";

            PowerPreviewCard(window, "Lunch Power", "Leave class five minutes early for lunch, as permitted by your teacher.",
                "Available · example only", "Crystal cost: TBD", 0.53f);
            PowerPreviewCard(window, "Evaluation Power", "Ask your teacher for a clue on one evaluation question.",
                "Locked · example only", "Crystal cost: TBD", 0.31f);
            PowerPreviewCard(window, "Report Card Power", "Your teacher removes your lowest semester mark. Once per school year.",
                "Locked · example only", "Crystal cost: TBD", 0.09f);
            var footer = Text("PreviewNotice", window, new Vector2(0.04f, 0.015f), new Vector2(0.96f, 0.075f), 16);
            footer.text = "Preview only · Unlock levels and costs await configuration. No Crystals will be spent.";
            footer.enableWordWrapping = true;

            powersButton.onClick.AddListener(() =>
            {
                powersOverlay.gameObject.SetActive(true);
                close.Select();
            });
            powersOverlay.gameObject.SetActive(false);
        }

        private void PowerPreviewCard(Transform parent, string title, string description,
            string state, string cost, float bottom)
        {
            var card = Rect(title, parent, new Vector2(0.04f, bottom), new Vector2(0.96f, bottom + 0.20f));
            card.gameObject.AddComponent<Image>().color = new Color(0.13f, 0.11f, 0.19f, 1f);
            var heading = Text("Name", card, new Vector2(0.025f, 0.65f), new Vector2(0.66f, 0.97f), 24);
            heading.text = title;
            heading.fontStyle = FontStyles.Bold;
            var detail = Text("Description", card, new Vector2(0.025f, 0.28f), new Vector2(0.66f, 0.65f), 18);
            detail.text = description;
            detail.enableWordWrapping = true;
            var status = Text("State", card, new Vector2(0.025f, 0.03f), new Vector2(0.66f, 0.28f), 16);
            status.text = state;
            status.color = new Color(0.84f, 0.73f, 1f);
            var price = Text("Cost", card, new Vector2(0.70f, 0.62f), new Vector2(0.975f, 0.94f), 18);
            price.text = cost;
            price.alignment = TextAlignmentOptions.Center;
            var use = PreviewButton("UsePower", card,
                new Vector2(0.70f, 0.12f), new Vector2(0.975f, 0.53f), "Use Power (soon)");
            use.interactable = false;
            // Deliberately no click handler or backend dependency.
        }

        private Button PreviewButton(string name, Transform parent, Vector2 min, Vector2 max, string caption)
        {
            var rect = Rect(name, parent, min, max);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.27f, 0.23f, 0.17f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var label = Text("Label", rect, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), 20);
            label.text = caption;
            label.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private TMP_Text Stat(string label, string caption, float left, float right, Color accent)
        {
            var card = Rect(label, panel, new Vector2(left, 0.05f), new Vector2(right, 0.47f));
            var image = card.gameObject.AddComponent<Image>();
            image.color = new Color(accent.r * 0.15f, accent.g * 0.15f, accent.b * 0.15f, 1f);
            image.raycastTarget = false;
            var heading = Text("Label", card, new Vector2(0.04f, 0.69f), new Vector2(0.96f, 0.97f), 18);
            heading.text = label;
            heading.color = accent;
            var value = Text("Value", card, new Vector2(0.04f, 0.27f), new Vector2(0.96f, 0.72f), 28);
            value.fontStyle = FontStyles.Bold;
            var hint = Text("Caption", card, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.28f), 14);
            hint.text = caption;
            hint.color = accent;
            return value;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private TMP_Text Text(string name, Transform parent, Vector2 min, Vector2 max, float size)
        {
            var text = Rect(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = text.fontSizeMax = size;
            text.fontSizeMin = Mathf.Min(12f, size);
            text.enableAutoSizing = true;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.color = new Color(1f, 0.97f, 0.88f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            return text;
        }
    }
}
