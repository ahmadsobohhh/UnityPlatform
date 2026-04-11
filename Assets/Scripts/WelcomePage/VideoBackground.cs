// Script: VideoBackground
// Path: Assets/Scripts/WelcomePage/VideoBackground.cs
// Purpose: Controls looping background video playback and fallback handling.

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
[RequireComponent(typeof(RawImage))]
public class VideoBackground : MonoBehaviour
{
    private VideoPlayer player;
    private RawImage rawImage;
    private RenderTexture rt;
    private int lastW, lastH;

    private void Awake()
    {
        player = GetComponent<VideoPlayer>();
        rawImage = GetComponent<RawImage>();

        player.isLooping = true;
        player.playOnAwake = true;
        player.audioOutputMode = VideoAudioOutputMode.None;
        player.renderMode = VideoRenderMode.RenderTexture;

        CreateRT();
    }

    private void Update()
    {
        if (Screen.width != lastW || Screen.height != lastH)
            CreateRT();
    }

    private void CreateRT()
    {
        int w = Mathf.Max(Screen.width, 1920);
        int h = Mathf.Max(Screen.height, 1080);

        if (rt != null)
        {
            player.targetTexture = null;
            rt.Release();
            Destroy(rt);
        }

        rt = new RenderTexture(w, h, 0);
        rt.filterMode = FilterMode.Bilinear;
        player.targetTexture = rt;
        rawImage.texture = rt;

        lastW = Screen.width;
        lastH = Screen.height;
    }

    private void OnDestroy()
    {
        if (rt != null)
        {
            rt.Release();
            Destroy(rt);
        }
    }
}


