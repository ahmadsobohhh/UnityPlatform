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
        private readonly WorkshopBladeTrial bladeTrial = new WorkshopBladeTrial();
        private bool raidPractice, bladeForged, inRaid, raidWon;
        private static bool launchRaidPractice;
        private GameObject raidDoor, raidQuestion, raidResult, raidGate;
        private TMP_Text raidTitle, raidScore, raidQuestionText, raidHint, raidResultText, raidResultTitle;
        private Button raidRetry;
        private int raidStation;
        private WorkshopRaidCombat raidCombat;
        private WorkshopRaidEffects raidEffects;
        private Transform raidHammer;
        private float hammerTime;
        private int hammerBeat;
        private Image raidHealthBar, raidDamageVeil;
        private float raidDamageFlash;

        public static void LaunchRaidPractice()
        {
            if (!Application.CanStreamedLevelBeLoaded("BlacksmithRoom")) return;
            launchRaidPractice = true;
            SceneTransition.LoadScene("BlacksmithRoom");
        }

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
            RaidButton(pause, "Back to the ship", 30, 158, 390, () => ReturnToShip(false));

            raidResult = RaidCard("Raid outcome", new Vector2(530, 284), out var result);
            raidResultTitle = Label(result, "", 32, 29, 466, 55, 34, Cream);
            if (raidHeadingFont != null) raidResultTitle.font = raidHeadingFont;
            raidResultText = Label(result, "", 32, 88, 466, 45, 20, Gold);
            raidRetry = RaidButton(result, "Fight again", 32, 146, 223, RetryRaid);
            RaidButton(result, "Reforge", 275, 146, 223, ResetRaid);
            RaidButton(result, "Back to the ship", 32, 208, 466, () => ReturnToShip(false));

            raidEffects = new WorkshopRaidEffects(transform, effectsAudio, generatedAudio, materials);
            raidCombat = new WorkshopRaidCombat(player.transform, view, Surface, Glow, meshes, raidEffects);
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
            if (returning || !Application.isFocused) return;
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
            if (player.transform.position.y < -3) PlaceRaidPlayer(inRaid ? YardStart : new Vector3(0, .06f, -2.8f), inRaid ? 180 : 0);
            if (inRaid)
            {
                int previousHealth = raidCombat.Health;
                if (!raidWon && raidCombat.Tick(Time.deltaTime, Mouse.current))
                    ShowRaidResult(raidCombat.Health > 0);
                if (raidCombat.Health < previousHealth) raidDamageFlash = .25f;
                raidDamageFlash = Mathf.Max(0, raidDamageFlash - Time.deltaTime);
                raidDamageVeil.color = new Color(.6f, .07f, .03f, raidDamageFlash);
                UpdateRaidHud();
                return;
            }
            if (bladeForged && player.transform.position.z < -7)
            {
                StartRaid();
                return;
            }
            if (bladeForged) raidCombat.UpdateHeldBlade(Time.deltaTime);
            int station = NearbyStation();
            prompt.text = bladeForged ? "Head outside" :
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
                return;
            }
            const float beatLength = .44f;
            float phase = Mathf.Repeat(hammerTime, beatLength) / beatLength;
            int strikes = Mathf.FloorToInt((hammerTime + beatLength * .18f) / beatLength);
            if (strikes > hammerBeat)
            {
                hammerBeat = strikes;
                raidEffects.Burst(RaidCue.Forge, AnvilPosition + new Vector3(0, 1.28f, -.08f), Vector3.up);
            }
            float lift = Mathf.Sin(phase * Mathf.PI);
            raidHammer.position = AnvilPosition + new Vector3(.39f, 1.38f + .62f * lift, -.07f);
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
            raidWon = false;
            raidDamageFlash = 0;
            raidDamageVeil.color = Color.clear;
            raidGate.SetActive(true);
            PlaceRaidPlayer(YardStart, 180);
            raidCombat.Begin(bladeTrial.Damage);
            prompt.text = "";
            UpdateRaidHud();
        }

        private void ShowRaidResult(bool won)
        {
            raidWon = won;
            raidResultTitle.text = won ? "The quay is safe." : "The dead broke through.";
            raidResultText.text = bladeTrial.BladeName + "   /   " + bladeTrial.Correct + " of 3 correct";
            raidRetry.gameObject.SetActive(!won);
            ShowModal(raidResult);
        }

        private void RetryRaid()
        {
            StartRaid();
            ShowModal(null);
        }

        private void ResetRaid()
        {
            raidCombat.Clear();
            raidHammer.gameObject.SetActive(false);
            raidDamageVeil.color = Color.clear;
            bladeTrial.Reset();
            inRaid = bladeForged = raidWon = false;
            raidDoor.SetActive(true);
            raidGate.SetActive(false);
            for (int i = 0; i < 3; i++)
            {
                stationLabels[i].text = StationNames[i];
                stationLabels[i].color = Gold;
                stationRunes[i].sharedMaterial = runeIdle;
            }
            PlaceRaidPlayer(new Vector3(0, .06f, -2.8f), 0);
            UpdateRaidHud();
            ShowModal(null);
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
            raidTitle.text = inRaid ? "Defend the quay" : bladeForged ? bladeTrial.BladeName :
                bladeTrial.Complete ? "Forge your blade" : "Work the three stations";
            raidScore.text = inRaid ? $"HP {raidCombat.Health}   •   Wave {Mathf.Max(1, raidCombat.Wave)}/2   •   {raidCombat.Remaining} left"
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
