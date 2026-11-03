using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace ImagineQuest.Gameplay
{
    /// <summary>Client-art portal presentation. The workshop owns saved progress and scene travel.</summary>
    public sealed class WorkshopForgeReward : MonoBehaviour
    {
        public static readonly Vector3 StonePosition = new Vector3(0, .85f, .2f);
        public static readonly Vector3 PortalPosition = new Vector3(0, 1.7f, -4.42f);
        private const float StoneRadius = .34f;
        private readonly List<Material> materials = new List<Material>();
        private GameObject stone, portal;
        private Material portalMaterial;
        private RenderTexture videoTexture;
        private VideoPlayer video;
        private Texture poster;
        private Func<bool> persistActivation;
        private float openedAt, prepareStarted;
        private bool preparing, videoFailed;

        public bool StoneRevealed { get; private set; }
        public bool PortalOpened { get; private set; }
        public bool VideoFrameReceived { get; private set; }
        public bool IsVideoPlaying => video != null && video.isPlaying;
        public string MediaStatus { get; private set; } = "Portal dormant";
        public Renderer PortalRenderer { get; private set; }

        public void Initialize(Func<bool> saveActivation) => persistActivation = saveActivation;

        public void SetCompletedStations(int completed) => RestoreProgress(completed, PortalOpened);

        public void RestoreProgress(int completed, bool opened)
        {
            bool eligible = completed >= 3;
            StoneRevealed = eligible;
            if (eligible && stone == null) BuildStone();
            if (eligible && opened)
            {
                RevealPortal();
                return;
            }
            PortalOpened = false;
            if (portal != null) portal.SetActive(false);
            if (video != null && video.isPlaying) video.Pause();
            if (stone != null) stone.SetActive(eligible);
        }

        // The player's body must touch the stone volume. An avatar on another floor cannot activate it.
        public bool CheckPlayerProximity(Vector3 worldFeet, float bodyRadius = .28f, float bodyHeight = 1.8f,
            bool hasControl = true)
        {
            if (!hasControl || !StoneRevealed || PortalOpened) return false;
            Vector3 feet = transform.InverseTransformPoint(worldFeet);
            if (feet.y > StonePosition.y + StoneRadius || feet.y + bodyHeight < StonePosition.y - StoneRadius)
                return false;
            Vector2 delta = new Vector2(feet.x - StonePosition.x, feet.z - StonePosition.z);
            if (delta.sqrMagnitude > Mathf.Pow(StoneRadius + bodyRadius, 2)) return false;
            // Never consume the stone if the workshop could not save this transition.
            if (persistActivation != null && !persistActivation()) return false;
            RevealPortal();
            return true;
        }

        public bool CanEnterPortal(Vector3 worldFeet, bool hasControl = true)
        {
            if (!hasControl || !PortalOpened || Time.unscaledTime - openedAt < .9f) return false;
            Vector3 feet = transform.InverseTransformPoint(worldFeet);
            return Mathf.Abs(feet.x - PortalPosition.x) < .72f &&
                   Mathf.Abs(feet.z - PortalPosition.z) < .48f &&
                   feet.y > -.4f && feet.y < 1.4f;
        }

        private void BuildStone()
        {
            stone = new GameObject("Forge Stone Reward");
            stone.transform.SetParent(transform, false);
            stone.transform.localPosition = StonePosition;
            Shape("Ancient forge stone", stone.transform, PrimitiveType.Sphere, Vector3.zero,
                new Vector3(.48f, .65f, .42f), Lit(new Color(.13f, .22f, .16f)));
            var gem = Shape("Glowing emerald core", stone.transform, PrimitiveType.Cube,
                new Vector3(0, .08f, -.04f), Vector3.one * .32f, Lit(new Color(.4f, 1, .16f), 1.8f));
            gem.transform.localRotation = Quaternion.Euler(25, 45, 35);
            Glow(stone.transform, 1.6f, 3);
        }

        private void RevealPortal()
        {
            if (PortalOpened) return;
            PortalOpened = true;
            if (stone != null) stone.SetActive(false);
            if (portal == null) BuildPortal();
            portal.SetActive(true);
            portal.transform.localScale = Vector3.one * .01f;
            openedAt = Time.unscaledTime;
            if (video != null && !videoFailed)
            {
                if (video.isPrepared) video.Play();
                else
                {
                    preparing = true;
                    prepareStarted = Time.realtimeSinceStartup;
                    MediaStatus = "Loading client portal animation";
                    video.Prepare();
                }
            }
        }

        private void BuildPortal()
        {
            portal = new GameObject("Living Emerald Portal");
            portal.transform.SetParent(transform, false);
            portal.transform.localPosition = PortalPosition;
            var shader = Resources.Load<Shader>("Workshop/EmeraldPortal");
            if (shader == null) throw new InvalidOperationException("The client portal shader is missing.");
            portalMaterial = new Material(shader);
            materials.Add(portalMaterial);
            poster = Resources.Load<Texture2D>("Workshop/ClientPortalPoster");
            if (poster == null) throw new InvalidOperationException("The client portal poster is missing.");
            portalMaterial.mainTexture = poster;
            var surface = Shape("Client vine and flower portal", portal.transform, PrimitiveType.Quad,
                Vector3.zero, new Vector3(2.02f, 3.3f, 1), portalMaterial);
            PortalRenderer = surface.GetComponent<Renderer>();
            PortalRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            PortalRenderer.receiveShadows = false;
            Glow(portal.transform, 2, 4);

            var clip = Resources.Load<VideoClip>("Workshop/ClientPortal");
            if (clip == null) { UsePoster("Portal animation is missing; showing the client artwork."); return; }
            videoTexture = new RenderTexture((int)clip.width, (int)clip.height, 0, RenderTextureFormat.ARGB32);
            videoTexture.name = "Client portal color and alpha";
            videoTexture.Create();
            video = gameObject.AddComponent<VideoPlayer>();
            video.playOnAwake = false;
            video.source = VideoSource.VideoClip;
            video.clip = clip;
            video.audioOutputMode = VideoAudioOutputMode.None;
            video.renderMode = VideoRenderMode.RenderTexture;
            video.targetTexture = videoTexture;
            video.isLooping = true;
            video.waitForFirstFrame = true;
            video.skipOnDrop = true;
            video.sendFrameReadyEvents = true;
            video.prepareCompleted += Prepared;
            video.frameReady += FrameReady;
            video.errorReceived += VideoError;
        }

        private void Prepared(VideoPlayer source)
        {
            preparing = false;
            if (PortalOpened && isActiveAndEnabled) source.Play();
        }

        private void FrameReady(VideoPlayer source, long frame)
        {
            if (videoFailed || !PortalOpened) return;
            VideoFrameReceived = true;
            portalMaterial.mainTexture = videoTexture;
            MediaStatus = "Client portal animation playing";
        }

        private void VideoError(VideoPlayer source, string error) =>
            UsePoster("Portal animation unavailable; client artwork remains visible. " + error);

        private void UsePoster(string message)
        {
            videoFailed = true;
            preparing = false;
            if (video != null) video.Stop();
            if (portalMaterial != null) portalMaterial.mainTexture = poster;
            MediaStatus = message;
            Debug.LogWarning("[Workshop portal] " + message);
        }

        private void Update()
        {
            if (stone != null && !PortalOpened)
            {
                stone.transform.localPosition = StonePosition + Vector3.up * Mathf.Sin(Time.time * 2) * .045f;
                stone.transform.Rotate(0, 35 * Time.deltaTime, 0, Space.Self);
            }
            if (PortalOpened && portal != null)
                portal.transform.localScale = Vector3.one * Mathf.SmoothStep(.01f, 1,
                    Mathf.Clamp01((Time.unscaledTime - openedAt) / .9f));
            if (preparing && Time.realtimeSinceStartup - prepareStarted > 12)
                UsePoster("Portal animation timed out; client artwork remains visible.");
        }

        private Material Lit(Color color, float emission = 0)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            if (emission > 0)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
            }
            materials.Add(material);
            return material;
        }

        private static GameObject Shape(string label, Transform parent, PrimitiveType primitive,
            Vector3 position, Vector3 scale, Material material)
        {
            var actor = GameObject.CreatePrimitive(primitive);
            actor.name = label;
            actor.transform.SetParent(parent, false);
            actor.transform.localPosition = position;
            actor.transform.localScale = scale;
            actor.GetComponent<Renderer>().sharedMaterial = material;
            var collider = actor.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            return actor;
        }

        private static void Glow(Transform parent, float intensity, float range)
        {
            var actor = new GameObject("Emerald glow", typeof(Light));
            actor.transform.SetParent(parent, false);
            var light = actor.GetComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(.45f, 1, .16f);
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private void OnDisable()
        {
            if (video != null) video.Pause();
        }

        private void OnEnable()
        {
            if (PortalOpened && video != null && video.isPrepared && !videoFailed) video.Play();
        }

        private void OnDestroy()
        {
            if (video != null)
            {
                video.prepareCompleted -= Prepared;
                video.frameReady -= FrameReady;
                video.errorReceived -= VideoError;
                video.Stop();
                video.targetTexture = null;
            }
            if (videoTexture != null) { videoTexture.Release(); Destroy(videoTexture); }
            foreach (var material in materials) Destroy(material);
        }
    }
}
