// Script: AudioManager
// Path: Assets/Scripts/Audio/AudioManager.cs
// Purpose: Manages global music and sound effects playback across scenes.

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [Range(0f, 1f)]
    [SerializeField] private float defaultMusicVolume = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float defaultEffectsVolume = 0.5f;

    [Header("Button SFX")]
    [SerializeField] private AudioClip buttonHoverClip;
    [SerializeField] private AudioClip buttonClickClip;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private Slider boundMusicSlider;
    private Slider boundEffectsSlider;

    private const string MusicVolKey = "MusicVolume";
    private const string EffectsVolKey = "EffectsVolume";

    public float MusicVolume
    {
        get => PlayerPrefs.GetFloat(MusicVolKey, defaultMusicVolume);
        set
        {
            PlayerPrefs.SetFloat(MusicVolKey, value);
            if (musicSource != null) musicSource.volume = value;
        }
    }

    public float EffectsVolume
    {
        get => PlayerPrefs.GetFloat(EffectsVolKey, defaultEffectsVolume);
        set => PlayerPrefs.SetFloat(EffectsVolKey, value);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = MusicVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    private void Start()
    {
        BindSliders();
        AttachButtonSFX();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        boundMusicSlider = null;
        boundEffectsSlider = null;
        BindSliders();
        AttachButtonSFX();
    }

    private void Update()
    {
        if (boundMusicSlider == null || boundEffectsSlider == null)
            BindSliders();
    }

    private Slider FindSliderByName(string name)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var slider in root.GetComponentsInChildren<Slider>(true))
                {
                    if (slider.gameObject.name == name)
                        return slider;
                }
            }
        }

        return null;
    }

    private void BindSliders()
    {
        if (boundMusicSlider == null)
        {
            var slider = FindSliderByName("MusicSlider");
            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.SetValueWithoutNotify(MusicVolume);
                slider.onValueChanged.RemoveListener(OnMusicSliderChanged);
                slider.onValueChanged.AddListener(OnMusicSliderChanged);
                boundMusicSlider = slider;
            }
        }

        if (boundEffectsSlider == null)
        {
            var slider = FindSliderByName("EffectsSlider");
            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.SetValueWithoutNotify(EffectsVolume);
                slider.onValueChanged.RemoveListener(OnEffectsSliderChanged);
                slider.onValueChanged.AddListener(OnEffectsSliderChanged);
                boundEffectsSlider = slider;
            }
        }
    }

    private void OnMusicSliderChanged(float value)
    {
        MusicVolume = value;
    }

    private void OnEffectsSliderChanged(float value)
    {
        EffectsVolume = value;
    }

    private void AttachButtonSFX()
    {
        foreach (var button in Resources.FindObjectsOfTypeAll<Button>())
        {
            if (!button.gameObject.scene.isLoaded) continue;
            if (button.GetComponent<ButtonSFX>() == null)
                button.gameObject.AddComponent<ButtonSFX>();
        }
    }

    public void PlayHover()
    {
        if (buttonHoverClip != null)
            sfxSource.PlayOneShot(buttonHoverClip, EffectsVolume);
    }

    public void PlayClick()
    {
        if (buttonClickClip != null)
            sfxSource.PlayOneShot(buttonClickClip, EffectsVolume);
    }

    /// <summary>
    /// Ensure background music keeps playing across scene changes.
    /// </summary>
    public void EnsureMusicPlaying()
    {
        if (musicSource == null)
            return;

        if (musicSource.clip == null && backgroundMusic != null)
            musicSource.clip = backgroundMusic;

        musicSource.volume = MusicVolume;

        if (musicSource.clip != null && !musicSource.isPlaying)
            musicSource.Play();
    }

    /// <summary>
    /// Play a one-shot sound effect at the current effects volume.
    /// Call from anywhere: AudioManager.Instance.PlayEffect(clip);
    /// </summary>
    public void PlayEffect(AudioClip clip)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip, EffectsVolume);
    }

    /// <summary>
    /// Swap the background music track at runtime.
    /// </summary>
    public void ChangeMusic(AudioClip newClip)
    {
        if (musicSource.clip == newClip) return;
        musicSource.Stop();
        musicSource.clip = newClip;
        if (newClip != null) musicSource.Play();
    }
}


