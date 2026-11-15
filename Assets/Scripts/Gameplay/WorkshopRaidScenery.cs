using UnityEngine;

namespace ImagineQuest.Gameplay
{
    public sealed partial class BlacksmithRoomBootstrap
    {
        [SerializeField] private Material raidSky, raidWater;

        private void BuildRaidVillage(Material oak, Material iron, Material stone, Material ember)
        {
            var soot = Surface(new Color(.11f, .10f, .09f));
            var cobble = Surface(new Color(.31f, .29f, .26f));
            var plaster = Surface(new Color(.48f, .39f, .30f));
            var slate = Surface(new Color(.095f, .16f, .20f));
            var red = Surface(new Color(.28f, .092f, .067f));
            var amber = Glow(new Color(1, .49f, .13f), 1.8f);
            var sea = Surface(new Color(.046f, .13f, .17f), .25f);
            var rng = new System.Random(91);

            Block("Quay foundations", new Vector3(0, -.24f, -16), new Vector3(23, .47f, 22), stone);
            Block("Workshop threshold", new Vector3(0, -.11f, -5), new Vector3(2.2f, .22f, 1.1f), cobble);
            raidGate = Block("Workshop storm shutter", new Vector3(0, 1.25f, -5.15f), new Vector3(2.2f, 2.5f, .19f), oak);
            raidGate.SetActive(false);
            Decor("Harbor water", PrimitiveType.Plane, new Vector3(0, -.92f, -45), new Vector3(18, 1, 12), raidWater != null ? raidWater : sea);
            // Clear fighting ground, with irregular paving just above the continuous collision surface.
            for (int row = 0; row < 26; row++)
                for (int col = 0; col < 14; col++)
                {
                    float x = (col - 6.5f) * 1.5f + (row % 2 == 0 ? .13f : -.13f);
                    float z = -5.7f - row * .78f;
                    Decor("Quay setts", PrimitiveType.Cube, new Vector3(x, -.016f, z),
                        new Vector3(1.42f + (float)rng.NextDouble() * .065f, .07f, .72f),
                        rng.Next(6) == 0 ? soot : cobble).Rotate(0, rng.Next(-2, 3), 0);
                }

            for (int side = -1; side <= 1; side += 2)
            {
                Block("Quay side parapet", new Vector3(side * 10.8f, .45f, -16), new Vector3(.48f, .9f, 22), stone);
                for (int i = 0; i < 3; i++)
                {
                    float z = -9 - i * 6.1f;
                    float x = side * (8.85f + (i % 2) * .5f);
                    BuildQuayHouse(x, z, side, i % 2 == 0 ? plaster : red, oak, slate, amber);
                    if (i != 1) PlaceProp(arenaBarrel, "Tar barrel", new Vector3(side * 6.8f, 0, z + 1.1f), .76f, i * 39);
                }
                Block("Seawall gate pillar", new Vector3(side * 2.3f, 1.55f, -24.7f), new Vector3(.65f, 3.1f, .75f), stone);
                Block("Seawall", new Vector3(side * 6.6f, .68f, -26.6f), new Vector3(8.2f, 1.36f, .5f), stone);
                Block("Gate barrier", new Vector3(side * 1.1f, .48f, -26.6f), new Vector3(2.3f, .96f, .45f), oak);
                BuildQuayLantern(new Vector3(side * 4.8f, 0, -10), oak, iron, amber);
                BuildQuayLantern(new Vector3(side * 3.5f, 0, -23.5f), oak, iron, amber);

                // Hammered iron, stocked shelves and cloth bring depth to the forge without more signage.
                if (authoredForgePrefab != null) continue;
                for (int row = 0; row < 4; row++)
                    Decor("Forge side masonry", PrimitiveType.Cube, new Vector3(side * 4.79f, .27f + row * .39f, -1.45f),
                        new Vector3(.13f, .34f, 5.8f), row % 2 == 0 ? stone : soot);
                Decor("Workshop pennant", PrimitiveType.Cube, new Vector3(side * 2.95f, 2.97f, -4.73f),
                    new Vector3(.86f, 1.18f, .035f), side < 0 ? red : slate);
                Decor("Pennant rail", PrimitiveType.Cube, new Vector3(side * 2.95f, 3.61f, -4.66f),
                    new Vector3(1.14f, .085f, .11f), oak);
            }
            Block("Gate beam", new Vector3(0, 3.08f, -24.7f), new Vector3(5.4f, .33f, .75f), oak);
            Decor("Gate sign", PrimitiveType.Cube, new Vector3(0, 3.08f, -24.28f), new Vector3(2.6f, .56f, .13f), soot);
            WorldLabel("East gate lettering", "EAST GATE", new Vector3(0, 3.11f, -24.19f), new Vector2(340, 60), 29, Gold, false, 180);
            Block("West retaining wall", new Vector3(-9, .6f, -5), new Vector3(8, 1.2f, .5f), stone);
            Block("East retaining wall", new Vector3(9, .6f, -5), new Vector3(8, 1.2f, .5f), stone);
            if (authoredForgePrefab == null)
                Block("Back of workshop", new Vector3(0, .65f, 5.4f), new Vector3(10, 1.3f, .4f), stone);

            var sky = new GameObject("Quay moonlight", typeof(Light));
            sky.transform.SetParent(transform);
            sky.transform.rotation = Quaternion.Euler(48, 145, 0);
            var key = sky.GetComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(.56f, .69f, 1);
            key.intensity = .75f;
            key.shadows = LightShadows.Soft;
            view.GetComponent<Camera>().backgroundColor = new Color(.065f, .105f, .16f);
            if (raidSky != null)
            {
                RenderSettings.skybox = raidSky;
                view.GetComponent<Camera>().clearFlags = CameraClearFlags.Skybox;
            }
            else Decor("Low moon", PrimitiveType.Sphere, new Vector3(-17, 13, -49), Vector3.one * 3.3f,
                Glow(new Color(.6f, .76f, .85f), 1.2f));
            for (int i = 0; i < 7; i++)
                Decor("Distant sea stack", PrimitiveType.Sphere, new Vector3(-27 + i * 9.4f, -.3f, -43 - i % 2 * 5),
                    new Vector3(8, 3 + i % 3 * 2, 6), slate);
        }

        private void BuildQuayHouse(float x, float z, int side, Material plaster, Material oak, Material roof, Material amber)
        {
            Block("Fisherman's cottage", new Vector3(x, 1.39f, z), new Vector3(3.3f, 2.78f, 4.25f), plaster);
            float front = x - side * 1.69f;
            for (int i = -1; i <= 1; i++)
                Decor("Cottage timber", PrimitiveType.Cube, new Vector3(front, 1.39f, z + i * 1.96f),
                    new Vector3(.15f, 2.82f, .16f), oak);
            for (int i = -1; i <= 1; i += 2)
            {
                Decor("Slate roof pitch", PrimitiveType.Cube, new Vector3(x + i * .94f, 3.25f, z),
                    new Vector3(2.2f, .16f, 4.7f), roof).rotation = Quaternion.Euler(0, 0, -i * 28);
                Decor("Roof fascia", PrimitiveType.Cube, new Vector3(x + i * 1.92f, 2.74f, z),
                    new Vector3(.17f, .22f, 4.77f), oak);
            }
            for (int i = 0; i < 2; i++)
            {
                float windowZ = z + (i == 0 ? -.93f : .93f);
                Decor("Window oak frame", PrimitiveType.Cube, new Vector3(front, 1.75f, windowZ), new Vector3(.18f, .76f, .82f), oak);
                Decor("Candlelit window", PrimitiveType.Cube, new Vector3(front - side * .10f, 1.75f, windowZ), new Vector3(.035f, .58f, .65f), amber);
                Decor("Window mullion", PrimitiveType.Cube, new Vector3(front - side * .13f, 1.75f, windowZ), new Vector3(.045f, .64f, .055f), oak);
                Decor("Window transom", PrimitiveType.Cube, new Vector3(front - side * .13f, 1.75f, windowZ), new Vector3(.045f, .055f, .71f), oak);
            }
            Decor("Timber sill", PrimitiveType.Cube, new Vector3(front, .18f, z), new Vector3(.27f, .32f, 4.31f), oak);
        }

        private void BuildQuayLantern(Vector3 p, Material wood, Material iron, Material amber)
        {
            Decor("Quay lantern post", PrimitiveType.Cube, p + Vector3.up * 1.25f, new Vector3(.15f, 2.5f, .15f), wood);
            Decor("Lantern glass", PrimitiveType.Cube, p + Vector3.up * 2.57f, new Vector3(.21f, .34f, .21f), amber);
            for (int i = -1; i <= 1; i += 2)
                for (int j = -1; j <= 1; j += 2)
                    Decor("Lantern iron corner", PrimitiveType.Cube, p + new Vector3(i * .115f, 2.57f, j * .115f), new Vector3(.035f, .4f, .035f), iron);
            Decor("Lantern bonnet", PrimitiveType.Cube, p + Vector3.up * 2.78f, new Vector3(.31f, .10f, .31f), iron);
            Lamp("Quay lantern", p + Vector3.up * 2.62f, new Color(1, .56f, .24f), 5, 7).shadows = LightShadows.None;
        }
    }
}
