using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class TeacherClassManagerTests
{
    // ---------- Reflection helpers ----------
    private static void CallPrivate(object target, string methodName, params object[] args)
    {
        var mi = target.GetType().GetMethod(methodName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(mi, $"Missing private method {methodName}");
        mi.Invoke(target, args);
    }

    private static T GetPrivate<T>(object target, string fieldName)
    {
        var fi = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(fi, $"Missing private field {fieldName}");
        return (T)fi.GetValue(target);
    }

    private static void SetPrivate<T>(object target, string fieldName, T value)
    {
        var fi = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(fi, $"Missing private field {fieldName}");
        fi.SetValue(target, value);
    }

    private static object NewClassRow(string id, string name, string code, long createdAtSeconds)
    {
        var rowType = typeof(TeacherClassManager).GetNestedType("ClassRow", BindingFlags.NonPublic);
        var row = Activator.CreateInstance(rowType);
        rowType.GetField("id").SetValue(row, id);
        rowType.GetField("name").SetValue(row, name);
        rowType.GetField("code").SetValue(row, code);
        rowType.GetField("createdAtSeconds").SetValue(row, createdAtSeconds);
        return row;
    }

    // ---------- Test rig ----------
    private static GameObject MakeListItemPrefab()
    {
        var prefab = new GameObject("ClassItem", typeof(RectTransform), typeof(Image), typeof(Button));

        var nameGO = new GameObject("ClassName", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameGO.transform.SetParent(prefab.transform);

        var codeGO = new GameObject("ClassCode", typeof(RectTransform), typeof(TextMeshProUGUI));
        codeGO.transform.SetParent(prefab.transform);

        return prefab;
    }

    private static TeacherClassManager MakeManager(int pageSize = 2)
    {
        var root = new GameObject("TeacherClassManager");
        var mgr = root.AddComponent<TeacherClassManager>();

        // Panels
        var listPanel = new GameObject("ListPanel");
        var editPanel = new GameObject("EditPanel");
        var createPanel = new GameObject("CreatePanel");
        listPanel.SetActive(true); editPanel.SetActive(false); createPanel.SetActive(false);

        // List container + empty graphic
        var containerGO = new GameObject("ClassListContainer", typeof(RectTransform));
        var emptyGO = new GameObject("EmptyGraphic");

        // Pagination
        var prevBtn = new GameObject("PrevBtn", typeof(Button)).GetComponent<Button>();
        var nextBtn = new GameObject("NextBtn", typeof(Button)).GetComponent<Button>();
        var pageLbl = new GameObject("Page", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();

        // Create panel controls
        var createName = new GameObject("CreateName", typeof(TMP_InputField)).GetComponent<TMP_InputField>();
        var createOk = new GameObject("CreateOK", typeof(Button)).GetComponent<Button>();

        // Edit panel controls
        var editTitle = new GameObject("EditTitle", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        var editName = new GameObject("EditName", typeof(TMP_InputField)).GetComponent<TMP_InputField>();
        var editOk = new GameObject("EditOK", typeof(Button)).GetComponent<Button>();
        var delOk = new GameObject("DeleteOK", typeof(Button)).GetComponent<Button>();

        // List panel buttons
        var joinBtn = new GameObject("JoinBtn", typeof(Button)).GetComponent<Button>();
        var createBtn = new GameObject("CreateBtn", typeof(Button)).GetComponent<Button>();
        var editBtn = new GameObject("EditBtn", typeof(Button)).GetComponent<Button>();

        // Prefab for list items
        var itemPrefab = MakeListItemPrefab();

        // Wire serialized fields via reflection
        void Set<T>(string name, T v) => SetPrivate(mgr, name, v);
        Set("classListPanel", listPanel);
        Set("editPanel", editPanel);
        Set("createClassPanel", createPanel);

        Set("classListContainer", containerGO.transform);
        Set("classListItemPrefab", itemPrefab);
        Set("emptyListGraphic", emptyGO);

        Set("prevPageBtn", prevBtn);
        Set("nextPageBtn", nextBtn);
        Set("pageLabel", pageLbl);
        Set("pageSize", pageSize);

        Set("createClassNameInput", createName);
        Set("createConfirmBtn", createOk);
        Set("codeLength", 6);

        Set("editTitleLabel", editTitle);
        Set("editNameInput", editName);
        Set("editConfirmBtn", editOk);
        Set("deleteConfirmBtn", delOk);

        Set("joinBtn", joinBtn);
        Set("createBtn", createBtn);
        Set("editBtn", editBtn);

        // Nice visible colors for assertions if needed
        Set("unselectedColor", Color.white);
        Set("selectedColor", new Color(0.8f, 0.9f, 1f, 1f));

        return mgr;
    }

    // ---------- Tests ----------
    [UnityTest]
    public IEnumerator RenderPage_Empty_ShowsEmptyGraphic_And_PageLabel()
    {
        var mgr = MakeManager(pageSize: 3);

        // Ensure _all is empty and render
        var all = GetPrivate<IList>(mgr, "_all");
        all.Clear();

        CallPrivate(mgr, "RenderPage");
        yield return null;

        var empty = GetPrivate<GameObject>(mgr, "emptyListGraphic");
        Assert.IsTrue(empty.activeSelf, "Empty list graphic should be visible when there are no classes.");

        var container = GetPrivate<Transform>(mgr, "classListContainer");
        Assert.AreEqual(0, container.childCount, "No list items should be instantiated.");

        var pageLbl = GetPrivate<TMP_Text>(mgr, "pageLabel");
        Assert.IsTrue(pageLbl.text.Contains("Page 1 of 1"), "Page label should show 1 of 1.");
    }

    [UnityTest]
    public IEnumerator Pagination_And_Selection_Tints_Items_And_Shows_Buttons()
    {
        var mgr = MakeManager(pageSize: 2);

        // Build three fake rows (sorted newest first by createdAtSeconds)
        var all = GetPrivate<IList>(mgr, "_all");
        all.Clear();
        all.Add(NewClassRow("c1", "Math", "ABC123", 100));
        all.Add(NewClassRow("c2", "Sci", "XYZ789", 90));
        all.Add(NewClassRow("c3", "Hist", "H1S7", 80));

        // First render -> page 1: two items
        CallPrivate(mgr, "RenderPage");
        yield return null;

        var container = GetPrivate<Transform>(mgr, "classListContainer");
        Assert.AreEqual(2, container.childCount, "First page should show 2 items (pageSize=2).");

        // Click the first item to select it
        var firstBtn = container.GetChild(0).GetComponent<Button>();
        firstBtn.onClick.Invoke();
        yield return null;

        // Buttons should now be visible due to selection
        var joinBtn = GetPrivate<Button>(mgr, "joinBtn");
        var editBtn = GetPrivate<Button>(mgr, "editBtn");
        Assert.IsTrue(joinBtn.gameObject.activeSelf, "Join button should be visible after a selection.");
        Assert.IsTrue(editBtn.gameObject.activeSelf, "Edit button should be visible after a selection.");

        // First item should be tinted as selected
        var selColor = GetPrivate<Color>(mgr, "selectedColor");
        var unsel = GetPrivate<Color>(mgr, "unselectedColor");
        var firstImg = container.GetChild(0).GetComponent<Image>().color;
        var secondImg = container.GetChild(1).GetComponent<Image>().color;
        Assert.AreEqual(selColor, firstImg);
        Assert.AreEqual(unsel, secondImg);

        // Next page -> should show the remaining 1 item
        var nextBtn = GetPrivate<Button>(mgr, "nextPageBtn");
        nextBtn.onClick.Invoke(); // calls NextPage() via listener set in Start, but Start is private
        // In case Start wasn't called, call NextPage directly:
        CallPrivate(mgr, "NextPage");
        yield return null;

        container = GetPrivate<Transform>(mgr, "classListContainer");
        Assert.AreEqual(1, container.childCount, "Second page should show last remaining item.");
    }

    [UnityTest]
    public IEnumerator ShowCreate_And_ShowEdit_Toggle_Panels_And_Prefill_Text()
    {
        var mgr = MakeManager();

        // Add one row and select it
        var all = GetPrivate<IList>(mgr, "_all");
        all.Clear();
        all.Add(NewClassRow("c42", "Robotics", "ROB007", 123));

        CallPrivate(mgr, "RenderPage");
        yield return null;

        // Select the single card
        var container = GetPrivate<Transform>(mgr, "classListContainer");
        container.GetChild(0).GetComponent<Button>().onClick.Invoke();
        yield return null;

        // ShowEditPanel -> should display edit panel and prefill
        CallPrivate(mgr, "ShowEditPanel");
        yield return null;

        var editPanel = GetPrivate<GameObject>(mgr, "editPanel");
        var listPanel = GetPrivate<GameObject>(mgr, "classListPanel");
        var createPanel = GetPrivate<GameObject>(mgr, "createClassPanel");
        var editTitle = GetPrivate<TMP_Text>(mgr, "editTitleLabel");
        var editName = GetPrivate<TMP_InputField>(mgr, "editNameInput");

        Assert.IsTrue(editPanel.activeSelf);
        Assert.IsFalse(listPanel.activeSelf);
        Assert.IsFalse(createPanel.activeSelf);
        StringAssert.Contains("Robotics", editTitle.text);
        Assert.AreEqual("Robotics", editName.text);

        // ShowCreatePanel -> swaps panels and clears input
        var createInput = GetPrivate<TMP_InputField>(mgr, "createClassNameInput");
        createInput.text = "ShouldBeCleared";

        CallPrivate(mgr, "ShowCreatePanel");
        yield return null;

        Assert.IsTrue(createPanel.activeSelf);
        Assert.IsFalse(editPanel.activeSelf);
        Assert.IsFalse(listPanel.activeSelf);
        Assert.AreEqual("", createInput.text, "Create input should be cleared when opening Create panel.");
    }
}
