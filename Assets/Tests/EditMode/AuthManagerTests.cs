using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using Firebase;
using Firebase.Auth;

public class AuthManagerTests
{
    /// <summary>
    /// Calls the private static Key(string) method via reflection to verify normalization.
    /// </summary>
    [TestCase(" Alice ", "alice")]
    [TestCase("ALICE", "alice")]
    [TestCase("  Alice.Bob  ", "alice.bob")]
    [TestCase("", "")]
    [TestCase("   ", "")]
    [TestCase(null, "")]
    public void Key_Normalizes_Trim_And_Lower(string input, string expected)
    {
        // Arrange
        var mi = typeof(AuthManager)
            .GetMethod("Key", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(mi, "Could not reflect private static method 'Key' on AuthManager.");

        // Act
        var result = (string)mi.Invoke(null, new object[] { input });

        // Assert
        Assert.AreEqual(expected, result);
    }

    /// <summary>
    /// Verifies that HandleAuthError maps Firebase AuthError codes to UI text.
    /// </summary>
    [TestCase(AuthError.MissingEmail, "Missing Email")]
    [TestCase(AuthError.MissingPassword, "Missing Password")]
    [TestCase(AuthError.WeakPassword, "Weak Password")]
    [TestCase(AuthError.EmailAlreadyInUse, "Email already in use")]
    [TestCase(AuthError.InvalidEmail, "Invalid Email")]
    public void HandleAuthError_Maps_Common_Codes(AuthError code, string expected)
    {
        // Arrange: make a FirebaseException with the desired error code
        // FirebaseException has a public ctor (int code, string message)
        var firebaseEx = new FirebaseException((int)code, code.ToString());
        var agg = new AggregateException(firebaseEx);
        var go = new GameObject("tmp");
        var label = go.AddComponent<TextMeshProUGUI>();

        var mgr = go.AddComponent<AuthManager>();

        // Get private HandleAuthError via reflection (it is private in your script).
        var mi = typeof(AuthManager)
            .GetMethod("HandleAuthError", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(mi, "Could not reflect 'HandleAuthError' on AuthManager");

        // Act
        mi.Invoke(mgr, new object[] { agg, label });

        // Assert
        Assert.AreEqual(expected, label.text);

        UnityEngine.Object.DestroyImmediate(go);
    }

    /// <summary>
    /// If we pass a non-Firebase inner exception, the default message should be used.
    /// </summary>
    [Test]
    public void HandleAuthError_Falls_Back_When_Not_Firebase()
    {
        var nonFirebase = new InvalidOperationException("boom");
        var agg = new AggregateException(nonFirebase);
        var go = new GameObject("tmp");
        var label = go.AddComponent<TextMeshProUGUI>();
        var mgr = go.AddComponent<AuthManager>();

        var mi = typeof(AuthManager)
            .GetMethod("HandleAuthError", BindingFlags.NonPublic | BindingFlags.Instance);

        mi.Invoke(mgr, new object[] { agg, label });

        // Your method sets "Register failed" for unrecognized cases.
        StringAssert.Contains("Register failed", label.text);

        UnityEngine.Object.DestroyImmediate(go);
    }
}
