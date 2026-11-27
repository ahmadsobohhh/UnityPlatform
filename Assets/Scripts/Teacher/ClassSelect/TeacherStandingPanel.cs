using System;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Firestore;
using ImagineQuest.Student;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ImagineQuest.Gameplay
{
    /// <summary>Class-owner-only Heart controls and assignment entry point.</summary>
    public sealed class TeacherStandingPanel : MonoBehaviour
    {
        [Serializable] private sealed class Target { public string classId, studentId; }
        [Serializable] private sealed class Change { public string classId, studentId, eventId, reason; public int delta; }
        [Serializable] private sealed class Balance { public int hearts; public long xp; }
        [Serializable] private sealed class Saved { public int hearts; public bool reset, alreadyProcessed; }
        [Serializable] private sealed class Assignment { public string classId; public bool isOpen; }
        [Serializable] private sealed class Assigned { public bool isOpen; }
        private RectTransform overlay;
        private TMP_Text heading, totals, changePreview, status, assignmentStatus, pageText;
        private TMP_InputField reason;
        private Button apply, previous, next, assignmentButton;
        private readonly List<Button> rows = new List<Button>();
        private readonly List<(string id, string name)> students = new List<(string, string)>();
        private string classId, owner, selected, selectedName, requestId;
        private int page, hearts, delta = -1, version;
        private bool busy, loaded, confirmReset, workshopOpen;

        private void Start()
        {
            var parent = QuestLobbyUi.FindSceneRect("StudentListPanel");
            StatusUi.Button("Open student standing", parent != null ? parent : transform, "HEARTS & PROGRESS",
                .65f, .025f, .96f, .105f, Open);
            Build();
        }
        private void Build()
        {
            overlay = StatusUi.Panel("Teacher standing", transform, 0, 0, 1, 1, new Color(.008f, .015f, .025f, .94f), true);
            var card = StatusUi.Panel("Standing card", overlay, .045f, .045f, .955f, .955f, StatusUi.Ink);
            StatusUi.Panel("Accent", card, 0, .995f, 1, 1, StatusUi.Gold);
            StatusUi.Label("Eyebrow", card, "YOUR CLASS  /  STUDENT STANDING", .035f, .925f, .77f, .98f, 14, StatusUi.Gold);
            StatusUi.Label("Title", card, "Hearts & progress", .035f, .84f, .78f, .925f, 35).fontStyle = FontStyles.Bold;
            StatusUi.Button("Close", card, "CLOSE", .83f, .885f, .965f, .965f, () => { ++version; overlay.gameObject.SetActive(false); });
            var roster = StatusUi.Panel("Roster", card, .035f, .23f, .32f, .81f, StatusUi.Card);
            StatusUi.Label("Roster heading", roster, "CHOOSE A STUDENT", .06f, .89f, .94f, .98f, 14, StatusUi.Muted);
            for (int i = 0; i < 6; i++)
            {
                int slot = i; float bottom = .75f - i * .125f;
                rows.Add(StatusUi.Button("Student " + i, roster, "", .04f, bottom, .96f, bottom + .105f, () => Select(page * 6 + slot)));
            }
            previous = StatusUi.Button("Previous page", card, "PREV", .035f, .14f, .125f, .205f, () => { --page; RenderRoster(); });
            next = StatusUi.Button("Next page", card, "NEXT", .225f, .14f, .32f, .205f, () => { ++page; RenderRoster(); });
            pageText = StatusUi.Label("Roster page", card, "", .135f, .14f, .215f, .205f, 15);
            heading = StatusUi.Label("Selected student", card, "Select a student", .36f, .72f, .96f, .81f, 30);
            totals = StatusUi.Label("Student totals", card, "Their saved Hearts and XP appear here.", .36f, .65f, .96f, .72f, 21, StatusUi.Teal);
            StatusUi.Label("Standing guidance", card, "Hearts reflect classroom standing. Quiz mistakes never deduct Hearts.", .36f, .57f, .96f, .64f, 17, StatusUi.Muted);
            var choices = new[] { -5, -1, 1, 5 };
            for (int i = 0; i < choices.Length; i++)
            {
                int amount = choices[i]; float left = .36f + i * .15f;
                StatusUi.Button("Adjust " + amount, card, (amount > 0 ? "+" : "") + amount + (Math.Abs(amount) == 1 ? " HEART" : " HEARTS"), left, .47f, left + .135f, .55f,
                    () => { if (busy || !loaded) return; delta = amount; requestId = null; confirmReset = false; RenderChange(); });
            }
            changePreview = StatusUi.Label("Change preview", card, "", .36f, .385f, .96f, .465f, 20, StatusUi.Gold);
            var input = StatusUi.Panel("Reason field", card, .36f, .295f, .96f, .375f, StatusUi.Card, true);
            input.gameObject.AddComponent<RectMask2D>();
            reason = input.gameObject.AddComponent<TMP_InputField>(); reason.characterLimit = 140;
            var inputText = StatusUi.Label("Reason text", input, "", .025f, .08f, .975f, .92f, 18); inputText.richText = false;
            var placeholder = StatusUi.Label("Reason placeholder", input, "Reason visible to the student (required)", .025f, .08f, .975f, .92f, 18, StatusUi.Muted);
            reason.textViewport = input; reason.textComponent = inputText; reason.placeholder = placeholder;
            reason.onValueChanged.AddListener(_ => { if (!busy) { requestId = null; confirmReset = false; RenderChange(); } });
            apply = StatusUi.Button("Save Hearts", card, "SAVE CHANGE", .36f, .19f, .96f, .275f, Save);
            status = StatusUi.Label("Teacher feedback", card, "", .36f, .09f, .96f, .18f, 17, StatusUi.Muted);
            StatusUi.Panel("Bottom rule", card, .035f, .12f, .32f, .122f, StatusUi.Gold);
            assignmentStatus = StatusUi.Label("Workshop assignment status", card, "Checking workshop assignment...", .035f, .035f, .69f, .095f, 16, StatusUi.Muted);
            assignmentButton = StatusUi.Button("Assign workshop", card, "ASSIGN WORKSHOP", .71f, .025f, .965f, .105f, ToggleAssignment);
            overlay.gameObject.SetActive(false);
        }
        private bool Current(int expected) => this != null && version == expected && FirebaseAuth.DefaultInstance.CurrentUser?.UserId == owner;
        private void Open() => OpenForStudent(null);

        public async void OpenForStudent(string studentId)
        {
            if (busy || overlay == null) return;
            overlay.gameObject.SetActive(true); overlay.SetAsLastSibling();
            classId = ClassSelection.CurrentClassId;
            if (string.IsNullOrWhiteSpace(classId)) classId = PlayerPrefs.GetString("SelectedClassId", "");
            owner = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            int expected = ++version; students.Clear(); loaded = false; selected = null; page = 0; heading.text = "Select a student"; totals.text = "Loading roster..."; RenderRoster(); RenderChange();
            assignmentButton.interactable = false;
            try
            {
                if (string.IsNullOrWhiteSpace(classId) || owner == null) throw new InvalidOperationException("Select your class first.");
                var db = FirebaseFirestore.DefaultInstance;
                var room = await db.Collection("classes").Document(classId).GetSnapshotAsync(Source.Server);
                if (!Current(expected)) return;
                if (!room.Exists || room.GetValue<string>("ownerUid") != owner) throw new InvalidOperationException("Only this class's teacher can manage standing.");
                var members = await room.Reference.Collection("members").GetSnapshotAsync(Source.Server);
                if (!Current(expected)) return;
                foreach (var member in members.Documents)
                {
                    if (member.Id == owner) continue;
                    string first = member.ContainsField("firstName") ? member.GetValue<string>("firstName") : "";
                    string last = member.ContainsField("lastName") ? member.GetValue<string>("lastName") : "";
                    string name = (first + " " + last).Trim();
                    foreach (string field in new[] { "displayName", "username", "name" })
                        if (member.ContainsField(field) && !string.IsNullOrWhiteSpace(member.GetValue<string>(field))) { name = member.GetValue<string>(field); break; }
                    if (string.IsNullOrWhiteSpace(name)) name = "Student";
                    students.Add((member.Id, name));
                }
                students.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.CurrentCultureIgnoreCase)); RenderRoster();
                if (!StudentCloudConfig.Enabled)
                {
                    RenderAssignment();
                    status.text = students.Count == 0 ? "No students have joined this class yet." : "Select a student to review this device's local standing.";
                    SelectRequested(studentId);
                    return;
                }
                var assignment = await room.Reference.Collection("assignments").Document("blacksmith-workshop").GetSnapshotAsync(Source.Server);
                if (!Current(expected)) return;
                workshopOpen = assignment.Exists && assignment.GetValue<string>("state") == "open"; RenderAssignment();
                status.text = students.Count == 0 ? "No students have joined this class yet." : "Select a student to review their saved standing.";
                SelectRequested(studentId);
            }
            catch (Exception ex) { if (Current(expected)) status.text = ex.Message; }
        }
        private void SelectRequested(string studentId)
        {
            if (string.IsNullOrEmpty(studentId)) return;
            int index = students.FindIndex(student => student.id == studentId);
            if (index < 0) { status.text = "Student is not in this class."; return; }
            page = index / 6;
            RenderRoster();
            Select(index);
        }

        private void RenderRoster()
        {
            int pages = Math.Max(1, (students.Count + 5) / 6); page = Mathf.Clamp(page, 0, pages - 1);
            for (int i = 0; i < rows.Count; i++)
            {
                int index = page * 6 + i; rows[i].gameObject.SetActive(index < students.Count);
                if (index < students.Count) { rows[i].GetComponentInChildren<TMP_Text>().text = students[index].name; rows[i].interactable = !busy; }
            }
            pageText.text = (page + 1) + " / " + pages; previous.interactable = page > 0 && !busy; next.interactable = page + 1 < pages && !busy;
        }
        private async void Select(int index)
        {
            if (busy || index >= students.Count) return;
            selected = students[index].id; selectedName = students[index].name; heading.text = selectedName;
            loaded = false; requestId = null; confirmReset = false; reason.text = ""; totals.text = "Loading saved standing..."; RenderChange();
            int expected = ++version;
            try
            {
                if (!StudentCloudConfig.Enabled)
                {
                    var local = new LocalStudentStatusStore(StudentStatusService.DefaultLocalDirectory).Load(selected);
                    hearts = local.hearts; totals.text = hearts + " / 50 Hearts    ·    " + local.xp.ToString("N0") + " local XP";
                    loaded = true; RenderChange();
                    status.text = "Local progress on this device. Cloud changes are not enabled.";
                    return;
                }
                var result = await GameCloudClient.CallAsync<Target, Balance>("getClassStudentStatus", new Target { classId = classId, studentId = selected });
                if (!Current(expected)) return;
                hearts = result.hearts; totals.text = hearts + " / 50 Hearts    ·    " + result.xp.ToString("N0") + " XP"; loaded = true; RenderChange();
            }
            catch (Exception ex) { if (Current(expected)) status.text = ex.Message; }
        }
        private void RenderChange()
        {
            if (apply == null) return;
            int after = hearts + delta;
            bool valid = loaded && after >= 0 && after <= 50;
            changePreview.text = !loaded ? "" : !valid ? "Choose a change that keeps Hearts between 0 and 50." : HeartCount(hearts) + "  →  " + HeartCount(after);
            status.text = valid && after == 0 ? "At zero Hearts, XP, Crystals and Gold reset and Powers are revoked. This cannot be undone by restoring Hearts." : "";
            apply.interactable = valid && !busy && !string.IsNullOrWhiteSpace(reason.text);
            apply.GetComponentInChildren<TMP_Text>().text = busy ? "SAVING..." : confirmReset ? "CONFIRM FRESH START" : "SAVE CHANGE";
        }
        private async void Save()
        {
            if (!loaded || busy || !apply.interactable) return;
            if (hearts + delta == 0 && !confirmReset) { confirmReset = true; RenderChange(); return; }
            requestId = requestId ?? Guid.NewGuid().ToString("N");
            var change = new Change { classId = classId, studentId = selected, eventId = requestId, reason = reason.text.Trim(), delta = delta };
            busy = true; reason.interactable = false; int expected = version; RenderChange(); RenderRoster();
            bool saved = false;
            try
            {
                if (!StudentCloudConfig.Enabled)
                {
                    var local = new LocalStudentStatusStore(StudentStatusService.DefaultLocalDirectory)
                        .AdjustHearts(selected, requestId, delta, change.reason, out _);
                    hearts = local.hearts;
                    totals.text = hearts + " / 50 Hearts    ·    " + local.xp.ToString("N0") + " local XP";
                    status.text = "Saved on this device. The student's local recap updates automatically.";
                }
                else
                {
                    var result = await GameCloudClient.CallAsync<Change, Saved>("adjustStudentHearts", change);
                    if (!Current(expected)) return;
                    hearts = result.hearts; totals.text = hearts + " / 50 Hearts";
                    status.text = "Saved. " + selectedName + " sees the update automatically.";
                }
                requestId = null; confirmReset = false;
                saved = true;
            }
            catch (Exception ex) { if (Current(expected)) status.text = ex.Message + " Retry uses the same request safely."; }
            finally
            {
                if (this != null)
                {
                    busy = false; reason.interactable = true;
                    apply.interactable = !saved && loaded && Current(expected);
                    apply.GetComponentInChildren<TMP_Text>().text = saved ? "SAVED" : "RETRY CHANGE";
                    RenderRoster();
                }
            }
        }
        private static string HeartCount(int value) => value + (value == 1 ? " Heart" : " Hearts");
        private void RenderAssignment()
        {
            if (!StudentCloudConfig.Enabled)
            {
                assignmentStatus.text = "Local workshop practice is available. Cloud assignments are not enabled.";
                assignmentButton.GetComponentInChildren<TMP_Text>().text = "LOCAL PRACTICE";
                assignmentButton.interactable = false;
                return;
            }
            assignmentStatus.text = workshopOpen ? "The Aurora's Repair is assigned • 25 XP per craft + 100 XP for delivery" : "Assign the workshop so enrolled students can earn verified XP.";
            assignmentButton.GetComponentInChildren<TMP_Text>().text = workshopOpen ? "CLOSE WORKSHOP" : "ASSIGN WORKSHOP";
            assignmentButton.interactable = true;
        }
        private async void ToggleAssignment()
        {
            if (!StudentCloudConfig.Enabled) { RenderAssignment(); return; }
            assignmentButton.interactable = false; int expected = version;
            try
            {
                var saved = await GameCloudClient.CallAsync<Assignment, Assigned>("setWorkshopAssignment", new Assignment { classId = classId, isOpen = !workshopOpen });
                if (!Current(expected)) return; workshopOpen = saved.isOpen; RenderAssignment();
            }
            catch (Exception ex) { if (Current(expected)) { assignmentStatus.text = ex.Message; assignmentButton.interactable = true; } }
        }
    }
}
