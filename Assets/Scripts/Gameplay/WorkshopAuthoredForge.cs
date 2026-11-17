using System.Linq;
using UnityEngine;

namespace ImagineQuest.Gameplay
{
    public sealed partial class BlacksmithRoomBootstrap
    {
        [SerializeField] private GameObject authoredForgePrefab;
        [SerializeField] private GameObject bundledRaidSword;
        private Vector3 raidIndoorSpawn = new Vector3(0, .06f, -2.8f);
        private Vector3 raidAnvilTop = AnvilPosition + new Vector3(0, 1.28f, -.08f);
        private Vector3 workshopEmberPosition = new Vector3(-2.7f, 1.3f, 3.4f);

        private void ResolveLocalForgeArt()
        {
#if UNITY_EDITOR
            // Use an independently imported local copy without adding the Asset
            // Store source files or unresolved scene references to the public repo.
            if (authoredForgePrefab == null) authoredForgePrefab = Load("fdc8fc6e42a289a41a47a11a9d9e4ece");
            if (authoredRaidSword == null) authoredRaidSword = Load("a9505d88eeedcfb468ab544f5776ca36");

            static GameObject Load(string guid)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                return string.IsNullOrEmpty(path) ? null : UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
#endif
            if (authoredRaidSword == null) authoredRaidSword = bundledRaidSword;
        }

        private void BuildBundledForge(Material wood, Material iron, Material stone, Material ember)
        {
            // Same playable footprint and sight lines as the optional authored room.
            var forge = new GameObject("Cinder Quay — Blacksmith's Forge").transform;
            forge.SetParent(transform, false);
            var floor = new Bounds(new Vector3(0, 0, .85f), new Vector3(9.38f, 0, 11.72f));
            CryptSolid("Bundled forge floor", new Vector3(0, -.10f, .85f), new Vector3(9.38f, .2f, 11.72f));
            CryptSolid("Bundled rear wall", new Vector3(0, 1.68f, 6.71f), new Vector3(9.4f, 3.36f, .36f));
            CryptSolid("Bundled forge ceiling", new Vector3(0, 3.46f, .85f), new Vector3(9.4f, .2f, 11.72f));
            foreach (int side in new[] { -1, 1 })
            {
                CryptSolid("Bundled side wall", new Vector3(side * 4.69f, 1.68f, .85f), new Vector3(.36f, 3.36f, 11.72f));
                CryptSolid("Bundled front wall", new Vector3(side * 2.82f, 1.68f, -5.03f), new Vector3(3.76f, 3.36f, .36f));
            }
            CryptSolid("Bundled door lintel", new Vector3(0, 3.04f, -5.03f), new Vector3(2f, .64f, .35f));
            raidDoor = new GameObject("Forge doorway", typeof(BoxCollider));
            raidDoor.transform.SetParent(forge, false);
            raidDoor.transform.position = new Vector3(0, 1.27f, -5.03f);
            raidDoor.GetComponent<BoxCollider>().size = new Vector3(1.92f, 2.54f, .20f);

            var anvil = new GameObject("Forge practice anvil").transform;
            anvil.SetParent(forge, false);
            int before = transform.childCount;
            BuildAnvil(wood, iron);
            for (int i = transform.childCount - 1; i >= before; i--)
            {
                var piece = transform.GetChild(i);
                piece.SetParent(anvil, true);
                if (piece.name == "Anvil oak stump") piece.gameObject.SetActive(false);
            }
            anvil.position = new Vector3(.32f, -.21f, -.28f) - AnvilPosition;
            PlaceAnvilBesideAisle(anvil);
            Bounds anvilBounds = ForgeBounds(anvil);

            var hearth = new GameObject("Forge practice hearth").transform;
            hearth.SetParent(forge, false);
            Block("Forge masonry", new Vector3(0, .36f, 3f), new Vector3(2.37f, .72f, 1.85f), stone).transform.SetParent(hearth, true);
            Block("Hot coals", new Vector3(0, .76f, 3f), new Vector3(1.67f, .12f, .99f), ember).transform.SetParent(hearth, true);
            foreach (float side in new[] { -1f, 1f })
                Block("Hearth stone pier", new Vector3(side * .95f, 1.18f, 2.83f),
                    new Vector3(.38f, .93f, .9f), stone).transform.SetParent(hearth, true);
            Block("Hearth back wall", new Vector3(0f, 1.25f, 3.74f),
                new Vector3(2.05f, 1.06f, .25f), stone).transform.SetParent(hearth, true);
            for (int course = 0; course < 6; course++)
            {
                float width = 2.26f - course * .24f;
                float y = 1.75f + course * .265f;
                float joint = course % 2 == 0 ? -.14f : .14f;
                float leftWidth = width * .5f + joint;
                float rightWidth = width - leftWidth;
                Block("Chimney left stone", new Vector3(joint - leftWidth * .5f - .013f, y, 3.14f),
                    new Vector3(leftWidth, .25f, .99f), stone).transform.SetParent(hearth, true);
                Block("Chimney right stone", new Vector3(joint + rightWidth * .5f + .013f, y, 3.14f),
                    new Vector3(rightWidth, .25f, .99f), stone).transform.SetParent(hearth, true);
            }
            var bench = new Bounds(new Vector3(-3.82f, .53f, 2.91f), new Vector3(.98f, 1.06f, 2.21f));
            CryptSolid("Bundled bench", bench.center, bench.size);
            SetForgeStations(anvilBounds, ForgeBounds(hearth), bench);
            DressForgeInCryptStyle(forge, anvil, hearth, anvilBounds, bench, floor);
        }

        private void BuildAuthoredForge()
        {
            var forge = Instantiate(authoredForgePrefab, transform);
            forge.name = "Cinder Quay — Blacksmith's Forge";
            // Enlarge the assembled room around the doorway, not the player or quay.
            var dimensions = new Vector3(1.25f, 1.12f, 1.18f);
            forge.transform.localScale = dimensions;
            forge.transform.SetPositionAndRotation(
                Vector3.Scale(new Vector3(-8.199731f, .87087464f, 5.893f), dimensions) + Vector3.back * 5,
                Quaternion.Euler(0, 180, 0));

            var parts = forge.GetComponentsInChildren<Transform>(true);
            raidDoor = parts.First(part => part.name == "fi_vil_door04").gameObject;
            var anvil = parts.First(part => part.name == "fi_vil_forge_anvil");
            var hearth = parts.First(part => part.name == "fi_vil_forge_forge3_lit");
            var bench = parts.Where(part => part.name == "fi_vil_forge_workbensh_large1")
                .OrderBy(part => Vector3.SqrMagnitude(part.position - new Vector3(-3.9f, 0, 2.9f))).First();

            PlaceAnvilBesideAisle(anvil);
            Bounds anvilBounds = AddForgeFixtureCollider(anvil);
            Bounds hearthBounds = AddForgeFixtureCollider(hearth);
            Bounds benchBounds = AddForgeFixtureCollider(bench);
            SetForgeStations(anvilBounds, hearthBounds, benchBounds);

            // The original asset stops its tiled floor before the doorway. Bridge the
            // vestibule to the crypt flush, without covering the original walkable interior.
            var floor = ForgeBounds(parts.First(part => part.name == "FLOORS"));
            const float outerEdge = -5.17f;
            float innerEdge = floor.min.z + .018f;
            CryptSolid("Forge vestibule floor", new Vector3(floor.center.x, -.101f, (outerEdge + innerEdge) * .5f),
                new Vector3(floor.size.x - .02f, .2f, innerEdge - outerEdge));
            DressForgeInCryptStyle(forge.transform, anvil, hearth, anvilBounds, benchBounds, floor);
        }

        private static void PlaceAnvilBesideAisle(Transform anvil)
        {
            var center = ForgeBounds(anvil).center;
            anvil.position += new Vector3(2.95f - center.x, 0, .72f - center.z);
        }

        private void SetForgeStations(Bounds anvil, Bounds hearth, Bounds bench)
        {
            raidAnvilTop = new Vector3(anvil.center.x, anvil.max.y, anvil.center.z);
            stationPositions[0] = raidAnvilTop + new Vector3(0, .25f, -.14f);
            stationPositions[1] = new Vector3(bench.max.x, bench.max.y + .25f, bench.center.z);
            stationPositions[2] = new Vector3(hearth.center.x, 1.54f, hearth.min.z - .03f);
            workshopEmberPosition = new Vector3(hearth.center.x, 1.2f, hearth.center.z);
            raidIndoorSpawn = new Vector3(.12f, .06f, -3.82f);
        }

        private void DressForgeInCryptStyle(Transform forge, Transform anvil, Transform hearth, Bounds anvilBounds, Bounds bench, Bounds floor)
        {
            InitializeCryptPalette();
            var iron = Surface(new Color(.21f, .22f, .23f), .38f);
            var masonry = Surface(new Color(.37f, .34f, .31f));
            var coals = Glow(new Color(.8f, .29f, .08f), 1.25f);
            foreach (var light in forge.GetComponentsInChildren<Light>(true)) light.enabled = false;
            foreach (var renderer in forge.GetComponentsInChildren<Renderer>(true))
            {
                var part = renderer.transform;
                if (!part.IsChildOf(anvil) && !part.IsChildOf(hearth))
                {
                    renderer.enabled = false;
                    continue;
                }
                if (renderer is ParticleSystemRenderer) continue;
                var name = part.name.ToLowerInvariant();
                var material = name.Contains("coal") ? coals : part.IsChildOf(anvil) || name.Contains("tool") ? iron : masonry;
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++) slots[i] = material;
                renderer.sharedMaterials = slots;
            }

            // The new meshes are visual only: preserve the tested floor, walls,
            // authored anvil/hearth, and interactive door collision underneath.
            var dressing = new GameObject("Tide Crypt forge dressing").transform;
            dressing.SetParent(forge, true);
            const float south = -5.03f, roomHeight = 3.36f;
            float left = floor.min.x, right = floor.max.x, north = floor.max.z;
            float width = right - left, depth = north - south, column = width / 3, row = depth / 3;
            float aisleFloorTop = float.NegativeInfinity;
            for (int i = 0; i < 3; i++)
            {
                float x = left + (i + .5f) * column;
                float z = south + (i + .5f) * row;
                for (int j = 0; j < 3; j++)
                {
                    var tile = new Vector3(x, -.045f, south + (j + .5f) * row);
                    var floorTile = Add("floor_tile_large", tile, 0, new Vector3(column / 4, 1, row / 4));
                    if (i == 1 && j < 2)
                        aisleFloorTop = Mathf.Max(aisleFloorTop, ForgeBounds(floorTile).max.y);
                    Add("floor_tile_large", tile + Vector3.up * (roomHeight + .05f), 0,
                        new Vector3(column / 4, 1, row / 4)).Rotate(0, 0, 180, Space.Self);
                }
                Add(i == 1 ? "wall_doorway" : "wall", new Vector3(x, 0, south), 0,
                    new Vector3(column / 4, roomHeight / 4, .8f));
                Add(i == 1 ? "wall_cracked" : "wall", new Vector3(x, 0, north), 180,
                    new Vector3(column / 4, roomHeight / 4, .8f));
                Add("wall", new Vector3(left, 0, z), 90, new Vector3(row / 4, roomHeight / 4, .8f));
                Add("wall_cracked", new Vector3(right, 0, z), -90, new Vector3(row / 4, roomHeight / 4, .8f));
            }

            // The exit keeps its authored collider and open/reset logic; the
            // visible gate now uses the same asset as the puzzle doors.
            var door = Add("wall_doorway_door", new Vector3(0, 0, south + .04f), 0, new Vector3(.89f, .94f, .9f));
            door.SetParent(raidDoor.transform, true);

            BuildForgeArchitecture(dressing, left, right, south, north, roomHeight, bench);
            CryptModel("banner_patternA_red", new Vector3(left + .38f, .25f, -.1f), 90, .75f, dressing);
            BuildForgeCarpet(dressing, new Vector3(floor.center.x, aisleFloorTop, south + depth * .33f));
            Torch(new Vector3(left + .51f, 2.07f, -.9f), 90, dressing);
            Torch(new Vector3(right - .51f, 2.07f, -.9f), -90, dressing);
            Torch(new Vector3(right - .51f, 2.07f, 3.8f), -90, dressing);
            forgeLight = Lamp("Workshop hearth", workshopEmberPosition + new Vector3(0, .23f, -.38f),
                new Color(1, .55f, .22f), 2.5f, 7);
            forgeLight.shadows = LightShadows.None;
            BuildHearthFire(dressing, ForgeBounds(hearth));
            DressWorkingForge(dressing, anvilBounds, bench, right);

            Transform Add(string model, Vector3 p, float yaw, Vector3 scale)
            {
                var part = CryptModel(model, p, yaw);
                part.localScale = scale;
                part.SetParent(dressing, true);
                return part;
            }
        }

        private void BuildForgeCarpet(Transform dressing, Vector3 center)
        {
            var wool = Surface(new Color(.29f, .075f, .052f));
            var weave = Surface(new Color(.47f, .28f, .115f));
            var trim = Surface(new Color(.14f, .064f, .040f));
            var carpet = new GameObject("Forge aisle carpet").transform;
            carpet.SetParent(dressing, true);
            carpet.position = center;

            Inlay("Wool field", new Vector3(0f, .008f, 0f), new Vector3(1.72f, .016f, 4.12f), wool);
            foreach (float side in new[] { -1f, 1f })
            {
                Inlay("Woven side border", new Vector3(side * .78f, .016f, 0f), new Vector3(.075f, .004f, 3.96f), weave);
                Inlay("Woven end border", new Vector3(0f, .016f, side * 1.94f), new Vector3(1.63f, .004f, .075f), weave);
            }
            for (int i = -1; i <= 1; i++)
            {
                var offset = new Vector3(0f, .016f, i * 1.10f);
                Inlay("Woven diamond", offset, new Vector3(.54f, .004f, .54f), weave).rotation = Quaternion.Euler(0f, 45f, 0f);
                offset.y += .003f;
                Inlay("Diamond center", offset, new Vector3(.31f, .003f, .31f), trim).rotation = Quaternion.Euler(0f, 45f, 0f);
            }

            Transform Inlay(string label, Vector3 offset, Vector3 size, Material material)
            {
                var piece = Decor(label, PrimitiveType.Cube, center + offset, size, material);
                piece.SetParent(carpet, true);
                return piece;
            }
        }

        private void BuildHearthFire(Transform dressing, Bounds hearth)
        {
            var coal = Glow(new Color(.66f, .16f, .035f), 1.35f);
            var orange = Glow(new Color(1f, .34f, .038f), 1.9f);
            var yellow = Glow(new Color(1f, .75f, .24f), 2.2f);
            float z = hearth.min.z + hearth.size.z * .47f;
            for (int i = 0; i < 9; i++)
            {
                float x = hearth.center.x + (i % 5 - 2) * .16f;
                float row = i / 5 * .23f;
                var ember = Decor("Hearth coal", PrimitiveType.Sphere, new Vector3(x, .85f, z + row),
                    new Vector3(.21f, .13f, .21f), coal);
                ember.SetParent(dressing, true);
                float y = .99f + (i % 3) * .037f;
                var flame = Decor("Hearth flame", PrimitiveType.Sphere, new Vector3(x, y, z + row),
                    new Vector3(.09f, .23f, .09f), i % 3 == 0 ? yellow : orange);
                flame.rotation = Quaternion.Euler(0, 0, i % 2 == 0 ? -12f : 14f);
                flame.SetParent(dressing, true);
                flames.Add(flame);
            }
        }

        private void BuildForgeArchitecture(Transform dressing, float left, float right, float south, float north, float height, Bounds bench)
        {
            var oak = Surface(new Color(.23f, .14f, .085f));
            var lead = Surface(new Color(.115f, .095f, .075f), .25f);
            var glass = Glow(new Color(.92f, .83f, .66f), .9f);
            float width = right - left;
            float midX = (left + right) * .5f;
            float midZ = (south + north) * .5f;
            foreach (float x in new[] { left + .21f, right - .21f })
            {
                Piece("Upper wall timber", new Vector3(x, height - .29f, midZ),
                    new Vector3(.25f, .26f, north - south), oak);
                foreach (float z in new[] { south + 1f, midZ + .65f, north - .7f })
                    Piece("Wall timber post", new Vector3(x, (height - .40f) * .5f, z),
                        new Vector3(.26f, height - .40f, .27f), oak);
            }
            foreach (float z in new[] { south + 1f, midZ + .65f, north - .7f })
            {
                Piece("Arch crown beam", new Vector3(midX, height - .21f, z),
                    new Vector3(width - 1.70f, .25f, .29f), oak);
                foreach (float side in new[] { -1f, 1f })
                {
                    var spring = new Vector3(midX + side * (width * .5f - .24f), height - .84f, z);
                    var shoulder = new Vector3(midX + side * (width * .5f - .56f), height - .46f, z);
                    var crown = new Vector3(midX + side * (width * .5f - .91f), height - .21f, z);
                    Brace(spring, shoulder);
                    Brace(shoulder, crown);
                }
            }

            BuildWindowBank(new Vector3(left + .31f, 0f, south + 2.55f), 0f);
            BuildWindowBank(new Vector3(right - .31f, 0f, south + 2.55f), 180f);
            foreach (float side in new[] { -1f, 1f })
                BuildWindowBank(new Vector3(midX + side * width * .29f, .40f, north - .32f), 90f);

            // Finished blades belong above the workbench, across from the anvil tools.
            float rackX = left + .36f;
            Piece("Sword display backboard", new Vector3(rackX, 2.03f, bench.center.z),
                new Vector3(.12f, 1.16f, 1.62f), oak);
            var steel = Surface(new Color(.39f, .45f, .50f), .48f);
            foreach (float side in new[] { -1f, 1f })
            {
                float z = bench.center.z + .39f * side;
                float x = rackX + .093f;
                Piece("Display sword blade", new Vector3(x, 2.13f, z), new Vector3(.035f, .60f, .084f), steel);
                Piece("Display sword point", new Vector3(x, 2.43f, z),
                    new Vector3(.035f, .065f, .065f), steel).Rotate(45f, 0f, 0f, Space.World);
                Piece("Display sword guard", new Vector3(x, 1.81f, z), new Vector3(.071f, .049f, .25f), lead);
                Piece("Display sword grip", new Vector3(x, 1.72f, z), new Vector3(.065f, .15f, .062f), oak);
                Piece("Display sword pommel", new Vector3(x, 1.63f, z), new Vector3(.083f, .055f, .084f), lead);
            }

            // Seat the practice targets against the back wall, away from the three station approaches.
            var straw = Surface(new Color(.49f, .33f, .16f));
            var leather = Surface(new Color(.27f, .135f, .075f));
            float dummyZ = north - .61f;
            foreach (float x in new[] { right - 2.1f, right - .97f })
            {
                Piece("Training dummy foot", new Vector3(x, .075f, dummyZ), new Vector3(.68f, .15f, .56f), oak);
                Piece("Training dummy post", new Vector3(x, .80f, dummyZ), new Vector3(.14f, 1.53f, .15f), oak);
                Piece("Training dummy crossbar", new Vector3(x, 1.26f, dummyZ),
                    new Vector3(.82f, .13f, .15f), oak);
                var torso = Decor("Padded training dummy torso", PrimitiveType.Capsule,
                    new Vector3(x, 1.16f, dummyZ - .09f), new Vector3(.48f, .32f, .28f), straw);
                torso.SetParent(dressing, true);
                var head = Decor("Training dummy head", PrimitiveType.Sphere,
                    new Vector3(x, 1.59f, dummyZ - .065f), new Vector3(.28f, .29f, .25f), straw);
                head.SetParent(dressing, true);
                Piece("Dummy binding", new Vector3(x, 1.08f, dummyZ - .095f),
                    new Vector3(.46f, .075f, .27f), leather);
                CryptSolid("Training dummy support", new Vector3(x, .81f, dummyZ), new Vector3(.68f, 1.62f, .53f));
            }

            void Brace(Vector3 from, Vector3 to)
            {
                var delta = to - from;
                var beam = Piece("Timber arch brace", (from + to) * .5f,
                    new Vector3(.24f, delta.magnitude + .12f, .28f), oak);
                beam.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            }

            void BuildWindowBank(Vector3 origin, float yaw)
            {
                var rotation = Quaternion.Euler(0f, yaw, 0f);
                WindowPart("Leaded window surround", PrimitiveType.Cube,
                    new Vector3(0f, 1.84f, 0f), new Vector3(.17f, 1.65f, 2.22f), oak);
                WindowPart("Window sill", PrimitiveType.Cube,
                    new Vector3(.04f, .99f, 0f), new Vector3(.28f, .13f, 2.29f), oak);
                for (int pane = -1; pane <= 1; pane++)
                {
                    float z = pane * .68f;
                    WindowPart("Glowing window pane", PrimitiveType.Cube,
                        new Vector3(.094f, 1.72f, z), new Vector3(.026f, 1.15f, .59f), glass);
                    WindowPart("Arched window glass", PrimitiveType.Sphere,
                        new Vector3(.094f, 2.295f, z), new Vector3(.026f, .43f, .59f), glass);
                    for (int diamond = -1; diamond <= 1; diamond++)
                    {
                        float y = 1.84f + diamond * .43f;
                        var top = At(.114f, y + .21f, z);
                        var bottom = At(.114f, y - .21f, z);
                        for (int side = -1; side <= 1; side += 2)
                        {
                            var edge = At(.114f, y, z + side * .285f);
                            LeadCame(top, edge);
                            LeadCame(edge, bottom);
                        }
                    }
                }
                var windowLight = Lamp("Window light", At(.58f, 1.9f, 0f),
                    new Color(1f, .90f, .75f), 2.0f, 6f);
                windowLight.shadows = LightShadows.None;
                windowLight.transform.SetParent(dressing, true);

                Vector3 At(float x, float y, float z) => origin + rotation * new Vector3(x, y, z);

                void WindowPart(string label, PrimitiveType shape, Vector3 offset, Vector3 scale, Material finish)
                {
                    var part = Decor(label, shape, origin + rotation * offset, scale, finish);
                    part.rotation = rotation;
                    part.SetParent(dressing, true);
                }
            }

            void LeadCame(Vector3 from, Vector3 to)
            {
                var delta = to - from;
                var strip = Piece("Window lead came", (from + to) * .5f,
                    new Vector3(.024f, delta.magnitude, .024f), lead);
                strip.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            }

            Transform Piece(string label, Vector3 position, Vector3 scale, Material finish)
            {
                var piece = Decor(label, PrimitiveType.Cube, position, scale, finish);
                piece.SetParent(dressing, true);
                return piece;
            }
        }

        private static Bounds AddForgeFixtureCollider(Transform fixture)
        {
            var bounds = ForgeBounds(fixture);
            if (fixture.GetComponentInChildren<Collider>() == null)
            {
                var solid = new GameObject(fixture.name + " solid");
                solid.transform.position = bounds.center;
                solid.transform.SetParent(fixture, true);
                solid.AddComponent<BoxCollider>().size = bounds.size;
            }
            return bounds;
        }

        private static Bounds ForgeBounds(Transform fixture)
        {
            var renderers = fixture.GetComponentsInChildren<MeshRenderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
    }
}
