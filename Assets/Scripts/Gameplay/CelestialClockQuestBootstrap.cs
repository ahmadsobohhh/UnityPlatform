using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using ImagineQuest.QuestFramework;

namespace ImagineQuest.Gameplay
{
    /// <summary>
    /// The first full Imagine Quest learning adventure. The scene deliberately builds its
    /// presentation at runtime so the gameplay is portable while the art team iterates on
    /// a dedicated ship scene. It uses the migrated ship/pirate assets when they are
    /// assigned by the companion editor setup command.
    /// </summary>
    public sealed class CelestialClockQuestBootstrap : MonoBehaviour
    {
        private const string QuestTitle = "THE RIDDLE OF THE CELESTIAL CLOCK";
        private const int RequiredSealsBeforeFinale = 3;

        [Header("Migrated visual assets")]
        [SerializeField] private GameObject colonialShipPrefab;
        [SerializeField] private GameObject pirateCaptainPrefab;
        [SerializeField] private GameObject treasureChestPrefab;
        [SerializeField] private QuestDefinition questDefinition;
        [SerializeField, Range(0.15f, 0.5f)] private float shipScale = 0.28f;

        [Header("Comfort and presentation")]
        [SerializeField, Range(0.05f, 0.3f)] private float lookSensitivity = 0.13f;
        [SerializeField, Range(3f, 8f)] private float moveSpeed = 4.6f;

        private readonly List<CelestialClockStation> stations = new List<CelestialClockStation>();
        private readonly List<Button> answerButtons = new List<Button>();
        private readonly List<string> completedNodeIds = new List<string>();

        private CelestialClockPlayerController playerController;
        private CelestialClockStation activeStation;
        private TMP_Text objectiveText;
        private TMP_Text sealText;
        private TMP_Text xpText;
        private TMP_Text interactionText;
        private TMP_Text journalText;
        private GameObject briefingPanel;
        private TMP_Text briefingSpeakerText;
        private TMP_Text briefingBodyText;
        private TMP_Text briefingStepText;
        private Button briefingPrimaryButton;
        private GameObject lessonPanel;
        private TMP_Text lessonEyebrowText;
        private TMP_Text lessonTitleText;
        private TMP_Text lessonBodyText;
        private TMP_Text lessonFeedbackText;
        private Button lessonPrimaryButton;
        private Button lessonSecondaryButton;
        private GameObject journalPanel;
        private GameObject pausePanel;
        private GameObject completionPanel;
        private TMP_Text completionSummaryText;
        private bool briefingVisible;
        private bool lessonVisible;
        private bool journalVisible;
        private bool pauseVisible;
        private bool questComplete;
        private int briefingIndex;
        private int provisionalXp;

        private readonly DialogueLine[] briefingLines =
        {
            new DialogueLine("CAPTAIN ESME", "The Celestial Clock has fallen silent. Without it, the Aurora cannot find the next learning isle."),
            new DialogueLine("CAPTAIN ESME", "Four glowing seals keep its gears in motion. Each one opens only when a careful thinker gives it the right answer."),
            new DialogueLine("FIRST MATE LUMA", "Every station has a Teaching Note before its practice. Read it, try bravely, and use the journal whenever you need a refresher."),
            new DialogueLine("CAPTAIN ESME", "No rush, navigator. Restore the seals, wake the clock, and chart a course through the stars.")
        };

        private void Awake()
        {
            EnsurePersistence();
            ValidateContentDefinition();
            ConfigureAtmosphere();
            BuildWorld();
            BuildInterface();
        }

        private void Start()
        {
            ShowBriefing(0);
        }

        private void Update()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (lessonVisible)
                {
                    CloseLesson();
                    return;
                }

                if (journalVisible)
                {
                    CloseJournal();
                    return;
                }

                if (!briefingVisible && !completionPanel.activeSelf)
                    TogglePause();
            }

            if (!briefingVisible && !lessonVisible && !journalVisible && !pauseVisible && !questComplete &&
                Keyboard.current.jKey.wasPressedThisFrame)
                OpenJournal();
        }

        private void EnsurePersistence()
        {
            if (FindFirstObjectByType<QuestProgressRepository>() == null)
                new GameObject("Quest Progress Repository").AddComponent<QuestProgressRepository>();
        }

        private void ValidateContentDefinition()
        {
            if (questDefinition == null)
            {
                Debug.LogWarning("[CelestialClockQuest] No QuestDefinition assigned. The scene will use its built-in launch content.");
                return;
            }

            if (questDefinition.CountRequiredChallenges() != 4)
            {
                Debug.LogWarning("[CelestialClockQuest] The authored definition should contain four required challenge nodes.");
            }
        }

        private void ConfigureAtmosphere()
        {
            RenderSettings.ambientLight = new Color(0.13f, 0.19f, 0.36f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.018f, 0.055f, 0.14f);
            RenderSettings.fogDensity = 0.012f;
        }

        private void BuildWorld()
        {
            CreateMoonlight();
            CreateOcean();
            CreateStarfield();
            CreateShipAndDeck();
            CreatePlayer();
            CreateCaptain();
            CreateQuestStations();
        }

        private void CreateMoonlight()
        {
            var moonLightObject = new GameObject("Moonlight");
            var moonLight = moonLightObject.AddComponent<Light>();
            moonLight.type = LightType.Directional;
            moonLight.color = new Color(0.55f, 0.7f, 1f);
            moonLight.intensity = 1.2f;
            moonLightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            var moon = CreatePrimitive("Celestial Moon", PrimitiveType.Sphere, new Vector3(-18f, 22f, 37f),
                new Vector3(5f, 5f, 5f), CreateMaterial(new Color(0.76f, 0.88f, 1f), true));
            moon.AddComponent<CelestialPulse>().Configure(0.09f, 0.4f);
            var moonGlow = new GameObject("Moon Glow");
            moonGlow.transform.position = moon.transform.position;
            var glow = moonGlow.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.range = 42f;
            glow.intensity = 5.5f;
            glow.color = new Color(0.28f, 0.48f, 1f);
        }

        private void CreateOcean()
        {
            var oceanMaterial = CreateMaterial(new Color(0.015f, 0.11f, 0.28f));
            var ocean = CreatePrimitive("Astral Ocean", PrimitiveType.Plane, new Vector3(0f, 0.2f, 4f),
                new Vector3(12f, 1f, 12f), oceanMaterial);
            ocean.AddComponent<CelestialOceanMotion>().Configure(0.1f, 0.65f);

            var underglow = CreatePrimitive("Astral Ocean Underglow", PrimitiveType.Plane, new Vector3(0f, 0.05f, 4f),
                new Vector3(12.5f, 1f, 12.5f), CreateMaterial(new Color(0.025f, 0.27f, 0.4f), true));
            underglow.AddComponent<CelestialOceanMotion>().Configure(0.08f, 0.48f);
        }

        private void CreateStarfield()
        {
            var root = new GameObject("Constellation Field");
            var starMaterial = CreateMaterial(new Color(0.72f, 0.9f, 1f), true);
            var random = new System.Random(1207);
            for (var i = 0; i < 68; i++)
            {
                var x = (float)(random.NextDouble() * 80d - 40d);
                var y = (float)(random.NextDouble() * 29d + 5d);
                var z = (float)(random.NextDouble() * 104d - 38d);
                var scale = (float)(random.NextDouble() * 0.12d + 0.045d);
                var star = CreatePrimitive("Star " + (i + 1), PrimitiveType.Sphere, new Vector3(x, y, z),
                    Vector3.one * scale, starMaterial);
                star.transform.SetParent(root.transform, true);
            }

            root.AddComponent<CelestialStarfieldDrift>().Configure(0.35f);
        }

        private void CreateShipAndDeck()
        {
            if (colonialShipPrefab != null)
            {
                var ship = Instantiate(colonialShipPrefab, Vector3.zero, Quaternion.identity);
                ship.name = "Aurora — Migrated Colonial Ship";
                ship.transform.localScale = Vector3.one * shipScale;

                // The imported environment has artist-authored colliders for a much larger
                // scene. A clear invisible deck below gives this focused quest predictable
                // movement while preserving all of the detailed ship visuals.
                foreach (var collider in ship.GetComponentsInChildren<Collider>(true))
                    collider.enabled = false;
            }
            else
            {
                CreateFallbackShip();
            }

            CreateDeckBounds();
            CreateDeckDressing();
        }

        private void CreateFallbackShip()
        {
            var fallbackDeck = CreatePrimitive("Aurora Deck Placeholder", PrimitiveType.Cube, new Vector3(0f, 0.72f, 2f),
                new Vector3(6f, 0.55f, 20f), CreateMaterial(new Color(0.19f, 0.07f, 0.025f)));
            var mast = CreatePrimitive("Aurora Mast", PrimitiveType.Cylinder, new Vector3(0f, 6f, 2f),
                new Vector3(0.42f, 5.4f, 0.42f), CreateMaterial(new Color(0.13f, 0.045f, 0.015f)));
            mast.transform.localScale = new Vector3(0.42f, 5.4f, 0.42f);
        }

        private void CreateDeckBounds()
        {
            var deck = new GameObject("Invisible Playable Deck");
            var deckCollider = deck.AddComponent<BoxCollider>();
            // The imported ship's main deck is near y=6 before its 0.28 display scale,
            // so the walkable surface lands just under y=1.72 in this focused scene.
            deckCollider.center = new Vector3(0f, 1.58f, 1.9f);
            deckCollider.size = new Vector3(6.15f, 0.28f, 20.8f);

            CreateBarrier("Port Safety Rail", new Vector3(-3.08f, 2.85f, 1.9f), new Vector3(0.22f, 2.2f, 20.8f));
            CreateBarrier("Starboard Safety Rail", new Vector3(3.08f, 2.85f, 1.9f), new Vector3(0.22f, 2.2f, 20.8f));
            CreateBarrier("Stern Safety Rail", new Vector3(0f, 2.85f, -8.45f), new Vector3(6.15f, 2.2f, 0.22f));
            CreateBarrier("Bow Safety Rail", new Vector3(0f, 2.85f, 12.3f), new Vector3(6.15f, 2.2f, 0.22f));
        }

        private void CreateDeckDressing()
        {
            if (treasureChestPrefab != null)
            {
                var chest = Instantiate(treasureChestPrefab, new Vector3(0f, 1.73f, 11.1f), Quaternion.Euler(0f, 180f, 0f));
                chest.name = "Captain's Celestial Chart Chest";
                chest.transform.localScale = Vector3.one * shipScale;
                foreach (var collider in chest.GetComponentsInChildren<Collider>(true))
                    collider.enabled = false;
            }

            CreateLantern(new Vector3(-2.5f, 1.84f, -5.7f));
            CreateLantern(new Vector3(2.5f, 1.84f, -1.1f));
            CreateLantern(new Vector3(-2.5f, 1.84f, 3.2f));
            CreateLantern(new Vector3(2.5f, 1.84f, 7.8f));
        }

        private static void CreateLantern(Vector3 position)
        {
            var post = CreatePrimitive("Aurora Lantern Post", PrimitiveType.Cylinder, position + Vector3.up * 0.62f,
                new Vector3(0.09f, 0.62f, 0.09f), CreateMaterial(new Color(0.11f, 0.055f, 0.025f)));
            var lantern = CreatePrimitive("Aurora Lantern", PrimitiveType.Sphere, position + Vector3.up * 1.15f,
                Vector3.one * 0.22f, CreateMaterial(new Color(1f, 0.56f, 0.18f), true));
            lantern.AddComponent<CelestialPulse>().Configure(0.12f, 1.8f);
            var lightObject = new GameObject("Lantern Light");
            lightObject.transform.position = lantern.transform.position;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 5f;
            light.intensity = 1.45f;
            light.color = new Color(1f, 0.55f, 0.22f);
        }

        private static void CreateBarrier(string name, Vector3 center, Vector3 size)
        {
            var barrier = new GameObject(name);
            var collider = barrier.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
        }

        private void CreatePlayer()
        {
            var player = new GameObject("Student Navigator");
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1.73f, -5.8f);

            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.78f;
            controller.radius = 0.31f;
            controller.center = new Vector3(0f, 0.89f, 0f);
            controller.stepOffset = 0.25f;

            var cameraObject = new GameObject("Navigator Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.035f, 0.11f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 125f;
            cameraObject.AddComponent<AudioListener>();

            playerController = player.AddComponent<CelestialClockPlayerController>();
            playerController.Configure(this, moveSpeed, lookSensitivity);
            playerController.SetInputEnabled(false);
        }

        private void CreateCaptain()
        {
            var captainPosition = new Vector3(0f, 1.73f, -2.25f);
            if (pirateCaptainPrefab != null)
            {
                var captain = Instantiate(pirateCaptainPrefab, captainPosition, Quaternion.Euler(0f, 180f, 0f));
                captain.name = "Captain Esme";
                captain.transform.localScale = Vector3.one * 0.52f;
                return;
            }

            var fallbackCaptain = CreatePrimitive("Captain Esme", PrimitiveType.Capsule, captainPosition + Vector3.up,
                new Vector3(0.8f, 1.1f, 0.8f), CreateMaterial(new Color(0.42f, 0.08f, 0.09f)));
            fallbackCaptain.AddComponent<CelestialPulse>().Configure(0.05f, 1f);
        }

        private void CreateQuestStations()
        {
            if (TryCreateQuestStationsFromDefinition())
                return;

            // A self-contained fallback keeps the vertical slice playable if an artist
            // opens the scene before its authored QuestDefinition has been assigned.
            CreateBuiltInQuestStations();
        }

        private bool TryCreateQuestStationsFromDefinition()
        {
            if (questDefinition == null)
                return false;

            var layouts = new[]
            {
                new StationLayout("sail-fraction", "I.", new Vector3(-1.55f, 1.73f, -3.55f),
                    new Color(0.16f, 0.72f, 0.86f), false),
                new StationLayout("compass-angle", "II.", new Vector3(1.55f, 1.73f, 0.05f),
                    new Color(0.48f, 0.52f, 1f), false),
                new StationLayout("star-equation", "III.", new Vector3(-1.55f, 1.73f, 4.2f),
                    new Color(0.78f, 0.42f, 0.9f), false),
                new StationLayout("celestial-clock", "IV.", new Vector3(0f, 1.73f, 8.85f),
                    new Color(1f, 0.7f, 0.2f), true)
            };
            var content = new DefinitionStationContent[layouts.Length];

            // Validate every station before creating one. That prevents a partially
            // authored asset from leaving duplicate fallback seals in the scene.
            for (var index = 0; index < layouts.Length; index++)
            {
                if (!TryBuildDefinitionStationContent(layouts[index], out content[index]))
                    return false;
            }

            for (var index = 0; index < content.Length; index++)
            {
                var station = content[index];
                CreateStation(station.NodeId, station.DisplayTitle, station.TeachingNote, station.Question,
                    station.Answers, station.CorrectIndex, station.Position, station.Accent, station.XpAward,
                    station.InitiallyLocked);
            }

            return true;
        }

        private bool TryBuildDefinitionStationContent(StationLayout layout, out DefinitionStationContent content)
        {
            content = default;
            var node = questDefinition.FindNode(layout.NodeId);
            if (node == null || !node.RequiredForCompletion || node.Challenges == null)
                return false;

            QuestChallengeDefinition challenge = null;
            for (var index = 0; index < node.Challenges.Count; index++)
            {
                var candidate = node.Challenges[index];
                if (candidate != null && candidate.RequiredForCompletion &&
                    candidate.ChallengeType == QuestChallengeType.MultipleChoice)
                {
                    challenge = candidate;
                    break;
                }
            }

            if (challenge == null || challenge.Answers == null || challenge.Answers.Count != 3 ||
                string.IsNullOrWhiteSpace(challenge.Prompt))
                return false;

            var answers = new string[challenge.Answers.Count];
            var correctIndex = -1;
            for (var index = 0; index < challenge.Answers.Count; index++)
            {
                var answer = challenge.Answers[index];
                if (answer == null || string.IsNullOrWhiteSpace(answer.AnswerId) || string.IsNullOrWhiteSpace(answer.Label))
                    return false;

                answers[index] = answer.Label;
                if (challenge.IsCorrectAnswer(answer.AnswerId))
                    correctIndex = index;
            }

            if (correctIndex < 0)
                return false;

            var teachingNote = string.IsNullOrWhiteSpace(node.NarrativeText)
                ? challenge.HintText
                : node.NarrativeText;
            if (string.IsNullOrWhiteSpace(teachingNote))
                return false;

            var xpAward = questDefinition.RewardPolicy == null
                ? 30
                : questDefinition.RewardPolicy.ExperiencePerCorrectChallenge;
            content = new DefinitionStationContent(layout.NodeId, layout.Ordinal + " " + node.Title.ToUpperInvariant(),
                teachingNote, challenge.Prompt, answers, correctIndex, xpAward, layout.Position, layout.Accent,
                layout.InitiallyLocked);
            return true;
        }

        private void CreateBuiltInQuestStations()
        {
            CreateStation("sail-fraction", "I. THE SAILMAKER'S SEAL",
                "A fraction names equal parts of a whole. The bottom number tells how many equal parts there are; the top number tells how many are chosen.",
                "Three of four storm-sail panels must be raised. Which fraction names the raised panels?",
                new[] { "1/4", "3/4", "4/3" }, 1, new Vector3(-1.55f, 1.73f, -3.55f),
                new Color(0.16f, 0.72f, 0.86f), 28, false);

            CreateStation("compass-angle", "II. THE COMPASS SEAL",
                "A full turn is 360°. One quarter-turn is 90°, so it carries a ship from north to east when turning clockwise.",
                "The compass points north. The Aurora makes one quarter-turn clockwise. Which direction is it facing?",
                new[] { "West", "East", "South" }, 1, new Vector3(1.55f, 1.73f, 0.05f),
                new Color(0.48f, 0.52f, 1f), 30, false);

            CreateStation("star-equation", "III. THE STARLOCK SEAL",
                "An equation is balanced like a ship's scale. Undo the operation beside the unknown to discover its value.",
                "The starlock reads: x + 7 = 19. What value unlocks x?",
                new[] { "10", "12", "26" }, 1, new Vector3(-1.55f, 1.73f, 4.2f),
                new Color(0.78f, 0.42f, 0.9f), 32, false);

            CreateStation("celestial-clock", "IV. WAKE THE CELESTIAL CLOCK",
                "Clock angles use the same idea as turns. From 12 to 3 is one quarter of a full 360° turn, which is 90°.",
                "The minute hand points to 12 and the hour hand points to 3. What angle lies between the hands?",
                new[] { "45°", "90°", "180°" }, 1, new Vector3(0f, 1.73f, 8.85f),
                new Color(1f, 0.7f, 0.2f), 40, true);
        }

        private void CreateStation(string nodeId, string title, string teachingNote, string question, string[] answers,
            int correctIndex, Vector3 position, Color accent, int xpAward, bool initiallyLocked)
        {
            var root = new GameObject(title);
            root.transform.position = position;
            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.48f;

            var station = root.AddComponent<CelestialClockStation>();
            station.Configure(this, nodeId, title, teachingNote, question, answers, correctIndex, xpAward, accent, initiallyLocked);
            stations.Add(station);

            var accentMaterial = CreateMaterial(accent, true);
            var darkMaterial = CreateMaterial(Color.Lerp(accent, new Color(0.025f, 0.03f, 0.08f), 0.68f));
            var pedestal = CreatePrimitive("Seal Pedestal", PrimitiveType.Cylinder, position + new Vector3(0f, 0.22f, 0f),
                new Vector3(0.82f, 0.22f, 0.82f), darkMaterial);
            var ring = CreatePrimitive("Seal Halo", PrimitiveType.Cylinder, position + new Vector3(0f, 0.48f, 0f),
                new Vector3(0.64f, 0.06f, 0.64f), accentMaterial);
            var crystal = CreatePrimitive("Seal Crystal", PrimitiveType.Sphere, position + new Vector3(0f, 1.05f, 0f),
                new Vector3(0.33f, 0.48f, 0.33f), accentMaterial);
            crystal.AddComponent<CelestialPulse>().Configure(0.2f, 1.6f);
            station.RegisterVisual(pedestal.GetComponent<Renderer>());
            station.RegisterVisual(ring.GetComponent<Renderer>());
            station.RegisterVisual(crystal.GetComponent<Renderer>());
            station.RegisterGlyph(crystal.transform);

            var beacon = new GameObject("Seal Beacon");
            beacon.transform.SetParent(root.transform, false);
            beacon.transform.position = position + new Vector3(0f, 1.2f, 0f);
            var pointLight = beacon.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.range = 5.5f;
            pointLight.intensity = 2.7f;
            pointLight.color = accent;
            station.RegisterBeacon(pointLight);

            CreateStationLabel(root.transform, station, position + new Vector3(0f, 1.62f, 0f));
            if (nodeId == "celestial-clock")
                CreateClockFace(position, accentMaterial, station);

            station.RefreshVisualState();
        }

        private static void CreateStationLabel(Transform parent, CelestialClockStation station, Vector3 position)
        {
            var labelObject = new GameObject("Seal Label");
            labelObject.transform.SetParent(parent, true);
            labelObject.transform.position = position;
            labelObject.transform.localScale = Vector3.one * 0.075f;
            var label = labelObject.AddComponent<TextMeshPro>();
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = 3.8f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(1f, 0.94f, 0.75f);
            label.text = station.ShortLabel;
            station.RegisterLabel(label);
            labelObject.AddComponent<CelestialBillboard>();
        }

        private static void CreateClockFace(Vector3 position, Material accentMaterial, CelestialClockStation station)
        {
            var face = CreatePrimitive("Celestial Clock Face", PrimitiveType.Cylinder, position + new Vector3(0f, 2.35f, 0.15f),
                new Vector3(1.28f, 0.09f, 1.28f), CreateMaterial(new Color(0.08f, 0.06f, 0.17f)));
            face.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            station.RegisterVisual(face.GetComponent<Renderer>());

            for (var i = 0; i < 12; i++)
            {
                var angle = i * Mathf.PI * 2f / 12f;
                var marker = CreatePrimitive("Clock Marker", PrimitiveType.Sphere,
                    position + new Vector3(Mathf.Sin(angle) * 0.95f, 2.35f + Mathf.Cos(angle) * 0.95f, 0.03f),
                    Vector3.one * 0.09f, accentMaterial);
                station.RegisterVisual(marker.GetComponent<Renderer>());
            }
        }

        private void BuildInterface()
        {
            EnsureUiEventSystem();

            var canvasObject = new GameObject("Celestial Clock Interface");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();

            BuildHud(canvasObject.transform);
            BuildBriefing(canvasObject.transform);
            BuildLessonPanel(canvasObject.transform);
            BuildJournalPanel(canvasObject.transform);
            BuildPausePanel(canvasObject.transform);
            BuildCompletionPanel(canvasObject.transform);
        }

        /// <summary>
        /// The quest scene is intentionally self-contained. StudentHub's EventSystem is
        /// destroyed during the scene transition, so this scene must make its own UI input
        /// bridge for the briefing, teaching, journal, and completion buttons.
        /// </summary>
        private static void EnsureUiEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
                return;

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var inputModule = eventSystemObject.GetComponent<InputSystemUIInputModule>();
            // Unity also assigns these defaults on OnEnable for runtime-created modules,
            // but doing it explicitly keeps this portable across Input System versions.
            inputModule.AssignDefaultActions();
        }

        private void BuildHud(Transform parent)
        {
            var topStrip = CreatePanel("Top Story Strip", parent, new Vector2(0.5f, 1f), new Vector2(1640f, 108f),
                new Vector2(0f, -26f), new Color(0.012f, 0.027f, 0.08f, 0.9f), 16);
            var title = CreateText("Quest Title", topStrip.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(38f, -20f), new Vector2(940f, 38f), 29f, TextAlignmentOptions.Left, new Color(1f, 0.82f, 0.36f));
            title.fontStyle = FontStyles.Bold;
            title.text = QuestTitle;
            var subtitle = CreateText("Quest Subtitle", topStrip.transform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(38f, 18f), new Vector2(970f, 28f), 17f, TextAlignmentOptions.Left, new Color(0.75f, 0.86f, 1f));
            subtitle.text = "Aboard the Aurora  •  Read the note, solve the seal, chart the stars";

            sealText = CreateText("Seal Counter", topStrip.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-36f, -20f), new Vector2(310f, 34f), 23f, TextAlignmentOptions.Right, new Color(0.96f, 0.92f, 0.68f));
            xpText = CreateText("Provisional XP", topStrip.transform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-36f, 18f), new Vector2(310f, 28f), 17f, TextAlignmentOptions.Right, new Color(0.6f, 0.86f, 1f));

            var objectiveCard = CreatePanel("Objective Card", parent, new Vector2(0f, 0f), new Vector2(545f, 132f),
                new Vector2(34f, 34f), new Color(0.014f, 0.04f, 0.12f, 0.92f), 12);
            var objectiveHeader = CreateText("Objective Header", objectiveCard.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(22f, -18f), new Vector2(500f, 24f), 15f, TextAlignmentOptions.Left, new Color(0.55f, 0.83f, 1f));
            objectiveHeader.text = "CURRENT COURSE";
            objectiveText = CreateText("Objective", objectiveCard.transform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(22f, 18f), new Vector2(500f, 68f), 21f, TextAlignmentOptions.Left, new Color(1f, 0.94f, 0.8f));
            objectiveText.textWrappingMode = TextWrappingModes.Normal;

            interactionText = CreateText("Interaction Prompt", parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 92f), new Vector2(820f, 42f), 23f, TextAlignmentOptions.Center, new Color(1f, 0.92f, 0.66f));
            interactionText.fontStyle = FontStyles.Bold;
            interactionText.gameObject.SetActive(false);

            var journalButton = CreateButton("Journal Button", parent, "J  JOURNAL", new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-34f, 42f), new Vector2(230f, 50f), new Color(0.12f, 0.3f, 0.52f));
            journalButton.onClick.AddListener(OpenJournal);
            var pauseButton = CreateButton("Pause Button", parent, "MENU", new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-34f, 104f), new Vector2(230f, 42f), new Color(0.08f, 0.14f, 0.28f));
            pauseButton.onClick.AddListener(TogglePause);

            UpdateHud();
        }

        private void BuildBriefing(Transform parent)
        {
            briefingPanel = CreateFullOverlay("Briefing Overlay", parent, new Color(0.005f, 0.012f, 0.04f, 0.88f));
            var card = CreatePanel("Briefing Card", briefingPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(1040f, 570f),
                Vector2.zero, new Color(0.018f, 0.045f, 0.13f, 0.98f), 24);
            var eyebrow = CreateText("Briefing Eyebrow", card.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -52f), new Vector2(860f, 34f), 18f, TextAlignmentOptions.Center, new Color(0.55f, 0.84f, 1f));
            eyebrow.text = "QUEST BRIEFING  •  AURORA'S NIGHT WATCH";
            briefingSpeakerText = CreateText("Speaker", card.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -112f), new Vector2(860f, 42f), 32f, TextAlignmentOptions.Center, new Color(1f, 0.8f, 0.32f));
            briefingSpeakerText.fontStyle = FontStyles.Bold;
            briefingBodyText = CreateText("Briefing Copy", card.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 12f), new Vector2(820f, 205f), 31f, TextAlignmentOptions.Center, new Color(0.95f, 0.97f, 1f));
            briefingBodyText.textWrappingMode = TextWrappingModes.Normal;
            briefingStepText = CreateText("Briefing Step", card.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 132f), new Vector2(350f, 28f), 17f, TextAlignmentOptions.Center, new Color(0.58f, 0.7f, 0.9f));
            briefingPrimaryButton = CreateButton("Briefing Primary", card.transform, "CONTINUE", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 55f), new Vector2(400f, 58f), new Color(0.14f, 0.45f, 0.72f));
            briefingPrimaryButton.onClick.AddListener(AdvanceBriefing);
            var skip = CreateButton("Skip Briefing", card.transform, "SKIP TO DECK", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, -17f), new Vector2(240f, 34f), new Color(0.07f, 0.11f, 0.22f));
            skip.onClick.AddListener(BeginExploration);
        }

        private void BuildLessonPanel(Transform parent)
        {
            lessonPanel = CreateFullOverlay("Lesson Overlay", parent, new Color(0.002f, 0.008f, 0.028f, 0.76f));
            var card = CreatePanel("Lesson Card", lessonPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(1100f, 715f),
                Vector2.zero, new Color(0.02f, 0.05f, 0.14f, 0.99f), 22);
            lessonEyebrowText = CreateText("Lesson Eyebrow", card.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -39f), new Vector2(920f, 28f), 17f, TextAlignmentOptions.Center, new Color(0.48f, 0.8f, 1f));
            lessonTitleText = CreateText("Lesson Title", card.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -87f), new Vector2(960f, 46f), 30f, TextAlignmentOptions.Center, new Color(1f, 0.82f, 0.35f));
            lessonTitleText.fontStyle = FontStyles.Bold;
            lessonBodyText = CreateText("Lesson Body", card.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -155f), new Vector2(890f, 175f), 25f, TextAlignmentOptions.Center, new Color(0.96f, 0.97f, 1f));
            lessonBodyText.textWrappingMode = TextWrappingModes.Normal;
            lessonFeedbackText = CreateText("Lesson Feedback", card.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -324f), new Vector2(880f, 42f), 20f, TextAlignmentOptions.Center, new Color(0.98f, 0.78f, 0.38f));
            lessonFeedbackText.textWrappingMode = TextWrappingModes.Normal;

            for (var i = 0; i < 3; i++)
            {
                var answer = CreateButton("Answer " + (i + 1), card.transform, "", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, -58f - i * 70f), new Vector2(760f, 55f), new Color(0.11f, 0.3f, 0.52f));
                var capturedIndex = i;
                answer.onClick.AddListener(() => AnswerSelected(capturedIndex));
                answerButtons.Add(answer);
            }

            lessonPrimaryButton = CreateButton("Lesson Primary", card.transform, "START PRACTICE", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-215f, 42f), new Vector2(370f, 56f), new Color(0.13f, 0.46f, 0.72f));
            lessonSecondaryButton = CreateButton("Lesson Secondary", card.transform, "RETURN TO DECK", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(215f, 42f), new Vector2(370f, 56f), new Color(0.08f, 0.14f, 0.27f));
            lessonPanel.SetActive(false);
        }

        private void BuildJournalPanel(Transform parent)
        {
            journalPanel = CreateFullOverlay("Navigator Journal", parent, new Color(0.002f, 0.009f, 0.03f, 0.78f));
            var card = CreatePanel("Journal Card", journalPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(900f, 640f),
                Vector2.zero, new Color(0.025f, 0.06f, 0.15f, 0.99f), 20);
            var title = CreateText("Journal Title", card.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -48f), new Vector2(760f, 38f), 30f, TextAlignmentOptions.Center, new Color(1f, 0.82f, 0.34f));
            title.fontStyle = FontStyles.Bold;
            title.text = "NAVIGATOR'S JOURNAL";
            journalText = CreateText("Journal Copy", card.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f), new Vector2(750f, 400f), 22f, TextAlignmentOptions.TopLeft, new Color(0.94f, 0.97f, 1f));
            journalText.textWrappingMode = TextWrappingModes.Normal;
            var close = CreateButton("Close Journal", card.transform, "CLOSE JOURNAL", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 42f), new Vector2(320f, 52f), new Color(0.12f, 0.36f, 0.6f));
            close.onClick.AddListener(CloseJournal);
            journalPanel.SetActive(false);
        }

        private void BuildPausePanel(Transform parent)
        {
            pausePanel = CreateFullOverlay("Pause Overlay", parent, new Color(0.002f, 0.008f, 0.028f, 0.84f));
            var card = CreatePanel("Pause Card", pausePanel.transform, new Vector2(0.5f, 0.5f), new Vector2(620f, 390f),
                Vector2.zero, new Color(0.02f, 0.055f, 0.15f, 0.99f), 20);
            var title = CreateText("Pause Title", card.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -72f), new Vector2(500f, 42f), 32f, TextAlignmentOptions.Center, new Color(1f, 0.82f, 0.34f));
            title.fontStyle = FontStyles.Bold;
            title.text = "PAUSED";
            var note = CreateText("Pause Note", card.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 10f), new Vector2(480f, 82f), 21f, TextAlignmentOptions.Center, new Color(0.9f, 0.95f, 1f));
            note.text = "Your course is safe. This session keeps your progress; class-wide results sync after the Firebase server setup is deployed.";
            note.textWrappingMode = TextWrappingModes.Normal;
            var resume = CreateButton("Resume", card.transform, "RETURN TO DECK", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 98f), new Vector2(360f, 52f), new Color(0.12f, 0.43f, 0.7f));
            resume.onClick.AddListener(TogglePause);
            var leave = CreateButton("Leave Quest", card.transform, "LEAVE QUEST", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 32f), new Vector2(260f, 38f), new Color(0.16f, 0.08f, 0.12f));
            leave.onClick.AddListener(ReturnToStudentHub);
            pausePanel.SetActive(false);
        }

        private void BuildCompletionPanel(Transform parent)
        {
            completionPanel = CreateFullOverlay("Quest Complete Overlay", parent, new Color(0.005f, 0.012f, 0.04f, 0.82f));
            var card = CreatePanel("Quest Complete Card", completionPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(1000f, 590f),
                Vector2.zero, new Color(0.03f, 0.075f, 0.17f, 0.99f), 25);
            var title = CreateText("Completion Title", card.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -66f), new Vector2(880f, 46f), 35f, TextAlignmentOptions.Center, new Color(1f, 0.82f, 0.34f));
            title.fontStyle = FontStyles.Bold;
            title.text = "THE CLOCK IS SINGING AGAIN";
            completionSummaryText = CreateText("Completion Summary", card.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 16f), new Vector2(820f, 220f), 25f, TextAlignmentOptions.Center, new Color(0.94f, 0.97f, 1f));
            completionSummaryText.textWrappingMode = TextWrappingModes.Normal;
            var hub = CreateButton("Completion Hub", card.transform, "RETURN TO STUDENT HUB", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 82f), new Vector2(420f, 56f), new Color(0.12f, 0.46f, 0.72f));
            hub.onClick.AddListener(ReturnToStudentHub);
            var explore = CreateButton("Completion Explore", card.transform, "EXPLORE THE DECK", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 16f), new Vector2(280f, 36f), new Color(0.07f, 0.13f, 0.25f));
            explore.onClick.AddListener(CloseCompletionPanel);
            completionPanel.SetActive(false);
        }

        private void ShowBriefing(int index)
        {
            briefingVisible = true;
            briefingIndex = Mathf.Clamp(index, 0, briefingLines.Length - 1);
            var line = briefingLines[briefingIndex];
            briefingSpeakerText.text = line.Speaker;
            briefingBodyText.text = line.Body;
            briefingStepText.text = "BRIEFING " + (briefingIndex + 1) + " OF " + briefingLines.Length;
            briefingPrimaryButton.GetComponentInChildren<TMP_Text>().text = briefingIndex == briefingLines.Length - 1
                ? "SET SAIL"
                : "CONTINUE";
            briefingPanel.SetActive(true);
            playerController.SetInputEnabled(false);
            UnlockCursor();
        }

        private void AdvanceBriefing()
        {
            if (briefingIndex < briefingLines.Length - 1)
            {
                ShowBriefing(briefingIndex + 1);
                return;
            }

            BeginExploration();
        }

        private void BeginExploration()
        {
            briefingVisible = false;
            briefingPanel.SetActive(false);
            EnableDeckInput();
            UpdateHud();
        }

        public void OpenStation(CelestialClockStation station)
        {
            if (station == null || station.IsComplete || station.IsLocked || briefingVisible || lessonVisible || pauseVisible)
                return;

            activeStation = station;
            lessonVisible = true;
            lessonPanel.SetActive(true);
            interactionText.gameObject.SetActive(false);
            playerController.SetInputEnabled(false);
            UnlockCursor();
            ShowTeachingNote();
        }

        private void ShowTeachingNote()
        {
            if (activeStation == null)
                return;

            lessonEyebrowText.text = "TEACHING NOTE  •  READ BEFORE PRACTICE";
            lessonTitleText.text = activeStation.Title;
            lessonBodyText.text = activeStation.TeachingNote;
            lessonFeedbackText.text = "When you are ready, use the practice prompt. You can return to this note any time.";
            lessonFeedbackText.color = new Color(0.98f, 0.78f, 0.38f);
            SetAnswersVisible(false);
            ConfigureLessonButton(lessonPrimaryButton, "START PRACTICE", BeginPractice, new Color(0.13f, 0.46f, 0.72f));
            ConfigureLessonButton(lessonSecondaryButton, "RETURN TO DECK", CloseLesson, new Color(0.08f, 0.14f, 0.27f));
        }

        private void BeginPractice()
        {
            if (activeStation == null)
                return;

            lessonEyebrowText.text = "PRACTICE PORTAL  •  TAKE YOUR TIME";
            lessonTitleText.text = activeStation.Title;
            lessonBodyText.text = activeStation.Question;
            lessonFeedbackText.text = "Choose the answer that makes the seal glow. A retry is part of learning.";
            lessonFeedbackText.color = new Color(0.72f, 0.87f, 1f);
            SetAnswersVisible(true);
            for (var i = 0; i < answerButtons.Count; i++)
            {
                answerButtons[i].GetComponentInChildren<TMP_Text>().text = activeStation.Answers[i];
                answerButtons[i].interactable = true;
            }
            ConfigureLessonButton(lessonPrimaryButton, "READ TEACHING NOTE", ShowTeachingNote, new Color(0.08f, 0.22f, 0.4f));
            ConfigureLessonButton(lessonSecondaryButton, "RETURN TO DECK", CloseLesson, new Color(0.08f, 0.14f, 0.27f));
        }

        private void AnswerSelected(int selectedIndex)
        {
            if (activeStation == null || selectedIndex < 0 || selectedIndex >= activeStation.Answers.Length)
                return;

            if (selectedIndex != activeStation.CorrectIndex)
            {
                lessonFeedbackText.text = "Not yet — reread the teaching note, then use the clue in the question and try again.";
                lessonFeedbackText.color = new Color(1f, 0.7f, 0.34f);
                return;
            }

            var completed = activeStation;
            completed.MarkCompleted();
            if (!completedNodeIds.Contains(completed.NodeId))
                completedNodeIds.Add(completed.NodeId);
            provisionalXp += completed.XpAward;
            SaveRunSnapshot();

            var justUnlockedFinale = CompletedCount == RequiredSealsBeforeFinale;
            if (justUnlockedFinale)
                UnlockFinalStation();

            if (CompletedCount == stations.Count)
            {
                CloseLesson();
                FinishQuest();
                return;
            }

            lessonEyebrowText.text = "SEAL RESTORED";
            lessonTitleText.text = completed.Title;
            lessonBodyText.text = "Excellent navigation. The seal answers with starlight and the Aurora's clockwork hums a little louder.";
            lessonFeedbackText.text = justUnlockedFinale
                ? "The final clock seal is now awake at the bow. Follow its golden glow."
                : "Your journal has been updated. Find the next glowing seal when you are ready.";
            lessonFeedbackText.color = new Color(0.6f, 1f, 0.75f);
            SetAnswersVisible(false);
            ConfigureLessonButton(lessonPrimaryButton, "RETURN TO DECK", CloseLesson, new Color(0.13f, 0.46f, 0.72f));
            ConfigureLessonButton(lessonSecondaryButton, "OPEN JOURNAL", OpenJournalFromLesson, new Color(0.08f, 0.22f, 0.4f));
            UpdateHud();
        }

        private void UnlockFinalStation()
        {
            foreach (var station in stations)
            {
                if (station.NodeId != "celestial-clock")
                    continue;
                station.SetLocked(false);
                break;
            }
        }

        private void CloseLesson()
        {
            activeStation = null;
            lessonVisible = false;
            lessonPanel.SetActive(false);
            if (!pauseVisible && !briefingVisible && !journalVisible && !completionPanel.activeSelf)
                EnableDeckInput();
        }

        private void OpenJournal()
        {
            if (briefingVisible || lessonVisible || pauseVisible || completionPanel.activeSelf)
                return;

            journalVisible = true;
            journalPanel.SetActive(true);
            journalText.text = BuildJournalText();
            playerController.SetInputEnabled(false);
            UnlockCursor();
        }

        private void OpenJournalFromLesson()
        {
            // The completion card is still a lesson overlay, so close it before using
            // the regular journal route. This keeps the button actionable and returns
            // the learner to the deck after they review their notes.
            CloseLesson();
            OpenJournal();
        }

        private string BuildJournalText()
        {
            var text = "<b>Course notes</b>\n\n";
            text += "<color=#9CCFFF>• Fractions:</color> top = chosen parts; bottom = equal parts in the whole.\n\n";
            text += "<color=#9CCFFF>• Angle turns:</color> one quarter of 360° is 90°.\n\n";
            text += "<color=#9CCFFF>• Equations:</color> undo the operation beside x to keep both sides balanced.\n\n";
            text += "<b>Seal log</b>\n";
            for (var i = 0; i < stations.Count; i++)
            {
                var station = stations[i];
                var status = station.IsComplete ? "RESTORED" : station.IsLocked ? "SLEEPING" : "WAITING";
                text += "• " + station.ShortLabel + " — <b>" + status + "</b>\n";
            }
            text += "\n<color=#B8D5F0>Controls: WASD to move, mouse to look, E to inspect a nearby seal, J for this journal, Esc for menu.</color>";
            return text;
        }

        private void CloseJournal()
        {
            journalVisible = false;
            journalPanel.SetActive(false);
            if (!pauseVisible && !lessonVisible && !briefingVisible && !completionPanel.activeSelf)
                EnableDeckInput();
        }

        private void TogglePause()
        {
            pauseVisible = !pauseVisible;
            pausePanel.SetActive(pauseVisible);
            if (pauseVisible)
            {
                playerController.SetInputEnabled(false);
                UnlockCursor();
            }
            else
            {
                EnableDeckInput();
            }
        }

        private void FinishQuest()
        {
            questComplete = true;
            ApplyCompletionExperience();
            SaveRunSnapshot();
            UpdateHud();
            completionSummaryText.text = "You restored all four seals and woke the Celestial Clock.\n\n" +
                "Practice complete: " + CompletedCount + " seals  •  " + provisionalXp + " provisional XP recorded\n\n" +
                "Your teacher can now see this completion after the Firebase classroom backend is deployed.";
            completionPanel.SetActive(true);
            playerController.SetInputEnabled(false);
            UnlockCursor();
        }

        private void ApplyCompletionExperience()
        {
            if (questDefinition == null || questDefinition.RewardPolicy == null ||
                !questDefinition.RewardPolicy.Enabled || !questDefinition.RewardPolicy.ShowProvisionalExperience)
                return;

            provisionalXp += questDefinition.RewardPolicy.CompletionExperience;
        }

        private void CloseCompletionPanel()
        {
            completionPanel.SetActive(false);
            EnableDeckInput();
        }

        private void ReturnToStudentHub()
        {
            QuestSession.Instance?.Clear();
            SceneTransition.LoadScene("StudentHub");
        }

        private void SaveRunSnapshot()
        {
            var session = QuestSession.Instance;
            var questId = session == null || string.IsNullOrWhiteSpace(session.QuestId) ? "celestial-clock" : session.QuestId;
            var classId = session == null ? string.Empty : session.ClassId;
            var assignmentId = session == null ? questId : session.AssignmentId;
            QuestProgressRepository.Instance?.SaveQuestRunSnapshot(questId, classId, assignmentId, completedNodeIds,
                provisionalXp, questComplete);
        }

        private void UpdateHud()
        {
            if (sealText == null)
                return;

            sealText.text = "SEALS " + CompletedCount + " / " + stations.Count;
            xpText.text = "PROVISIONAL XP  " + provisionalXp;
            if (questComplete)
            {
                objectiveText.text = "The Celestial Clock is awake. The Aurora has a new course.";
                return;
            }

            if (CompletedCount < RequiredSealsBeforeFinale)
            {
                objectiveText.text = "Restore the three learning seals on the deck. Read each Teaching Note before practice.";
                return;
            }

            objectiveText.text = "The golden final seal is awake at the bow. Wake the Celestial Clock.";
        }

        public CelestialClockStation FindNearestAvailableStation(Vector3 position, float radius)
        {
            CelestialClockStation nearest = null;
            var nearestDistance = radius;
            foreach (var station in stations)
            {
                if (station.IsComplete || station.IsLocked)
                    continue;

                var distance = Vector3.Distance(position, station.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = station;
                }
            }
            return nearest;
        }

        public void UpdateInteractionPrompt(CelestialClockStation station)
        {
            if (interactionText == null || lessonVisible || briefingVisible || pauseVisible || journalVisible || questComplete)
                return;

            if (station == null)
            {
                interactionText.gameObject.SetActive(false);
                return;
            }

            interactionText.text = "PRESS  E  TO INSPECT  " + station.ShortLabel;
            interactionText.gameObject.SetActive(true);
        }

        private void EnableDeckInput()
        {
            if (playerController == null)
                return;

            playerController.SetInputEnabled(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private int CompletedCount
        {
            get
            {
                var count = 0;
                foreach (var station in stations)
                {
                    if (station.IsComplete)
                        count++;
                }
                return count;
            }
        }

        private void SetAnswersVisible(bool visible)
        {
            foreach (var button in answerButtons)
                button.gameObject.SetActive(visible);
        }

        private static void ConfigureLessonButton(Button button, string label, UnityEngine.Events.UnityAction action, Color color)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            button.GetComponentInChildren<TMP_Text>().text = label;
            button.GetComponent<Image>().color = color;
        }

        private static GameObject CreateFullOverlay(string name, Transform parent, Color color)
        {
            var overlay = new GameObject(name, typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(parent, false);
            var rect = overlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = overlay.GetComponent<Image>();
            image.sprite = QuestUiAssetCache.SolidSprite;
            image.color = color;
            return overlay;
        }

        private static GameObject CreatePanel(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 position, Color color, float outlineWidth)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor == Vector2.zero ? Vector2.zero : new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = panel.GetComponent<Image>();
            image.sprite = QuestUiAssetCache.SolidSprite;
            image.color = color;
            var outline = panel.GetComponent<Outline>();
            outline.effectColor = new Color(0.52f, 0.76f, 1f, 0.34f);
            outline.effectDistance = new Vector2(outlineWidth * 0.08f, -outlineWidth * 0.08f);
            return panel;
        }

        private static TMP_Text CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position,
            Vector2 size, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(anchorMin.x == anchorMax.x ? anchorMin.x : 0.5f, anchorMin.y == anchorMax.y ? anchorMin.y : 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            var shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.62f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 position, Vector2 size, Color color)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(anchorMin.x == anchorMax.x ? anchorMin.x : 0.5f, anchorMin.y == anchorMax.y ? anchorMin.y : 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = buttonObject.GetComponent<Image>();
            image.sprite = QuestUiAssetCache.SolidSprite;
            image.color = color;
            var outline = buttonObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.89f, 0.78f, 0.42f, 0.68f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 0.85f, 1f);
            colors.pressedColor = new Color(0.72f, 0.82f, 1f, 1f);
            colors.disabledColor = new Color(0.45f, 0.5f, 0.58f, 0.55f);
            button.colors = colors;
            var text = CreateText("Label", buttonObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                20f, TextAlignmentOptions.Center, new Color(1f, 0.96f, 0.84f));
            text.fontStyle = FontStyles.Bold;
            text.text = label;
            return button;
        }

        private static GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.position = position;
            primitive.transform.localScale = scale;
            var collider = primitive.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;
            var renderer = primitive.GetComponent<Renderer>();
            if (renderer != null && material != null)
                renderer.material = material;
            return primitive;
        }

        private static Material CreateMaterial(Color color, bool emissive = false)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            var material = new Material(shader);
            material.color = color;
            if (emissive && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.8f);
            }
            return material;
        }

        private readonly struct DialogueLine
        {
            public DialogueLine(string speaker, string body)
            {
                Speaker = speaker;
                Body = body;
            }

            public string Speaker { get; }
            public string Body { get; }
        }

        private readonly struct StationLayout
        {
            public StationLayout(string nodeId, string ordinal, Vector3 position, Color accent, bool initiallyLocked)
            {
                NodeId = nodeId;
                Ordinal = ordinal;
                Position = position;
                Accent = accent;
                InitiallyLocked = initiallyLocked;
            }

            public string NodeId { get; }
            public string Ordinal { get; }
            public Vector3 Position { get; }
            public Color Accent { get; }
            public bool InitiallyLocked { get; }
        }

        private readonly struct DefinitionStationContent
        {
            public DefinitionStationContent(string nodeId, string displayTitle, string teachingNote, string question,
                string[] answers, int correctIndex, int xpAward, Vector3 position, Color accent, bool initiallyLocked)
            {
                NodeId = nodeId;
                DisplayTitle = displayTitle;
                TeachingNote = teachingNote;
                Question = question;
                Answers = answers;
                CorrectIndex = correctIndex;
                XpAward = xpAward;
                Position = position;
                Accent = accent;
                InitiallyLocked = initiallyLocked;
            }

            public string NodeId { get; }
            public string DisplayTitle { get; }
            public string TeachingNote { get; }
            public string Question { get; }
            public string[] Answers { get; }
            public int CorrectIndex { get; }
            public int XpAward { get; }
            public Vector3 Position { get; }
            public Color Accent { get; }
            public bool InitiallyLocked { get; }
        }
    }

    /// <summary>Data and visual state for one teaching + practice seal on the ship deck.</summary>
    public sealed class CelestialClockStation : MonoBehaviour
    {
        private readonly List<Renderer> visuals = new List<Renderer>();
        private CelestialClockQuestBootstrap owner;
        private Color accent;
        private Light beacon;
        private Transform glyph;
        private TextMeshPro label;
        private bool locked;

        public string NodeId { get; private set; }
        public string Title { get; private set; }
        public string ShortLabel { get; private set; }
        public string TeachingNote { get; private set; }
        public string Question { get; private set; }
        public string[] Answers { get; private set; }
        public int CorrectIndex { get; private set; }
        public int XpAward { get; private set; }
        public bool IsComplete { get; private set; }
        public bool IsLocked => locked;

        public void Configure(CelestialClockQuestBootstrap quest, string nodeId, string title, string teachingNote, string question,
            string[] answers, int correctIndex, int xpAward, Color accentColor, bool initiallyLocked)
        {
            owner = quest;
            NodeId = nodeId;
            Title = title;
            ShortLabel = title.Contains(".") ? title.Substring(title.IndexOf('.') + 1).Trim() : title;
            TeachingNote = teachingNote;
            Question = question;
            Answers = answers;
            CorrectIndex = correctIndex;
            XpAward = xpAward;
            accent = accentColor;
            locked = initiallyLocked;
        }

        public void RegisterVisual(Renderer renderer)
        {
            if (renderer != null)
                visuals.Add(renderer);
        }

        public void RegisterBeacon(Light pointLight) => beacon = pointLight;
        public void RegisterGlyph(Transform transformToRotate) => glyph = transformToRotate;
        public void RegisterLabel(TextMeshPro stationLabel) => label = stationLabel;

        public void SetLocked(bool value)
        {
            locked = value;
            RefreshVisualState();
        }

        public void MarkCompleted()
        {
            IsComplete = true;
            RefreshVisualState();
        }

        public void RefreshVisualState()
        {
            var color = IsComplete ? new Color(0.28f, 0.95f, 0.55f) : locked ? new Color(0.13f, 0.16f, 0.27f) : accent;
            foreach (var renderer in visuals)
            {
                if (renderer == null)
                    continue;
                renderer.material.color = color;
                if (renderer.material.HasProperty("_EmissionColor"))
                    renderer.material.SetColor("_EmissionColor", color * (locked ? 0.25f : 1.7f));
            }

            if (beacon != null)
            {
                beacon.color = color;
                beacon.intensity = locked ? 0.35f : IsComplete ? 3.7f : 2.7f;
            }

            if (label != null)
            {
                label.text = IsComplete ? ShortLabel + "\n✓ RESTORED" : locked ? "FINAL SEAL\nSLEEPING" : ShortLabel;
                label.color = IsComplete ? new Color(0.63f, 1f, 0.75f) : locked ? new Color(0.52f, 0.58f, 0.72f) : new Color(1f, 0.94f, 0.75f);
            }
        }

        private void Update()
        {
            if (glyph != null && !locked)
                glyph.Rotate(0f, 45f * Time.deltaTime, 0f, Space.World);
        }
    }

    /// <summary>Keyboard/mouse first-person controller limited to the small ship deck.</summary>
    public sealed class CelestialClockPlayerController : MonoBehaviour
    {
        private CelestialClockQuestBootstrap quest;
        private CharacterController characterController;
        private Transform playerCamera;
        private float movementSpeed;
        private float sensitivity;
        private float verticalVelocity;
        private float pitch;
        private bool inputEnabled;
        private CelestialClockStation nearbyStation;

        public void Configure(CelestialClockQuestBootstrap owner, float moveSpeed, float lookSensitivity)
        {
            quest = owner;
            movementSpeed = moveSpeed;
            sensitivity = lookSensitivity;
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            var camera = GetComponentInChildren<Camera>();
            playerCamera = camera == null ? transform : camera.transform;
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (!enabled && quest != null)
                quest.UpdateInteractionPrompt(null);
        }

        private void Update()
        {
            if (!inputEnabled || Keyboard.current == null || Mouse.current == null)
                return;

            var move = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) move.y += 1f;
            if (Keyboard.current.sKey.isPressed) move.y -= 1f;
            if (Keyboard.current.dKey.isPressed) move.x += 1f;
            if (Keyboard.current.aKey.isPressed) move.x -= 1f;
            var direction = (transform.forward * move.y + transform.right * move.x).normalized;
            if (characterController.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            else
                verticalVelocity += Physics.gravity.y * Time.deltaTime;
            characterController.Move((direction * movementSpeed + Vector3.up * verticalVelocity) * Time.deltaTime);

            var look = Mouse.current.delta.ReadValue() * sensitivity;
            transform.Rotate(0f, look.x, 0f);
            pitch = Mathf.Clamp(pitch - look.y, -72f, 72f);
            playerCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            nearbyStation = quest.FindNearestAvailableStation(transform.position, 2.05f);
            quest.UpdateInteractionPrompt(nearbyStation);
            if (Keyboard.current.eKey.wasPressedThisFrame && nearbyStation != null)
                quest.OpenStation(nearbyStation);
        }
    }

    internal sealed class CelestialPulse : MonoBehaviour
    {
        private float amplitude;
        private float speed;
        private Vector3 initialScale;

        public void Configure(float pulseAmplitude, float pulseSpeed)
        {
            amplitude = pulseAmplitude;
            speed = pulseSpeed;
            initialScale = transform.localScale;
        }

        private void Start()
        {
            if (initialScale == Vector3.zero)
                initialScale = transform.localScale;
        }

        private void Update()
        {
            var pulse = 1f + Mathf.Sin(Time.time * speed) * amplitude;
            transform.localScale = initialScale * pulse;
        }
    }

    internal sealed class CelestialOceanMotion : MonoBehaviour
    {
        private float amplitude;
        private float speed;
        private float baseHeight;

        public void Configure(float bobAmplitude, float bobSpeed)
        {
            amplitude = bobAmplitude;
            speed = bobSpeed;
            baseHeight = transform.position.y;
        }

        private void Start()
        {
            if (Mathf.Approximately(baseHeight, 0f))
                baseHeight = transform.position.y;
        }

        private void Update()
        {
            var position = transform.position;
            position.y = baseHeight + Mathf.Sin(Time.time * speed) * amplitude;
            transform.position = position;
        }
    }

    internal sealed class CelestialStarfieldDrift : MonoBehaviour
    {
        private float speed;
        public void Configure(float driftSpeed) => speed = driftSpeed;
        private void Update() => transform.Rotate(0f, speed * Time.deltaTime, 0f, Space.World);
    }

    internal sealed class CelestialBillboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            var camera = Camera.main;
            if (camera == null)
                return;
            transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position);
        }
    }

    internal static class QuestUiAssetCache
    {
        private static Sprite solidSprite;
        public static Sprite SolidSprite
        {
            get
            {
                if (solidSprite == null)
                    solidSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
                return solidSprite;
            }
        }
    }
}
