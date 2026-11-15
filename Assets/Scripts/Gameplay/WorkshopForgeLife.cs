using UnityEngine;

namespace ImagineQuest.Gameplay
{
    public sealed partial class BlacksmithRoomBootstrap
    {
        [SerializeField] private GameObject forgeResidentPrefab;
        [SerializeField] private Avatar forgeResidentAvatar;
        [SerializeField] private RuntimeAnimatorController forgeResidentController;
        [SerializeField] private GameObject forgeResidentHammer;

        private readonly ForgeResident[] forgeResidents = new ForgeResident[3];
        private float nextWorkSpark;

        private sealed class ForgeResident
        {
            public Transform Root, Head, Arm, Forearm;
            public Quaternion HeadRest, ArmRest, ForearmRest, Facing;
        }

        private void BuildAnvilStand(Transform dressing, Bounds anvil)
        {
            var centre = new Vector3(anvil.center.x, 0, anvil.center.z);
            var stand = CryptModel("pillar_decorated", centre, 0, parent: dressing);
            stand.name = "Grounded anvil pedestal";
            var bounds = ForgeBounds(stand);
            float standHeight = Mathf.Max(.04f, anvil.min.y + .016f);
            stand.localScale = Vector3.Scale(stand.localScale, new Vector3(
                Mathf.Clamp(anvil.size.x * .64f, .53f, .84f) / bounds.size.x,
                standHeight / bounds.size.y,
                Mathf.Clamp(anvil.size.z * .91f, .42f, .65f) / bounds.size.z));
            bounds = ForgeBounds(stand);
            stand.position += new Vector3(centre.x - bounds.center.x, -.012f - bounds.min.y, centre.z - bounds.center.z);
            bounds = ForgeBounds(stand);
            CryptSolid("Anvil pedestal support", bounds.center, bounds.size);
        }

        private void DressWorkingForge(Transform dressing, Bounds anvil, Bounds bench, float left, float right, float north)
        {
            BuildAnvilStand(dressing, anvil);
            var iron = Surface(new Color(.24f, .26f, .27f), .42f);
            var hot = Glow(new Color(1, .28f, .07f), 1.45f);
            var timberDark = Surface(new Color(.18f, .11f, .063f));
            var doorway = -4.9f;

            // Keep the centre aisle and the approach to each of the three stations open.
            var supply = CryptModel("table_long_decorated_A", new Vector3(right - .84f, 0, 1.2f), 90, .78f, dressing);
            supply.name = "Quench and finishing bench";
            AddFixtureCollider(supply);
            AddStack(new Vector3(left + .72f, 0, doorway + 1.2f), 11, .74f);
            AddStack(new Vector3(right - .7f, 0, doorway + 1.28f), -23, .74f);
            AddStack(new Vector3(left + .67f, 0, north - .88f), -9, .74f);
            CryptModel("barrel_small", new Vector3(right - .74f, 0, north - 2.15f), 27, 1.05f, dressing);
            CryptModel("barrel_small", new Vector3(left + .72f, 0, north - 2.18f), -8, 1.12f, dressing);
            CryptModel("shelf_small_candles", new Vector3(left + .38f, 1.4f, -2.4f), 90, .86f, dressing);
            CryptModel("shelf_small_candles", new Vector3(right - .39f, 1.65f, north - 1.35f), -90, .9f, dressing);
            CryptModel("candle_triple", new Vector3(bench.center.x, bench.max.y, bench.center.z - .58f), -17, .68f, dressing);

            AddToolRack(new Vector3(right - .34f, 1.88f, .4f), -90);
            AddToolRack(new Vector3(left + .35f, 1.92f, north - .68f), 90);
            for (int i = 0; i < 4; i++)
            {
                var billet = Decor("Billet awaiting temper", PrimitiveType.Cube,
                    new Vector3(bench.center.x -.14f, bench.max.y + .029f, bench.center.z + .38f + i * .12f),
                    new Vector3(.38f, .054f, .083f), iron);
                billet.SetParent(dressing, true);
                billet.Rotate(0, 8 - i * 4, 0);
            }
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

            var crew = new GameObject("Forge hands").transform;
            crew.SetParent(transform, false);
            BuildResident(crew, 0, "Bram • Smith", new Vector3(anvil.center.x + .88f, .015f, anvil.center.z + .43f),
                new Vector3(anvil.center.x, 0, anvil.center.z), "bandit");
            BuildResident(crew, 1, "Flint • Quartermaster", new Vector3(right - 1.02f, .015f, north - 2.7f),
                new Vector3(right, 0, north - 1.3f), "mage");
            BuildResident(crew, 2, "Rook • Watch", new Vector3(left + 1.3f, .015f, -1.7f),
                new Vector3(0, 0, doorway + 1.1f), "archer");

            void AddStack(Vector3 p, float yaw, float scale)
            {
                var crates = CryptModel("crates_stacked", p, yaw, scale, dressing);
                AddFixtureCollider(crates);
            }

            void AddToolRack(Vector3 p, float yaw)
            {
                var rack = new GameObject("Wall-mounted smithing tools").transform;
                rack.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0));
                rack.SetParent(dressing, true);
                var beam = Decor("Tool rack oak", PrimitiveType.Cube, p, new Vector3(.94f, .14f, .12f), timberDark);
                beam.SetParent(rack, false);
                beam.localPosition = Vector3.zero;
                for (int i = 0; i < 3; i++)
                {
                    var tool = Decor("Tool handle", PrimitiveType.Cylinder, Vector3.zero, new Vector3(.046f, .23f, .046f), timberDark);
                    tool.SetParent(rack, false);
                    tool.localPosition = new Vector3(-.31f + i * .31f, -.18f, .11f);
                    tool.localRotation = Quaternion.Euler(0, 0, 6 - i * 6);
                    var head = Decor("Forged tool head", PrimitiveType.Cube, Vector3.zero, new Vector3(.17f, .09f, .10f), iron);
                    head.SetParent(rack, false);
                    head.localPosition = tool.localPosition + new Vector3(-.02f, .22f, 0);
                }
            }
        }

        private void AddFixtureCollider(Transform fixture)
        {
            var bounds = ForgeBounds(fixture);
            CryptSolid(fixture.name + " support", bounds.center, bounds.size);
        }

        private void BuildResident(Transform crew, int index, string label, Vector3 home, Vector3 lookAt, string outfit)
        {
            if (forgeResidentPrefab == null || forgeResidentController == null || forgeResidentAvatar == null) return;
            var anchor = new GameObject(label).transform;
            anchor.SetParent(crew, false);
            anchor.SetPositionAndRotation(home, Quaternion.LookRotation(Vector3.ProjectOnPlane(lookAt - home, Vector3.up)));
            var visual = Instantiate(forgeResidentPrefab, anchor);
            visual.transform.localScale = Vector3.one * (index == 1 ? .79f : .83f);
            foreach (var collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            var animator = visual.GetComponentInChildren<Animator>();
            if (animator == null) return;
            animator.avatar = forgeResidentAvatar;
            animator.runtimeAnimatorController = forgeResidentController;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            animator.Rebind();
            animator.SetFloat("Speed", 0f);
            animator.SetBool("isGrounded", true);
            visual.AddComponent<PirateAvatarStyler>().Apply(outfit);
            CelestialClockQuestBootstrap.AlignVisualFeetToPlayerRoot(visual, anchor);
            var resident = new ForgeResident
            {
                Root = anchor, Facing = anchor.rotation,
                Head = animator.GetBoneTransform(HumanBodyBones.Head),
                Arm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm),
                Forearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm)
            };
            forgeResidents[index] = resident;
            if (resident.Head != null) resident.HeadRest = resident.Head.localRotation;
            if (resident.Arm != null) resident.ArmRest = resident.Arm.localRotation;
            if (resident.Forearm != null) resident.ForearmRest = resident.Forearm.localRotation;
            if (index == 0)
            {
                var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (hand != null)
                {
                    var hammer = forgeResidentHammer != null ? Instantiate(forgeResidentHammer, hand) : new GameObject();
                    hammer.name = "Smith's hammer";
                    hammer.transform.SetParent(hand, false);
                    if (forgeResidentHammer == null)
                    {
                        var wood = Surface(new Color(.24f, .12f, .055f));
                        var metal = Surface(new Color(.22f, .24f, .25f), .35f);
                        var shaft = Decor("Hammer handle", PrimitiveType.Cylinder, Vector3.zero,
                            new Vector3(.046f, .18f, .046f), wood);
                        shaft.SetParent(hammer.transform, false);
                        shaft.localPosition = new Vector3(0, .16f, 0);
                        var head = Decor("Hammer steel", PrimitiveType.Cube, Vector3.zero, new Vector3(.195f, .085f, .09f), metal);
                        head.SetParent(hammer.transform, false);
                        head.localPosition = new Vector3(0, .34f, 0);
                    }
                    hammer.transform.localPosition = new Vector3(.035f, -.012f, .04f);
                    hammer.transform.localRotation = Quaternion.Euler(12, 0, 78);
                    hammer.transform.localScale = Vector3.one * .85f;
                    foreach (var collider in hammer.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                }
            }
        }

        private void LateUpdate()
        {
            if (!raidPractice || inRaid || returning || modal != null) return;
            float t = Time.time;
            for (int i = 0; i < forgeResidents.Length; i++)
            {
                var person = forgeResidents[i];
                if (person == null) continue;
                if (i == 2)
                {
                    float scan = Mathf.Sin(t * .49f) * 32;
                    person.Root.rotation = person.Facing * Quaternion.Euler(0, scan, 0);
                }
                if (i == 1)
                {
                    // Check the stock in place, without crossing the player's path.
                    float reach = .5f + .5f * Mathf.Sin(t * .9f);
                    if (person.Arm != null)
                        person.Arm.localRotation = Quaternion.AngleAxis(-22 - 28 * reach,
                            person.Arm.parent.InverseTransformDirection(person.Root.right)) * person.ArmRest;
                    if (person.Forearm != null)
                        person.Forearm.localRotation = Quaternion.AngleAxis(-12 - 16 * reach,
                            person.Forearm.parent.InverseTransformDirection(person.Root.right)) * person.ForearmRest;
                }
                if (person.Head != null)
                    person.Head.localRotation = person.HeadRest * Quaternion.Euler(i == 0 ? 12 : i == 1 ? 8 : -3,
                        Mathf.Sin(t * .8f + i * 1.4f) * 17, 0);
                if (i != 0 || bladeForged) continue;
                float beat = Mathf.Repeat(t, 3.5f);
                float raise = beat < 1.12f ? Mathf.SmoothStep(0, 1, beat / .76f) *
                    (1 - Mathf.SmoothStep(0, 1, (beat - .84f) / .28f)) : 0;
                if (person.Arm != null)
                    person.Arm.localRotation = Quaternion.AngleAxis(-22 - 74 * raise,
                        person.Arm.parent.InverseTransformDirection(person.Root.right)) * person.ArmRest;
                if (person.Forearm != null)
                    person.Forearm.localRotation = Quaternion.AngleAxis(-17 - 44 * raise,
                        person.Forearm.parent.InverseTransformDirection(person.Root.right)) * person.ForearmRest;

                if (t >= nextWorkSpark && beat > 1.03f && beat < 1.16f)
                {
                    nextWorkSpark = t + 2.8f;
                    raidEffects?.Burst(RaidCue.Forge, raidAnvilTop + Vector3.up * .04f, Vector3.up, false);
                }
            }
        }
    }
}
