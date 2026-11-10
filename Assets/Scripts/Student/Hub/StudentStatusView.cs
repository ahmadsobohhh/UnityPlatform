using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ImagineQuest.Student
{
    public static class StatusUi
    {
        public static readonly Color Ink = new Color(.035f, .055f, .085f, .99f);
        public static readonly Color Card = new Color(.075f, .105f, .145f, .98f);
        public static readonly Color Cream = new Color(.95f, .94f, .89f);
        public static readonly Color Muted = new Color(.62f, .69f, .75f);
        public static readonly Color Gold = new Color(.97f, .75f, .37f);
        public static readonly Color Teal = new Color(.40f, .85f, .75f);
        public static readonly Color Rose = new Color(.96f, .43f, .49f);
        public static RectTransform Rect(string name, Transform parent, float x, float y, float right, float top)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = new Vector2(x, y); rect.anchorMax = new Vector2(right, top);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
        public static RectTransform Panel(string name, Transform parent, float x, float y, float right, float top, Color color, bool blocks = false)
        {
            var rect = Rect(name, parent, x, y, right, top);
            var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = blocks;
            return rect;
        }
        public static TMP_Text Label(string name, Transform parent, string value, float x, float y, float right, float top, int size, Color? color = null)
        {
            var label = Rect(name, parent, x, y, right, top).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = value; label.fontSize = size; label.color = color ?? Cream; label.raycastTarget = false;
            label.richText = false; label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }
        public static Button Button(string name, Transform parent, string value, float x, float y, float right, float top, Action clicked)
        {
            var panel = Panel(name, parent, x, y, right, top, new Color(.13f, .23f, .28f), true);
            var button = panel.gameObject.AddComponent<Button>(); button.targetGraphic = panel.GetComponent<Image>();
            var colors = button.colors; colors.highlightedColor = new Color(1.2f, 1.2f, 1.15f); colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(.72f, .85f, .83f); colors.disabledColor = new Color(.5f, .5f, .5f, .6f); button.colors = colors;
            var label = Label("Label", panel, value, .06f, .08f, .94f, .92f, 19); label.alignment = TextAlignmentOptions.Center;
            button.onClick.AddListener(() => clicked?.Invoke()); return button;
        }
    }

    /// <summary>Quiet HUD plus a personal recap, shared by the hub, ship and workshop.</summary>
    public sealed class StudentStatusView : MonoBehaviour
    {
        private StudentStatusService live;
        private RectTransform widget, recap;
        private TMP_Text hudHearts, hudXp, hudXpCaption, heartTotal, xpTotal, sessionXp, connection, historyStatus, toast;
        private readonly List<TMP_Text> activityTitle = new List<TMP_Text>(), activityDetail = new List<TMP_Text>(), activityDelta = new List<TMP_Text>();
        private Image heartFill;
        private long previousSession;
        private float toastUntil;
        private Button closeButton;
        private Button reloadButton;
        private GameObject previousSelection;
        public bool IsOpen => recap != null && recap.gameObject.activeSelf;

        public static StudentStatusView Attach(Transform canvas, bool showHud, bool connect = true)
        {
            var view = canvas.gameObject.GetComponent<StudentStatusView>() ?? canvas.gameObject.AddComponent<StudentStatusView>();
            if (view.recap != null) return view;
            view.Build(canvas, showHud);
            if (connect)
            {
                view.live = StudentStatusService.GetOrCreate(); view.previousSession = view.live.SessionXp;
                view.live.Changed += view.Refresh; view.Refresh();
            }
            return view;
        }
        public void OpenRecap()
        {
            if (!IsOpen && EventSystem.current != null) previousSelection = EventSystem.current.currentSelectedGameObject;
            if (widget != null) widget.gameObject.SetActive(false);
            recap.gameObject.SetActive(true); recap.SetAsLastSibling(); Refresh();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }
        public void CloseRecap()
        {
            recap.gameObject.SetActive(false); if (widget != null) widget.gameObject.SetActive(true);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
        }
        private void Refresh()
        {
            if (live == null) return;
            Render(live.Snapshot, live.Connection, live.SessionXp, live.Activity, live.HistoryStatus);
        }
        public void Render(StudentStatusSnapshot snapshot, string state, long earned, IReadOnlyList<StudentActivity> activity, string history)
        {
            bool ready = snapshot != null;
            bool isLive = ready && !snapshot.IsCached && state.StartsWith("Live", StringComparison.Ordinal);
            bool isLocal = ready && state.StartsWith("Local", StringComparison.Ordinal);
            string hearts = ready ? snapshot.Hearts.ToString("N0", CultureInfo.CurrentCulture) : "—";
            string xp = ready ? snapshot.Xp.ToString("N0", CultureInfo.CurrentCulture) : "—";
            if (hudHearts != null) { hudHearts.text = hearts; hudXp.text = xp; }
            if (hudXpCaption != null) hudXpCaption.text = isLocal ? "LOCAL XP" : !ready || isLive ? "TOTAL XP" : "XP · OFFLINE";
            heartTotal.text = hearts + (ready ? " / 50" : ""); xpTotal.text = xp;
            heartFill.fillAmount = ready ? snapshot.Hearts / 50f : 0;
            heartFill.color = ready && snapshot.Hearts <= 10 ? StatusUi.Rose : StatusUi.Teal;
            sessionXp.text = ready ? "+" + earned.ToString("N0") + " XP earned this visit" : "Your saved progress will appear here";
            connection.text = state; connection.color = isLive || isLocal ? StatusUi.Teal : StatusUi.Muted;
            reloadButton.GetComponentInChildren<TMP_Text>().text = isLocal ? "RELOAD SAVE" : "RECONNECT";
            historyStatus.text = history;
            for (int i = 0; i < activityTitle.Count; i++)
            {
                bool present = activity != null && i < activity.Count;
                activityTitle[i].transform.parent.gameObject.SetActive(present);
                if (!present) continue;
                var item = activity[i];
                activityTitle[i].text = item.Title;
                activityDetail[i].text = (item.Time.HasValue ? item.Time.Value.ToLocalTime().ToString("MMM d · h:mm tt") + "  ·  " : "") + item.Detail;
                activityDelta[i].text = item.Hearts != 0 ? Signed(item.Hearts) + (item.Hearts == 1 || item.Hearts == -1 ? " Heart" : " Hearts") : Signed(item.Xp) + " XP";
                activityDelta[i].color = item.Hearts < 0 || item.Xp < 0 ? StatusUi.Rose : StatusUi.Teal;
            }
            if (toast != null && earned > previousSession)
            { toast.text = "+" + (earned - previousSession).ToString("N0") + " XP  •  SAVED"; toastUntil = Time.unscaledTime + 3.5f; }
            previousSession = earned;
        }
        private static string Signed(long value) => (value > 0 ? "+" : "") + value.ToString("N0");
        private void Build(Transform canvas, bool showHud)
        {
            if (showHud)
            {
                widget = StatusUi.Panel("Live Hearts and XP", canvas, .5f, 1, .5f, 1, StatusUi.Ink);
                widget.pivot = new Vector2(.5f, 1); widget.sizeDelta = new Vector2(360, 70); widget.anchoredPosition = new Vector2(0, -24);
                widget.SetAsFirstSibling();
                Glyph(widget, true, .045f, .25f, .12f, .73f);
                hudHearts = StatusUi.Label("Hearts", widget, "—", .15f, .31f, .39f, .84f, 27);
                StatusUi.Label("Hearts caption", widget, "HEARTS", .15f, .10f, .39f, .38f, 11, StatusUi.Muted);
                StatusUi.Panel("Divider", widget, .45f, .23f, .453f, .77f, new Color(.25f, .30f, .34f));
                Glyph(widget, false, .50f, .27f, .575f, .72f);
                hudXp = StatusUi.Label("Saved XP", widget, "—", .61f, .31f, .96f, .84f, 27);
                hudXpCaption = StatusUi.Label("XP caption", widget, "TOTAL XP", .61f, .10f, .96f, .38f, 11, StatusUi.Muted);
                toast = StatusUi.Label("XP earned toast", widget, "", -.05f, -.47f, 1.05f, -.05f, 17, StatusUi.Gold);
                toast.alignment = TextAlignmentOptions.Center;
            }
            recap = StatusUi.Panel("Personal recap", canvas, 0, 0, 1, 1, new Color(.008f, .014f, .025f, .91f), true);
            var card = StatusUi.Panel("Recap card", recap, .13f, .09f, .87f, .91f, StatusUi.Ink);
            StatusUi.Panel("Accent", card, 0, .995f, 1, 1, StatusUi.Gold);
            StatusUi.Label("Eyebrow", card, "YOUR JOURNEY", .04f, .91f, .70f, .965f, 14, StatusUi.Gold);
            StatusUi.Label("Title", card, "Personal recap", .04f, .83f, .79f, .915f, 36).fontStyle = FontStyles.Bold;
            closeButton = StatusUi.Button("Close recap", card, "CLOSE", .83f, .86f, .96f, .95f, CloseRecap);
            var heart = StatusUi.Panel("Classroom standing", card, .04f, .55f, .48f, .80f, StatusUi.Card);
            Glyph(heart, true, .045f, .47f, .13f, .77f);
            StatusUi.Label("Heart label", heart, "CLASSROOM HEARTS", .17f, .71f, .94f, .91f, 14, StatusUi.Muted);
            heartTotal = StatusUi.Label("Heart total", heart, "—", .17f, .32f, .95f, .72f, 40);
            StatusUi.Label("Heart description", heart, "Your teacher manages classroom standing.", .06f, .11f, .94f, .28f, 15, StatusUi.Muted);
            var track = StatusUi.Panel("Heart track", heart, .06f, .06f, .94f, .08f, new Color(.18f, .23f, .27f));
            heartFill = StatusUi.Panel("Heart fill", track, 0, 0, 1, 1, StatusUi.Teal).GetComponent<Image>();
            // A built-in white sprite is needed for Unity's Filled image geometry.
            heartFill.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f);
            heartFill.type = Image.Type.Filled; heartFill.fillMethod = Image.FillMethod.Horizontal; heartFill.fillOrigin = 0;
            var experience = StatusUi.Panel("Experience", card, .51f, .55f, .96f, .80f, StatusUi.Card);
            Glyph(experience, false, .045f, .47f, .13f, .77f);
            StatusUi.Label("XP label", experience, "TOTAL EXPERIENCE", .17f, .71f, .94f, .91f, 14, StatusUi.Muted);
            xpTotal = StatusUi.Label("Experience total", experience, "—", .17f, .32f, .95f, .72f, 40);
            sessionXp = StatusUi.Label("Visit XP", experience, "", .06f, .09f, .94f, .28f, 16, StatusUi.Gold);
            var historyPanel = StatusUi.Rect("Activity list", card, .04f, .13f, .96f, .51f);
            StatusUi.Label("Activity heading", historyPanel, "RECENT ACTIVITY", 0, .90f, .6f, 1, 15, StatusUi.Gold);
            historyStatus = StatusUi.Label("Activity empty state", historyPanel, "Loading recent activity...", 0, .78f, 1, .90f, 13, StatusUi.Muted);
            for (int i = 0; i < 6; i++)
            {
                float bottom = .65f - i * .127f;
                var row = StatusUi.Rect("Activity " + i, historyPanel, 0, bottom, 1, bottom + .12f);
                StatusUi.Panel("Row rule", row, 0, 0, 1, .015f, new Color(.15f, .20f, .24f));
                activityTitle.Add(StatusUi.Label("Activity title", row, "", 0, .36f, .75f, .98f, 17));
                activityDetail.Add(StatusUi.Label("Activity detail", row, "", 0, .02f, .78f, .42f, 12, StatusUi.Muted));
                var delta = StatusUi.Label("Activity change", row, "", .80f, .15f, 1, .95f, 18, StatusUi.Teal);
                delta.alignment = TextAlignmentOptions.MidlineRight; activityDelta.Add(delta); row.gameObject.SetActive(false);
            }
            reloadButton = StatusUi.Button("Retry connection", card, "RECONNECT", .04f, .035f, .25f, .105f, () => live?.Reconnect());
            connection = StatusUi.Label("Cloud connection", card, "Connecting your progress...", .29f, .035f, .96f, .105f, 15, StatusUi.Muted);
            recap.gameObject.SetActive(false);
        }
        private static void Glyph(Transform parent, bool heart, float x, float y, float right, float top)
        {
            var rect = StatusUi.Rect(heart ? "Heart icon" : "XP icon", parent, x, y, right, top);
            var image = rect.gameObject.AddComponent<StatusGlyph>(); image.Heart = heart;
            image.color = heart ? StatusUi.Rose : StatusUi.Gold; image.raycastTarget = false;
        }
        private void Update() { if (toast != null && Time.unscaledTime > toastUntil) toast.text = ""; }
        private void OnDestroy()
        {
            if (live != null) live.Changed -= Refresh;
            if (heartFill != null && heartFill.sprite != null) Destroy(heartFill.sprite);
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StatusGlyph : MaskableGraphic
    {
        public bool Heart;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); var rect = rectTransform.rect;
            float size = Mathf.Min(rect.width, rect.height); Vector2 center = rect.center;
            mesh.AddVert(center, color, Vector2.zero);
            int count = Heart ? 48 : 10;
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2 / count;
                Vector2 point;
                if (Heart) point = new Vector2(16 * Mathf.Pow(Mathf.Sin(angle), 3), 13 * Mathf.Cos(angle) - 5 * Mathf.Cos(2 * angle) - 2 * Mathf.Cos(3 * angle) - Mathf.Cos(4 * angle)) * (size / 34);
                else point = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * (size * (i % 2 == 0 ? .48f : .23f));
                mesh.AddVert(center + point, color, Vector2.zero);
            }
            for (int i = 0; i < count; i++) mesh.AddTriangle(0, i + 1, (i + 1) % count + 1);
        }
    }
}
