using System.Collections.Generic;
using UnityEngine;

namespace ImagineQuest.Gameplay
{
    /// <summary>Session-only reward for the three workshop stations. No saved XP is awarded.</summary>
    public sealed class WorkshopForgeReward : MonoBehaviour
    {
        private const float ActivationRadius = 1.15f;
        private static readonly Vector3 StonePosition = new Vector3(0f, 0.85f, 0.2f);
        private static readonly Vector3 PortalPosition = new Vector3(0f, 1.75f, -2.4f);
        private readonly List<Material> materials = new List<Material>();
        private readonly List<Mesh> meshes = new List<Mesh>();
        private GameObject stone;
        private GameObject portal;
        private float openedAt;

        public bool StoneRevealed { get; private set; }
        public bool PortalOpened { get; private set; }

        // The caller supplies the number of distinct, correctly answered stations.
        public void SetCompletedStations(int completed)
        {
            if (completed < 3 || StoneRevealed) return;
            StoneRevealed = true;
            stone = new GameObject("Forge Stone Reward");
            stone.transform.SetParent(transform, false);
            stone.transform.localPosition = StonePosition;
            var rock = Lit(new Color(0.13f, 0.22f, 0.16f));
            var crystal = Lit(new Color(0.4f, 1f, 0.16f), 1.8f);
            Shape("Ancient forge stone", stone.transform, PrimitiveType.Sphere, Vector3.zero,
                new Vector3(0.48f, 0.65f, 0.42f), rock);
            var gem = Shape("Glowing emerald core", stone.transform, PrimitiveType.Cube,
                new Vector3(0f, 0.08f, -0.04f), Vector3.one * 0.32f, crystal);
            gem.transform.localRotation = Quaternion.Euler(25f, 45f, 35f);
            Glow(stone.transform, 1.6f, 3f);
        }

        /// <summary>Called only while the learner has control, never from a modal or pause menu.</summary>
        public void CheckPlayerProximity(Vector3 worldPosition)
        {
            if (!StoneRevealed || PortalOpened) return;
            var offset = transform.InverseTransformPoint(worldPosition) - StonePosition;
            // Compare the player's feet with the stone's floor position, including deck height.
            if (Mathf.Abs(offset.y + StonePosition.y) > 0.75f) return;
            offset.y = 0f;
            if (offset.sqrMagnitude > ActivationRadius * ActivationRadius) return;
            PortalOpened = true;
            stone.SetActive(false);
            BuildPortal();
            openedAt = Time.time;
        }

        private void Update()
        {
            if (stone != null && !PortalOpened)
            {
                stone.transform.localPosition = StonePosition + Vector3.up * (Mathf.Sin(Time.time * 2f) * 0.09f);
                stone.transform.Rotate(0f, 35f * Time.deltaTime, 0f, Space.Self);
            }
            if (portal != null)
                portal.transform.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01((Time.time - openedAt) / 0.9f));
        }

        private void BuildPortal()
        {
            portal = new GameObject("Living Emerald Portal");
            portal.transform.SetParent(transform, false);
            portal.transform.localPosition = PortalPosition;
            portal.transform.localScale = Vector3.zero;
            var bark = Lit(new Color(0.24f, 0.11f, 0.035f));
            var vine = Lit(new Color(0.38f, 0.23f, 0.07f));
            var leaf = Lit(new Color(0.24f, 0.52f, 0.035f));
            var petals = Lit(new Color(1f, 0.94f, 0.72f));
            var pollen = Lit(new Color(1f, 0.65f, 0.08f), 0.4f);
            // Three intertwined woody vines frame a tall oval, like the reference image.
            for (var strand = 0; strand < 3; strand++)
                BuildVine(strand, strand == 0 ? bark : vine);
            for (var i = 0; i < 32; i++)
            {
                var angle = i * Mathf.PI * 2f / 32f;
                var radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                var position = new Vector3(radial.x * 1.13f, radial.y * 1.55f, Mathf.Sin(i * 2f) * 0.14f);
                var foliage = Shape("Emerald leaf", portal.transform, PrimitiveType.Sphere,
                    position + radial * 0.12f, new Vector3(0.16f, 0.36f, 0.065f), leaf);
                foliage.transform.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg - 60f);
                if (i % 4 != 0) continue;
                // Flowers are visible from either side of the portal.
                for (var side = -1; side <= 1; side += 2)
                {
                    var center = position + Vector3.forward * (side * 0.19f);
                    Shape("Golden flower heart", portal.transform, PrimitiveType.Sphere,
                        center, Vector3.one * 0.12f, pollen);
                    for (var p = 0; p < 5; p++)
                    {
                        var a = p * Mathf.PI * 2f / 5f;
                        var petal = Shape("Ivory petal", portal.transform, PrimitiveType.Sphere,
                            center + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 0.115f,
                            new Vector3(0.13f, 0.22f, 0.055f), petals);
                        petal.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg - 90f);
                    }
                }
            }
            var shader = Resources.Load<Shader>("Workshop/EmeraldPortal");
            if (shader != null)
            {
                var surface = new Material(shader);
                materials.Add(surface);
                Shape("Swirling emerald gateway", portal.transform, PrimitiveType.Quad,
                    Vector3.zero, new Vector3(2.12f, 3.02f, 1f), surface);
            }
            else
            {
                Debug.LogWarning("[Workshop] EmeraldPortal shader is missing; showing a glowing gateway.");
                Shape("Emerald gateway", portal.transform, PrimitiveType.Sphere, Vector3.zero,
                    new Vector3(2.12f, 3.02f, 0.06f), Lit(new Color(0.12f, 0.95f, 0.25f), 1.5f));
            }
            Glow(portal.transform, 2.2f, 4f);
        }

        private void BuildVine(int strand, Material material)
        {
            const int segments = 96;
            const int sides = 8;
            var vertices = new Vector3[(segments + 1) * sides];
            var triangles = new int[segments * sides * 6];
            for (var i = 0; i <= segments; i++)
            {
                var a = i * Mathf.PI * 2f / segments;
                var twist = a * 7f + strand * Mathf.PI * 2f / 3f;
                var radial = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                var center = new Vector3(Mathf.Cos(a) * 1.08f, Mathf.Sin(a) * 1.48f, 0f)
                    + radial * (Mathf.Cos(twist) * 0.065f) + Vector3.forward * (Mathf.Sin(twist) * 0.09f);
                for (var s = 0; s < sides; s++)
                {
                    var b = s * Mathf.PI * 2f / sides;
                    vertices[i * sides + s] = center + (radial * Mathf.Cos(b) + Vector3.forward * Mathf.Sin(b)) * 0.085f;
                    if (i == segments) continue;
                    var index = (i * sides + s) * 6;
                    var here = i * sides + s;
                    var next = i * sides + (s + 1) % sides;
                    triangles[index] = here; triangles[index + 1] = here + sides; triangles[index + 2] = next;
                    triangles[index + 3] = next; triangles[index + 4] = here + sides; triangles[index + 5] = next + sides;
                }
            }
            var mesh = new Mesh { name = "Twisting portal vine", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            meshes.Add(mesh);
            var actor = new GameObject("Twisting wooden vine", typeof(MeshFilter), typeof(MeshRenderer));
            actor.transform.SetParent(portal.transform, false);
            actor.GetComponent<MeshFilter>().sharedMesh = mesh;
            actor.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private Material Lit(Color color, float emission = 0f)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            if (emission > 0f)
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
            var glow = new GameObject("Emerald glow", typeof(Light));
            glow.transform.SetParent(parent, false);
            var light = glow.GetComponent<Light>();
            light.color = new Color(0.45f, 1f, 0.16f);
            light.intensity = intensity;
            light.range = range;
            light.type = LightType.Point;
        }

        private void OnDestroy()
        {
            foreach (var material in materials) Destroy(material);
            foreach (var mesh in meshes) Destroy(mesh);
        }
    }
}
