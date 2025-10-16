
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools; 
using TMPro;
using UnityEngine.UI;


public class LoginSceneTests
{
    const string SceneName = "Login";
    const string EmailGO = "UserInput";
    const string PasswordGO = "PasswordInput";
    const string SignInBtnGO = "SignInBtn";
    const string SignUpBtnGO = "RegisterBtn"; // parent of your SignUpTxt

    TMP_InputField email;
    TMP_InputField password;
    Button signInBtn;
    Button signUpBtn;

    [UnitySetUp]
    public IEnumerator LoadScene()
    {
        yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
        yield return null;

        email = GameObject.Find(EmailGO)?.GetComponent<TMP_InputField>();
        password = GameObject.Find(PasswordGO)?.GetComponent<TMP_InputField>();
        signInBtn = GameObject.Find(SignInBtnGO)?.GetComponent<Button>();
        signUpBtn = GameObject.Find(SignUpBtnGO)?.GetComponent<Button>();
    }

    [UnityTest]
    public IEnumerator LoginScene_Wires_Exist()
    {
        Assert.NotNull(email, "UserInput TMP_InputField not found");
        Assert.NotNull(password, "PasswordInput TMP_InputField not found");
        Assert.NotNull(signInBtn, "SignInBtn Button not found");
        Assert.NotNull(signUpBtn, "RegisterBtn Button not found");
        yield break;
    }

    [UnityTest]
    public IEnumerator SignIn_Disabled_When_Empty_Enabled_When_Valid()
    {
        // empty -> should be disabled
        email.text = ""; password.text = "";
        yield return null;
        Assert.False(signInBtn.interactable, "SignIn should be disabled when fields are empty.");

        // invalid email -> still disabled
        email.text = "not an email"; password.text = "pw";
        yield return null;
        Assert.False(signInBtn.interactable, "SignIn should be disabled for invalid email.");

        // valid -> enabled
        email.text = "a@b.com"; password.text = "pw";
        yield return null;
        Assert.True(signInBtn.interactable, "SignIn should enable when email+password are valid.");
    }

    [UnityTest]
    public IEnumerator SignUp_Click_Does_Not_Throw()
    {
        bool clicked = false;
        signUpBtn.onClick.AddListener(() => clicked = true);

        signUpBtn.onClick.Invoke();
        yield return null;

        Assert.True(clicked, "SignUp button click did not invoke listeners.");
    }
}
