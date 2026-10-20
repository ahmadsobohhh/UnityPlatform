using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ImagineQuest.Gameplay
{
    public sealed partial class BlacksmithRoomBootstrap
    {
        private readonly List<Transform> motes = new List<Transform>();
        private readonly List<Vector3> moteOrigins = new List<Vector3>();
        private readonly List<Transform> portalShards = new List<Transform>();
        private readonly List<Transform> flames = new List<Transform>();
        private readonly List<Transform> stationHalos = new List<Transform>();
        private float celebrationUntil;
        private int celebratingStation;

        private Material Glow(Color color, float strength = 1)
        {
            var material = Surface(color, .25f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * strength);
            return material;
        }

        private Transform Decor(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            go.GetComponent<Collider>().enabled = false;
            return go.transform;
        }

        private Transform Ring(string name, Vector3 center, float radius, float thickness, Material material, bool horizontal = false)
        {
            var parent = new GameObject(name).transform;
            parent.SetParent(transform, false);
            parent.position = center;
            // Joined segments form an inexpensive, readable metal/rune ring.
            for (int i = 0; i < 48; i++)
            {
                float a = i * Mathf.PI * 2 / 48;
                var segment = Decor(name + " segment", PrimitiveType.Cube,
                    center + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0),
                    new Vector3(thickness, radius * .145f, thickness), material);
                segment.rotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg);
                segment.SetParent(parent, true);
            }
            if (horizontal) parent.rotation = Quaternion.Euler(90, 0, 0);
            return parent;
        }

        private TMP_Text WorldLabel(string name, string copy, Vector3 position, Vector2 size, int fontSize, Color color, bool billboard = false, float yaw = 0)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale = Vector3.one * .003f;
            go.GetComponent<RectTransform>().sizeDelta = size;
            go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            if (billboard) go.AddComponent<CelestialBillboard>();
            var plate = Panel(name + " backing", go.transform, new Vector2(.5f, .5f), Vector2.zero, size, Ink);
            var label = Text(plate, name + " text", new Vector2(.5f, .5f), Vector2.zero,
                size - new Vector2(20, 8), fontSize, color, TextAlignmentOptions.Center);
            label.text = copy;
            return label;
        }

        private void BuildAtmosphere(Material wood, Material iron, Material stone)
        {
            var brass = Surface(new Color(.65f, .40f, .12f), .7f);
            var blue = Surface(new Color(.035f, .13f, .20f));
            var paleStone = Surface(new Color(.38f, .40f, .39f));
            var fire = Glow(new Color(1, .28f, .025f), 2.5f);
            var hot = Glow(new Color(1, .65f, .12f), 2);
            var moon = Glow(new Color(.14f, .40f, .65f), .7f);
            runeIdle = Glow(new Color(.72f, .37f, .08f), .8f);
            runeComplete = Glow(new Color(.1f, .72f, .62f), 1.4f);
            portalMaterial = Glow(new Color(.12f, .55f, .65f), .5f);

            // Brick courses and inset trim give the forge a more architectural silhouette.
            for (int row = 0; row < 4; row++)
                for (int col = 0; col < 7; col++)
                    Decor("Hearth masonry", PrimitiveType.Cube,
                        new Vector3(-3.78f + col * .36f, .16f + row * .20f, 2.57f),
                        new Vector3(.34f, .18f, .10f), (row + col) % 3 == 0 ? paleStone : stone);
            Block("Forge lintel", new Vector3(-2.7f, 2.5f, 3.22f), new Vector3(2.52f, .25f, .26f), iron);
            WorldLabel("Forge inscription", "IGNIS  /  THE HEART OF THE WORKSHOP", new Vector3(-2.7f, 2.9f, 3.15f), new Vector2(670, 65), 22, Gold);
            for (int i = 0; i < 10; i++)
            {
                var flame = Decor("Living flame", PrimitiveType.Sphere, new Vector3(-3.3f + i * .135f, 1.18f, 3.4f + (i % 2) * .3f),
                    new Vector3(.10f, .3f, .10f), i % 2 == 0 ? hot : fire);
                flames.Add(flame);
            }
            var random = new System.Random(106);
            for (int i = 0; i < 38; i++)
            {
                Vector3 p = new Vector3(-3.35f + (float)random.NextDouble() * 1.35f, 1.1f, 3.3f + (float)random.NextDouble() * .7f);
                motes.Add(Decor("Rising forge ember", PrimitiveType.Sphere, p, Vector3.one * (.013f + i % 3 * .006f), hot));
                moteOrigins.Add(p);
            }

            // A twelve-nail example makes the multiplication lesson visible in the room.
            for (int tray = 0; tray < 3; tray++)
            {
                float z = 1.2f + tray * .43f;
                Decor("Fastener tray", PrimitiveType.Cube, new Vector3(3.65f, 1.16f, z), new Vector3(.65f, .025f, .34f), blue);
                for (int nail = 0; nail < 4; nail++)
                {
                    float x = 3.42f + nail * .15f;
                    Decor("Repair nail shaft", PrimitiveType.Cube, new Vector3(x, 1.19f, z), new Vector3(.025f, .025f, .21f), iron);
                    Decor("Repair nail head", PrimitiveType.Cube, new Vector3(x, 1.20f, z + .11f), new Vector3(.07f, .04f, .035f), brass);
                }
            }
            WorldLabel("Workbench calculation", "3 TRAYS  x  4 NAILS", new Vector3(3.45f, 1.40f, .82f), new Vector2(420, 65), 25, Cream, true);
            Decor("Hot repair brace", PrimitiveType.Cube, new Vector3(-.1f, 1.28f, 4.17f), new Vector3(.64f, .045f, .14f), hot);

            // Navigation and completed states are represented in the world, as well as the HUD.
            for (int i = 0; i < 3; i++)
            {
                Vector3 basePos = stationPositions[i]; basePos.y = .045f;
                if (i == 0) basePos.z -= .7f;
                if (i == 1) basePos.x -= .5f;
                if (i == 2) basePos.z -= .65f;
                stationHalos.Add(Ring("Station " + i + " floor inlay", basePos, .55f, .018f, runeIdle, true));
                var rune = Decor("Station " + i + " beacon", PrimitiveType.Cube, stationPositions[i] + Vector3.up * .25f, Vector3.one * .11f, runeIdle);
                rune.rotation = Quaternion.Euler(0, 0, 45);
                stationRunes.Add(rune.GetComponent<Renderer>());
                stationLabels.Add(WorldLabel("Station " + i + " label", "", stationPositions[i] + Vector3.up * .57f, new Vector2(560, 64), 24, Gold, true));
            }

            // Large visual destination, physically separated from the work areas.
            portalRing = Ring("Return portal brass frame", PortalPosition, 1.35f, .13f, brass);
            Ring("Return portal energy", PortalPosition + Vector3.forward * .08f, 1.22f, .045f, portalMaterial);
            Ring("Return portal inner energy", PortalPosition + Vector3.forward * .10f, 1.08f, .018f, portalMaterial);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2 / 12;
                var shard = Decor("Portal rune", PrimitiveType.Cube, PortalPosition + new Vector3(Mathf.Cos(a) * 1.43f, Mathf.Sin(a) * 1.43f, .1f),
                    new Vector3(.08f, .14f, .045f), portalMaterial);
                shard.rotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg + 45);
                portalShards.Add(shard);
            }
            portalLight = Lamp("Portal cyan spill", new Vector3(0, 1.8f, -3.8f), new Color(.15f, .7f, 1), .6f, 6);
            portalLight.shadows = LightShadows.None;
            portalLabel = WorldLabel("Portal destination", "THE AURORA\nFORGE THE RETURN KEY", new Vector3(0, 3.25f, -4.28f), new Vector2(720, 125), 30, Teal, false, 180);

            // Portholes and hanging navy standards introduce cool color and a ship identity.
            for (int side = -1; side <= 1; side += 2)
            {
                var frame = Ring("Ocean porthole", new Vector3(side * 4.7f, 2.55f, -2.9f), .58f, .09f, brass);
                frame.rotation = Quaternion.Euler(0, 90, 0);
                var glass = Decor("Moonlit ocean glass", PrimitiveType.Cylinder, new Vector3(side * 4.72f, 2.55f, -2.9f), new Vector3(1.06f, .012f, 1.06f), moon);
                glass.rotation = Quaternion.Euler(0, 0, 90);
                Lamp("Porthole moonlight", new Vector3(side * 4.3f, 2.5f, -2.9f), new Color(.26f, .55f, 1), 1.3f, 5).shadows = LightShadows.None;
                Decor("Aurora hanging standard", PrimitiveType.Cube, new Vector3(side * 1.25f, 2.93f, 4.7f), new Vector3(.67f, 1.38f, .06f), blue);
                Decor("Standard gold bar", PrimitiveType.Cube, new Vector3(side * 1.25f, 3.64f, 4.64f), new Vector3(.82f, .07f, .09f), brass);
                Decor("Standard crest", PrimitiveType.Cube, new Vector3(side * 1.25f, 2.99f, 4.65f), new Vector3(.26f, .26f, .045f), brass).rotation = Quaternion.Euler(0, 0, 45);
            }
            WorldLabel("Workshop guild plaque", "A U R O R A\nTHE WAYFARER'S FORGE", new Vector3(0, 3.12f, 4.6f), new Vector2(650, 120), 29, Gold);

            // The repair kit visibly fills as each craft is mastered.
            Block("Repair kit display", new Vector3(-3.7f, .65f, -1), new Vector3(1.4f, .16f, 1), wood);
            Block("Display pedestal", new Vector3(-3.7f, .3f, -1), new Vector3(.85f, .6f, .65f), stone);
            repairParts.Add(Decor("Completed hull brace", PrimitiveType.Cube, new Vector3(-4.05f, .81f, -1), new Vector3(.22f, .12f, .65f), brass).gameObject);
            repairParts.Add(Decor("Completed fastener box", PrimitiveType.Cube, new Vector3(-3.7f, .84f, -1), new Vector3(.3f, .2f, .4f), iron).gameObject);
            repairParts.Add(Decor("Completed prepared ingot", PrimitiveType.Cube, new Vector3(-3.3f, .82f, -1), new Vector3(.24f, .16f, .5f), brass).gameObject);
            WorldLabel("Repair kit sign", "THE CAPTAIN'S REPAIR KIT\nYour work takes shape here", new Vector3(-3.7f, 1.2f, -1), new Vector2(540, 110), 24, Cream, true);
            BuildWorkshopAudio();
        }

        private void AnimateAtmosphere(float time)
        {
            for (int i = 0; i < motes.Count; i++)
            {
                float phase = Mathf.Repeat(time * (.28f + i % 4 * .035f) + i * .137f, 1);
                motes[i].position = moteOrigins[i] + new Vector3(Mathf.Sin(time * 1.3f + i) * .10f, phase * 1.6f, Mathf.Cos(time + i) * .07f);
                motes[i].localScale = Vector3.one * .025f * (1 - phase);
            }
            for (int i = 0; i < flames.Count; i++)
                flames[i].localScale = new Vector3(.09f, .16f + Mathf.PerlinNoise(time * 4, i * .7f) * .34f, .09f);
            for (int i = 0; i < portalShards.Count; i++)
                portalShards[i].localScale = new Vector3(.08f, .14f, .045f) * (1 + Mathf.Sin(time * 1.6f + i * .5f) * .12f);
            for (int i = 0; i < stationRunes.Count; i++)
            {
                stationRunes[i].transform.rotation = Quaternion.Euler(0, time * 32, 45);
            }
            if (time < celebrationUntil && celebratingStation < stationRunes.Count)
                stationRunes[celebratingStation].transform.localScale = Vector3.one * (.14f + Mathf.Sin(time * 12) * .035f);
            else foreach (var rune in stationRunes) rune.transform.localScale = Vector3.one * .11f;
        }

        private void CelebrateStation(int station)
        {
            celebratingStation = station;
            celebrationUntil = Time.time + 2.5f;
        }

        private void BuildWorkshopAudio()
        {
            effectsAudio = gameObject.AddComponent<AudioSource>();
            effectsAudio.playOnAwake = false;
            effectsAudio.spatialBlend = 0;
            correctSound = Chime("Craft complete", new[] { 523.25f, 659.25f, 783.99f }, .65f);
            keySound = Chime("Return key forged", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 1.3f);
            errorSound = Chime("Try again", new[] { 293.66f, 261.63f }, .25f);
            var hearth = new GameObject("Quiet hearth ambience", typeof(AudioSource));
            hearth.transform.SetParent(transform, false);
            hearth.transform.position = new Vector3(-2.7f, 1.3f, 3.4f);
            var ambient = hearth.GetComponent<AudioSource>();
            const int rate = 22050;
            var samples = new float[rate * 4];
            var random = new System.Random(73);
            float smooth = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                smooth = Mathf.Lerp(smooth, (float)random.NextDouble() * 2 - 1, .06f);
                samples[i] = smooth * .25f * Mathf.Min(1, Mathf.Min(i, samples.Length - 1 - i) / 300f);
            }
            var clip = AudioClip.Create("Hearth embers", samples.Length, 1, rate, false);
            clip.SetData(samples, 0); generatedAudio.Add(clip);
            ambient.clip = clip; ambient.loop = true; ambient.volume = .18f;
            ambient.spatialBlend = 1; ambient.minDistance = 1; ambient.maxDistance = 9;
            ambient.Play();
        }

        private AudioClip Chime(string name, float[] frequencies, float duration)
        {
            const int rate = 22050;
            var samples = new float[Mathf.CeilToInt(duration * rate)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                for (int n = 0; n < frequencies.Length; n++)
                {
                    float local = t - n * duration / (frequencies.Length * 2);
                    if (local >= 0)
                        samples[i] += Mathf.Sin(local * frequencies[n] * Mathf.PI * 2) * Mathf.Exp(-local * 10) * Mathf.Min(local * 100, 1) * .16f;
                }
                samples[i] *= Mathf.Min(1, (samples.Length - 1 - i) / 250f);
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0); generatedAudio.Add(clip); return clip;
        }
    }
}
