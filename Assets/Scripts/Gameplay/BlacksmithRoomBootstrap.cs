using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

namespace ImagineQuest.Gameplay
{
    /// <summary>Builds the Aurora workshop environment and its repair quest experience.</summary>
    public sealed partial class BlacksmithRoomBootstrap : MonoBehaviour
    {
        [SerializeField] private Material arenaWood;
        [SerializeField] private Texture2D weatheredStone;
        [SerializeField] private RenderPipelineAsset workshopPipeline;
        private RenderPipelineAsset previousPipeline;
        [SerializeField] private GameObject arenaBarrel;
        [SerializeField] private GameObject arenaChest;
        private static readonly Vector3 AnvilPosition = new Vector3(0f, 0f, 4.2f);
        private Material timber;
        private Material planks;
        private Light forgeLight;
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Material> materials = new List<Material>();
        private CharacterController player;
        private Transform view;
        private float pitch;
        private float fallSpeed;
        private bool paused;

        private void Awake()
        {
            Time.timeScale = 1f;
            previousPipeline = QualitySettings.renderPipeline;
            if (workshopPipeline != null) QualitySettings.renderPipeline = workshopPipeline;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.36f, 0.37f, 0.42f);
            RenderSettings.skybox = null;
            var stone = Surface(new Color(0.27f, 0.25f, 0.23f));
            var wood = Surface(new Color(0.6f, 0.48f, 0.34f));
            if (arenaWood != null) wood.mainTexture = arenaWood.GetTexture("_MainTex");
            wood.color = new Color(0.78f, 0.68f, 0.54f);
            timber = wood;
            planks = new Material(wood);
            materials.Add(planks);
            AddStoneTexture(stone);
            var iron = Surface(new Color(0.28f, 0.29f, 0.3f), 0.35f);
            iron.SetFloat("_Smoothness", 0.42f);
            var ember = Surface(new Color(0.22f, 0.045f, 0.012f));
            ember.EnableKeyword("_EMISSION");
            ember.SetColor("_EmissionColor", new Color(0.85f, 0.12f, 0.005f));

            BuildFloor(wood);
            Block("Back wall", new Vector3(0, 2, 5), new Vector3(10, 4, 0.4f), stone);
            Block("Left wall", new Vector3(-5, 2, 0), new Vector3(0.4f, 4, 10), stone);
            Block("Right wall", new Vector3(5, 2, 0), new Vector3(0.4f, 4, 10), stone);
            Block("Front wall", new Vector3(0, 2, -5), new Vector3(10, 4, 0.4f), stone);
            Block("Ceiling", new Vector3(0, 4.1f, 0), new Vector3(10, 0.2f, 10), wood);
            for (var x = -4; x <= 4; x += 2)
            {
                Block("Ceiling beam", new Vector3(x, 3.85f, 0), new Vector3(0.25f, 0.3f, 10), wood);
                Block("Wall timber", new Vector3(x, 2, 4.75f), new Vector3(0.22f, 4, 0.25f), wood);
            }

            Block("Forge hearth", new Vector3(-2.7f, 0.45f, 3.6f), new Vector3(2.5f, 0.9f, 2), stone);
            Block("Recessed coal bed", new Vector3(-2.7f, 0.94f, 3.6f), new Vector3(1.6f, 0.08f, 1.1f), iron);
            Block("Forge left pillar", new Vector3(-3.8f, 1.65f, 4), new Vector3(0.35f, 2.4f, 1.3f), stone);
            Block("Forge right pillar", new Vector3(-1.6f, 1.65f, 4), new Vector3(0.35f, 2.4f, 1.3f), stone);
            Block("Chimney hood", new Vector3(-2.7f, 3.15f, 4), new Vector3(2.6f, 1.3f, 1.6f), stone);
            forgeLight = Lamp("Forge glow", new Vector3(-2.7f, 1.5f, 3.3f), new Color(1, 0.47f, 0.2f), 2.4f, 6);

            BuildAnvil(wood, iron);

            Block("Workbench", new Vector3(3.6f, 1.05f, 2.4f), new Vector3(1.6f, 0.18f, 3.3f), wood);
            foreach (var z in new[] { 1.1f, 3.7f })
                foreach (var x in new[] { 3f, 4.2f })
                    Block("Bench leg", new Vector3(x, 0.5f, z), new Vector3(0.18f, 1, 0.18f), wood);
            Block("Hammer handle", new Vector3(3.4f, 1.2f, 2.2f), new Vector3(0.1f, 0.1f, 0.7f), wood);
            Block("Hammer head", new Vector3(3.4f, 1.3f, 2.5f), new Vector3(0.45f, 0.22f, 0.2f), iron);
            for (var i = 0; i < 4; i++)
                Block("Iron ingot", new Vector3(3.8f, 1.22f, 3f + i * 0.16f), new Vector3(0.5f, 0.12f, 0.12f), iron);
            Block("Tool rack", new Vector3(4.7f, 2.2f, 2.4f), new Vector3(0.12f, 0.2f, 2.5f), wood);
            for (var i = 0; i < 3; i++)
            {
                Block("Hanging tool handle", new Vector3(4.55f, 1.9f, 1.6f + i * 0.7f), new Vector3(0.08f, 0.7f, 0.08f), wood);
                Block("Hanging tool head", new Vector3(4.55f, 2.28f, 1.6f + i * 0.7f), new Vector3(0.18f, 0.15f, 0.35f), iron);
            }
            Block("Workshop door", new Vector3(0, 1.25f, -4.75f), new Vector3(1.6f, 2.5f, 0.15f), wood);
            Block("Door handle", new Vector3(0.55f, 1.15f, -4.6f), new Vector3(0.12f, 0.12f, 0.12f), iron);
            Lamp("Warm ceiling light", new Vector3(0, 3.5f, 0), new Color(1, 0.78f, 0.5f), 4.8f, 12);

            var fill = Lamp("Soft workshop fill", new Vector3(0, 2.8f, -2), new Color(0.40f, 0.65f, 1f), 2.8f, 10);
            fill.shadows = LightShadows.None;
            DressWorkshop(wood, iron, stone, ember);

            var actor = new GameObject("First Person Player");
            actor.transform.position = new Vector3(0, 0.05f, -2.8f);
            player = actor.AddComponent<CharacterController>();
            player.height = 1.8f;
            player.radius = 0.28f;
            player.center = new Vector3(0, 0.9f, 0);
            var cameraObject = new GameObject("Blacksmith Camera");
            cameraObject.tag = "MainCamera";
            view = cameraObject.transform;
            view.SetParent(actor.transform, false);
            view.localPosition = new Vector3(0, 1.65f, 0);
            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.06f, 0.04f);
            cameraObject.AddComponent<AudioListener>();
            camera.fieldOfView = 68;
            BuildAtmosphere(wood, iron, stone);
            BuildInterface();
            InitializeQuest();
        }

        private void BuildFloor(Material wood)
        {
            var random = new System.Random(34);
            for (var board = 0; board < 16; board++)
                for (var section = 0; section < 4; section++)
                {
                    var material = new Material(wood);
                    var shade = 0.76f + (float)random.NextDouble() * 0.15f;
                    material.color = new Color(shade, shade * 0.87f, shade * 0.69f);
                    materials.Add(material);
                    Block("Weathered oak floorboard", new Vector3(-4.6875f + board * 0.625f, -0.08f, -3.75f + section * 2.5f),
                        new Vector3(0.619f, 0.16f, 2.49f), material);
                }
            // A continuous hidden support keeps the controller grounded at board seams.
            Block("Floor support", new Vector3(0, -0.26f, 0), new Vector3(10, 0.2f, 10), wood);
        }

        private void BuildAnvil(Material wood, Material iron)
        {
            var stump = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stump.name = "Anvil oak stump";
            stump.transform.SetParent(transform, false);
            stump.transform.position = AnvilPosition + Vector3.up * 0.4f;
            stump.transform.localScale = new Vector3(0.92f, 0.4f, 0.85f);
            stump.GetComponent<Renderer>().sharedMaterial = wood;
            var stumpMesh = Instantiate(stump.GetComponent<MeshFilter>().sharedMesh);
            var stumpUv = stumpMesh.uv;
            for (var i = 0; i < stumpUv.Length; i++)
                stumpUv[i] = new Vector2(0.025f + stumpUv[i].x * 0.21f, 0.025f + stumpUv[i].y * 0.18f);
            stumpMesh.uv = stumpUv;
            stump.GetComponent<MeshFilter>().sharedMesh = stumpMesh;
            meshes.Add(stumpMesh);

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var heights = new[] { 0.8f, 0.85f, 0.91f, 1.01f, 1.1f, 1.14f, 1.22f, 1.25f };
            var widths = new[] { 0.37f, 0.39f, 0.32f, 0.2f, 0.21f, 0.5f, 0.5f, 0.48f };
            var depths = new[] { 0.26f, 0.28f, 0.22f, 0.17f, 0.17f, 0.25f, 0.25f, 0.23f };
            var rings = new List<Vector3[]>();
            for (var ring = 0; ring < heights.Length; ring++)
            {
                var w = widths[ring]; var d = depths[ring]; var bevel = 0.035f;
                var points = new[] { new Vector2(-w + bevel, -d), new Vector2(w - bevel, -d),
                    new Vector2(w, -d + bevel), new Vector2(w, d - bevel), new Vector2(w - bevel, d),
                    new Vector2(-w + bevel, d), new Vector2(-w, d - bevel), new Vector2(-w, -d + bevel) };
                var positions = new Vector3[8];
                for (var j = 0; j < 8; j++) positions[j] = AnvilPosition + new Vector3(points[j].x, heights[ring], points[j].y);
                rings.Add(positions);
            }
            for (var ring = 0; ring < rings.Count - 1; ring++)
                for (var j = 0; j < 8; j++)
                {
                    var next = (j + 1) % 8;
                    Quad(vertices, triangles, rings[ring][j], rings[ring + 1][j], rings[ring + 1][next], rings[ring][next]);
                }
            for (var j = 1; j < 7; j++)
            {
                Triangle(vertices, triangles, rings[0][0], rings[0][j], rings[0][j + 1]);
                var top = rings[rings.Count - 1];
                Triangle(vertices, triangles, top[0], top[j + 1], top[j]);
            }
            MeshObject("Forged steel anvil", vertices, triangles, iron);
            vertices.Clear(); triangles.Clear();
            for (var ring = 0; ring < 5; ring++)
                for (var segment = 0; segment < 24; segment++)
                {
                    var t = ring / 5f;
                    var tNext = (ring + 1) / 5f;
                    var a = segment * Mathf.PI * 2 / 24;
                    var b = (segment + 1) * Mathf.PI * 2 / 24;
                    Quad(vertices, triangles, Horn(t, a), Horn(tNext, a), Horn(tNext, b), Horn(t, b));
                }
            MeshObject("Tapered anvil horn", vertices, triangles, iron);
        }

        private static Vector3 Horn(float t, float angle)
        {
            var radius = Mathf.Lerp(0.22f, 0.012f, t);
            return AnvilPosition + new Vector3(0.5f + t * 0.72f, 1.13f + Mathf.Sin(angle) * radius * 0.45f + t * 0.06f,
                Mathf.Cos(angle) * radius);
        }

        private void MeshObject(string label, List<Vector3> vertices, List<int> triangles, Material material)
        {
            var mesh = new Mesh { name = label };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            meshes.Add(mesh);
            var actor = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            actor.transform.SetParent(transform, false);
            actor.GetComponent<MeshFilter>().sharedMesh = mesh;
            actor.GetComponent<MeshRenderer>().sharedMaterial = material;
            actor.GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        private static void Triangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
        {
            var offset = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            triangles.Add(offset); triangles.Add(offset + 1); triangles.Add(offset + 2);
        }

        private static void Quad(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Triangle(vertices, triangles, a, b, c); Triangle(vertices, triangles, a, c, d);
        }

        private static Mesh BeveledBox(Vector3 size)
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            var half = size * 0.5f;
            var radius = Mathf.Min(0.025f, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.18f);
            var inner = half - Vector3.one * radius;
            var normals = new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var normal in normals)
            {
                var tangent = Mathf.Abs(normal.y) > 0.5f ? Vector3.right : Vector3.up;
                var bitangent = Vector3.Cross(normal, tangent);
                var width = Vector3.Dot(new Vector3(Mathf.Abs(tangent.x), Mathf.Abs(tangent.y), Mathf.Abs(tangent.z)), half);
                var height = Vector3.Dot(new Vector3(Mathf.Abs(bitangent.x), Mathf.Abs(bitangent.y), Mathf.Abs(bitangent.z)), half);
                var depth = Vector3.Dot(new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z)), half);
                var stepsU = new[] { -width, -width + radius, width - radius, width };
                var stepsV = new[] { -height, -height + radius, height - radius, height };
                var start = vertices.Count;
                for (var y = 0; y < 4; y++)
                    for (var x = 0; x < 4; x++)
                    {
                        var point = normal * depth + tangent * stepsU[x] + bitangent * stepsV[y];
                        var core = new Vector3(Mathf.Clamp(point.x, -inner.x, inner.x), Mathf.Clamp(point.y, -inner.y, inner.y), Mathf.Clamp(point.z, -inner.z, inner.z));
                        point = core + (point - core).normalized * radius;
                        vertices.Add(new Vector3(point.x / size.x, point.y / size.y, point.z / size.z));
                        uv.Add(new Vector2((stepsU[x] / width + 1) * 0.5f, (stepsV[y] / height + 1) * 0.5f));
                    }
                for (var y = 0; y < 3; y++)
                    for (var x = 0; x < 3; x++)
                    {
                        var a = start + y * 4 + x;
                        triangles.Add(a); triangles.Add(a + 1); triangles.Add(a + 5);
                        triangles.Add(a); triangles.Add(a + 5); triangles.Add(a + 4);
                    }
            }
            var mesh = new Mesh { name = "Worn beveled joinery" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private void AddStoneTexture(Material stone)
        {
            stone.mainTexture = weatheredStone;
            stone.mainTextureScale = Vector2.one;
            stone.color = new Color(0.8f, 0.77f, 0.72f);
        }

        private void DressWorkshop(Material wood, Material iron, Material stone, Material ember)
        {
            // Plank seams and iron fittings break up the broad primitive surfaces.
            for (var x = -4; x <= 4; x += 2)
            {
                Block("Front timber post", new Vector3(x, 2, -4.75f), new Vector3(0.22f, 4, 0.25f), wood);
                Block("Timber iron collar", new Vector3(x, 0.45f, 4.58f), new Vector3(0.26f, 0.12f, 0.08f), iron);
            }
            for (var z = -4; z <= 4; z += 2)
            {
                Block("Left timber post", new Vector3(-4.75f, 2, z), new Vector3(0.25f, 4, 0.22f), wood);
                Block("Right timber post", new Vector3(4.75f, 2, z), new Vector3(0.25f, 4, 0.22f), wood);
            }

            Block("Forge stone apron", new Vector3(-2.7f, 0.015f, 2.4f), new Vector3(3.3f, 0.04f, 2.7f), stone);
            for (var i = 0; i < 28; i++)
            {
                var coal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                coal.name = "Glowing forge coal";
                coal.transform.SetParent(transform, false);
                coal.transform.position = new Vector3(-3.3f + (i % 7) * 0.2f, 1.01f + (i % 3) * 0.015f, 3.25f + (i / 7) * 0.24f);
                coal.transform.localScale = new Vector3(0.16f + (i % 3) * 0.02f, 0.08f, 0.16f);
                coal.GetComponent<Renderer>().sharedMaterial = ember;
                coal.GetComponent<Collider>().enabled = false;
            }
            foreach (var x in new[] { -4.55f, 4.55f })
            {
                var position = new Vector3(x, 2.4f, -1.4f);
                Block("Lantern bracket", position, new Vector3(0.4f, 0.12f, 0.12f), iron);
                Block("Lantern base", position + Vector3.down * 0.25f, new Vector3(0.3f, 0.08f, 0.3f), iron);
                Block("Lantern crown", position + Vector3.up * 0.2f, new Vector3(0.3f, 0.08f, 0.3f), iron);
                Block("Lantern warm glass", position, new Vector3(0.18f, 0.35f, 0.18f), ember);
                Lamp("Aurora wall lantern", position + new Vector3(-Mathf.Sign(x) * 0.3f, 0, 0), new Color(1, 0.69f, 0.32f), 1.4f, 5);
            }
            PlaceProp(arenaBarrel, "Water barrel", new Vector3(-3.8f, 0, -2.5f), 1.1f, 18);
            PlaceProp(arenaBarrel, "Supply barrel", new Vector3(3.8f, 0, -3.2f), 1.3f, -12);
            PlaceProp(arenaChest, "Blacksmith supply chest", new Vector3(2.3f, 0, -3.8f), 0.75f, 180);
            Block("Lower workbench shelf", new Vector3(3.6f, 0.3f, 2.4f), new Vector3(1.5f, 0.12f, 3.1f), wood);
            for (var i = 0; i < 3; i++)
                Block("Stored steel billet", new Vector3(3.5f, 0.42f, 1.8f + i * 0.3f), new Vector3(0.9f, 0.12f, 0.2f), iron);
            Block("Door lower iron strap", new Vector3(0, 0.6f, -4.64f), new Vector3(1.55f, 0.09f, 0.08f), iron);
            Block("Door upper iron strap", new Vector3(0, 1.9f, -4.64f), new Vector3(1.55f, 0.09f, 0.08f), iron);
        }

        private void PlaceProp(GameObject prefab, string label, Vector3 position, float height, float yaw)
        {
            if (prefab == null) return;
            var prop = Instantiate(prefab, transform);
            prop.name = label;
            prop.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var renderers = prop.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            prop.transform.localScale *= height / Mathf.Max(bounds.size.y, 0.01f);
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            prop.transform.position += position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            var collider = prop.AddComponent<BoxCollider>();
            collider.center = prop.transform.InverseTransformPoint(bounds.center + position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
            collider.size = new Vector3(bounds.size.x / prop.transform.lossyScale.x, bounds.size.y / prop.transform.lossyScale.y, bounds.size.z / prop.transform.lossyScale.z);
        }

        private Material Surface(Color color, float metallic = 0)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", 0.25f);
            materials.Add(material);
            return material;
        }

        private void Block(string label, Vector3 position, Vector3 scale, Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = label;
            block.transform.SetParent(transform, false);
            block.transform.position = position;
            block.transform.localScale = scale;
            var bevelMesh = BeveledBox(scale);
            block.GetComponent<MeshFilter>().sharedMesh = bevelMesh;
            meshes.Add(bevelMesh);
            block.GetComponent<Renderer>().sharedMaterial = material;
            if (material.mainTexture == weatheredStone && weatheredStone != null)
            {
                var filter = block.GetComponent<MeshFilter>();
                var mesh = Instantiate(filter.sharedMesh);
                var uv = mesh.uv;
                var normals = mesh.normals;
                for (var i = 0; i < uv.Length; i++)
                {
                    var n = normals[i];
                    var tile = Mathf.Abs(n.y) > 0.5f ? new Vector2(scale.x, scale.z)
                        : Mathf.Abs(n.x) > 0.5f ? new Vector2(scale.z, scale.y) : new Vector2(scale.x, scale.y);
                    uv[i] = Vector2.Scale(uv[i], tile * 0.35f);
                }
                mesh.uv = uv;
                filter.sharedMesh = mesh;
                meshes.Add(mesh);
            }
            if (arenaWood != null && material.mainTexture != null && material.mainTexture == timber.mainTexture)
            {
                var filter = block.GetComponent<MeshFilter>();
                var mesh = Instantiate(filter.sharedMesh);
                var uv = mesh.uv;
                for (var i = 0; i < uv.Length; i++)
                    uv[i] = material == planks
                        ? new Vector2(0.025f + uv[i].x * 0.21f, 0.025f + uv[i].y * 0.18f)
                        : new Vector2(0.015f + uv[i].x * 0.24f, 0.015f + uv[i].y * 0.20f);
                mesh.uv = uv;
                filter.sharedMesh = mesh;
                meshes.Add(mesh);
            }
        }

        private Light Lamp(string label, Vector3 position, Color color, float intensity, float range)
        {
            var lamp = new GameObject(label);
            lamp.transform.SetParent(transform, false);
            lamp.transform.position = position;
            var light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.7f;
            return light;
        }

    }
}
