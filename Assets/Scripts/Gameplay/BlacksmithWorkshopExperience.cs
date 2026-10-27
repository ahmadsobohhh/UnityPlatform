using System.Collections;
using System.Collections.Generic;
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
        private static readonly Color Ink = new Color(.035f, .065f, .09f, .97f);
        private static readonly Color Gold = new Color(1f, .73f, .32f);
        private static readonly Color Cream = new Color(.94f, .93f, .85f);
        private static readonly Color Teal = new Color(.33f, .9f, .83f);
        private static readonly string[] StationNames = { "ANVIL", "WORKBENCH", "FORGE" };
        private static readonly string[] CraftNames = { "Shape the brace", "Count the fasteners", "Prepare the metal" };
        private static readonly string[] Hints = {
            "Look for the heavy metal surface that supports the smith's hammer blows.",
            "Count four nails three times: 4 + 4 + 4. The three trays on the bench show the groups.",
            "Think about why a smith puts cold, hard metal into the glowing coals first."
        };
        private readonly Vector3[] stationPositions = {
            new Vector3(0, 1.5f, 4.05f), new Vector3(3, 1.5f, 2.4f), new Vector3(-2.7f, 1.5f, 2.6f)
        };
        private static readonly Vector3 PortalPosition = WorkshopForgeReward.PortalPosition;
        private WorkshopForgeReward forgeReward;
        private BlacksmithQuestService quest;
        private Transform hud;
        private GameObject modal, pausePanel, lessonPanel, briefingPanel, completionPanel;
        private TMP_Text objective, progressText, saveText, prompt, lessonTitle, lessonCopy, questionText, feedback;
        private TMP_Text completionCopy, journalCopy, keyText, portalLabel;
        private readonly List<TMP_Text> stationLabels = new List<TMP_Text>();
        private readonly List<TMP_Text> checklist = new List<TMP_Text>();
        private readonly List<Button> answerButtons = new List<Button>();
        private readonly List<Image> segments = new List<Image>();
        private readonly List<Renderer> stationRunes = new List<Renderer>();
        private readonly List<GameObject> repairParts = new List<GameObject>();
        private Material runeIdle, runeComplete;
        private int activeStation;
        private float idleSeconds;
        private bool showingHint;
        private bool returning;
        private AudioSource effectsAudio;
        private readonly List<AudioClip> generatedAudio = new List<AudioClip>();
        private AudioClip correctSound, errorSound, keySound;

        private void InitializeQuest()
        {
            quest = gameObject.AddComponent<BlacksmithQuestService>();
            quest.Changed += RefreshProgress;
            // An unrelated quest assignment must never receive workshop results.
            var session = QuestSession.Instance;
            bool assigned = session != null && session.QuestId == BlacksmithQuestContent.Id;
            quest.Initialize(assigned ? session.ClassId : null, assigned ? session.AssignmentId : null);
            RefreshProgress();
            ShowModal(quest.Ready && quest.State.introSeen ? null : briefingPanel);
        }

        private void Update()
        {
            var t = Time.time;
            if (forgeLight != null) forgeLight.intensity = 2.25f + Mathf.PerlinNoise(t * 3, .5f) * .6f;
            AnimateAtmosphere(t);
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                ShowModal(modal != null ? null : pausePanel);
                return;
            }
            if (modal == lessonPanel && keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) AnswerStation(0);
                if (keyboard.digit2Key.wasPressedThisFrame) AnswerStation(1);
                if (keyboard.digit3Key.wasPressedThisFrame) AnswerStation(2);
                if (keyboard.hKey.wasPressedThisFrame) ShowHint();
            }
            if (modal != null || returning || !Application.isFocused) return;
            if (keyboard != null && keyboard.jKey.wasPressedThisFrame) { ShowModal(pausePanel); return; }
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ShowModal(null);
                return;
            }
            int station = NearbyStation();
            bool portal = NearPortal();
            prompt.text = portal ? (quest.Ready && quest.State.portalOpened ? "WALK INTO THE PORTAL TO RETURN  /  [ E ] Review repair kit" : "PORTAL DORMANT  /  Touch the Forge Stone after completing all three crafts")
                : station >= 0 ? "[ E ]  " + StationNames[station] + "  /  " + CraftNames[station]
                : quest.Ready && quest.State.HasKey && !quest.State.portalOpened
                    ? "Touch the glowing Forge Stone in the center of the workshop"
                    : quest.Ready && quest.State.portalOpened ? "Walk into the emerald portal to return to the Aurora"
                    : "Find a glowing station to begin your repair";
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                if (portal && quest.Ready && quest.State.portalOpened) ShowCompletion();
                else if (station >= 0) OpenStation(station);
                return;
            }
            var mouse = Mouse.current;
            Vector2 look = mouse == null ? Vector2.zero : mouse.delta.ReadValue();
            player.transform.Rotate(0, look.x * .095f, 0);
            pitch = Mathf.Clamp(pitch - look.y * .095f, -75, 75);
            view.localRotation = Quaternion.Euler(pitch, 0, 0);
            Vector2 move = Vector2.zero;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y++;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y--;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x++;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x--;
            }
            move = Vector2.ClampMagnitude(move, 1);
            float speed = keyboard != null && keyboard.leftShiftKey.isPressed ? 4 : 2.8f;
            fallSpeed = player.isGrounded ? -2 : fallSpeed + Physics.gravity.y * Time.deltaTime;
            var velocity = (player.transform.forward * move.y + player.transform.right * move.x) * speed;
            velocity.y = fallSpeed;
            player.Move(velocity * Time.deltaTime);
            CheckPortalContact();
            idleSeconds = move.sqrMagnitude > 0 || look.sqrMagnitude > 1 ? 0 : idleSeconds + Time.deltaTime;
            if (idleSeconds > 35 && (!quest.Ready || !quest.State.HasKey))
                prompt.text = "Need a direction? Follow the station labels.  [ J ] opens your journal.";
        }

        private void CheckPortalContact()
        {
            if (modal != null || returning || !quest.Ready) return;
            if (forgeReward.CheckPlayerProximity(player.transform.position, player.radius, player.height))
            {
                effectsAudio.PlayOneShot(keySound, .4f);
                RefreshProgress();
            }
            if (forgeReward.CanEnterPortal(player.transform.position)) ReturnToShip(true);
        }

        private bool TryActivateForgeStone()
        {
            if (!quest.Ready || !quest.State.TryOpenPortal()) return false;
            if (quest.Save()) return true;
            quest.State.portalOpened = false;
            RefreshProgress();
            return false;
        }

        private int NearbyStation()
        {
            int closest = -1;
            float distance = 2.15f;
            for (int i = 0; i < stationPositions.Length; i++)
            {
                Vector3 flat = stationPositions[i] - player.transform.position;
                flat.y = 0;
                if (flat.magnitude < distance && Vector3.Dot(view.forward, (stationPositions[i] - view.position).normalized) > .45f)
                { closest = i; distance = flat.magnitude; }
            }
            return closest;
        }

        private bool NearPortal()
        {
            Vector3 delta = PortalPosition - view.position;
            return delta.magnitude < 2.7f && Vector3.Dot(view.forward, delta.normalized) > .45f;
        }

        private void ShowModal(GameObject next)
        {
            if (modal != null) modal.SetActive(false);
            modal = next;
            paused = next != null;
            if (next != null) { next.SetActive(true); next.transform.SetAsLastSibling(); }
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused;
            if (prompt != null) prompt.gameObject.SetActive(!paused);
            // Keep keyboard navigation available without requiring a mouse click.
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                var first = next == null ? null : next.GetComponentInChildren<Button>();
                if (first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
            }
        }

        private void OpenStation(int index)
        {
            if (!quest.Ready) { ShowModal(pausePanel); return; }
            activeStation = index;
            showingHint = false;
            quest.State.ReadLesson(index);
            quest.Save();
            lessonTitle.text = "0" + (index + 1) + "  /  " + StationNames[index];
            lessonCopy.text = BlacksmithQuestContent.Lessons[index];
            questionText.text = BlacksmithQuestContent.Questions[index];
            for (int i = 0; i < 3; i++)
            {
                answerButtons[i].GetComponentInChildren<TMP_Text>().text = "  " + (i + 1) + "    " + BlacksmithQuestContent.Answers[index][i];
                answerButtons[i].interactable = true;
                answerButtons[i].GetComponent<Image>().color = new Color(.12f, .22f, .27f);
            }
            feedback.color = Cream;
            feedback.text = quest.State.HasAnswer(index) ? "Craft completed. You can practise it again." : "Read the craft note, then put it into practice.";
            ShowModal(lessonPanel);
        }

        private void AnswerStation(int choice)
        {
            if (!quest.Ready || modal != lessonPanel || !answerButtons[choice].interactable) return;
            bool hadKey = quest.State.HasKey;
            bool correct = quest.State.Answer(activeStation, choice);
            quest.Save();
            if (correct)
            {
                feedback.color = Teal;
                feedback.text = quest.State.HasKey
                    ? "REPAIR KIT READY! Touch the glowing Forge Stone in the center to awaken the emerald portal."
                    : "CRAFT COMPLETE! Your repair kit gained a part. Explore the next station.";
                answerButtons[choice].GetComponent<Image>().color = new Color(.10f, .43f, .35f);
                foreach (var button in answerButtons) button.interactable = false;
                effectsAudio.PlayOneShot(!hadKey && quest.State.HasKey ? keySound : correctSound, .4f);
                CelebrateStation(activeStation);
                quest.RetrySync();
            }
            else
            {
                feedback.color = Gold;
                feedback.text = "Not quite. " + Hints[activeStation];
                answerButtons[choice].GetComponent<Image>().color = new Color(.4f, .20f, .16f);
                effectsAudio.PlayOneShot(errorSound, .18f);
            }
            RefreshProgress();
        }

        private void ShowHint()
        {
            showingHint = !showingHint;
            feedback.color = Gold;
            feedback.text = showingHint ? Hints[activeStation] : "Take your time. Read the craft note again when you need it.";
        }

        private void RefreshProgress()
        {
            if (quest == null || saveText == null) return;
            saveText.text = quest.Status;
            if (!quest.Ready) { objective.text = "Progress unavailable - open the journal"; return; }
            var state = quest.State;
            progressText.text = state.CompletedCount + " / 3  CRAFTS MASTERED";
            objective.text = state.portalOpened ? "Walk through the emerald portal" : state.HasKey ? "Touch the Forge Stone" : "Build the Aurora's repair kit";
            keyText.text = state.portalOpened ? "PORTAL  /  OPEN" : state.HasKey ? "FORGE STONE  /  READY" : "FORGE STONE  /  LOCKED";
            keyText.color = state.HasKey ? Teal : Gold;
            for (int i = 0; i < 3; i++)
            {
                bool done = state.HasAnswer(i);
                checklist[i].text = (done ? "DONE   " : "0" + (i + 1) + "       ") + CraftNames[i];
                checklist[i].color = done ? Teal : Cream;
                segments[i].color = done ? Teal : new Color(.2f, .28f, .32f);
                stationLabels[i].text = "0" + (i + 1) + "  " + StationNames[i] + (done ? "  /  COMPLETE" : "  /  EXPLORE");
                stationLabels[i].color = done ? Teal : Gold;
                stationRunes[i].sharedMaterial = done ? runeComplete : runeIdle;
                foreach (var renderer in stationHalos[i].GetComponentsInChildren<Renderer>())
                    renderer.sharedMaterial = done ? runeComplete : runeIdle;
                repairParts[i].SetActive(done);
            }
            forgeReward.RestoreProgress(state.CompletedCount, state.portalOpened);
            portalLabel.text = state.portalOpened ? "THE AURORA\nWALK THROUGH TO RETURN" : "THE AURORA\nAWAKEN THE FORGE STONE";
            journalCopy.text = "THE AURORA'S REPAIR\n\nA storm has damaged the ship. Master each craft to assemble a repair kit and forge your key home.\n\n" +
                state.CompletedCount + " of 3 crafts complete. " + (state.portalOpened ? "The emerald portal is open. Walk through to return." : state.HasKey ? "Touch the glowing Forge Stone to awaken the portal." : "Explore the anvil, workbench and forge in any order.") +
                "\n\n" + (quest.IsPractice ? "Practice progress is saved on this PC. No wallet rewards are awarded." : "Class progress is saved on this PC and submitted for cloud verification.");
        }

        private void ShowCompletion()
        {
            completionCopy.text = "The hull brace is shaped, twelve fasteners are counted, and the metal is ready.\n\nThe Forge Stone has awakened the emerald portal. Walk through it to bring your repair kit to the Aurora.\n\n" +
                (quest.IsPractice ? "All three crafts complete. Practice progress saved on this PC." : "All three crafts complete. Rewards require server verification.");
            ShowModal(completionPanel);
        }

        private void ReturnToShip(bool complete)
        {
            if (returning) return;
            if (!Application.CanStreamedLevelBeLoaded("QuestArena"))
            {
                saveText.text = "The ship scene is unavailable. Your progress is preserved; stay in the workshop.";
                return;
            }
            if (complete && (!quest.Ready || !quest.State.TurnIn())) return;
            if (quest.Ready && !quest.Save()) { RefreshProgress(); ShowModal(pausePanel); return; }
            if (complete) quest.RetrySync();
            returning = true;
            // Pending events are checkpointed and replayed on the next workshop visit.
            SceneTransition.LoadScene("QuestArena");
        }

        private void BuildInterface()
        {
            if (EventSystem.current == null)
                new GameObject("Workshop Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var canvas = new GameObject("Workshop UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 1;
            hud = canvas.transform;
            var mission = Panel("Mission HUD", hud, new Vector2(0, 1), new Vector2(26, -26), new Vector2(390, 262), Ink);
            Label(mission, "IMAGINE QUEST   /   WORKSHOP 01", 22, 18, 346, 25, 15, Gold);
            objective = Label(mission, "Build the Aurora's repair kit", 22, 53, 346, 50, 25, Cream);
            objective.fontStyle = FontStyles.Bold;
            progressText = Label(mission, "0 / 3  CRAFTS MASTERED", 22, 110, 346, 25, 15, Gold);
            for (int i = 0; i < 3; i++)
            {
                segments.Add(Panel("Progress segment", mission, Vector2.up, new Vector2(22 + i * 116, -144), new Vector2(108, 4), Teal).GetComponent<Image>());
                checklist.Add(Label(mission, "", 22, 163 + i * 27, 346, 25, 17, Cream));
            }
            var key = Panel("Key HUD", hud, Vector2.one, new Vector2(-26, -26), new Vector2(290, 78), Ink);
            keyText = Label(key, "RETURN KEY  /  LOCKED", 18, 14, 258, 27, 18, Gold);
            Label(key, "Three crafts. Touch the stone.", 18, 44, 258, 23, 15, Cream);
            var footer = Panel("Controls backing", hud, Vector2.zero, new Vector2(0, 0), new Vector2(1600, 52), Ink);
            Label(footer, "WASD  Move    SHIFT  Walk faster    MOUSE  Look    E  Interact    J  Journal    ESC  Menu", 26, 15, 1200, 28, 16, Cream);
            saveText = Text(hud, "Save status", new Vector2(1, 0), new Vector2(-26, 65), new Vector2(780, 30), 15, Teal, TextAlignmentOptions.Right);
            prompt = Text(hud, "Interaction prompt", new Vector2(.5f, 0), new Vector2(0, 112), new Vector2(1100, 36), 22, Gold, TextAlignmentOptions.Center);
            Text(hud, "Reticle", new Vector2(.5f, .5f), Vector2.zero, new Vector2(22, 22), 24, Cream, TextAlignmentOptions.Center).text = "+";

            briefingPanel = Overlay("Captain's commission", out Transform brief, new Vector2(820, 580));
            Label(brief, "CAPTAIN ESME'S COMMISSION", 48, 38, 724, 30, 17, Gold);
            var heading = Label(brief, "The Aurora's repair", 48, 90, 724, 72, 44, Cream);
            heading.fontStyle = FontStyles.Bold;
            Label(brief, "A storm has torn through the Aurora. Before the next voyage, the captain needs a repair kit from this workshop.", 48, 182, 724, 90, 24, Cream);
            Label(brief, "01   Learn at the anvil, workbench and forge.\n02   Complete all three crafts, then touch the Forge Stone.\n03   Walk through the emerald portal to return to the ship.", 48, 300, 724, 125, 22, Cream);
            Button(brief, "ENTER THE WORKSHOP", 48, 478, 450, 58, () => {
                if (quest.Ready) { quest.State.introSeen = true; quest.Save(); }
                ShowModal(null);
            });

            lessonPanel = Overlay("Craft lesson", out Transform lesson, new Vector2(1040, 650));
            lessonTitle = Label(lesson, "CRAFT NOTE", 40, 32, 950, 40, 28, Gold);
            var note = Panel("Craft note", lesson, Vector2.up, new Vector2(40, -100), new Vector2(340, 390), new Color(.10f, .18f, .22f));
            Label(note, "THE CRAFT", 24, 24, 292, 30, 17, Gold);
            lessonCopy = Label(note, "", 24, 80, 292, 215, 25, Cream);
            Button(note, "H  /  SHOW A HINT", 24, 315, 292, 50, ShowHint);
            Label(lesson, "PUT IT INTO PRACTICE", 416, 102, 580, 30, 17, Teal);
            questionText = Label(lesson, "", 416, 150, 580, 106, 27, Cream);
            for (int i = 0; i < 3; i++)
            {
                int choice = i;
                answerButtons.Add(Button(lesson, "Answer", 416, 280 + i * 70, 580, 56, () => AnswerStation(choice)));
            }
            feedback = Label(lesson, "", 40, 510, 960, 72, 21, Gold);
            Button(lesson, "BACK TO THE WORKSHOP", 40, 590, 420, 42, () => ShowModal(null));
            Label(lesson, "1 / 2 / 3   Answer       ESC   Close", 520, 598, 480, 25, 16, Cream);

            pausePanel = Overlay("Workshop journal", out Transform journal, new Vector2(820, 650));
            Label(journal, "YOUR WORKSHOP JOURNAL", 44, 36, 732, 40, 28, Gold);
            journalCopy = Label(journal, "", 44, 105, 732, 320, 23, Cream);
            Button(journal, "CONTINUE EXPLORING", 44, 452, 732, 54, () => ShowModal(null));
            Button(journal, "RETRY SAVE / CLOUD SYNC", 44, 523, 352, 48, () => quest.RetrySync());
            Button(journal, "LEAVE FOR SHIP", 420, 523, 356, 48, () => ReturnToShip(false));
            Label(journal, "Leaving early keeps your progress. Return later to finish the kit.", 44, 590, 732, 32, 17, Cream);

            completionPanel = Overlay("Repair complete", out Transform complete, new Vector2(820, 570));
            Label(complete, "RETURN KEY FORGED", 44, 36, 732, 32, 18, Teal);
            var doneTitle = Label(complete, "Ready for the next voyage.", 44, 95, 732, 110, 39, Cream);
            doneTitle.fontStyle = FontStyles.Bold;
            completionCopy = Label(complete, "", 44, 220, 732, 208, 23, Cream);
            Button(complete, "DELIVER THE KIT & RETURN", 44, 466, 500, 58, () => ReturnToShip(true));
            Button(complete, "STAY", 570, 466, 206, 58, () => ShowModal(null));
        }

        private static Transform Panel(string name, Transform parent, Vector2 anchor, Vector2 offset, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = anchor;
            r.anchoredPosition = offset; r.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            go.GetComponent<Image>().raycastTarget = false;
            return go.transform;
        }

        private static TMP_Text Text(Transform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size, int fontSize, Color color, TextAlignmentOptions alignment)
        {
            return CelestialClockQuestBootstrap.CreateText(name, parent, anchor, anchor, offset, size, fontSize, alignment, color);
        }

        private static TMP_Text Label(Transform parent, string copy, float x, float y, float w, float h, int size, Color color)
        {
            var text = Text(parent, copy.Length > 40 ? "Copy" : copy, Vector2.up, new Vector2(x, -y), new Vector2(w, h), size, color, TextAlignmentOptions.TopLeft);
            text.text = copy;
            return text;
        }

        private static Button Button(Transform parent, string label, float x, float y, float width, float height, UnityEngine.Events.UnityAction action)
        {
            var button = CelestialClockQuestBootstrap.CreateButton(label, parent, label, Vector2.up, Vector2.up,
                new Vector2(x, -y), new Vector2(width, height), new Color(.12f, .22f, .27f));
            button.onClick.AddListener(action);
            return button;
        }

        private GameObject Overlay(string name, out Transform card, Vector2 size)
        {
            var overlay = CelestialClockQuestBootstrap.CreateFullOverlay(name, hud, new Color(.008f, .018f, .03f, .78f));
            card = Panel(name + " card", overlay.transform, new Vector2(.5f, .5f), Vector2.zero, size, Ink);
            Panel("Gold rule", card, Vector2.up, Vector2.zero, new Vector2(size.x, 4), Gold);
            overlay.SetActive(false);
            return overlay;
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus && pausePanel != null && modal == null && !returning) ShowModal(pausePanel);
        }

        private void OnDestroy()
        {
            if (quest != null) quest.Changed -= RefreshProgress;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            foreach (var material in materials) Destroy(material);
            foreach (var mesh in meshes) Destroy(mesh);
            foreach (var clip in generatedAudio) Destroy(clip);
            if (workshopPipeline != null && QualitySettings.renderPipeline == workshopPipeline)
                QualitySettings.renderPipeline = previousPipeline;
        }
    }
}
