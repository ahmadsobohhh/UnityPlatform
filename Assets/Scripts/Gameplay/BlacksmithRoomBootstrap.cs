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
    /// <summary>A standalone workshop environment; no quest progress is awarded here.</summary>
    public sealed class BlacksmithRoomBootstrap : MonoBehaviour
    {
        [SerializeField] private Material arenaWood;
        [SerializeField] private Texture2D weatheredStone;
        [SerializeField] private RenderPipelineAsset workshopPipeline;
        private RenderPipelineAsset previousPipeline;
        [SerializeField] private GameObject arenaBarrel;
        [SerializeField] private GameObject arenaChest;
        private GameObject practicePanel;
        private TMP_Text practiceQuestion;
        private TMP_Text practiceFeedback;
        private readonly List<TMP_Text> practiceAnswers = new List<TMP_Text>();
        private static readonly Vector3 AnvilPosition = new Vector3(0f, 0f, 4.2f);
        private int practiceIndex;
        private TMP_Text stationPrompt;
        private TMP_Text stationProgress;
        private readonly bool[] stationAnswered = new bool[3];
        private WorkshopForgeReward forgeReward;
        private readonly List<TMP_Text> stationLabels = new List<TMP_Text>();
        private readonly string[] stationNames = { "ANVIL", "WORKBENCH", "FORGE" };
        private readonly Vector3[] stationPositions = {
            AnvilPosition + Vector3.up * 1.35f, new Vector3(3.0f, 1.4f, 2.4f), new Vector3(-2.7f, 1.4f, 2.7f)
        };
        private readonly string[] questions = {
            "Which tool gives a blacksmith a solid surface for shaping metal?",
            "A smith makes 3 batches of 4 nails. How many nails is that?",
            "What does the forge do before metal is shaped?"
        };
        private readonly string[][] answers = {
            new[] { "An anvil", "A barrel", "A compass" },
            new[] { "7 nails", "12 nails", "16 nails" },
            new[] { "Cools the metal", "Paints the metal", "Heats the metal" }
        };
        private GameObject pausePanel;
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
            RenderSettings.ambientLight = new Color(0.34f, 0.35f, 0.39f);
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
            Lamp("Warm ceiling light", new Vector3(0, 3.5f, 0), new Color(1, 0.78f, 0.5f), 4f, 12);

            var fill = Lamp("Soft workshop fill", new Vector3(0, 2.8f, -2), new Color(1f, 0.84f, 0.65f), 3.5f, 10);
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
            var rewardRoot = new GameObject("Workshop Completion Reward");
            rewardRoot.transform.SetParent(transform, false);
            // The workshop geometry is positioned in world space.
            rewardRoot.transform.position = Vector3.zero;
            rewardRoot.transform.rotation = Quaternion.identity;
            forgeReward = rewardRoot.AddComponent<WorkshopForgeReward>();
            BuildInterface();
            SetPaused(false);
            BuildQuestionStations();
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

        private void Update()
        {
            if (forgeLight != null) forgeLight.intensity = 2.1f + Mathf.PerlinNoise(Time.time * 3f, 0.5f) * 0.35f;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (practicePanel != null && practicePanel.activeSelf) SetPractice(false);
                else SetPaused(!paused);
            }

            if (practicePanel != null && practicePanel.activeSelf) return;
            if (paused || Cursor.lockState != CursorLockMode.Locked) return;
            var station = NearbyQuestionStation();
            stationPrompt.text = forgeReward.PortalOpened ? "PORTAL OPEN • The forge stone awakened the gateway!"
                : forgeReward.StoneRevealed ? "Walk to the glowing forge stone in the center of the workshop"
                : station < 0 ? "Explore the anvil, workbench and forge" : "PRESS E • " + stationNames[station] + " QUESTION";
            if (station >= 0 && keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                practiceIndex = station;
                ShowPracticeQuestion();
                SetPractice(true);
                return;
            }
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var look = mouse.delta.ReadValue() * 0.1f;
                player.transform.Rotate(0, look.x, 0);
                pitch = Mathf.Clamp(pitch - look.y, -80, 80);
                view.localRotation = Quaternion.Euler(pitch, 0, 0);
            }
            var move = Vector2.zero;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y++;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y--;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x++;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x--;
            }
            move = Vector2.ClampMagnitude(move, 1);
            fallSpeed = player.isGrounded ? -2f : fallSpeed + Physics.gravity.y * Time.deltaTime;
            var velocity = (player.transform.forward * move.y + player.transform.right * move.x) * 2.8f;
            velocity.y = fallSpeed;
            player.Move(velocity * Time.deltaTime);
            forgeReward.CheckPlayerProximity(player.transform.position);
        }

        private void SetPaused(bool value)
        {
            paused = value;
            if (pausePanel != null) pausePanel.SetActive(value);
            Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = value;
        }

        private void BuildInterface()
        {
            if (EventSystem.current == null)
                new GameObject("Workshop Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var canvas = new GameObject("Workshop UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var gold = new Color(1f, 0.85f, 0.5f);
            var cream = new Color(0.97f, 0.93f, 0.83f);
            var title = CelestialClockQuestBootstrap.CreateText("Workshop Title", canvas.transform, Vector2.up, Vector2.up,
                new Vector2(32, -28), new Vector2(650, 40), 24, TextAlignmentOptions.Left, gold);
            title.text = "THE AURORA  \u2022  BLACKSMITH WORKSHOP";
            title.fontStyle = FontStyles.Bold;
            var hint = CelestialClockQuestBootstrap.CreateText("Controls", canvas.transform, Vector2.zero, Vector2.zero,
                new Vector2(32, 28), new Vector2(1050, 35), 20, TextAlignmentOptions.Left, cream);
            hint.text = "WASD  MOVE     /     MOUSE  LOOK     /     E  INTERACT     /     ESC  MENU";
            var crosshair = CelestialClockQuestBootstrap.CreateText("Reticle", canvas.transform, Vector2.one * 0.5f,
                Vector2.one * 0.5f, Vector2.zero, new Vector2(24, 24), 20, TextAlignmentOptions.Center, gold);
            crosshair.text = "\u00b7";
            var menu = CelestialClockQuestBootstrap.CreateButton("Menu", canvas.transform, "MENU", Vector2.one, Vector2.one,
                new Vector2(-32, -28), new Vector2(160, 44), new Color(0.14f, 0.10f, 0.06f));
            menu.onClick.AddListener(() => SetPaused(true));
            BuildPractice(canvas.transform);
            pausePanel = CelestialClockQuestBootstrap.CreateFullOverlay("Workshop Menu", canvas.transform,
                new Color(0.005f, 0.012f, 0.025f, 0.72f));
            var card = CelestialClockQuestBootstrap.CreatePanel("Workshop Menu Card", pausePanel.transform,
                Vector2.one * 0.5f, new Vector2(650, 420), Vector2.zero, new Color(0.055f, 0.04f, 0.025f, 0.98f), 12);
            var heading = CelestialClockQuestBootstrap.CreateText("Menu Title", card.transform, new Vector2(0.5f, 1),
                new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(560, 50), 32, TextAlignmentOptions.Center, gold);
            heading.text = "BLACKSMITH WORKSHOP";
            heading.fontStyle = FontStyles.Bold;
            var note = CelestialClockQuestBootstrap.CreateText("Menu Copy", card.transform, Vector2.one * 0.5f,
                Vector2.one * 0.5f, new Vector2(0, 30), new Vector2(520, 70), 22, TextAlignmentOptions.Center, cream);
            note.text = "A warm hearth aboard the Aurora.\nTake a look around, or return to the ship.";
            var resume = CelestialClockQuestBootstrap.CreateButton("Resume", card.transform, "RETURN TO WORKSHOP",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 130), new Vector2(420, 55), new Color(0.4f, 0.25f, 0.075f));
            resume.onClick.AddListener(() => SetPaused(false));
            var back = CelestialClockQuestBootstrap.CreateButton("Return to Ship", card.transform, "RETURN TO SHIP",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 55), new Vector2(420, 50), new Color(0.14f, 0.10f, 0.06f));
            back.onClick.AddListener(() => SceneTransition.LoadScene("QuestArena"));
        }

        private void BuildQuestionStations()
        {
            var canvas = practicePanel.transform.parent;
            var gold = new Color(1f, 0.85f, 0.5f);
            stationPrompt = CelestialClockQuestBootstrap.CreateText("Station Interaction", canvas,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 85), new Vector2(900, 45),
                24, TextAlignmentOptions.Center, gold);
            stationProgress = CelestialClockQuestBootstrap.CreateText("Workshop Progress", canvas, Vector2.one,
                Vector2.one, new Vector2(-32, -85), new Vector2(400, 40), 22, TextAlignmentOptions.Right, gold);
            for (var i = 0; i < stationPositions.Length; i++)
            {
                var marker = new GameObject(stationNames[i] + " Question Marker", typeof(RectTransform), typeof(Canvas));
                marker.transform.SetParent(transform, false);
                marker.transform.position = stationPositions[i] + Vector3.up * 0.65f;
                marker.transform.localScale = Vector3.one * 0.002f;
                marker.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 100);
                marker.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                marker.AddComponent<CelestialBillboard>();
                var plate = CelestialClockQuestBootstrap.CreatePanel("Station Nameplate", marker.transform,
                    Vector2.one * 0.5f, new Vector2(580, 90), Vector2.zero, new Color(0.055f, 0.04f, 0.025f, 0.94f), 8);
                plate.GetComponent<Image>().raycastTarget = false;
                var label = CelestialClockQuestBootstrap.CreateText("Station Label", plate.transform, Vector2.zero,
                    Vector2.one, Vector2.zero, Vector2.zero, 30, TextAlignmentOptions.Center, gold);
                label.raycastTarget = false;
                label.text = stationNames[i] + " • QUESTION";
                stationLabels.Add(label);
            }
            UpdateStationProgress();
        }

        private int NearbyQuestionStation()
        {
            var closest = -1;
            var distance = 1.8f;
            for (var i = 0; i < stationPositions.Length; i++)
            {
                var offset = stationPositions[i] - player.transform.position;
                offset.y = 0;
                var look = stationPositions[i] - view.position;
                if (offset.magnitude < distance && Vector3.Dot(view.forward, look.normalized) > 0.55f)
                {
                    distance = offset.magnitude;
                    closest = i;
                }
            }
            return closest;
        }

        private void AnswerStation(int choice)
        {
            if (stationAnswered[practiceIndex]) return;
            if (choice != practiceIndex)
            {
                practiceFeedback.text = "Try again — choose another answer.";
                return;
            }
            stationAnswered[practiceIndex] = true;
            practiceFeedback.text = "Correct! Return to the room and find another station.";
            UpdateStationProgress();
            if (forgeReward.StoneRevealed)
                practiceFeedback.text = "All 3 complete! Walk to the glowing forge stone.";
        }

        private void UpdateStationProgress()
        {
            var completed = 0;
            for (var i = 0; i < stationAnswered.Length; i++)
            {
                if (stationAnswered[i]) completed++;
                if (i < stationLabels.Count)
                    stationLabels[i].text = stationNames[i] + (stationAnswered[i] ? " • COMPLETE" : " • QUESTION");
            }
            stationProgress.text = "WORKSHOP QUESTIONS  " + completed + " / 3";
            forgeReward.SetCompletedStations(completed);
        }
        private void BuildPractice(Transform parent)
        {
            practicePanel = CelestialClockQuestBootstrap.CreateFullOverlay("Workshop Questions", parent,
                new Color(0.005f, 0.012f, 0.025f, 0.65f));
            var card = CelestialClockQuestBootstrap.CreatePanel("Practice Card", practicePanel.transform,
                Vector2.one * 0.5f, new Vector2(740, 590), Vector2.zero, new Color(0.055f, 0.04f, 0.025f, 0.98f), 12);
            var gold = new Color(1f, 0.85f, 0.5f);
            var cream = new Color(0.97f, 0.93f, 0.83f);
            var title = CelestialClockQuestBootstrap.CreateText("Practice Title", card.transform,
                Vector2.up + Vector2.right * 0.5f, Vector2.up + Vector2.right * 0.5f,
                new Vector2(0, -42), new Vector2(660, 45), 28, TextAlignmentOptions.Center, gold);
            title.text = "BLACKSMITH PRACTICE";
            practiceQuestion = CelestialClockQuestBootstrap.CreateText("Sample Question", card.transform,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(640, 95),
                24, TextAlignmentOptions.Center, cream);
            for (var i = 0; i < 3; i++)
            {
                var choice = i;
                var answer = CelestialClockQuestBootstrap.CreateButton("Answer " + (i + 1), card.transform,
                    "", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -218 - i * 64),
                    new Vector2(580, 50), new Color(0.4f, 0.25f, 0.075f));
                practiceAnswers.Add(answer.GetComponentInChildren<TMP_Text>());
                answer.onClick.AddListener(() => AnswerStation(choice));
            }
            practiceFeedback = CelestialClockQuestBootstrap.CreateText("Answer Feedback", card.transform,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 140), new Vector2(640, 45),
                21, TextAlignmentOptions.Center, gold);
            var close = CelestialClockQuestBootstrap.CreateButton("Close Practice", card.transform, "BACK TO WORKSHOP",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 85), new Vector2(400, 48), new Color(0.14f, 0.1f, 0.06f));
            close.onClick.AddListener(() => SetPractice(false));
            var note = CelestialClockQuestBootstrap.CreateText("Practice Note", card.transform,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 36), new Vector2(650, 30),
                18, TextAlignmentOptions.Center, cream);
            note.text = "Sample questions • No scores or saved progress";
            ShowPracticeQuestion();
            practicePanel.SetActive(false);
        }

        private void ShowPracticeQuestion()
        {
            practiceQuestion.text = stationNames[practiceIndex] + "\n" + questions[practiceIndex];
            for (var i = 0; i < practiceAnswers.Count; i++) practiceAnswers[i].text = answers[practiceIndex][i];
            practiceFeedback.text = forgeReward != null && forgeReward.PortalOpened ? "All 3 complete! The emerald portal is open."
                : forgeReward != null && forgeReward.StoneRevealed ? "All 3 complete! Walk to the glowing forge stone."
                : stationAnswered[practiceIndex] ? "Completed! Explore another station." : "Choose an answer.";
        }

        private void SetPractice(bool open)
        {
            if (open && paused) SetPaused(false);
            practicePanel.SetActive(open);
            if (stationPrompt != null) stationPrompt.gameObject.SetActive(!open);
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;
        }
        private void OnDestroy()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            foreach (var material in materials) Destroy(material);
            foreach (var mesh in meshes) Destroy(mesh);
            if (workshopPipeline != null && QualitySettings.renderPipeline == workshopPipeline)
                QualitySettings.renderPipeline = previousPipeline;
        }
    }
}
