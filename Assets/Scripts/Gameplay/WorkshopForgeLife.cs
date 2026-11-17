using UnityEngine;

namespace ImagineQuest.Gameplay
{
    public sealed partial class BlacksmithRoomBootstrap
    {
        private void BuildSmithingTable(Transform dressing, string label, Vector3 center, Vector2 footprint,
            float top, Material timber, Material iron)
        {
            const float topThickness = .14f;
            const float legWidth = .16f;
            float legHeight = top - topThickness;
            var table = new GameObject(label).transform;
            table.SetParent(dressing, false);

            Piece("Heavy plank top", new Vector3(0, top - topThickness * .5f, 0),
                new Vector3(footprint.x, topThickness, footprint.y), timber);
            float legX = footprint.x * .5f - .15f;
            float legZ = footprint.y * .5f - .15f;
            foreach (float x in new[] { -legX, legX })
            {
                foreach (float z in new[] { -legZ, legZ })
                    Piece("Square oak leg", new Vector3(x, legHeight * .5f, z),
                        new Vector3(legWidth, legHeight, legWidth), timber);
                Piece("Iron end strap", new Vector3(x, top - topThickness * .5f, 0),
                    new Vector3(.073f, topThickness, footprint.y + .012f), iron);
            }
            foreach (float z in new[] { -legZ, legZ })
                Piece("Timber stretcher", new Vector3(0, legHeight * .46f, z),
                    new Vector3(legX * 2, .11f, .105f), timber);

            void Piece(string pieceName, Vector3 offset, Vector3 size, Material finish)
            {
                Decor(pieceName, PrimitiveType.Cube, center + offset, size, finish).SetParent(table, true);
            }
        }

        private void DressWorkingForge(Transform dressing, Bounds anvil, Bounds bench, float right)
        {
            var iron = Surface(new Color(.24f, .26f, .27f), .42f);
            var hot = Glow(new Color(1, .28f, .07f), 1.45f);
            var timberDark = Surface(new Color(.18f, .11f, .063f));
            var oak = Surface(new Color(.29f, .18f, .10f));
            var anvilCenter = new Vector3(anvil.center.x, 0f, anvil.center.z);
            var anvilFootprint = new Vector2(Mathf.Max(1.25f, anvil.size.x + .15f), Mathf.Max(.70f, anvil.size.z + .18f));
            BuildSmithingTable(dressing, "Anvil smithing table", anvilCenter, anvilFootprint, anvil.min.y, oak, iron);
            CryptSolid("Anvil table support", anvilCenter + Vector3.up * (anvil.min.y * .5f),
                new Vector3(anvilFootprint.x, anvil.min.y, anvilFootprint.y));
            BuildSmithingTable(dressing, "Smithing workbench", new Vector3(bench.center.x, 0f, bench.center.z),
                new Vector2(bench.size.x, bench.size.z), bench.max.y, oak, iron);

            // Keep supplies against the walls and leave an open approach to each station.
            var quench = new Vector3(right - .64f, 0, 2.6f);
            var water = Surface(new Color(.07f, .19f, .21f));
            water.SetFloat("_Smoothness", .72f);
            Detail("Quench trough", PrimitiveType.Cube, quench + Vector3.up * .27f, new Vector3(.62f, .48f, .86f), timberDark);
            Detail("Quench water", PrimitiveType.Cube, quench + Vector3.up * .518f, new Vector3(.52f, .016f, .76f), water);
            foreach (float side in new[] { -1f, 1f })
            {
                Detail("Trough side rim", PrimitiveType.Cube, quench + new Vector3(side * .29f, .555f, 0),
                    new Vector3(.06f, .09f, .88f), iron);
                Detail("Trough end rim", PrimitiveType.Cube, quench + new Vector3(0, .555f, side * .41f),
                    new Vector3(.54f, .09f, .06f), iron);
            }
            CryptSolid("Quench trough support", quench + Vector3.up * .30f, new Vector3(.64f, .60f, .88f));

            AddToolRack(new Vector3(right - .12f, 1.88f, anvil.center.z), -90);
            var tray = new Vector3(bench.center.x, bench.max.y, bench.center.z + .40f);
            Detail("Billet tray", PrimitiveType.Cube, tray + Vector3.up * .025f, new Vector3(.56f, .05f, .56f), timberDark);
            for (int i = 0; i < 4; i++)
                Detail("Billet awaiting temper", PrimitiveType.Cube, tray + new Vector3(0, .077f, (i - 1.5f) * .115f),
                    new Vector3(.38f, .054f, .083f), iron);

            var vise = new Vector3(bench.max.x - .24f, bench.max.y, bench.center.z - .51f);
            Detail("Vise base", PrimitiveType.Cylinder, vise + Vector3.up * .043f, new Vector3(.28f, .043f, .28f), iron);
            Detail("Vise slide", PrimitiveType.Cube, vise + Vector3.up * .12f, new Vector3(.16f, .10f, .35f), iron);
            foreach (float offset in new[] { -.12f, .12f })
                Detail("Vise jaw", PrimitiveType.Cube, vise + new Vector3(0, .20f, offset),
                    new Vector3(.24f, .12f, .07f), iron);
            Detail("Vise screw handle", PrimitiveType.Cylinder, vise + new Vector3(0, .12f, -.19f),
                new Vector3(.024f, .14f, .024f), iron).rotation = Quaternion.Euler(0, 0, 75f);
            var workpiece = Decor("Hot steel on the anvil", PrimitiveType.Cube,
                new Vector3(anvil.center.x, anvil.max.y + .02f, anvil.center.z), new Vector3(.32f, .036f, .076f), hot);
            workpiece.SetParent(dressing, true);

            var emberRoot = new Vector3(workshopEmberPosition.x, 1.04f, workshopEmberPosition.z - .52f);
            for (int i = 0; i < 16; i++)
            {
                var at = emberRoot + new Vector3(Mathf.Sin(i * 2.4f) * .41f, i % 3 * .09f, Mathf.Cos(i * 2.4f) * .16f);
                var ember = Decor("Drifting forge spark", PrimitiveType.Sphere, at, Vector3.one * .018f, cryptGold);
                ember.SetParent(dressing, true);
                motes.Add(ember);
                moteOrigins.Add(at);
            }

            Transform Detail(string label, PrimitiveType shape, Vector3 position, Vector3 size, Material finish)
            {
                var detail = Decor(label, shape, position, size, finish);
                detail.SetParent(dressing, true);
                return detail;
            }

            void AddToolRack(Vector3 p, float yaw)
            {
                var rack = new GameObject("Wall-mounted smithing tools").transform;
                rack.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0));
                rack.SetParent(dressing, true);
                RackPart("Oak tool board", PrimitiveType.Cube, Vector3.zero, new Vector3(1.20f, .74f, .10f), timberDark);
                RackPart("Iron hanging rail", PrimitiveType.Cube, new Vector3(0, .24f, .065f),
                    new Vector3(1.06f, .052f, .05f), iron);
                foreach (float x in new[] { -.49f, .49f })
                    RackPart("Tool board mounting stud", PrimitiveType.Sphere,
                        new Vector3(x, .24f, .097f), Vector3.one * .047f, iron);
                for (int i = 0; i < 2; i++)
                {
                    float x = -.36f + i * .34f;
                    RackPart("Hammer handle", PrimitiveType.Cylinder, new Vector3(x, -.055f, .11f),
                        new Vector3(.045f, .21f, .045f), timberDark);
                    RackPart("Hammer head", PrimitiveType.Cube, new Vector3(x, .17f, .11f),
                        new Vector3(i == 0 ? .20f : .15f, .083f, .091f), iron);
                }
                foreach (float side in new[] { -1f, 1f })
                {
                    var handle = RackPart("Forging tong handle", PrimitiveType.Cube,
                        new Vector3(.34f + side * .051f, -.10f, .12f), new Vector3(.027f, .34f, .031f), iron);
                    handle.localRotation = Quaternion.Euler(0, 0, side * 11f);
                    var jaw = RackPart("Forging tong jaw", PrimitiveType.Cube,
                        new Vector3(.34f + side * .035f, .13f, .12f), new Vector3(.034f, .16f, .049f), iron);
                    jaw.localRotation = Quaternion.Euler(0, 0, side * -12f);
                }
                RackPart("Tong pivot", PrimitiveType.Sphere, new Vector3(.34f, .061f, .145f),
                    Vector3.one * .063f, iron);

                Transform RackPart(string label, PrimitiveType shape, Vector3 position, Vector3 size, Material finish)
                {
                    var part = Decor(label, shape, Vector3.zero, size, finish);
                    part.SetParent(rack, false);
                    part.localPosition = position;
                    return part;
                }
            }
        }
    }
}
