using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine.UI;

public class StudentCharacterSelectFirebaseTests
{
    // helper: call a private instance method like Start(), CheckStudentClassrooms(), etc.
    private static void CallPrivate(object target, string methodName, params object[] args)
    {
        var mi = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        Assert.NotNull(mi, $"Could not find method {methodName} on {target.GetType().Name}");
        mi.Invoke(target, args);
    }

    // scene-less setup
    private StudentCharacterSelect MakeUI(int count = 4)
    {
        var go = new GameObject("StudentCharacterSelect");
        var scs = go.AddComponent<StudentCharacterSelect>();

        scs.characterSlots = new System.Collections.Generic.List<GameObject>();
        for (int i = 0; i < count; i++)
        {
            var slot = new GameObject($"Slot_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
            slot.transform.SetParent(go.transform);
            scs.characterSlots.Add(slot);
        }

        scs.joinGUI = new GameObject("JoinGUI", typeof(RectTransform));
        scs.classInfo = new GameObject("ClassInfo", typeof(RectTransform));
        scs.joinGUI.SetActive(false);
        scs.classInfo.SetActive(false);

        return scs;
    }

    [UnityTest]
    public IEnumerator Firebase_Is_Available_And_User_Is_Signed_In()
    {
        var depTask = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => depTask.IsCompleted);
        if (depTask.Result != DependencyStatus.Available)
            Assert.Inconclusive("Firebase deps not available in test runner.");

        var auth = FirebaseAuth.DefaultInstance;
        if (auth.CurrentUser == null)
            Assert.Inconclusive("No Firebase user signed in — open game, sign in, re-run.");

        Assert.IsNotNull(auth.CurrentUser);
    }

    [UnityTest]
    public IEnumerator StudentCharacterSelect_Loads_Classrooms_From_Firestore_When_User_Logged_In()
    {
        var depTask = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => depTask.IsCompleted);
        if (depTask.Result != DependencyStatus.Available)
            Assert.Inconclusive("Firebase deps not available.");

        var auth = FirebaseAuth.DefaultInstance;
        if (auth.CurrentUser == null)
            Assert.Inconclusive("Need signed-in Firebase user.");

        var scs = MakeUI(6);

        // run Start() (private)
        CallPrivate(scs, "Start");

        // wait for coroutine to finish firestore call
        yield return new WaitForSeconds(1.5f);

        var slot0 = scs.characterSlots[0].GetComponent<Button>();
        Assert.IsNotNull(slot0);
        Assert.IsTrue(slot0.interactable, "First slot should be clickable after load.");
    }

    [UnityTest]
    public IEnumerator Clicking_Active_Slot_Shows_Correct_Panel_After_Firestore_Load()
    {
        var depTask = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => depTask.IsCompleted);
        if (depTask.Result != DependencyStatus.Available)
            Assert.Inconclusive("Firebase deps not available.");

        var auth = FirebaseAuth.DefaultInstance;
        if (auth.CurrentUser == null)
            Assert.Inconclusive("Need signed-in user.");

        var scs = MakeUI(4);
        CallPrivate(scs, "Start");
        yield return new WaitForSeconds(1.5f);

        scs.characterSlots[0].GetComponent<Button>().onClick.Invoke();
        yield return null;

        bool anyPanel = scs.joinGUI.activeSelf || scs.classInfo.activeSelf;
        Assert.IsTrue(anyPanel, "After clicking an active slot, one of the panels should be visible.");
    }
}
