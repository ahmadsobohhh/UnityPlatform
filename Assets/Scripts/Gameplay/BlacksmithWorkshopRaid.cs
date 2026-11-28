using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ImagineQuest.Gameplay
{
    public sealed partial class BlacksmithRoomBootstrap
    {
        private static readonly Color Soot = new Color(.105f, .075f, .055f, .98f);
        private static readonly Color Leather = new Color(.27f, .16f, .095f);
        private static readonly Color Brass = new Color(.76f, .53f, .26f);
        private static readonly Vector3 YardStart = new Vector3(0, .06f, -9);
        [SerializeField] private TMP_FontAsset raidHeadingFont;
        [SerializeField] private GameObject authoredRaidSword;
        private readonly WorkshopBladeTrial bladeTrial = new WorkshopBladeTrial();
        private bool raidPractice, bladeForged, inRaid;
        private bool arrivingAtForge;
        private static bool launchRaidPractice;
        private GameObject raidDoor, raidQuestion, raidResult, raidGate;
        private Vector3 shutterRest;
        private float shutterProgress;
        private Collider shutterCollider;
        private bool shutterLatched;
        private TMP_Text raidTitle, raidScore, raidQuestionText, raidHint, raidResultText, raidResultTitle;
        private Button raidRetry, raidReturn;
        private int raidStation;
        private WorkshopRaidCombat raidCombat;
        private WorkshopRaidEffects raidEffects;
        private Transform raidHammer;
        private float hammerTime;
        private int hammerBeat;
        private Image raidHealthBar, raidDamageVeil;
        private float raidDamageFlash;
        private GameObject raidDeparture;
        private CanvasGroup raidDepartureFade;
        private RectTransform raidDepartureTitle, raidDepartureRule;
        private TMP_Text raidPauseStatus, raidResultStatus;

        public static void LaunchRaidPractice()
        {
            if (!Application.CanStreamedLevelBeLoaded("BlacksmithRoom")) return;
            launchRaidPractice = true;
            SceneTransition.LoadScene("BlacksmithRoom");
        }

#if UNITY_EDITOR
        public void PreviewCryptInEditor()
        {
            if (!raidPractice) return;
            StopAllCoroutines();
            var arrival = transform.Find("CINDER QUAY arrival");
            if (arrival != null) Destroy(arrival.gameObject);
            arrivingAtForge = false;
            view.localPosition = new Vector3(0f, 1.65f, 0f);
            bladeForged = true;
            raidDoor.SetActive(false);
            raidHammer.gameObject.SetActive(false);
            raidCombat.Equip(bladeTrial.Damage);
            ShowModal(null);
            PlaceRaidPlayer(YardStart, 180f);
            StartRaid();
        }
#endif

        private void BuildRaidInterface()
        {
            if (EventSystem.current == null)
                new GameObject("Workshop Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var canvas = new GameObject("Cinder Quay UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = .5f;
            hud = canvas.transform;

            raidDamageVeil = Panel("Damage flash", hud, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero, Color.clear).GetComponent<Image>();
            raidDamageVeil.rectTransform.anchorMin = Vector2.zero;
            raidDamageVeil.rectTransform.anchorMax = Vector2.one;
            var status = Panel("Forge ledger", hud, Vector2.up, new Vector2(24, -24), new Vector2(348, 106), Soot);
            Panel("Brass edge", status, Vector2.up, Vector2.zero, new Vector2(348, 3), Brass);
            Label(status, "CINDER QUAY", 20, 12, 308, 24, 15, Gold);
            raidTitle = Label(status, "", 20, 37, 308, 30, 25, Cream);
            if (raidHeadingFont != null) raidTitle.font = raidHeadingFont;
            raidScore = Label(status, "", 20, 70, 308, 23, 16, Gold);
            raidHealthBar = Panel("Health bar", status, Vector2.up, new Vector2(20, -95), new Vector2(308, 3),
                new Color(.79f, .31f, .18f)).GetComponent<Image>();
            prompt = Text(hud, "Action", new Vector2(.5f, 0), new Vector2(0, 86), new Vector2(800, 38), 23, Gold, TextAlignmentOptions.Center);
            Text(hud, "Controls", new Vector2(.5f, 0), new Vector2(0, 25), new Vector2(850, 23), 16, Cream, TextAlignmentOptions.Center).text =
                "WASD  Move     •     E  Use     •     LMB  Strike     •     RMB  Guard     •     Esc  Pause";
            Text(hud, "Reticle", new Vector2(.5f, .5f), Vector2.zero, new Vector2(20, 20), 18, Cream, TextAlignmentOptions.Center).text = "+";

            forgeIntroduction = BuildBramGuide("Forge your legend!",
                "Visit all three stations and give each question your best shot. " +
                "Every correct answer makes your sword stronger. What mighty blade will you create?",
                "Enter the workshop");
            sealIntroduction = BuildBramGuide("Let’s crack the code!",
                "These symbols hold the secret to the next room. Look for the clue, work out the order, " +
                "and show this old crypt how clever you are!", "Let’s solve it!");
            combatIntroduction = BuildBramGuide("Time to test your blade!",
                "You forged it—now give it a swing! Defeat the skeleton crew, guard against their attacks, " +
                "and explore the hall for treasure.", "I’m ready!");
            vaultIntroduction = BuildBramGuide("You’ve rattled the skeleton crew!",
                "Great sword work, adventurer! Another sealed door awaits. " +
                "Read the question, work out the answer, and choose the matching number to open the vault.",
                "Let’s crack it!");
            secondCombatIntroduction = BuildBramGuide("The next crew is waiting!",
                "You cracked the vault puzzle! More rattling rivals are ahead. Keep moving, " +
                "guard against their attacks, and put your sword to work. The treasure is getting closer!",
                "Onward, adventurer!");
            battleVictoryIntroduction = BuildBramGuide("You’ve beaten the skeleton crew!",
                "Brilliant work, adventurer! Your sharp thinking and sword skills have carried you through. " +
                "Follow the passage ahead to find the Tidekeeper’s treasure.",
                "To the treasure!");
            relicIntroduction = BuildBramGuide("The Tidekeeper awaits!",
                "You’ve reached the final treasure room! Walk up to the golden chest, " +
                "look at it, and press E to claim the Tidekeeper. You’ve earned this moment!",
                "Let’s claim it!");

            raidQuestion = RaidCard("Smith's notes", new Vector2(650, 408), out var card);
            raidQuestionText = Label(card, "", 32, 30, 586, 77, 27, Cream);
            for (int i = 0; i < 3; i++)
            {
                int choice = i;
                answerButtons.Add(RaidButton(card, "", 32, 115 + i * 56, 586, () => SubmitBladeAnswer(choice)));
            }
            raidHint = Label(card, "", 32, 290, 586, 47, 19, Gold);
            RaidButton(card, "Close", 32, 348, 175, () => ShowModal(null));

            pausePanel = RaidCard("Paused", new Vector2(450, 240), out var pause);
            Label(pause, "Take a breather", 30, 28, 390, 55, 31, Cream);
            RaidButton(pause, "Resume", 30, 100, 390, () => ShowModal(null));
            RaidButton(pause, "Back to the ship", 30, 158, 390, ReturnFromRaid);
            raidPauseStatus = Label(pause, "", 30, 213, 390, 22, 16, Gold);

            raidResult = RaidCard("Raid outcome", new Vector2(530, 284), out var result);
            raidResultTitle = Label(result, "", 32, 29, 466, 55, 34, Cream);
            if (raidHeadingFont != null) raidResultTitle.font = raidHeadingFont;
            raidResultText = Label(result, "", 32, 88, 466, 45, 20, Gold);
            raidRetry = RaidButton(result, "Fight again", 32, 146, 466, RetryRaid);
            raidReturn = RaidButton(result, "Back to the ship", 32, 208, 466, ReturnFromRaid);
            raidResultStatus = Label(result, "", 32, 257, 466, 22, 16, Gold);
            BuildRaidDeparture();

            raidEffects = new WorkshopRaidEffects(transform, effectsAudio, generatedAudio, materials);
            raidCombat = new WorkshopRaidCombat(player.transform, view, Surface, Glow, meshes, raidEffects, authoredRaidSword, CryptWaveReady);
            raidHammer = new GameObject("Forging hammer").transform;
            raidHammer.SetParent(transform, false);
            Decor("Hammer shaft", PrimitiveType.Cylinder, new Vector3(0, .18f, 0),
                new Vector3(.07f, .25f, .07f), timber).SetParent(raidHammer, false);
            Decor("Hammer face", PrimitiveType.Cube, new Vector3(0, .45f, 0),
                new Vector3(.22f, .16f, .26f), Surface(new Color(.29f, .31f, .31f), .44f)).SetParent(raidHammer, false);
            raidHammer.gameObject.SetActive(false);
            for (int i = 0; i < 3; i++) stationLabels[i].text = StationNames[i];
            UpdateRaidHud();
            ShowModal(null);
            StartCoroutine(ArriveAtForge());
        }

        private GameObject BuildBramGuide(string heading, string copy, string buttonText)
        {
            var overlay = RaidCard("Bram guide", new Vector2(760, 330), out var introduction);
            var portrait = new GameObject("Bram guide portrait", typeof(RectTransform), typeof(RawImage), typeof(Outline));
            portrait.transform.SetParent(introduction, false);
            var portraitRect = portrait.GetComponent<RectTransform>();
            portraitRect.anchorMin = portraitRect.anchorMax = Vector2.up;
            portraitRect.pivot = Vector2.up;
            portraitRect.anchoredPosition = new Vector2(32, -40);
            portraitRect.sizeDelta = new Vector2(128, 128);
            var portraitImage = portrait.GetComponent<RawImage>();
            portraitImage.texture = Resources.Load<Texture2D>("TideCrypt/BramGuidePortrait");
            portraitImage.raycastTarget = false;
            portrait.GetComponent<Outline>().effectColor = Brass;
            portrait.GetComponent<Outline>().effectDistance = new Vector2(2, -2);
            Label(introduction, "BRAM • YOUR BLACKSMITH GUIDE", 192, 32, 536, 28, 17, Gold);
            Label(introduction, heading, 192, 72, 536, 44, 30, Cream);
            var explanation = Label(introduction, copy, 192, 128, 536, 114, 23, Cream);
            explanation.textWrappingMode = TextWrappingModes.Normal;
            RaidButton(introduction, buttonText, 192, 260, 536, () => ShowModal(null));

            return overlay;
        }

        private GameObject forgeIntroduction, sealIntroduction, combatIntroduction, vaultIntroduction;
        private GameObject secondCombatIntroduction, swordReveal;
        private GameObject battleVictoryIntroduction, relicIntroduction;
        private bool combatGuideShown, vaultGuideShown, secondCombatGuideShown, relicGuideShown;

        private IEnumerator ArriveAtForge()
        {
            arrivingAtForge = true;
            Vector3 eye = view.localPosition;
            bool handoff = SceneTransition.IsTransitioning;
            while (SceneTransition.IsTransitioning) yield return null;
            yield return QuestArrival.Play(transform, "CINDER QUAY", "THE BLACKSMITH", t =>
            {
                float reveal = 1 - t;
                view.localPosition = eye + new Vector3(-.13f, -.15f, -.35f) * reveal;
                view.localRotation = Quaternion.Euler(8 * reveal, -29 * reveal, -4 * reveal);
            }, handoff);
            pitch = 0;
            arrivingAtForge = false;
            ShowModal(forgeIntroduction);
        }

        private void BuildRaidDeparture()
        {
            raidDeparture = CelestialClockQuestBootstrap.CreateFullOverlay("Back to the Aurora", hud, Color.black);
            raidDepartureFade = raidDeparture.AddComponent<CanvasGroup>();
            raidDepartureFade.alpha = 0;
            raidDepartureFade.interactable = false;
            var title = Text(raidDeparture.transform, "Departure title", Vector2.one * .5f, new Vector2(0, 18),
                new Vector2(480, 54), 34, Cream, TextAlignmentOptions.Center);
            if (raidHeadingFont != null) title.font = raidHeadingFont;
            title.text = "Back to the Aurora";
            raidDepartureTitle = title.rectTransform;
            raidDepartureRule = (RectTransform)Panel("Departure brass rule", raidDeparture.transform, Vector2.one * .5f,
                new Vector2(0, -19), new Vector2(180, 2), Brass);
            raidDeparture.SetActive(false);
        }

        private void ReturnFromRaid()
        {
            if (returning) return;
            if (!Application.CanStreamedLevelBeLoaded("QuestArena"))
            {
                (modal == raidResult ? raidResultStatus : raidPauseStatus).text = "The ship is unavailable in this build.";
                return;
            }

            // Practice has no quest service or checkpoint to turn in. Keep the
            // existing QuestSession so the returning ship still belongs to the class.
            returning = true;
            ShowModal(null);
            raidDeparture.transform.SetAsLastSibling();
            raidDeparture.SetActive(true);
            effectsAudio.PlayOneShot(keySound, .25f);
            StartCoroutine(AnimateRaidDeparture());
        }

        private IEnumerator AnimateRaidDeparture()
        {
            const float duration = .5f;
            float elapsed = 0;
            while (elapsed < duration)
            {
                float progress = Mathf.SmoothStep(0, 1, elapsed / duration);
                raidDepartureFade.alpha = progress;
                raidDepartureTitle.anchoredPosition = new Vector2(0, Mathf.Lerp(5, 18, progress));
                raidDepartureRule.localScale = new Vector3(progress, 1, 1);
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
            raidDepartureFade.alpha = 1;
            // The persistent transition keeps the frame covered while the scene
            // changes, and fades the ship back in on arrival.
            SceneTransition.LoadScene("QuestArena", .32f);
        }

        private GameObject RaidCard(string name, Vector2 size, out Transform card)
        {
            var overlay = CelestialClockQuestBootstrap.CreateFullOverlay(name, hud, new Color(.025f, .015f, .012f, .65f));
            card = Panel(name, overlay.transform, new Vector2(.5f, .5f), Vector2.zero, size, Soot);
            Panel("Brass trim", card, Vector2.up, Vector2.zero, new Vector2(size.x, 4), Brass);
            Panel("Bottom seam", card, Vector2.zero, Vector2.zero, new Vector2(size.x, 2), Brass);
            overlay.SetActive(false);
            return overlay;
        }

        private static Button RaidButton(Transform card, string label, float x, float y, float width, UnityEngine.Events.UnityAction action)
        {
            var button = Button(card, label, x, y, width, 46, action);
            button.image.color = Leather;
            return button;
        }

        private void UpdateRaid()
        {
            if (returning || arrivingAtForge || !Application.isFocused) return;
            var key = Keyboard.current;
            if (key != null && key.escapeKey.wasPressedThisFrame && modal != raidResult)
            {
                ShowModal(modal == null ? pausePanel : null);
                return;
            }
            if (modal == raidQuestion && key != null)
            {
                if (key.digit1Key.wasPressedThisFrame) SubmitBladeAnswer(0);
                else if (key.digit2Key.wasPressedThisFrame) SubmitBladeAnswer(1);
                else if (key.digit3Key.wasPressedThisFrame) SubmitBladeAnswer(2);
            }
            if (modal != null) return;
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (Mouse.current?.leftButton.wasPressedThisFrame == true) ShowModal(null);
                return;
            }

            MoveWorkshopPlayer(key);
            AnimateRaidHammer(Time.deltaTime);
            if (modal != null) return;
            if (player.transform.position.y < -3) PlaceRaidPlayer(inRaid ? YardStart : raidIndoorSpawn, inRaid ? 180 : 0);
            if (!inRaid && bladeForged && player.transform.position.z < -7) StartRaid();
            if (modal != null) return;
            if (inRaid)
            {
                if (!combatGuideShown && cryptFirstOpen && player.transform.position.z < -17f)
                {
                    combatGuideShown = true;
                    ShowModal(combatIntroduction);
                    return;
                }
                if (!vaultGuideShown && raidCombat.Wave == 1 && raidCombat.Remaining == 0 &&
                    !cryptSecondOpen && player.transform.position.z < -25f)
                {
                    vaultGuideShown = true;
                    ShowModal(vaultIntroduction);
                    return;
                }
                if (!secondCombatGuideShown && cryptSecondOpen && player.transform.position.z < -37f)
                {
                    secondCombatGuideShown = true;
                    ShowModal(secondCombatIntroduction);
                    return;
                }
                StepClosingShutter(Time.deltaTime);
                int previousHealth = raidCombat.Health;
                if (!cryptCleared && raidCombat.Tick(Time.deltaTime, Mouse.current))
                {
                    if (raidCombat.Health <= 0) { ShowRaidResult(false); return; }
                    cryptCleared = true;
                    UpdateRaidHud();
                    ShowModal(battleVictoryIntroduction);
                    return;
                }
                if (cryptCleared && !relicGuideShown && player.transform.position.z < -53f)
                {
                    relicGuideShown = true;
                    ShowModal(relicIntroduction);
                    return;
                }
                if (cryptCleared) raidCombat.UpdateHeldBlade(Time.deltaTime);
                UpdateCryptInteraction(key);
                if (raidCombat.Health < previousHealth) raidDamageFlash = .25f;
                raidDamageFlash = Mathf.Max(0, raidDamageFlash - Time.deltaTime);
                raidDamageVeil.color = new Color(.6f, .07f, .03f, raidDamageFlash);
                UpdateRaidHud();
                return;
            }
            if (bladeForged) raidCombat.UpdateHeldBlade(Time.deltaTime);
            int station = NearbyStation();
            prompt.text = bladeForged ? "Enter the crypt" :
                bladeTrial.Complete ? (station == 0 ? "[E]  Forge blade" : "Return to the anvil") :
                station >= 0 && !bladeTrial.Answered(station) ? "[E]  " + StationNames[station] : "";
            if (key == null || !key.eKey.wasPressedThisFrame || bladeForged) return;
            if (bladeTrial.Complete && station == 0)
            {
                ForgeRaidBlade();
            }
            else if (station >= 0 && !bladeTrial.Answered(station))
            {
                raidStation = station;
                raidQuestionText.text = WorkshopBladeTrial.Questions[station];
                raidHint.text = "";
                for (int i = 0; i < 3; i++)
                {
                    answerButtons[i].interactable = true;
                    answerButtons[i].GetComponentInChildren<TMP_Text>().text = (i + 1) + "   " + WorkshopBladeTrial.Options[station][i];
                }
                ShowModal(raidQuestion);
            }
        }

        private void ForgeRaidBlade()
        {
            if (!bladeTrial.Complete || bladeForged) return;
            bladeForged = true;
            swordReveal = BuildBramGuide("Your " + bladeTrial.BladeName + " is ready!",
                "You got " + bladeTrial.Correct + " out of 3 questions correct and forged the " +
                bladeTrial.BladeName + "! Your sword deals " + bladeTrial.Damage +
                " damage with each hit. Take your new blade into the crypt—adventure awaits!",
                "Let’s explore!");
            raidDoor.SetActive(false);
            raidCombat.Equip(bladeTrial.Damage);
            hammerTime = 0;
            hammerBeat = 0;
            raidHammer.gameObject.SetActive(true);
            AnimateRaidHammer(0);
            CelebrateStation(0);
            UpdateRaidHud();
        }

        private void AnimateRaidHammer(float dt)
        {
            if (raidHammer == null || !raidHammer.gameObject.activeSelf) return;
            hammerTime += dt;
            if (hammerTime >= .88f)
            {
                raidHammer.gameObject.SetActive(false);
                if (swordReveal != null && !inRaid) ShowModal(swordReveal);
                return;
            }
            const float beatLength = .44f;
            float phase = Mathf.Repeat(hammerTime, beatLength) / beatLength;
            int strikes = Mathf.FloorToInt((hammerTime + beatLength * .18f) / beatLength);
            if (strikes > hammerBeat)
            {
                hammerBeat = strikes;
                raidEffects.Burst(RaidCue.Forge, raidAnvilTop, Vector3.up);
            }
            float lift = Mathf.Sin(phase * Mathf.PI);
            raidHammer.position = raidAnvilTop + new Vector3(.39f, .10f + .62f * lift, .01f);
            raidHammer.rotation = Quaternion.Euler(0, 0, 105 - 66 * lift);
        }

        private void SubmitBladeAnswer(int choice)
        {
            if (modal != raidQuestion || !bladeTrial.TryAnswer(raidStation, choice, out bool correct)) return;
            foreach (var button in answerButtons) button.interactable = false;
            raidHint.text = (correct ? "Correct. " : "Not quite. ") + WorkshopBladeTrial.Explanations[raidStation];
            stationLabels[raidStation].text = StationNames[raidStation] + "  ✓";
            stationLabels[raidStation].color = Cream;
            stationRunes[raidStation].sharedMaterial = correct ? runeComplete : runeIdle;
            effectsAudio.PlayOneShot(correct ? correctSound : errorSound, .35f);
            CelebrateStation(raidStation);
            UpdateRaidHud();
        }

        private void StartRaid()
        {
            inRaid = true;
            ResetCrypt();
            raidDamageFlash = 0;
            raidDamageVeil.color = Color.clear;
            shutterProgress = 0;
            shutterLatched = false;
            shutterCollider.enabled = false;
            raidGate.transform.position = shutterRest + Vector3.up * 2.7f;
            raidGate.SetActive(true);
            raidCombat.Begin(bladeTrial.Damage);
            combatGuideShown = vaultGuideShown = secondCombatGuideShown = relicGuideShown = false;
            prompt.text = "";
            UpdateRaidHud();
            ShowModal(sealIntroduction);
        }

        private void StepClosingShutter(float dt)
        {
            if (shutterLatched) return;
            // Never move a solid collider onto the player as they cross the threshold.
            // Slide back open until the full capsule is clear on the crypt side.
            bool clear = player.transform.position.z < shutterRest.z - player.radius - .36f;
            shutterProgress = Mathf.MoveTowards(shutterProgress, clear ? 1 : 0, dt * 1.65f);
            raidGate.transform.position = shutterRest + Vector3.up * (2.7f * (1 - Mathf.SmoothStep(0, 1, shutterProgress)));
            shutterLatched = clear && shutterProgress >= 1f;
            shutterCollider.enabled = shutterLatched;
        }

        private void ShowRaidResult(bool won)
        {
            raidResultTitle.text = won ? "Tidekeeper recovered." : "The crypt claims another.";
            raidResultText.text = $"{bladeTrial.BladeName}   /   {cryptGoldEarned} gold   •   {cryptXpEarned} XP";
            raidRetry.gameObject.SetActive(!won);
            raidReturn.GetComponent<RectTransform>().anchoredPosition = new Vector2(32, won ? -146 : -208);
            ShowModal(raidResult);
        }

        private void RetryRaid()
        {
            PlaceRaidPlayer(YardStart, 180);
            StartRaid();
        }

        private void PlaceRaidPlayer(Vector3 position, float yaw)
        {
            player.enabled = false;
            player.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            player.enabled = true;
            fallSpeed = 0;
            pitch = 0;
            view.localRotation = Quaternion.identity;
        }

        private void UpdateRaidHud()
        {
            raidTitle.text = inRaid ? (cryptCleared ? "Claim the relic" : !cryptFirstOpen ? "Read the seal" :
                !cryptSecondOpen && raidCombat.Wave == 1 && raidCombat.Remaining == 0 ? "Open the vault" : "Explore the crypt") : bladeForged ? bladeTrial.BladeName :
                bladeTrial.Complete ? "Forge your blade" : "Work the three stations";
            raidScore.text = inRaid ? (raidCombat.Remaining > 0
                ? $"HP {raidCombat.Health}   •   {raidCombat.Remaining} left   •   {cryptGoldEarned}g / {cryptXpEarned} XP"
                : $"HP {raidCombat.Health}   •   {cryptGoldEarned} gold   •   {cryptXpEarned} XP")
                : bladeForged ? $"{bladeTrial.Damage} damage   •   {bladeTrial.Correct}/3 correct" : $"{bladeTrial.Total}/3 stations";
            raidHealthBar.enabled = inRaid;
            raidHealthBar.rectTransform.sizeDelta = new Vector2(308f * raidCombat.Health / WorkshopRaidCombat.MaxHealth, 3);
        }
    }

    public sealed class WorkshopBladeTrial
    {
        public static readonly string[] Questions = {
            "A 36 cm blade needs a fuller half its length. How long is the fuller?",
            "Three billets weigh 250 g each. What do they weigh together?",
            "What does quenching hot steel in oil do?"
        };
        public static readonly string[][] Options = {
            new[] { "12 cm", "18 cm", "72 cm" },
            new[] { "500 g", "1,000 g", "750 g" },
            new[] { "Cools it rapidly", "Heats it further", "Makes it heavier" }
        };
        public static readonly string[] Explanations = {
            "Half of 36 is 18 cm.", "3 × 250 g = 750 g.", "Oil draws heat out of the steel."
        };
        private static readonly int[] Keys = { 1, 2, 0 };
        private int answered;
        public int Total { get; private set; }
        public int Correct { get; private set; }
        public bool Complete => Total == 3;
        public int Damage => 16 + Correct * 8;
        public string BladeName => Correct == 3 ? "Quaykeeper" : Correct >= 2 ? "Tempered Cutlass" : "Rough Cutlass";
        public bool Answered(int station) => station >= 0 && station < 3 && (answered & (1 << station)) != 0;
        public bool TryAnswer(int station, int choice, out bool correct)
        {
            correct = false;
            if (station < 0 || station >= 3 || choice < 0 || choice >= 3 || Answered(station)) return false;
            answered |= 1 << station;
            Total++;
            correct = Keys[station] == choice;
            if (correct) Correct++;
            return true;
        }
        public void Reset() { answered = Total = Correct = 0; }
    }
}
