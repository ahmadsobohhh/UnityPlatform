using System;
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
    /// Adds the learner-facing quest card to a student class lobby.  A learner can only
    /// launch a quest after a teacher has unlocked it for the currently selected class.
    /// </summary>
    public sealed class StudentQuestLobby : MonoBehaviour
    {
        // Keep the old assignment document ID while the Firebase Functions migration
        // moves this card to classes/{classId}/assignments.
        private const string AssignmentId = "pirate-voyage";
        private const string AssignmentCollection = "questAssignments";

        private Button playButton;
        private TMP_Text buttonLabel;
        private TMP_Text statusLabel;
        private QuestLauncher launcher;
        private bool usesExistingHubButton;

        private void Start()
        {
            launcher = GetComponent<QuestLauncher>() ?? gameObject.AddComponent<QuestLauncher>();
            BuildUi();
            StartCoroutine(RefreshRoutine());
        }

        private void BuildUi()
        {
            // The student hub already has a styled pirate button. Reuse it, and use its
            // disabled state as the access gate rather than creating a duplicate route.
            var existing = GameObject.Find("StartPirateQuestButton");
            if (existing != null && SceneManager.GetActiveScene().name == "StudentHub")
            {
                usesExistingHubButton = true;
                playButton = existing.GetComponent<Button>();
                buttonLabel = existing.GetComponentInChildren<TMP_Text>(true);
                statusLabel = QuestLobbyUi.CreateText("PirateQuestHubStatus", transform, new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(700f, 34f), 18f);
                statusLabel.alignment = TextAlignmentOptions.Center;
                return;
            }

            var panel = QuestLobbyUi.CreatePanel("PirateQuestLobbyCard", transform, new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-34f, 42f), new Vector2(460f, 142f));
            statusLabel = QuestLobbyUi.CreateText("Status", panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -20f), new Vector2(410f, 55f), 20f);
            statusLabel.alignment = TextAlignmentOptions.Center;
            statusLabel.textWrappingMode = TextWrappingModes.Normal;
            playButton = QuestLobbyUi.CreateButton("PlayPirateQuestButton", panel.transform, "BEGIN CELESTIAL CLOCK",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(360f, 48f));
            buttonLabel = playButton.GetComponentInChildren<TMP_Text>();
            playButton.onClick.AddListener(() => launcher.LaunchPirateQuest());
        }

        private IEnumerator RefreshRoutine()
        {
            // ClassroomScene can be reached directly from character selection, which
            // supplies ClassSelection but does not necessarily write PlayerPrefs.
            var classId = ClassSelection.CurrentClassId;
            if (string.IsNullOrWhiteSpace(classId))
                classId = PlayerPrefs.GetString("SelectedClassId", string.Empty);
            if (string.IsNullOrWhiteSpace(classId))
            {
                SetLocked("Choose a class to begin a teacher-assigned quest.");
                yield break;
            }

            SetLocked("Checking your class quest...");
            var task = FirebaseFirestore.DefaultInstance.Collection("classes").Document(classId)
                .Collection(AssignmentCollection).Document(AssignmentId).GetSnapshotAsync();
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted || task.IsCanceled)
            {
                SetLocked("Quest status could not be loaded. Please try again.");
                yield break;
            }

            var assignment = task.Result;
            var unlocked = assignment.Exists && assignment.ContainsField("isUnlocked") && assignment.GetValue<bool>("isUnlocked");
            if (!unlocked)
            {
                SetLocked("The Celestial Clock mission is locked by your teacher.");
                yield break;
            }

            if (statusLabel != null)
                statusLabel.text = "The Riddle of the Celestial Clock is ready for your crew.";
            if (buttonLabel != null)
                buttonLabel.text = "BEGIN CELESTIAL CLOCK";
            if (playButton != null)
                playButton.interactable = true;
        }

        private void SetLocked(string message)
        {
            if (statusLabel != null)
                statusLabel.text = message;
            if (buttonLabel != null && usesExistingHubButton)
                buttonLabel.text = "PIRATE QUEST LOCKED";
            if (playButton != null)
                playButton.interactable = false;
        }
    }

    /// <summary>
    /// Adds a compact teacher control to the class lobby.  It writes one class-scoped
    /// assignment document which student lobbies use as their launch gate.
    /// </summary>
    public sealed class TeacherQuestLobby : MonoBehaviour
    {
        private const string AssignmentId = "pirate-voyage";
        private const string QuestDefinitionId = "celestial-clock";
        private TMP_Text statusLabel;
        private Button toggleButton;
        private TMP_Text toggleLabel;
        private bool unlocked;
        private string classId;

        private void Start()
        {
            BuildUi();
            StartCoroutine(RefreshRoutine());
        }

        private void BuildUi()
        {
            var panel = QuestLobbyUi.CreatePanel("TeacherQuestControl", transform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-34f, 42f), new Vector2(470f, 150f));
            statusLabel = QuestLobbyUi.CreateText("Status", panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -20f), new Vector2(425f, 62f), 19f);
            statusLabel.alignment = TextAlignmentOptions.Center;
            statusLabel.textWrappingMode = TextWrappingModes.Normal;
            toggleButton = QuestLobbyUi.CreateButton("TogglePirateQuestButton", panel.transform, "LOADING QUEST...",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(370f, 48f));
            toggleLabel = toggleButton.GetComponentInChildren<TMP_Text>();
            toggleButton.onClick.AddListener(() => StartCoroutine(ToggleRoutine()));
        }

        private IEnumerator RefreshRoutine()
        {
            classId = ClassSelection.CurrentClassId;
            if (string.IsNullOrWhiteSpace(classId))
                classId = PlayerPrefs.GetString("SelectedClassId", string.Empty);
            if (string.IsNullOrWhiteSpace(classId))
            {
                SetUnavailable("Select a class before managing its quests.");
                yield break;
            }

            var task = FirebaseFirestore.DefaultInstance.Collection("classes").Document(classId)
                .Collection("questAssignments").Document(AssignmentId).GetSnapshotAsync();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted || task.IsCanceled)
            {
                SetUnavailable("Quest status could not be loaded.");
                yield break;
            }

            unlocked = task.Result.Exists && task.Result.ContainsField("isUnlocked") && task.Result.GetValue<bool>("isUnlocked");
            Render();
        }

        private IEnumerator ToggleRoutine()
        {
            if (string.IsNullOrWhiteSpace(classId))
                yield break;

            toggleButton.interactable = false;
            var user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user == null)
            {
                SetUnavailable("Sign in as the class teacher to manage quests.");
                yield break;
            }

            // This is a useful client-side guard. Production Firestore rules must also
            // enforce ownerUid == request.auth.uid; UI checks are not authorization.
            var ownerTask = FirebaseFirestore.DefaultInstance.Collection("classes").Document(classId).GetSnapshotAsync();
            yield return new WaitUntil(() => ownerTask.IsCompleted);
            if (ownerTask.IsFaulted || ownerTask.IsCanceled || !ownerTask.Result.Exists ||
                !ownerTask.Result.ContainsField("ownerUid") ||
                ownerTask.Result.GetValue<string>("ownerUid") != user.UserId)
            {
                SetUnavailable("Only this class's teacher can manage its quests.");
                yield break;
            }

            var nextState = !unlocked;
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
            if (task.IsFaulted || task.IsCanceled)
            {
                SetUnavailable("Could not update the quest. Please try again.");
                yield break;
            }

            unlocked = nextState;
            Render();
        }

        private void Render()
        {
            statusLabel.text = unlocked
                ? "The Celestial Clock mission is unlocked for this class."
                : "The Celestial Clock mission is currently locked for students.";
            toggleLabel.text = unlocked ? "LOCK CELESTIAL CLOCK" : "UNLOCK CELESTIAL CLOCK";
            toggleButton.interactable = true;
        }

        private void SetUnavailable(string message)
        {
            statusLabel.text = message;
            toggleLabel.text = "QUEST UNAVAILABLE";
            toggleButton.interactable = false;
        }
    }

    internal static class QuestLobbyUi
    {
        private static Sprite solidSprite;

        internal static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 position, Vector2 size)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = panel.GetComponent<Image>();
            image.sprite = GetSolidSprite();
            image.color = new Color(0.035f, 0.08f, 0.16f, 0.92f);
            return panel;
        }

        internal static TMP_Text CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 position, Vector2 size, float fontSize)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.color = new Color(1f, 0.9f, 0.57f, 1f);
            text.raycastTarget = false;
            return text;
        }

        internal static Button CreateButton(string name, Transform parent, string label, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = buttonObject.GetComponent<Image>();
            image.sprite = GetSolidSprite();
            image.color = new Color(0.14f, 0.42f, 0.64f, 1f);
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.2f, 0.55f, 0.8f, 1f);
            colors.pressedColor = new Color(0.08f, 0.27f, 0.45f, 1f);
            button.colors = colors;
            var text = CreateText("Label", buttonObject.transform, Vector2.zero, Vector2.one, Vector2.zero,
                Vector2.zero, 20f);
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            return button;
        }

        private static Sprite GetSolidSprite()
        {
            if (solidSprite != null)
                return solidSprite;
            solidSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            return solidSprite;
        }
    }

}
