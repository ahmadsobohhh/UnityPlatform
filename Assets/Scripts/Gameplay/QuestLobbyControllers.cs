using System.Collections;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ImagineQuest.Gameplay
{
    /// <summary>
    /// Student-facing assignment gate. The hub reuses its existing quest button, while
    /// the classroom receives one contained quest card instead of a floating overlay.
    /// </summary>
    public sealed class StudentQuestLobby : MonoBehaviour
    {
        private const string AssignmentId = "pirate-voyage";
        private const string AssignmentCollection = "questAssignments";

        private Button playButton;
        private TMP_Text buttonLabel;
        private TMP_Text statusLabel;
        private QuestLauncher launcher;
        private GameObject runtimeCard;
        private bool usesExistingHubButton;
        private string requestUserId;

        private void Start()
        {
            launcher = GetComponent<QuestLauncher>() ?? gameObject.AddComponent<QuestLauncher>();
            BuildUi();
            StartCoroutine(RefreshRoutine());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        private void OnDestroy()
        {
            if (runtimeCard != null)
                Destroy(runtimeCard);
        }

        private void BuildUi()
        {
            var existing = GameObject.Find("StartPirateQuestButton");
            if (existing != null && SceneManager.GetActiveScene().name == "StudentHub")
            {
                usesExistingHubButton = true;
                playButton = existing.GetComponent<Button>();
                buttonLabel = existing.GetComponentInChildren<TMP_Text>(true);
                return;
            }

            RectTransform contentPanel = QuestLobbyUi.FindSceneRect("ContentPanel");
            Transform parent = contentPanel != null ? contentPanel : transform;
            runtimeCard = QuestLobbyUi.CreatePanel("PirateQuestLobbyCard", parent,
                new Vector2(0.66f, 0.08f), new Vector2(0.97f, 0.78f));

            var eyebrow = QuestLobbyUi.CreateText("Eyebrow", runtimeCard.transform,
                new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.94f), 17f);
            eyebrow.text = "ASSIGNED QUEST";
            eyebrow.fontStyle = FontStyles.Bold;
            eyebrow.alignment = TextAlignmentOptions.MidlineLeft;
            eyebrow.color = QuestLobbyUi.MutedGold;

            var title = QuestLobbyUi.CreateText("Title", runtimeCard.transform,
                new Vector2(0.08f, 0.61f), new Vector2(0.92f, 0.82f), 27f);
            title.text = "The Celestial Clock";
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.MidlineLeft;
            title.color = QuestLobbyUi.Cream;

            statusLabel = QuestLobbyUi.CreateText("Status", runtimeCard.transform,
                new Vector2(0.08f, 0.29f), new Vector2(0.92f, 0.58f), 19f);
            statusLabel.alignment = TextAlignmentOptions.TopLeft;
            statusLabel.textWrappingMode = TextWrappingModes.Normal;
            statusLabel.color = new Color(0.90f, 0.84f, 0.72f, 0.94f);

            playButton = QuestLobbyUi.CreateButton("PlayPirateQuestButton", runtimeCard.transform,
                "BEGIN QUEST", new Vector2(0.08f, 0.07f), new Vector2(0.92f, 0.23f));
            buttonLabel = playButton.GetComponentInChildren<TMP_Text>();
            playButton.onClick.AddListener(() => launcher.LaunchPirateQuest());
        }

        private IEnumerator RefreshRoutine()
        {
            var user = FirebaseAuth.DefaultInstance.CurrentUser;
            requestUserId = user != null ? user.UserId : string.Empty;
            if (string.IsNullOrWhiteSpace(requestUserId))
            {
                SetLocked("Sign in to view your assigned quest.", "SIGN IN TO PLAY");
                yield break;
            }

            string classId = ClassSelection.CurrentClassId;
            if (string.IsNullOrWhiteSpace(classId))
                classId = PlayerPrefs.GetString("SelectedClassId", string.Empty);
            if (string.IsNullOrWhiteSpace(classId))
            {
                SetLocked("Choose a class to see its assigned quest.", "SELECT A CLASS");
                yield break;
            }

            SetLocked("Checking your teacher's quest assignment...", "CHECKING QUEST");
            var task = FirebaseFirestore.DefaultInstance.Collection("classes").Document(classId)
                .Collection(AssignmentCollection).Document(AssignmentId).GetSnapshotAsync();
            yield return new WaitUntil(() => task.IsCompleted);

            if (!SessionStillMatches())
                yield break;

            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("[StudentQuestLobby] Could not read class quest assignment: " + task.Exception);
                SetLocked("This class has no available quest assignment right now.", "QUEST UNAVAILABLE");
                yield break;
            }

            var assignment = task.Result;
            bool unlocked = assignment.Exists && assignment.ContainsField("isUnlocked") &&
                            assignment.GetValue<bool>("isUnlocked");
            if (!unlocked)
            {
                SetLocked("Waiting for your teacher to unlock this mission.", "QUEST LOCKED");
                yield break;
            }

            if (statusLabel != null)
                statusLabel.text = "Ready for launch. Your teacher has opened this mission for the class.";
            if (buttonLabel != null)
                buttonLabel.text = "BEGIN CELESTIAL CLOCK";
            if (playButton != null)
                playButton.interactable = true;
        }

        private bool SessionStillMatches()
        {
            var current = FirebaseAuth.DefaultInstance.CurrentUser;
            return current != null && current.UserId == requestUserId && isActiveAndEnabled;
        }

        private void SetLocked(string message, string buttonText)
        {
            if (statusLabel != null)
                statusLabel.text = message;
            if (buttonLabel != null)
                buttonLabel.text = buttonText;
            if (playButton != null)
                playButton.interactable = false;

            // The hub deliberately has no extra status label. Its existing button is
            // the single, unobtrusive source of quest state.
            if (usesExistingHubButton && playButton != null)
                playButton.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Teacher assignment control integrated into the class command deck.
    /// </summary>
    public sealed class TeacherQuestLobby : MonoBehaviour
    {
        private const string AssignmentId = "pirate-voyage";
        private const string QuestDefinitionId = "celestial-clock";

        private TMP_Text statusLabel;
        private Button toggleButton;
        private TMP_Text toggleLabel;
        private GameObject runtimeCard;
        private bool unlocked;
        private string classId;
        private string requestUserId;

        private void Start()
        {
            BuildUi();
            StartCoroutine(RefreshRoutine());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        private void OnDestroy()
        {
            if (runtimeCard != null)
                Destroy(runtimeCard);
        }

        private void BuildUi()
        {
            RectTransform classPanel = QuestLobbyUi.FindSceneRect("StudentListPanel");
            Transform parent = classPanel != null ? classPanel : transform;
            runtimeCard = QuestLobbyUi.CreatePanel("TeacherQuestControl", parent,
                new Vector2(0.65f, 0.13f), new Vector2(0.96f, 0.73f));

            var eyebrow = QuestLobbyUi.CreateText("Eyebrow", runtimeCard.transform,
                new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.94f), 17f);
            eyebrow.text = "CLASS QUEST";
            eyebrow.fontStyle = FontStyles.Bold;
            eyebrow.alignment = TextAlignmentOptions.MidlineLeft;
            eyebrow.color = QuestLobbyUi.MutedGold;

            var title = QuestLobbyUi.CreateText("Title", runtimeCard.transform,
                new Vector2(0.08f, 0.61f), new Vector2(0.92f, 0.82f), 27f);
            title.text = "The Celestial Clock";
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.MidlineLeft;
            title.color = QuestLobbyUi.Cream;

            statusLabel = QuestLobbyUi.CreateText("Status", runtimeCard.transform,
                new Vector2(0.08f, 0.29f), new Vector2(0.92f, 0.58f), 18f);
            statusLabel.alignment = TextAlignmentOptions.TopLeft;
            statusLabel.textWrappingMode = TextWrappingModes.Normal;
            statusLabel.color = new Color(0.90f, 0.84f, 0.72f, 0.94f);

            toggleButton = QuestLobbyUi.CreateButton("TogglePirateQuestButton", runtimeCard.transform,
                "LOADING QUEST", new Vector2(0.08f, 0.07f), new Vector2(0.92f, 0.23f));
            toggleLabel = toggleButton.GetComponentInChildren<TMP_Text>();
            toggleButton.onClick.AddListener(() => StartCoroutine(ToggleRoutine()));
        }

        private IEnumerator RefreshRoutine()
        {
            var user = FirebaseAuth.DefaultInstance.CurrentUser;
            requestUserId = user != null ? user.UserId : string.Empty;
            if (string.IsNullOrWhiteSpace(requestUserId))
            {
                SetUnavailable("Sign in as the class teacher to manage quests.");
                yield break;
            }

            classId = ClassSelection.CurrentClassId;
            if (string.IsNullOrWhiteSpace(classId))
                classId = PlayerPrefs.GetString("SelectedClassId", string.Empty);
            if (string.IsNullOrWhiteSpace(classId))
            {
                SetUnavailable("Select a class before managing its quest.");
                yield break;
            }

            statusLabel.text = "Checking the current assignment...";
            toggleButton.interactable = false;
            var task = FirebaseFirestore.DefaultInstance.Collection("classes").Document(classId)
                .Collection("questAssignments").Document(AssignmentId).GetSnapshotAsync();
            yield return new WaitUntil(() => task.IsCompleted);

            if (!SessionStillMatches())
                yield break;

            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("[TeacherQuestLobby] Could not read class quest assignment: " + task.Exception);
                SetUnavailable("Quest permissions need to be published to Firebase.");
                yield break;
            }

            unlocked = task.Result.Exists && task.Result.ContainsField("isUnlocked") &&
                       task.Result.GetValue<bool>("isUnlocked");
            Render();
        }

        private IEnumerator ToggleRoutine()
        {
            if (string.IsNullOrWhiteSpace(classId))
                yield break;

            toggleButton.interactable = false;
            var user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user == null || user.UserId != requestUserId)
            {
                SetUnavailable("Sign in as the class teacher to manage quests.");
                yield break;
            }

            var ownerTask = FirebaseFirestore.DefaultInstance.Collection("classes").Document(classId)
                .GetSnapshotAsync();
            yield return new WaitUntil(() => ownerTask.IsCompleted);
            if (!SessionStillMatches())
                yield break;

            if (ownerTask.IsFaulted || ownerTask.IsCanceled || !ownerTask.Result.Exists ||
                !ownerTask.Result.ContainsField("ownerUid") ||
                ownerTask.Result.GetValue<string>("ownerUid") != user.UserId)
            {
                Debug.LogError("[TeacherQuestLobby] The current account could not verify ownership of " + classId + ".");
                SetUnavailable("Only this class's teacher can manage its quest.");
                yield break;
            }

            bool nextState = !unlocked;
            var payload = new Dictionary<string, object>
            {
                { "questId", QuestDefinitionId },
                { "title", "The Riddle of the Celestial Clock" },
                { "questDefinitionId", QuestDefinitionId },
                { "questVersion", "1.0.0" },
                { "assignmentId", AssignmentId },
                { "state", nextState ? "open" : "draft" },
                { "isUnlocked", nextState },
                { "updatedAt", Timestamp.GetCurrentTimestamp() },
                { "updatedBy", user.UserId }
            };
            var task = FirebaseFirestore.DefaultInstance.Collection("classes").Document(classId)
                .Collection("questAssignments").Document(AssignmentId).SetAsync(payload, SetOptions.MergeAll);
            yield return new WaitUntil(() => task.IsCompleted);

            if (!SessionStillMatches())
                yield break;

            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("[TeacherQuestLobby] Could not update class quest assignment: " + task.Exception);
                SetUnavailable("The quest could not be updated. Check Firebase permissions.");
                yield break;
            }

            unlocked = nextState;
            Render();
        }

        private bool SessionStillMatches()
        {
            var current = FirebaseAuth.DefaultInstance.CurrentUser;
            return current != null && current.UserId == requestUserId && isActiveAndEnabled;
        }

        private void Render()
        {
            if (statusLabel != null)
            {
                statusLabel.text = unlocked
                    ? "Open for students. They can launch it from their class page."
                    : "Locked. Students can see the mission but cannot launch it yet.";
            }
            if (toggleLabel != null)
                toggleLabel.text = unlocked ? "LOCK QUEST" : "UNLOCK FOR CLASS";
            if (toggleButton != null)
                toggleButton.interactable = true;
        }

        private void SetUnavailable(string message)
        {
            if (statusLabel != null)
                statusLabel.text = message;
            if (toggleLabel != null)
                toggleLabel.text = "QUEST SETUP REQUIRED";
            if (toggleButton != null)
                toggleButton.interactable = false;
        }
    }

    internal static class QuestLobbyUi
    {
        private static Sprite solidSprite;

        internal static readonly Color Cream = new Color(1f, 0.95f, 0.80f, 1f);
        internal static readonly Color MutedGold = new Color(0.88f, 0.70f, 0.38f, 1f);

        internal static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            var image = panel.GetComponent<Image>();
            image.sprite = GetSolidSprite();
            image.color = new Color(0.075f, 0.055f, 0.035f, 0.96f);

            var outline = panel.GetComponent<Outline>();
            outline.effectColor = new Color(0.78f, 0.60f, 0.28f, 0.65f);
            outline.effectDistance = new Vector2(2f, -2f);
            return panel;
        }

        internal static TMP_Text CreateText(string name, Transform parent, Vector2 anchorMin,
            Vector2 anchorMax, float fontSize)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.color = Cream;
            text.raycastTarget = false;
            return text;
        }

        internal static Button CreateButton(string name, Transform parent, string label,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button),
                typeof(Outline));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            var image = buttonObject.GetComponent<Image>();
            image.sprite = GetSolidSprite();
            image.color = new Color(0.43f, 0.28f, 0.075f, 1f);
            var outline = buttonObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.95f, 0.78f, 0.40f, 0.56f);
            outline.effectDistance = new Vector2(1f, -1f);

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.16f, 1.09f, 0.92f, 1f);
            colors.pressedColor = new Color(0.75f, 0.72f, 0.68f, 1f);
            colors.disabledColor = new Color(0.55f, 0.52f, 0.48f, 0.72f);
            button.colors = colors;

            var text = CreateText("Label", buttonObject.transform, Vector2.zero, Vector2.one, 17f);
            text.text = label;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Cream;
            return button;
        }

        internal static RectTransform FindSceneRect(string objectName)
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
                {
                    if (rect != null && rect.name == objectName)
                        return rect;
                }
            }

            return null;
        }

        private static Sprite GetSolidSprite()
        {
            if (solidSprite != null)
                return solidSprite;
            solidSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f));
            return solidSprite;
        }
    }
}
