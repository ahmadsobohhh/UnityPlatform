using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class StudentCharacterSelectTests
{
    // helper to call private methods
    private static void CallPrivate(object target, string methodName, params object[] args)
    {
        var mi = target.GetType().GetMethod(methodName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(mi, $"Could not find method {methodName}");
        mi.Invoke(target, args);
    }

    // helper to set private field classroomCount
    private static void SetClassroomCount(StudentCharacterSelect scs, int value)
    {
        var fi = typeof(StudentCharacterSelect).GetField("classroomCount",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(fi, "Could not find field classroomCount");
        fi.SetValue(scs, value);
    }

    private StudentCharacterSelect MakeSceneObject(int slotCount = 5)
    {
        var root = new GameObject("StudentCharacterSelect");
        var scs = root.AddComponent<StudentCharacterSelect>();

        // slots
        scs.characterSlots = new System.Collections.Generic.List<GameObject>();
        for (int i = 0; i < slotCount; i++)
        {
            var slot = new GameObject($"Slot_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
            slot.transform.SetParent(root.transform);
            scs.characterSlots.Add(slot);
        }

        // join + class info
        scs.joinGUI = new GameObject("JoinGUI", typeof(RectTransform));
        scs.classInfo = new GameObject("ClassInfo", typeof(RectTransform));
        scs.joinGUI.SetActive(false);
        scs.classInfo.SetActive(false);

        return scs;
    }

    [UnityTest]
    public IEnumerator NoClassrooms_MakesOnlyFirstSlotBright_And_ClickOpensJoin()
    {
        var scs = MakeSceneObject(4);

        // simulate: 0 classrooms
        SetClassroomCount(scs, 0);
        CallPrivate(scs, "ApplySlotColors");
        CallPrivate(scs, "AssignSlotListeners");

        // slot0 should be normal, others dim
        var slot0Img = scs.characterSlots[0].GetComponent<Image>();
        Assert.AreEqual(scs.normalColor, slot0Img.color);

        for (int i = 1; i < scs.characterSlots.Count; i++)
        {
            var img = scs.characterSlots[i].GetComponent<Image>();
            Assert.AreEqual(scs.dimColor, img.color, $"Slot {i} should be dim when no classrooms");
            // and also disabled
            Assert.IsFalse(scs.characterSlots[i].GetComponent<Button>().interactable);
        }

        // click slot0
        scs.characterSlots[0].GetComponent<Button>().onClick.Invoke();
        yield return null;

        Assert.IsTrue(scs.joinGUI.activeSelf, "JoinGUI should open when no classrooms and slot0 clicked");
        Assert.IsFalse(scs.classInfo.activeSelf, "ClassInfo should stay closed");
    }

    [UnityTest]
    public IEnumerator ThreeClassrooms_MakesFirstThreeBright_And_ClickOpensClassInfo()
    {
        var scs = MakeSceneObject(6);

        SetClassroomCount(scs, 3);
        CallPrivate(scs, "ApplySlotColors");
        CallPrivate(scs, "AssignSlotListeners");

        // first 3 bright & interactable
        for (int i = 0; i < 3; i++)
        {
            var img = scs.characterSlots[i].GetComponent<Image>();
            Assert.AreEqual(scs.normalColor, img.color, $"Slot {i} should be bright");
            Assert.IsTrue(scs.characterSlots[i].GetComponent<Button>().interactable, $"Slot {i} should be clickable");
        }

        // others dim & disabled
        for (int i = 3; i < scs.characterSlots.Count; i++)
        {
            var img = scs.characterSlots[i].GetComponent<Image>();
            Assert.AreEqual(scs.dimColor, img.color, $"Slot {i} should be dim");
            Assert.IsFalse(scs.characterSlots[i].GetComponent<Button>().interactable, $"Slot {i} should be disabled");
        }

        // click slot1 (existing classroom)
        scs.characterSlots[1].GetComponent<Button>().onClick.Invoke();
        yield return null;

        Assert.IsTrue(scs.classInfo.activeSelf, "ClassInfo should open for existing classroom slot");
        Assert.IsFalse(scs.joinGUI.activeSelf, "JoinGUI should stay closed");
    }

    [UnityTest]
    public IEnumerator AssignSlotListeners_ClearsOldListeners()
    {
        var scs = MakeSceneObject(2);

        // put fake listener on slot0
        bool oldCalled = false;
        var btn0 = scs.characterSlots[0].GetComponent<Button>();
        btn0.onClick.AddListener(() => oldCalled = true);

        SetClassroomCount(scs, 1);
        CallPrivate(scs, "AssignSlotListeners");

        // click -> should call ONLY the new listener (which opens classInfo)
        btn0.onClick.Invoke();
        yield return null;

        // old listener should not have run
        Assert.IsFalse(oldCalled, "Old listeners should be removed when AssignSlotListeners runs");
    }
}
