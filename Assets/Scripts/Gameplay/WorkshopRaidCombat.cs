using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImagineQuest.Gameplay
{
    // Driven by the workshop so enemies, attack timing and input all pause with its menus.
    internal sealed class WorkshopRaidCombat
    {
        public const int MaxHealth = 100;
        private const float SwingDuration = .55f, Windup = .66f;
        public int Health { get; private set; } = MaxHealth;
        public int Wave { get; private set; }
        public int Remaining => enemies.Count;
        private readonly Transform player, camera;
        private readonly WorkshopRaidEffects effects;
        private readonly Material bone, dark, leather, steel, brass, cloth, eyes, flare;
        private readonly Material[] blades;
        private readonly Mesh bladeMesh;
        private readonly List<Raider> enemies = new List<Raider>();
        private readonly List<GameObject> debris = new List<GameObject>();
        private Transform sword;
        private TrailRenderer trail;
        private int damage;
        private float swing, nextWave, age, guardBlend, hitPause, recoil, strideTime;
        private Vector3 previousPosition;
        private bool strikePending, ended, reverseSwing;

        private sealed class Raider
        {
            public Transform Root, LeftLeg, RightLeg, LeftShin, RightShin, SwordArm, FreeArm, Head, Flash, Body;
            public int Health;
            public float Timer, Phase, Stagger, Recovery, Rise, Gait;
            public bool WindingUp, AttackPending;
        }

        public WorkshopRaidCombat(Transform player, Transform camera, Func<Color, float, Material> surface,
            Func<Color, float, Material> glow, List<Mesh> ownedMeshes, WorkshopRaidEffects effects)
        {
            this.player = player;
            this.camera = camera;
            this.effects = effects;
            bone = surface(new Color(.72f, .64f, .49f), 0);
            dark = surface(new Color(.045f, .035f, .026f), 0);
            leather = surface(new Color(.17f, .086f, .045f), 0);
            steel = surface(new Color(.41f, .47f, .49f), .32f);
            brass = surface(new Color(.62f, .39f, .15f), .26f);
            cloth = surface(new Color(.25f, .055f, .045f), 0);
            eyes = glow(new Color(.19f, .9f, .71f), 1.1f);
            flare = glow(new Color(1f, .31f, .06f), 1.6f);
            blades = new[] { surface(new Color(.34f, .26f, .20f), .22f), steel,
                surface(new Color(.62f, .72f, .76f), .28f), glow(new Color(.56f, .80f, .81f), .28f) };
            bladeMesh = BuildBlade();
            ownedMeshes.Add(bladeMesh);
        }

        public void Equip(int swordDamage)
        {
            if (sword != null) UnityEngine.Object.Destroy(sword.gameObject);
            damage = swordDamage;
            sword = Cutlass(camera, blades[Mathf.Clamp((damage - 16) / 8, 0, 3)], brass, leather, .75f);
            var hand = Part(sword, PrimitiveType.Capsule, new Vector3(.018f, .045f, -.054f),
                new Vector3(.137f, .109f, .13f), leather);
            hand.localRotation = Quaternion.Euler(0, 0, -17);
            Part(sword, PrimitiveType.Capsule, new Vector3(.09f, -.105f, -.105f),
                new Vector3(.143f, .19f, .14f), cloth).localRotation = Quaternion.Euler(18, 0, 24);
            var tip = Joint(sword, "Cutlass swing trail", new Vector3(.12f, .97f, 0));
            trail = tip.gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = effects.ParticleMaterial;
            trail.time = .14f;
            trail.minVertexDistance = .025f;
            trail.startWidth = .13f;
            trail.endWidth = 0;
            trail.startColor = damage >= 32 ? new Color(.57f, .94f, .91f, .8f) : new Color(1, .8f, .45f, .65f);
            trail.endColor = new Color(.9f, .57f, .3f, 0);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.emitting = false;
            previousPosition = player.position;
            PoseSword(0, false, 1);
        }

        public void Begin(int swordDamage)
        {
            ClearEnemies();
            ClearDebris();
            if (sword == null) Equip(swordDamage);
            damage = swordDamage;
            Health = MaxHealth;
            Wave = 0;
            swing = age = guardBlend = hitPause = recoil = strideTime = 0;
            previousPosition = player.position;
            trail.Clear();
            ended = strikePending = false;
            nextWave = .9f;
        }

        public bool Tick(float dt, Mouse mouse)
        {
            if (ended) return false;
            if (Remaining == 0 && Wave == 2)
            {
                ended = true;
                trail.emitting = false;
                return true;
            }
            if (hitPause > 0)
            {
                float remaining = dt - hitPause;
                hitPause -= dt;
                if (remaining <= 0) return false;
                dt = remaining;
            }
            age += dt;
            swing = Mathf.Max(0, swing - dt);
            bool guard = mouse?.rightButton.isPressed == true && swing <= 0;
            if (!guard && swing <= 0 && mouse?.leftButton.wasPressedThisFrame == true)
            {
                swing = SwingDuration;
                strikePending = true;
                reverseSwing = !reverseSwing;
                effects.Play(RaidCue.Swing);
            }
            if (strikePending && swing < .31f)
            {
                strikePending = false;
                Strike();
            }
            PoseSword(swing, guard, dt);

            for (int i = enemies.Count - 1; i >= 0; i--)
                Advance(enemies[i], dt, guard);

            if (Health <= 0)
            {
                ended = true;
                ClearEnemies();
                return true;
            }
            if (Remaining != 0) return false;
            if (Wave == 2)
            {
                ended = true;
                return true;
            }
            nextWave -= dt;
            if (nextWave <= 0)
            {
                Wave++;
                effects.Play(RaidCue.Wave);
                int count = Wave + 1;
                for (int i = 0; i < count; i++)
                    Spawn(new Vector3((i - (count - 1) * .5f) * 2.1f, .026f, -23 - i % 2 * 1.1f), i);
                nextWave = 1.6f;
            }
            return false;
        }

        private void Strike()
        {
            var origin = camera.position - Vector3.up * .47f;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var foe = enemies[i];
                Vector3 delta = foe.Root.position + Vector3.up * 1.12f - origin;
                if (delta.magnitude > 2.75f || Vector3.Dot(camera.forward, delta.normalized) < .46f ||
                    Physics.Linecast(origin + camera.forward * .33f, foe.Root.position + Vector3.up * 1.12f,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                effects.Burst(RaidCue.Bone, foe.Root.position + Vector3.up * 1.12f, camera.right);
                hitPause = .045f;
                recoil = .85f;
                foe.Health -= damage;
                foe.WindingUp = false;
                foe.AttackPending = false;
                foe.Timer = .5f;
                foe.Stagger = .3f;
                if (foe.Health > 0) continue;
                Collapse(foe, camera.forward);
                enemies.RemoveAt(i);
            }
        }

        private void Advance(Raider foe, float dt, bool guard)
        {
            Vector3 delta = player.position - foe.Root.position;
            delta.y = 0;
            float distance = delta.magnitude;
            Vector3 direction = distance > .01f ? delta / distance : Vector3.forward;
            foe.Root.rotation = Quaternion.RotateTowards(foe.Root.rotation, Quaternion.LookRotation(direction), dt * 490);
            foe.Timer -= dt;
            foe.Stagger -= dt;
            if (foe.Rise > 0)
            {
                foe.Rise -= dt;
                float emerge = 1 - Mathf.Clamp01(foe.Rise / .48f);
                foe.Body.localPosition = Vector3.down * (.57f * (1 - emerge) * (1 - emerge));
                if (foe.Rise > 0) return;
            }
            if (foe.Stagger > 0)
            {
                foe.Flash.gameObject.SetActive(false);
                foe.Body.localRotation = Quaternion.Euler(-foe.Stagger * 67, 0, foe.Stagger * 38);
                foe.SwordArm.localRotation = Quaternion.Euler(-38 - foe.Stagger * 65, 0, 17);
                return;
            }
            foe.Body.localRotation = Quaternion.Slerp(foe.Body.localRotation, Quaternion.identity, 1 - Mathf.Exp(-dt * 9));
            if (foe.Recovery > 0)
            {
                foe.Recovery = Mathf.Max(0, foe.Recovery - dt);
                float release = 1 - foe.Recovery / .45f;
                foe.SwordArm.localRotation = Quaternion.Euler(Mathf.Lerp(-115, 68, Mathf.SmoothStep(0, 1, Mathf.Min(1, release * 5))), 0, -20);
                foe.Body.localRotation = Quaternion.Euler(Mathf.Sin(release * Mathf.PI) * 14, -release * 21, 0);
                if (foe.AttackPending && release >= .17f)
                {
                    foe.AttackPending = false;
                    if (distance <= 1.92f && player.position.z < -6.5f)
                    {
                        bool blocked = guard && Vector3.Dot(player.forward, -direction) > .2f;
                        Health = Mathf.Max(0, Health - (blocked ? 3 : 18));
                        recoil = 1;
                        hitPause = .055f;
                        if (blocked) effects.Burst(RaidCue.Guard, camera.position + camera.forward * .7f - Vector3.up * .35f, -direction);
                        else effects.Play(RaidCue.Hurt);
                    }
                }
                return;
            }
            if (foe.WindingUp)
            {
                foe.Flash.gameObject.SetActive(true);
                float load = Mathf.SmoothStep(0, 1, 1 - Mathf.Clamp01(foe.Timer / Windup));
                foe.SwordArm.localRotation = Quaternion.Euler(Mathf.Lerp(-22, -140, load), -load * 15, -12);
                foe.Body.localRotation = Quaternion.Euler(-load * 13, load * 28, 0);
                if (foe.Timer > 0) return;
                foe.WindingUp = false;
                foe.Flash.gameObject.SetActive(false);
                foe.Timer = 1.05f;
                foe.Recovery = .45f;
                foe.AttackPending = true;
                return;
            }
            if (distance < 1.62f && foe.Timer <= 0 && player.position.z < -6.5f)
            {
                foe.WindingUp = true;
                foe.Timer = Windup;
                return;
            }
            Vector3 step = direction;
            foreach (var other in enemies)
            {
                if (other == foe) continue;
                Vector3 away = foe.Root.position - other.Root.position;
                if (away.sqrMagnitude < .65f && away.sqrMagnitude > .0001f) step += away.normalized * .65f;
            }
            bool walking = distance > 1.43f;
            if (walking)
            {
                Vector3 advance = step.normalized * dt * (1.42f + Wave * .16f);
                Vector3 target = foe.Root.position + advance;
                target.x = Mathf.Clamp(target.x, -5.6f, 5.6f);
                target.z = Mathf.Clamp(target.z, -24.7f, -7.35f);
                if (!Physics.Linecast(foe.Root.position + Vector3.up * .65f, target + Vector3.up * .65f))
                    foe.Root.position = target;
            }
            foe.Gait = Mathf.MoveTowards(foe.Gait, walking ? 1 : 0, dt * 5);
            float phase = age * 8 + foe.Phase;
            float stride = Mathf.Sin(phase) * 27 * foe.Gait;
            foe.Body.localPosition = Vector3.up * (.012f + Mathf.Abs(Mathf.Cos(phase)) * .035f * foe.Gait);
            foe.LeftLeg.localRotation = Quaternion.Euler(stride, 0, 0);
            foe.RightLeg.localRotation = Quaternion.Euler(-stride, 0, 0);
            foe.LeftShin.localRotation = Quaternion.Euler(Mathf.Max(0, -stride) * 1.35f, 0, 0);
            foe.RightShin.localRotation = Quaternion.Euler(Mathf.Max(0, stride) * 1.35f, 0, 0);
            foe.Head.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(age * 3 + foe.Phase) * 5);
            foe.SwordArm.localRotation = Quaternion.Euler(-20 - stride * .4f, 0, -8);
            foe.FreeArm.localRotation = Quaternion.Euler(-18 + stride * .65f, 0, 12);
        }

        public void UpdateHeldBlade(float dt)
        {
            age += dt;
            PoseSword(0, false, dt);
        }

        private void PoseSword(float timer, bool guard, float dt)
        {
            if (sword == null) return;
            Vector3 displacement = player.position - previousPosition;
            previousPosition = player.position;
            displacement.y = 0;
            float pace = Mathf.Clamp01(displacement.magnitude / Mathf.Max(dt * 2.5f, .001f));
            strideTime += dt * (5 + 4 * pace);
            if (pace > .6f && Mathf.FloorToInt(strideTime / Mathf.PI) !=
                Mathf.FloorToInt((strideTime - dt * (5 + 4 * pace)) / Mathf.PI)) effects.Play(RaidCue.Footstep);
            guardBlend = Mathf.MoveTowards(guardBlend, guard ? 1 : 0, dt * 8);
            recoil = Mathf.MoveTowards(recoil, 0, dt * 6);
            Vector3 rest = new Vector3(.41f + Mathf.Sin(strideTime) * .038f * pace,
                -.53f + Mathf.Sin(age * 2.5f) * .012f + Mathf.Abs(Mathf.Cos(strideTime)) * .032f * pace, .71f - recoil * .16f);
            Quaternion idle = Quaternion.Euler(19 + recoil * 18, -15, -19);
            float phase = timer > 0 ? 1 - timer / SwingDuration : 1;
            if (timer > 0)
            {
                float side = reverseSwing ? -1 : 1;
                var wind = new Vector3(side * .47f, -.39f, .53f);
                var follow = new Vector3(side * -.55f, -.42f, .72f);
                var windRot = Quaternion.Euler(-16, side * 38, side * -55);
                var followRot = Quaternion.Euler(56, side * -53, side * 108);
                if (phase < .31f)
                {
                    float t = Mathf.SmoothStep(0, 1, phase / .31f);
                    rest = Vector3.Lerp(rest, wind, t);
                    idle = Quaternion.Slerp(idle, windRot, t);
                }
                else if (phase < .60f)
                {
                    float t = Mathf.SmoothStep(0, 1, (phase - .31f) / .29f);
                    rest = Vector3.Lerp(wind, follow, t) + Vector3.forward * Mathf.Sin(t * Mathf.PI) * .23f;
                    idle = Quaternion.Slerp(windRot, followRot, t);
                }
                else
                {
                    float t = Mathf.SmoothStep(0, 1, (phase - .60f) / .40f);
                    rest = Vector3.Lerp(follow, rest, t);
                    idle = Quaternion.Slerp(followRot, idle, t);
                }
            }
            trail.emitting = phase >= .30f && phase <= .65f;
            sword.localPosition = Vector3.Lerp(rest, new Vector3(.13f, -.38f, .68f - recoil * .15f), guardBlend);
            sword.localRotation = Quaternion.Slerp(idle, Quaternion.Euler(12 + recoil * 21, -9, 68), guardBlend);
        }

        private void Spawn(Vector3 at, int index)
        {
            var root = Joint(null, "Drowned raider", at);
            root.SetParent(player.parent, true);
            var foe = new Raider { Root = root, Health = 48, Timer = .7f + index * .22f, Phase = index * 2.1f, Rise = .48f };
            Part(root, PrimitiveType.Sphere, new Vector3(0, .91f, 0), new Vector3(.34f, .24f, .2f), bone);
            Part(root, PrimitiveType.Capsule, new Vector3(0, 1.16f, -.045f), new Vector3(.10f, .27f, .095f), bone);
            for (int i = 0; i < 4; i++)
            {
                float y = 1.07f + i * .082f, width = .205f - i * .013f;
                for (int side = -1; side <= 1; side += 2)
                {
                    var outer = new Vector3(side * width, y, .035f);
                    Rib(root, new Vector3(0, y + .022f, -.068f), outer);
                    Rib(root, outer, new Vector3(side * .055f, y - .025f, .119f));
                }
            }
            Part(root, PrimitiveType.Cube, new Vector3(0, 1.22f, .106f), new Vector3(.1f, .32f, .08f), bone);
            Part(root, PrimitiveType.Cube, new Vector3(-.11f, .91f, .105f), new Vector3(.24f, .13f, .055f), cloth).localRotation = Quaternion.Euler(0, 0, -12);
            foe.Head = Joint(root, "Skull", new Vector3(0, 1.57f, .018f));
            Part(foe.Head, PrimitiveType.Sphere, Vector3.up * .09f, new Vector3(.32f, .34f, .29f), bone);
            Part(foe.Head, PrimitiveType.Cube, new Vector3(0, -.046f, .048f), new Vector3(.19f, .12f, .16f), bone);
            Part(foe.Head, PrimitiveType.Cube, new Vector3(0, -.04f, .132f), new Vector3(.15f, .026f, .02f), dark);
            Part(foe.Head, PrimitiveType.Cube, new Vector3(0, .184f, .016f), new Vector3(.34f, .078f, .31f), cloth);
            for (int side = -1; side <= 1; side += 2)
            {
                Part(foe.Head, PrimitiveType.Sphere, new Vector3(.074f * side, .079f, .129f), new Vector3(.084f, .081f, .053f), dark);
                Part(foe.Head, PrimitiveType.Sphere, new Vector3(.074f * side, .081f, .157f), Vector3.one * .034f, eyes);
                var leg = Joint(root, "Leg", new Vector3(side * .13f, .88f, 0));
                Part(leg, PrimitiveType.Capsule, new Vector3(0, -.218f, 0), new Vector3(.091f, .24f, .092f), bone);
                Part(leg, PrimitiveType.Sphere, new Vector3(0, -.444f, .017f), Vector3.one * .112f, bone);
                var shin = Joint(leg, "Shin", new Vector3(0, -.444f, .017f));
                Part(shin, PrimitiveType.Capsule, new Vector3(0, -.199f, -.017f), new Vector3(.069f, .21f, .077f), bone);
                Part(shin, PrimitiveType.Sphere, new Vector3(0, -.397f, .041f), new Vector3(.126f, .07f, .22f), leather);
                var arm = Joint(root, "Arm", new Vector3(side * .252f, 1.36f, 0));
                arm.localRotation = Quaternion.Euler(-16, 0, side * 12);
                Part(arm, PrimitiveType.Sphere, Vector3.zero, Vector3.one * .129f, bone);
                Part(arm, PrimitiveType.Capsule, new Vector3(0, -.17f, 0), new Vector3(.083f, .18f, .08f), bone);
                Part(arm, PrimitiveType.Capsule, new Vector3(0, -.405f, .057f), new Vector3(.071f, .15f, .08f), bone).localRotation = Quaternion.Euler(-25, 0, 0);
                if (side > 0)
                {
                    foe.RightLeg = leg;
                    foe.RightShin = shin;
                    foe.SwordArm = arm;
                    var blade = Cutlass(arm, steel, brass, leather, .66f);
                    blade.localPosition = new Vector3(0, -.51f, .12f);
                    blade.localRotation = Quaternion.Euler(105, 0, 0);
                }
                else
                {
                    foe.LeftLeg = leg;
                    foe.LeftShin = shin;
                    foe.FreeArm = arm;
                }
            }
            foe.Flash = Part(foe.Head, PrimitiveType.Sphere, Vector3.up * .39f, Vector3.one * .103f, flare);
            foe.Flash.gameObject.SetActive(false);
            var body = Joint(root, "Animated bones", Vector3.zero);
            while (root.childCount > 1) root.GetChild(0).SetParent(body, false);
            body.localPosition = Vector3.down * .57f;
            foe.Body = body;
            effects.Burst(RaidCue.Wave, at + Vector3.up * .14f, Vector3.up, false);
            enemies.Add(foe);
        }

        private void Rib(Transform root, Vector3 a, Vector3 b)
        {
            var rib = Part(root, PrimitiveType.Capsule, (a + b) * .5f,
                new Vector3(.041f, Vector3.Distance(a, b) * .56f, .041f), bone);
            rib.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
        }

        private Transform Cutlass(Transform parent, Material edge, Material hilt, Material grip, float size)
        {
            var root = Joint(parent, "Cutlass", Vector3.zero);
            root.localScale = Vector3.one * size;
            Part(root, PrimitiveType.Capsule, new Vector3(0, .03f, 0), new Vector3(.075f, .15f, .073f), grip);
            Part(root, PrimitiveType.Sphere, new Vector3(0, -.098f, 0), new Vector3(.105f, .09f, .105f), hilt);
            Part(root, PrimitiveType.Cube, new Vector3(0, .187f, 0), new Vector3(.29f, .049f, .10f), hilt);
            Part(root, PrimitiveType.Cube, new Vector3(-.131f, .067f, 0), new Vector3(.047f, .25f, .057f), hilt).localRotation = Quaternion.Euler(0, 0, 14);
            for (int i = 0; i < 4; i++)
                Part(root, PrimitiveType.Cylinder, new Vector3(0, -.04f + i * .044f, 0),
                    new Vector3(.088f, .008f, .087f), dark).localRotation = Quaternion.Euler(0, 0, 11);
            Part(root, PrimitiveType.Sphere, new Vector3(.143f, .183f, 0), Vector3.one * .072f, hilt);
            Part(root, PrimitiveType.Sphere, new Vector3(-.143f, .183f, 0), Vector3.one * .072f, hilt);
            if (edge == blades[2] || edge == blades[3])
            {
                var inlay = edge == blades[3] ? eyes : brass;
                for (int i = 0; i < 3; i++)
                    for (int sign = -1; sign <= 1; sign += 2)
                        Part(root, PrimitiveType.Cube, new Vector3(.009f + i * .009f, .34f + i * .11f, sign * .012f),
                            new Vector3(.018f, .018f, .007f), inlay).localRotation = Quaternion.Euler(0, 0, 45);
            }
            var blade = new GameObject("Beveled steel", typeof(MeshFilter), typeof(MeshRenderer));
            blade.transform.SetParent(root, false);
            blade.GetComponent<MeshFilter>().sharedMesh = bladeMesh;
            blade.GetComponent<MeshRenderer>().sharedMaterial = edge;
            return root;
        }

        private static Mesh BuildBlade()
        {
            var rings = new[] { new Vector3(0, .2f, .048f), new Vector3(.014f, .45f, .061f),
                new Vector3(.042f, .72f, .073f), new Vector3(.079f, .89f, .047f),
                new Vector3(.125f, 1.005f, .001f) };
            var vertices = new List<Vector3>();
            var indices = new List<int>();
            Vector3 Corner(Vector3 ring, int side)
            {
                float half = ring.z;
                if (side == 0) return new Vector3(ring.x - half, ring.y, 0);
                if (side == 1) return new Vector3(ring.x, ring.y, half * .23f);
                if (side == 2) return new Vector3(ring.x + half, ring.y, 0);
                return new Vector3(ring.x, ring.y, -half * .23f);
            }
            for (int i = 0; i < rings.Length - 1; i++)
                for (int side = 0; side < 4; side++)
                {
                    int next = (side + 1) % 4, start = vertices.Count;
                    vertices.Add(Corner(rings[i], side));
                    vertices.Add(Corner(rings[i], next));
                    vertices.Add(Corner(rings[i + 1], next));
                    vertices.Add(Corner(rings[i + 1], side));
                    indices.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
                }
            var mesh = new Mesh { name = "Cutlass blade bevels" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Transform Joint(Transform parent, string label, Vector3 position)
        {
            var joint = new GameObject(label).transform;
            joint.SetParent(parent, false);
            joint.localPosition = position;
            return joint;
        }

        private static Transform Part(Transform parent, PrimitiveType type, Vector3 position, Vector3 size, Material material)
        {
            var part = GameObject.CreatePrimitive(type);
            var collider = part.GetComponent<Collider>();
            collider.enabled = false;
            UnityEngine.Object.Destroy(collider);
            part.GetComponent<Renderer>().sharedMaterial = material;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = size;
            return part.transform;
        }

        private void ClearEnemies()
        {
            foreach (var foe in enemies)
                if (foe.Root != null) UnityEngine.Object.Destroy(foe.Root.gameObject);
            enemies.Clear();
        }

        private void Collapse(Raider foe, Vector3 push)
        {
            effects.Burst(RaidCue.Collapse, foe.Root.position + Vector3.up, push);
            foe.Flash.gameObject.SetActive(false);
            var playerCollider = player.GetComponent<Collider>();
            var colliders = new List<Collider>(6);
            Drop(foe.Head, new Vector3(0, .06f, 0), new Vector3(.28f, .32f, .26f));
            Drop(foe.SwordArm, Vector3.down * .24f, new Vector3(.13f, .5f, .15f));
            Drop(foe.FreeArm, Vector3.down * .24f, new Vector3(.13f, .5f, .15f));
            Drop(foe.LeftLeg, Vector3.down * .42f, new Vector3(.13f, .81f, .14f));
            Drop(foe.RightLeg, Vector3.down * .42f, new Vector3(.13f, .81f, .14f));
            Drop(foe.Body, Vector3.up * 1.13f, new Vector3(.41f, .5f, .24f));
            UnityEngine.Object.Destroy(foe.Root.gameObject);

            void Drop(Transform segment, Vector3 center, Vector3 bounds)
            {
                var go = segment.gameObject;
                segment.SetParent(player.parent, true);
                go.name = "Fallen " + go.name;
                go.layer = 2; // Ignore Raycast: fallen bones never block a swing or pursuit.
                var collider = go.AddComponent<BoxCollider>();
                collider.center = center;
                collider.size = bounds;
                if (playerCollider != null) Physics.IgnoreCollision(playerCollider, collider);
                foreach (var other in colliders) Physics.IgnoreCollision(other, collider);
                colliders.Add(collider);
                var body = go.AddComponent<Rigidbody>();
                body.mass = .65f;
                body.angularDamping = .35f;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                body.AddForce(push * UnityEngine.Random.Range(.55f, 1.5f) + Vector3.up * UnityEngine.Random.Range(.6f, 1.3f)
                    + UnityEngine.Random.insideUnitSphere * .6f, ForceMode.VelocityChange);
                body.AddTorque(UnityEngine.Random.insideUnitSphere * 9, ForceMode.VelocityChange);
                debris.Add(go);
                UnityEngine.Object.Destroy(go, 6f);
            }
        }

        private void ClearDebris()
        {
            foreach (var part in debris)
                if (part != null) UnityEngine.Object.Destroy(part);
            debris.Clear();
        }

        public void Clear()
        {
            ClearEnemies();
            ClearDebris();
            if (sword != null) UnityEngine.Object.Destroy(sword.gameObject);
            sword = null;
        }
    }
}
