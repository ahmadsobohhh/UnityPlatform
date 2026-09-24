using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ImagineQuest.Gameplay
{
    /// <summary>
    /// First playable quest vertical slice. It builds a lightweight ship-deck exploration
    /// space so the integration stays runnable without depending on a legacy prototype scene.
    /// Art can be swapped for the migrated pirate environment without changing quest logic.
    /// </summary>
    public sealed class PirateQuestBootstrap : MonoBehaviour
    {
        [Header("Optional migrated art")]
        [SerializeField] private GameObject pirateNpcPrefab;

        private readonly List<PirateQuestChallenge> challenges = new List<PirateQuestChallenge>();
        private TMP_Text objectiveText;
        private TMP_Text xpText;
        private TMP_Text interactionText;
        private GameObject questionPanel;
        private TMP_Text questionText;
        private readonly List<Button> answerButtons = new List<Button>();
        private PirateQuestChallenge activeChallenge;
        private int earnedXp;
        private bool finished;

        private void Awake()
        {
            EnsureServices();
            BuildWorld();
            BuildInterface();
        }

        private void Start()
        {
            var session = QuestSession.Instance;
            var video = FindFirstObjectByType<UrlVideoSequencePlayer>();
            if (video != null && session != null)
                video.Play(session.IntroVideoUrl, UpdateHud);
            else
                UpdateHud();
        }

        private void EnsureServices()
        {
            if (FindFirstObjectByType<QuestProgressRepository>() == null)
                new GameObject("QuestProgressRepository").AddComponent<QuestProgressRepository>();
        }

        private void BuildWorld()
        {
            RenderSettings.ambientLight = new Color(0.38f, 0.43f, 0.55f);
            CreateLight();
            CreateOcean();
            CreateDeck();
            CreatePlayer();
            CreateCaptain();

            CreateChallenge(new Vector3(-7f, 1f, 8f), "Captain's Compass", "A ship sails 4 leagues east, then 3 leagues west. How many leagues east is it from its starting point?", new[] { "1", "7", "12" }, 0);
            CreateChallenge(new Vector3(0f, 1f, 13f), "Navigation Chart", "The crew has 24 gold coins and shares them equally among 6 sailors. How many coins does each sailor receive?", new[] { "3", "4", "6" }, 1);
            CreateChallenge(new Vector3(7f, 1f, 8f), "Treasure Chest", "A map is split into 8 equal pieces. The crew finds 3 pieces. What fraction of the map remains?", new[] { "3/8", "5/8", "8/5" }, 1);
        }

        private void CreateLight()
        {
            var sun = new GameObject("Sun");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            sun.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        }

        private void CreateOcean()
        {
            var ocean = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ocean.name = "Ocean";
            ocean.transform.localScale = Vector3.one * 20f;
            ocean.transform.position = new Vector3(0f, -0.1f, 0f);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = new Color(0.03f, 0.26f, 0.43f);
            ocean.GetComponent<Renderer>().material = material;
        }

        private void CreateDeck()
        {
            CreateBlock("Ship Deck", new Vector3(0f, 0f, 7f), new Vector3(20f, 0.5f, 20f), new Color(0.28f, 0.12f, 0.045f));
            CreateBlock("Port Rail", new Vector3(-9.5f, 1f, 7f), new Vector3(0.4f, 2f, 20f), new Color(0.16f, 0.07f, 0.02f));
            CreateBlock("Starboard Rail", new Vector3(9.5f, 1f, 7f), new Vector3(0.4f, 2f, 20f), new Color(0.16f, 0.07f, 0.02f));
            CreateBlock("Stern Rail", new Vector3(0f, 1f, -2.5f), new Vector3(20f, 2f, 0.4f), new Color(0.16f, 0.07f, 0.02f));
            CreateBlock("Mast", new Vector3(0f, 5f, 6f), new Vector3(0.8f, 10f, 0.8f), new Color(0.2f, 0.09f, 0.03f));
        }

        private void CreatePlayer()
        {
            var player = new GameObject("Student Adventurer");
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1.2f, 1f);
            player.AddComponent<CharacterController>();

            var cameraObject = new GameObject("Player Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform);
            cameraObject.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            player.AddComponent<PirateQuestPlayerController>();
        }

        private void CreateCaptain()
        {
            if (pirateNpcPrefab != null)
            {
                var captainModel = Instantiate(pirateNpcPrefab, new Vector3(0f, 0.25f, 4f), Quaternion.Euler(0f, 180f, 0f));
                captainModel.name = "Captain";
                return;
            }

            var captainPlaceholder = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            captainPlaceholder.name = "Captain Placeholder";
            captainPlaceholder.transform.position = new Vector3(0f, 1.2f, 4f);
            captainPlaceholder.GetComponent<Renderer>().material.color = new Color(0.38f, 0.08f, 0.05f);
        }

        private void CreateChallenge(Vector3 position, string title, string question, string[] answers, int correctIndex)
        {
            var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = title;
            pedestal.transform.position = position;
            pedestal.transform.localScale = new Vector3(1.3f, 1f, 1.3f);
            pedestal.GetComponent<Renderer>().material.color = new Color(0.86f, 0.62f, 0.12f);
            pedestal.GetComponent<Collider>().isTrigger = true;

            var challenge = pedestal.AddComponent<PirateQuestChallenge>();
            challenge.Configure(this, title, question, answers, correctIndex, 25);
            challenges.Add(challenge);
        }

        private static void CreateBlock(string label, Vector3 position, Vector3 scale, Color color)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = label;
            block.transform.position = position;
            block.transform.localScale = scale;
            block.GetComponent<Renderer>().material.color = color;
        }

        private void BuildInterface()
        {
            var canvasObject = new GameObject("Pirate Quest UI");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasObject.AddComponent<GraphicRaycaster>();

            objectiveText = CreateText(canvasObject.transform, "Objective", new Vector2(28f, -28f), new Vector2(850f, 70f), 30, TextAlignmentOptions.TopLeft);
            xpText = CreateText(canvasObject.transform, "XP", new Vector2(-28f, -28f), new Vector2(300f, 70f), 30, TextAlignmentOptions.TopRight, new Vector2(1f, 1f));
            interactionText = CreateText(canvasObject.transform, "Interaction", new Vector2(0f, 130f), new Vector2(800f, 70f), 28, TextAlignmentOptions.Center, new Vector2(0.5f, 0f));
            interactionText.gameObject.SetActive(false);

            questionPanel = CreatePanel(canvasObject.transform, "Question Panel", new Vector2(0.5f, 0.5f), new Vector2(980f, 620f));
            questionText = CreateText(questionPanel.transform, "Question", new Vector2(0f, -42f), new Vector2(860f, 190f), 34, TextAlignmentOptions.TopLeft, new Vector2(0.5f, 1f));
            questionText.textWrappingMode = TextWrappingModes.Normal;

            for (int i = 0; i < 3; i++)
            {
                var button = CreateButton(questionPanel.transform, "Answer " + i, new Vector2(0f, -250f - i * 105f), new Vector2(780f, 80f));
                int answerIndex = i;
                button.onClick.AddListener(() => AnswerSelected(answerIndex));
                answerButtons.Add(button);
            }
            questionPanel.SetActive(false);
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 size)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            panel.AddComponent<Image>().color = new Color(0.04f, 0.025f, 0.01f, 0.94f);
            var outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.95f, 0.72f, 0.25f, 0.8f);
            outline.effectDistance = new Vector2(3f, -3f);
            return panel;
        }

        private static TMP_Text CreateText(Transform parent, string name, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment, Vector2? anchor = null)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var rect = textObject.AddComponent<RectTransform>();
            var selectedAnchor = anchor ?? new Vector2(0f, 1f);
            rect.anchorMin = selectedAnchor;
            rect.anchorMax = selectedAnchor;
            rect.pivot = selectedAnchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(1f, 0.94f, 0.77f);
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var buttonObject = CreatePanel(parent, name, new Vector2(0.5f, 1f), size);
            buttonObject.GetComponent<RectTransform>().anchoredPosition = position;
            var button = buttonObject.AddComponent<Button>();
            var text = CreateText(buttonObject.transform, "Label", Vector2.zero, size, 28, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f));
            text.fontStyle = FontStyles.Bold;
            return button;
        }

        public void ShowInteraction(PirateQuestChallenge challenge)
        {
            if (finished || questionPanel.activeSelf)
                return;
            interactionText.text = "Press E to inspect " + challenge.Title;
            interactionText.gameObject.SetActive(true);
        }

        public void HideInteraction(PirateQuestChallenge challenge)
        {
            if (activeChallenge != challenge)
                interactionText.gameObject.SetActive(false);
        }

        public void OpenChallenge(PirateQuestChallenge challenge)
        {
            if (finished || challenge.IsComplete)
                return;
            activeChallenge = challenge;
            interactionText.gameObject.SetActive(false);
            questionText.text = challenge.Title + "\n\n" + challenge.Question;
            for (int i = 0; i < answerButtons.Count; i++)
            {
                answerButtons[i].GetComponentInChildren<TMP_Text>().text = challenge.Answers[i];
                answerButtons[i].interactable = true;
            }
            questionPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void AnswerSelected(int answerIndex)
        {
            if (activeChallenge == null)
                return;
            if (answerIndex != activeChallenge.CorrectIndex)
            {
                questionText.text = activeChallenge.Title + "\n\nNot quite. Read the question and try again.";
                return;
            }

            earnedXp += activeChallenge.XpReward;
            activeChallenge.Complete();
            activeChallenge = null;
            questionPanel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            UpdateHud();

            var completeCount = challenges.FindAll(challenge => challenge.IsComplete).Count;
            QuestProgressRepository.Instance.SaveProgress(GetQuestId(), GetClassId(), completeCount, earnedXp, completeCount == challenges.Count);
            if (completeCount == challenges.Count)
                FinishQuest();
        }

        private void FinishQuest()
        {
            finished = true;
            objectiveText.text = "Quest complete! The Captain has charted your next adventure.";
            interactionText.text = "Press Enter to return to your Student Hub.";
            interactionText.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (finished && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                QuestSession.Instance?.Clear();
                SceneTransition.LoadScene("StudentHub");
            }
        }

        private void UpdateHud()
        {
            int completeCount = challenges.FindAll(challenge => challenge.IsComplete).Count;
            objectiveText.text = "The Pirate Voyage: solve the captain's three navigation challenges (" + completeCount + "/" + challenges.Count + ")";
            xpText.text = "Quest XP: " + earnedXp;
        }

        private static string GetQuestId() => QuestSession.Instance == null ? "pirate-voyage" : QuestSession.Instance.QuestId;
        private static string GetClassId() => QuestSession.Instance == null ? string.Empty : QuestSession.Instance.ClassId;
    }

    public sealed class PirateQuestChallenge : MonoBehaviour
    {
        private PirateQuestBootstrap quest;
        public string Title { get; private set; }
        public string Question { get; private set; }
        public string[] Answers { get; private set; }
        public int CorrectIndex { get; private set; }
        public int XpReward { get; private set; }
        public bool IsComplete { get; private set; }

        public void Configure(PirateQuestBootstrap owner, string title, string question, string[] answers, int correctIndex, int xpReward)
        {
            quest = owner;
            Title = title;
            Question = question;
            Answers = answers;
            CorrectIndex = correctIndex;
            XpReward = xpReward;
        }

        public void Interact() => quest.OpenChallenge(this);

        public void Complete()
        {
            IsComplete = true;
            GetComponent<Renderer>().material.color = new Color(0.15f, 0.7f, 0.3f);
        }

        private void OnTriggerStay(Collider other)
        {
            if (!IsComplete && other.CompareTag("Player"))
                quest.ShowInteraction(this);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
                quest.HideInteraction(this);
        }
    }

    public sealed class PirateQuestPlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float lookSensitivity = 0.12f;
        private CharacterController characterController;
        private Transform playerCamera;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerCamera = GetComponentInChildren<Camera>().transform;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (Keyboard.current == null || Mouse.current == null || Cursor.lockState != CursorLockMode.Locked)
                return;

            Vector2 move = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) move.y += 1;
            if (Keyboard.current.sKey.isPressed) move.y -= 1;
            if (Keyboard.current.dKey.isPressed) move.x += 1;
            if (Keyboard.current.aKey.isPressed) move.x -= 1;
            characterController.Move((transform.forward * move.y + transform.right * move.x).normalized * moveSpeed * Time.deltaTime + Physics.gravity * Time.deltaTime);

            Vector2 look = Mouse.current.delta.ReadValue() * lookSensitivity;
            transform.Rotate(0f, look.x, 0f);
            playerCamera.localRotation = Quaternion.Euler(Mathf.Clamp(playerCamera.localEulerAngles.x - look.y, -75f, 75f), 0f, 0f);

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                var nearest = FindNearestChallenge();
                if (nearest != null)
                    nearest.Interact();
            }
        }

        private PirateQuestChallenge FindNearestChallenge()
        {
            var all = FindObjectsByType<PirateQuestChallenge>(FindObjectsSortMode.None);
            PirateQuestChallenge nearest = null;
            float nearestDistance = 3f;
            foreach (var challenge in all)
            {
                if (challenge.IsComplete)
                    continue;
                float distance = Vector3.Distance(transform.position, challenge.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = challenge;
                }
            }
            return nearest;
        }
    }
}
