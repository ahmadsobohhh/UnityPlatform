
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class WelcomePageTests
{
    const string SceneName = "WelcomePage";

    // Main menu buttons
    const string BeginBtnGO = "BeginBtn";
    const string OptionsBtnGO = "OptionsBtn";
    const string ExitBtnGO = "ExitBtn";

    // Options menu
    const string OptionsMenuGO = "OptionsMenu";
    const string BackBtnGO = "BackBtn";

    // Sliders
    const string MusicSliderGO = "MusicSlider";
    const string EffectsSliderGO = "EffectsSlider";

    Button beginBtn, optionsBtn, exitBtn, backBtn;
    GameObject optionsMenu;
    Slider musicSlider, effectsSlider;

    [UnitySetUp]
    public IEnumerator LoadScene()
    {
        yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
        yield return null;

        beginBtn = GameObject.Find(BeginBtnGO)?.GetComponent<Button>();
        optionsBtn = GameObject.Find(OptionsBtnGO)?.GetComponent<Button>();
        exitBtn = GameObject.Find(ExitBtnGO)?.GetComponent<Button>();

        optionsMenu = GameObject.Find(OptionsMenuGO);
        backBtn = GameObject.Find(BackBtnGO)?.GetComponent<Button>();

        musicSlider = GameObject.Find(MusicSliderGO)?.GetComponent<Slider>();
        effectsSlider = GameObject.Find(EffectsSliderGO)?.GetComponent<Slider>();
    }

    [UnityTest]
    public IEnumerator Welcome_Wires_Exist()
    {
        Assert.NotNull(beginBtn, "BeginBtn missing");
        Assert.NotNull(optionsBtn, "OptionsBtn missing");
        Assert.NotNull(exitBtn, "ExitBtn missing");

        Assert.NotNull(optionsMenu, "OptionsMenu missing");
        Assert.NotNull(backBtn, "BackBtn missing");

        Assert.NotNull(musicSlider, "MusicSlider missing");
        Assert.NotNull(effectsSlider, "EffectsSlider missing");
        yield break;
    }

    [UnityTest]
    public IEnumerator OptionsMenu_Toggles_With_Buttons()
    {
        // Many UIs start hidden; if yours starts shown, this still passes the toggle flow.
        bool initiallyActive = optionsMenu.activeSelf;

        // Open
        optionsBtn.onClick.Invoke();
        yield return null;
        Assert.True(optionsMenu.activeSelf, "OptionsMenu should be active after clicking Options.");

        // Close
        backBtn.onClick.Invoke();
        yield return null;
        Assert.False(optionsMenu.activeSelf, "OptionsMenu should be hidden after clicking Back.");

        // Restore original state so other tests aren't affected
        optionsMenu.SetActive(initiallyActive);
    }

    [UnityTest]
    public IEnumerator Sliders_Are_InRange_And_Interactable()
    {
        foreach (var s in new[] { musicSlider, effectsSlider })
        {
            Assert.AreEqual(0f, s.minValue, "Slider min should be 0.");
            Assert.AreEqual(1f, s.maxValue, "Slider max should be 1.");
            Assert.That(s.value, Is.InRange(0f, 1f), "Slider value out of range [0,1].");

            float before = s.value;
            s.value = Mathf.Clamp01(before + 0.1f);
            yield return null;
            Assert.AreNotEqual(before, s.value, "Slider did not change when set.");
        }
    }
}
