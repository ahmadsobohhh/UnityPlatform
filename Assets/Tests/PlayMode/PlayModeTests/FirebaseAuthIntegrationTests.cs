
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;

public class FirebaseAuthIntegrationTests
{
    FirebaseAuth _auth;
    FirebaseFirestore _db;

    // Set these via environment variables for safety:
    //   TEST_EMAIL, TEST_PASSWORD
    // Optionally set TEST_USERNAME_KEY (normalized username) to test the usernames/{key} mapping.
    static string TestEmail => Environment.GetEnvironmentVariable("TEST_EMAIL");
    static string TestPassword => Environment.GetEnvironmentVariable("TEST_PASSWORD");
    static string TestUserKey => Environment.GetEnvironmentVariable("TEST_USERNAME_KEY"); // e.g., "alice" (lowercased/trimmed)

    [UnitySetUp]
    public IEnumerator Setup()
    {
        var depTask = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => depTask.IsCompleted);
        Assert.AreEqual(DependencyStatus.Available, depTask.Result, $"Firebase deps not available: {depTask.Result}");

        _auth = FirebaseAuth.DefaultInstance;
        _db = FirebaseFirestore.DefaultInstance;
        Assert.NotNull(_auth);
        Assert.NotNull(_db);
    }

    [UnityTest]
    public IEnumerator Dependencies_Available()
    {
        // If we got here, Setup passed.
        Assert.Pass("Firebase dependencies resolved and instances created.");
        yield break;
    }

    [UnityTest]
    public IEnumerator Username_Map_Resolves_Email_If_Configured()
    {
        // Optional: only runs if TEST_USERNAME_KEY is provided.
        if (string.IsNullOrWhiteSpace(TestUserKey))
        {
            Assert.Ignore("Set TEST_USERNAME_KEY to run this test (e.g., 'alice').");
            yield break;
        }

        var docTask = _db.Collection("usernames").Document(TestUserKey).GetSnapshotAsync();
        yield return new WaitUntil(() => docTask.IsCompleted);

        Assert.False(docTask.IsFaulted || docTask.IsCanceled, "Failed to fetch usernames/{key} mapping from Firestore.");
        var snap = docTask.Result;
        Assert.True(snap.Exists, $"usernames/{TestUserKey} does not exist.");
        Assert.True(snap.ContainsField("email"), "Mapping doc missing 'email' field.");
        var email = snap.GetValue<string>("email");
        Assert.IsNotEmpty(email, "Mapped email was empty.");
    }

    [UnityTest]
    public IEnumerator SignIn_With_Test_Account_Then_Reads_Role()
    {
        if (string.IsNullOrWhiteSpace(TestEmail) || string.IsNullOrWhiteSpace(TestPassword))
        {
            Assert.Ignore("Set TEST_EMAIL and TEST_PASSWORD env vars to run this test.");
            yield break;
        }

        var loginTask = _auth.SignInWithEmailAndPasswordAsync(TestEmail.Trim(), TestPassword);
        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.Exception != null)
        {
            var baseEx = loginTask.Exception.GetBaseException() as FirebaseException;
            Assert.Fail($"SignIn failed: {(baseEx != null ? ((AuthError)baseEx.ErrorCode).ToString() : loginTask.Exception.Message)}");
        }

        var user = loginTask.Result.User;
        Assert.NotNull(user, "Auth returned null user.");
        Assert.IsNotEmpty(user.UserId, "UserId empty after sign-in.");

        // Read users/{uid} and check the role to confirm routing conditions.
        var userDocTask = _db.Collection("users").Document(user.UserId).GetSnapshotAsync();
        yield return new WaitUntil(() => userDocTask.IsCompleted);

        Assert.False(userDocTask.IsFaulted || userDocTask.IsCanceled, "Failed to fetch users/{uid} from Firestore.");
        var snap = userDocTask.Result;
        Assert.True(snap.Exists, $"users/{user.UserId} doc not found.");
        Assert.True(snap.ContainsField("role"), "users/{uid} missing 'role' field.");

        var role = snap.GetValue<string>("role");
        Assert.IsTrue(role == "teacher" || role == "student", $"Unexpected role '{role}'. Expected 'teacher' or 'student'.");

        // Optional: assert the scene you would navigate to based on role
        var expectedNext = role == "teacher" ? "TeacherHome" : "StudentCharacterSelect";
        Debug.Log($"[Firebase Test] Signed in as {user.Email}, role={role}, expected next scene: {expectedNext}");
    }
}
