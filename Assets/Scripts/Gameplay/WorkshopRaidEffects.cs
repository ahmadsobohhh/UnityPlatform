using System.Collections.Generic;
using UnityEngine;

namespace ImagineQuest.Gameplay
{
    internal enum RaidCue { Swing, Bone, Guard, Hurt, Collapse, Forge, Wave, Footstep }

    internal sealed class WorkshopRaidEffects
    {
        private readonly AudioSource audio;
        private readonly Dictionary<RaidCue, AudioClip> clips = new Dictionary<RaidCue, AudioClip>();
        private readonly ParticleSystem sparks, dust;
        public Material ParticleMaterial { get; }

        public WorkshopRaidEffects(Transform owner, AudioSource audio, List<AudioClip> ownedAudio, List<Material> ownedMaterials)
        {
            this.audio = audio;
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            var material = new Material(shader) { name = "Raid particle tint" };
            ParticleMaterial = material;
            ownedMaterials.Add(material);
            sparks = CreateEmitter(owner, material, "Hot sparks", 1.3f, true);
            dust = CreateEmitter(owner, material, "Bone dust", -.08f, false);
            foreach (RaidCue cue in System.Enum.GetValues(typeof(RaidCue)))
            {
                var clip = BuildClip(cue);
                clips.Add(cue, clip);
                ownedAudio.Add(clip);
            }
        }

        public void Play(RaidCue cue)
        {
            if (audio != null) audio.PlayOneShot(clips[cue], cue == RaidCue.Footstep ? .16f : .48f);
        }

        public void Burst(RaidCue cue, Vector3 position, Vector3 direction, bool sound = true)
        {
            if (sound) Play(cue);
            if (direction.sqrMagnitude < .01f) direction = Vector3.up;
            bool smoky = cue == RaidCue.Collapse || cue == RaidCue.Wave;
            Color tint = cue == RaidCue.Guard ? new Color(1, .88f, .56f) :
                cue == RaidCue.Wave ? new Color(.24f, .77f, .61f, .65f) :
                smoky ? new Color(.74f, .61f, .42f, .65f) : new Color(1, .48f, .12f);
            int count = cue == RaidCue.Forge ? 34 : smoky ? 20 : 13;
            for (int i = 0; i < count; i++)
            {
                var emit = new ParticleSystem.EmitParams
                {
                    position = position + Random.insideUnitSphere * (smoky ? .25f : .04f),
                    velocity = (direction.normalized + Random.insideUnitSphere * .9f + Vector3.up * .35f) *
                        Random.Range(smoky ? .22f : 1.1f, smoky ? 1.2f : 4.2f),
                    startLifetime = Random.Range(smoky ? .45f : .13f, smoky ? .85f : .38f),
                    startSize = Random.Range(smoky ? .09f : .013f, smoky ? .25f : .039f),
                    startColor = Color.Lerp(tint, Color.white, Random.value * .5f)
                };
                (smoky ? dust : sparks).Emit(emit, 1);
            }
        }

        private static ParticleSystem CreateEmitter(Transform owner, Material material, string label, float gravity, bool streak)
        {
            var go = new GameObject(label);
            go.transform.SetParent(owner, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 256;
            main.gravityModifier = gravity;
            var emission = system.emission;
            emission.enabled = false;
            var shape = system.shape;
            shape.enabled = false;
            var fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.85f, .42f), new GradientAlphaKey(0, 1) });
            fade.color = gradient;
            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, streak
                ? AnimationCurve.Linear(0, 1, 1, .15f) : AnimationCurve.Linear(0, .5f, 1, 1.65f));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = streak ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            renderer.velocityScale = streak ? .15f : 0;
            renderer.lengthScale = 1;
            system.Play();
            return system;
        }

        private static AudioClip BuildClip(RaidCue cue)
        {
            const int rate = 22050;
            float duration = cue == RaidCue.Wave ? .85f : cue == RaidCue.Collapse ? .39f : .23f;
            var data = new float[Mathf.CeilToInt(rate * duration)];
            var rng = new System.Random(519 + (int)cue);
            float filtered = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate;
                float noise = (float)rng.NextDouble() * 2 - 1;
                filtered = Mathf.Lerp(filtered, noise, .18f);
                float Envelope(float decay) => Mathf.Min(1, t * 440) * Mathf.Exp(-t * decay);
                float Tone(float hz) => Mathf.Sin(t * hz * 2 * Mathf.PI);
                data[i] = cue switch
                {
                    RaidCue.Swing => filtered * Mathf.Sin(Mathf.PI * t / duration) * .56f,
                    RaidCue.Guard => (Tone(1049) + .57f * Tone(1721) + .23f * noise) * Envelope(18) * .31f,
                    RaidCue.Forge => (Tone(535) + .57f * Tone(1487) + .29f * Tone(2257) + .38f * noise) * Envelope(12) * .32f,
                    RaidCue.Hurt => (Tone(72) + .48f * filtered) * Envelope(24) * .42f,
                    RaidCue.Bone => (Tone(212) + .65f * noise + .27f * Tone(771)) * Envelope(29) * .34f,
                    RaidCue.Collapse => (Tone(142) * .25f + noise * .6f) * Envelope(8) *
                        (.26f + .74f * Mathf.Pow(Mathf.Abs(Tone(19)), 5)),
                    RaidCue.Wave => (Tone(83) + .3f * Tone(125) + .1f * noise) * Envelope(4) * .37f,
                    _ => (filtered + .25f * Tone(94)) * Envelope(28) * .44f
                };
                data[i] *= Mathf.Min(1, (data.Length - i) / 100f);
            }
            var clip = AudioClip.Create("Quay " + cue, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
