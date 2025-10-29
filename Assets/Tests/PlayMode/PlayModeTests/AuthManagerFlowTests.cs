using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

// ---------- Test Doubles (fakes) ----------
public interface IAuthService
{
    IEnumerator SignIn(string email, string password, Action<AuthResult> done);
    IEnumerator CreateUser(string email, string password, Action<AuthResult> done);
    IEnumerator UpdateDisplayName(string uid, string displayName, Action<bool> done);
}

public sealed class AuthResult
{
    public bool Ok;
    public string Uid;
    public string Email;
    public string Error; // optional text for failure
}

public interface IFirestoreService
{
    IEnumerator GetUsernameMap(string usernameKey, Action<(bool ok, bool exists, string email, string uid, string role)> done);
    IEnumerator WriteUsernameMap(string usernameKey, string uid, string email, string role, Action<bool> done);
    IEnumerator WriteUserDoc(string uid, Dictionary<string, object> doc, Action<bool> done);
    IEnumerator ReadUserDoc(string uid, Action<(bool ok, bool exists, string role)> done);
}

// In-memory fake that simulates usernames & users collections.
public sealed class FakeFirestore : IFirestoreService
{
    private readonly Dictionary<string, (string uid, string email, string role)> _usernames
        = new Dictionary<string, (string uid, string email, string role)>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<string, object>> _users
        = new Dictionary<string, Dictionary<string, object>>();

    public void SeedUsername(string key, string uid, string email, string role)
        => _usernames[key] = (uid, email, role);

    public void SeedUser(string uid, Dictionary<string, object> doc)
        => _users[uid] = doc;

    public IEnumerator GetUsernameMap(string usernameKey, Action<(bool ok, bool exists, string email, string uid, string role)> done)
    {
        yield return null;
        if (_usernames.TryGetValue(usernameKey, out var v))
            done((true, true, v.email, v.uid, v.role));
        else
            done((true, false, null, null, null));
    }

    public IEnumerator WriteUsernameMap(string usernameKey, string uid, string email, string role, Action<bool> done)
    {
        yield return null;
        _usernames[usernameKey] = (uid, email, role);
        done(true);
    }

    public IEnumerator WriteUserDoc(string uid, Dictionary<string, object> doc, Action<bool> done)
    {
        yield return null;
        _users[uid] = doc;
        done(true);
    }

    public IEnumerator ReadUserDoc(string uid, Action<(bool ok, bool exists, string role)> done)
    {
        yield return null;
        if (_users.TryGetValue(uid, out var d) && d.TryGetValue("role", out var roleObj))
            done((true, true, roleObj as string));
        else
            done((true, false, null));
    }
}


public sealed class FakeAuth : IAuthService
{
    // single composite string key: email(lowercased) + "\n" + password
    private readonly Dictionary<string, (bool ok, string uid)> _signIn
        = new Dictionary<string, (bool ok, string uid)>(StringComparer.OrdinalIgnoreCase);

    private static string Key(string email, string password)
        => $"{(email ?? string.Empty).Trim().ToLowerInvariant()}\n{password ?? string.Empty}";

    public void SeedSignIn(string email, string password, bool ok, string uid)
        => _signIn[Key(email, password)] = (ok, uid);

    public IEnumerator SignIn(string email, string password, Action<AuthResult> done)
    {
        yield return null;
        if (_signIn.TryGetValue(Key(email, password), out var v) && v.ok)
            done(new AuthResult { Ok = true, Uid = v.uid, Email = email });
        else
            done(new AuthResult { Ok = false, Error = "Wrong Password" });
    }

    public IEnumerator CreateUser(string email, string password, Action<AuthResult> done)
    {
        yield return null;
        done(new AuthResult { Ok = true, Uid = Guid.NewGuid().ToString("N"), Email = email });
    }

    public IEnumerator UpdateDisplayName(string uid, string displayName, Action<bool> done)
    {
        yield return null;
        done(true);
    }
}


// ------------- Test harness MonoBehaviour (test-only) -------------
public sealed class TestableAuthManager : MonoBehaviour
{
    // UI (same fields as production so tests mirror setup)
    public TMP_InputField userLoginField;
    public TMP_InputField passwordLoginField;
    public TMP_Text warningLoginText;
    public TMP_Text confirmLoginText;

    public IAuthService auth;
    public IFirestoreService db;

    private static string Key(string s) => string.IsNullOrWhiteSpace(s) ? "" : s.Trim().ToLowerInvariant();

    public IEnumerator LoginFlow(string identifier, string pw, Action<string /*nextSceneOrNull*/> done)
    {
        string input = identifier.Trim();
        string email = input;
        string uidFromMap = null;

        // Username path
        if (!input.Contains("@"))
        {
            var key = Key(input);
            (bool ok, bool exists, string mappedEmail, string mappedUid, string _) res = default;
            yield return db.GetUsernameMap(key, r => res = r);

            if (!res.ok) { warningLoginText.text = "Network or permissions error."; done(null); yield break; }
            if (!res.exists) { warningLoginText.text = "Username not found."; done(null); yield break; }

            email = res.mappedEmail;
            uidFromMap = res.mappedUid;
        }

        AuthResult ar = null;
        yield return auth.SignIn(email, pw, r => ar = r);
        if (!ar.Ok)
        {
            warningLoginText.text = ar.Error ?? "Login Failed!";
            done(null); yield break;
        }

        confirmLoginText.text = "Logged In";
        (bool ok, bool exists, string role) rd = default;
        yield return db.ReadUserDoc(uidFromMap ?? ar.Uid, r => rd = r);

        if (!rd.ok || !rd.exists) { done(null); yield break; }
        done(rd.role == "teacher" ? "TeacherHome" : "StudentCharacterSelect");
    }
}

// ------------- The actual PlayMode tests -------------
public class AuthManagerFlowTests
{
    private GameObject _root;
    private TestableAuthManager _mgr;
    private FakeAuth _auth;
    private FakeFirestore _db;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        _root = new GameObject("Harness");
        _mgr = _root.AddComponent<TestableAuthManager>();

        // Hook up lightweight UI
        _mgr.userLoginField = NewTMPInput("UserInput");
        _mgr.passwordLoginField = NewTMPInput("PasswordInput");
        _mgr.warningLoginText = NewTMPText("Warn");
        _mgr.confirmLoginText = NewTMPText("Confirm");

        _auth = new FakeAuth();
        _db = new FakeFirestore();
        _mgr.auth = _auth;
        _mgr.db = _db;

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Teardown()
    {
        UnityEngine.Object.Destroy(_root);
        yield return null;
    }

    [UnityTest]
    public IEnumerator UsernameNotFound_ShowsWarning()
    {
        // No username mapping seeded
        string next = null;
        yield return _mgr.LoginFlow("missingUser", "pw", s => next = s);

        Assert.AreEqual("Username not found.", _mgr.warningLoginText.text);
        Assert.IsNull(next);
    }

    [UnityTest]
    public IEnumerator TeacherUsername_Routes_To_TeacherHome()
    {
        // Seed username map + user doc + auth success
        _db.SeedUsername("mrsmith", "UID_T", "smith@school.tld", "teacher");
        _db.SeedUser("UID_T", new Dictionary<string, object> { { "role", "teacher" } });
        _auth.SeedSignIn("smith@school.tld", "secret", ok: true, uid: "UID_T");

        string next = null;
        yield return _mgr.LoginFlow("MrSmith", "secret", s => next = s); // note casing: will normalize

        Assert.AreEqual("Logged In", _mgr.confirmLoginText.text);
        Assert.AreEqual("TeacherHome", next);
    }

    [UnityTest]
    public IEnumerator StudentUsername_Routes_To_StudentCharacterSelect()
    {
        _db.SeedUsername("alice", "UID_S", "alice@students.example", "student");
        _db.SeedUser("UID_S", new Dictionary<string, object> { { "role", "student" } });
        _auth.SeedSignIn("alice@students.example", "pw", ok: true, uid: "UID_S");

        string next = null;
        yield return _mgr.LoginFlow("  ALICE ", "pw", s => next = s);

        Assert.AreEqual("Logged In", _mgr.confirmLoginText.text);
        Assert.AreEqual("StudentCharacterSelect", next);
    }

    [UnityTest]
    public IEnumerator WrongPassword_Shows_WrongPassword_Message()
    {
        // Mapping exists, but auth fails (no SeedSignIn for that password)
        _db.SeedUsername("alice", "UID_S", "alice@students.example", "student");
        _db.SeedUser("UID_S", new Dictionary<string, object> { { "role", "student" } });

        string next = null;
        yield return _mgr.LoginFlow("alice", "badpw", s => next = s);

        StringAssert.Contains("Wrong Password", _mgr.warningLoginText.text);
        Assert.IsNull(next);
    }

    // ---------- helpers ----------
    private static TMP_InputField NewTMPInput(string name)
    {
        var go = new GameObject(name);
        var input = go.AddComponent<TMP_InputField>();
        go.AddComponent<TextMeshProUGUI>(); // child text is not required for our tests, but field needs TMP present
        return input;
    }

    private static TextMeshProUGUI NewTMPText(string name)
    {
        var go = new GameObject(name);
        return go.AddComponent<TextMeshProUGUI>();
    }
}
