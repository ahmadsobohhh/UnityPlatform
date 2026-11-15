using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImagineQuest.Gameplay
{
    public sealed partial class BlacksmithRoomBootstrap
    {
        private GameObject cryptSealGate, cryptVaultGate;
        private Transform cryptRelic;
        private readonly List<CryptLoot> cryptLoot = new List<CryptLoot>();
        private readonly RaycastHit[] cryptSightHits = new RaycastHit[16];
        private readonly List<CryptSwitch> cryptSwitches = new List<CryptSwitch>();
        private readonly Dictionary<string, GameObject> cryptModels = new Dictionary<string, GameObject>();
        private Material cryptStone, cryptGold;
        private int cryptOrder, cryptGoldEarned, cryptXpEarned;
        private bool cryptFirstOpen, cryptSecondOpen, cryptCleared;
        private bool cryptRelicClaimed;
        private float cryptFeedbackUntil;
        private string cryptFeedback;
        private static readonly int[] SealOrder = { 1, 2, 0 };
        private static readonly string[] SealNames = { "Flame", "Anchor", "Moon" };

        private sealed class CryptSwitch
        {
            public Transform Model;
            public TMP_Text Label;
            public int Value;
            public bool IsVault;
        }

        private sealed class CryptLoot
        {
            public Transform Lid;
            public string Name;
            public int Gold, Xp, Healing;
            public bool Claimed;
        }

        private void InitializeCryptPalette()
        {
            if (cryptStone != null) return;
            cryptStone = Surface(Color.white);
            cryptStone.mainTexture = Resources.Load<Texture2D>("TideCrypt/dungeon_texture");
            cryptGold = Glow(new Color(1, .63f, .23f), .9f);
        }

        private void BuildTideCrypt(Material oak)
        {
            InitializeCryptPalette();
            var footprint = new HashSet<Vector2Int>();
            AddSpan(0, 0, 0);                 // Entry tunnel
            AddSpan(1, -1, 1);                // Seal gallery
            AddSpan(2, 0, 0);
            AddSpan(3, -2, 2); AddSpan(4, -2, 2); AddSpan(5, -2, 2); // Grand Ossuary
            AddSpan(6, 0, 0);
            AddSpan(7, -1, 1);                // Gatehouse
            AddSpan(8, -2, 2); AddSpan(9, -3, 3); AddSpan(10, -3, 3); // Drowned Court
            AddSpan(11, -3, -1); AddSpan(11, 0, 3);
            AddSpan(12, -2, 2);              // Reliquary
            AddSpan(13, -1, 1);

            foreach (var cell in footprint)
            {
                var centre = new Vector3(cell.x * 4, 0, -7 - cell.y * 4);
                CryptModel("floor_tile_large", centre, 0);
                float ceiling = cell.y >= 8 ? 7f : cell.y >= 3 && cell.y <= 5 ? 6f : 4f;
                CryptModel("floor_tile_large", centre + Vector3.up * (ceiling - .06f), 0).Rotate(0, 0, 180, Space.Self);
                CryptSolid("Crypt floor", centre + Vector3.down * .095f, new Vector3(4, .19f, 4));
                Border(new Vector2Int(-1, 0), Vector3.left, 90);
                Border(new Vector2Int(1, 0), Vector3.right, 90);
                if (cell.y != 0) Border(new Vector2Int(0, -1), Vector3.forward, 0);
                Border(new Vector2Int(0, 1), Vector3.back, 0);
                if (cell.y % 2 == 1 && cell.x == 0)
                {
                    CryptCandleStand(centre + new Vector3(-1.75f, 0, 1.2f), 90);
                    CryptCandleStand(centre + new Vector3(1.75f, 0, -1.3f), -90);
                }

                void Border(Vector2Int adjacent, Vector3 direction, float yaw)
                {
                    if (footprint.Contains(cell + adjacent)) return;
                    var edge = centre + direction * 2;
                    var wall = CryptModel((cell.x + cell.y) % 3 == 0 ? "wall_cracked" : "wall", edge, yaw);
                    wall.localScale = new Vector3(1, ceiling / 4, 1);
                    CryptSolid("Crypt wall", edge + Vector3.up * (ceiling / 2),
                        yaw == 0 ? new Vector3(4.03f, ceiling, .66f) : new Vector3(.66f, ceiling, 4.03f));
                }
            }

            void AddSpan(int row, int left, int right)
            {
                for (int x = left; x <= right; x++) footprint.Add(new Vector2Int(x, row));
            }

            raidGate = Block("Forge storm shutter", new Vector3(0, 1.25f, -5.15f), new Vector3(2.2f, 2.5f, .19f), oak);
            shutterRest = raidGate.transform.position;
            shutterCollider = raidGate.GetComponent<Collider>();
            shutterCollider.enabled = false;
            raidGate.SetActive(false);
            cryptSealGate = CryptArch(-13f);
            cryptVaultGate = CryptArch(-29f);
            for (int i = 0; i < 3; i++)
            {
                AddSwitch(new Vector3(-3.5f + i * 3.5f, 0, -11.6f), i, false);
                AddSwitch(new Vector3(-5f + i * 1.7f, 0, -27.25f), i, true);
            }
            WorldLabel("Seal inscription", "ANCHOR  /  MOON  /  FLAME", new Vector3(0, 2.8f, -12.55f),
                new Vector2(390, 50), 24, Gold, false, 0);
            WorldLabel("Vault inscription", "OFFER  2 × 3", new Vector3(-3.4f, 2.35f, -28.55f),
                new Vector2(300, 49), 28, Gold, false, 0);

            foreach (float z in new[] { -20f, -25f, -39f, -44f, -49f, -54f })
                foreach (int sign in new[] { -1, 1 })
                {
                    var p = new Vector3(sign * 4.7f, 0, z);
                    float height = z > -29 ? 6 : 7;
                    CryptModel("pillar_decorated", p, 0).localScale = new Vector3(.78f, height / 4, .78f);
                    CryptSolid("Ancient pier", p + Vector3.up * (height / 2), new Vector3(1.3f, height, 1.3f));
                    CryptModel("banner_patternA_red", new Vector3(sign * 5.28f, 0, z - .2f), sign * -90);
                    CryptCandleStand(p + new Vector3(-sign * 1.06f, 0, .49f), sign * 30);
                }

            CryptModel("chest_gold", new Vector3(0, 0, -58.1f), 0);
            cryptRelic = CryptModel("chest_gold_lid", new Vector3(0, 0, -58.1f), 0);
            CryptSolid("Relic chest", new Vector3(0, .37f, -58.1f), new Vector3(1.5f, .74f, 1.15f));
            AddCryptCache("Salt-stained cache", new Vector3(-7.9f, 0, -20.3f), 15, 20, 0);
            AddCryptCache("Quartermaster's chest", new Vector3(7.8f, 0, -25.2f), 24, 30, 28);
            AddCryptCache("Smuggler's stash", new Vector3(-10.8f, 0, -44.3f), 35, 45, 28);
            AddCryptCache("Drowned captain's chest", new Vector3(11.1f, 0, -49.6f), 48, 55, 20);
            CryptCandleStand(new Vector3(-2.15f, 0, -58.0f), 30);
            CryptCandleStand(new Vector3(2.15f, 0, -58.0f), -30);
            Lamp("Crypt entrance light", new Vector3(0, 2.8f, -8.25f), new Color(.51f, .65f, 1), 4.8f, 9).shadows = LightShadows.None;
            ResetCrypt();
        }

        private void AddCryptCache(string name, Vector3 p, int gold, int xp, int healing)
        {
            CryptModel("chest_gold", p, 0, .79f);
            var lid = CryptModel("chest_gold_lid", p, 0, .79f);
            WorldLabel("Cache name", name, p + Vector3.up * 1.44f, new Vector2(285, 40), 19, Gold, true);
            CryptCandleStand(p + new Vector3(.88f, 0, -.30f), -25);
            CryptSolid(name + " body", p + Vector3.up * .29f, new Vector3(1.15f, .58f, .85f));
            cryptLoot.Add(new CryptLoot { Lid = lid, Name = name, Gold = gold, Xp = xp, Healing = healing });
        }

        private void CryptCandleStand(Vector3 floor, float yaw)
        {
            // Wall torches placed out in open rooms looked suspended in midair.
            // Match actual renderer bounds so candles sit on the plinth, even
            // when an imported mesh has an off-centre pivot.
            var stand = CryptModel("pillar_decorated", floor, yaw, .25f);
            stand.name = "Grounded crypt candle stand";
            var support = ForgeBounds(stand);
            stand.position += Vector3.up * (floor.y - .01f - support.min.y);
            support = ForgeBounds(stand);
            var at = new Vector3(support.center.x, support.max.y, support.center.z);
            var candles = CryptModel("candle_triple", at, yaw, .74f);
            candles.position += Vector3.up * (support.max.y - .012f - ForgeBounds(candles).min.y);
            candles.SetParent(stand, true);
            var light = Lamp("Crypt candlelight", at + Vector3.up * .38f, new Color(1, .66f, .34f), 4.1f, 6.6f);
            light.shadows = LightShadows.None;
            light.transform.SetParent(stand, true);
        }

        private GameObject CryptArch(float z)
        {
            CryptModel("wall_doorway", new Vector3(0, 0, z), 0);
            CryptSolid("Door jamb left", new Vector3(-1.51f, 2, z), new Vector3(1.02f, 4, .75f));
            CryptSolid("Door jamb right", new Vector3(1.51f, 2, z), new Vector3(1.02f, 4, .75f));
            CryptSolid("Door lintel", new Vector3(0, 3.63f, z), new Vector3(2, .74f, .75f));
            var gate = CryptModel("wall_doorway_door", new Vector3(0, 0, z), 0);
            var box = gate.gameObject.AddComponent<BoxCollider>();
            box.center = Vector3.up * 1.38f;
            box.size = new Vector3(2.05f, 2.77f, .44f);
            return gate.gameObject;
        }

        private void AddSwitch(Vector3 p, int value, bool vault)
        {
            var model = CryptModel("pillar_decorated", p, value * 90, .18f);
            var seal = Decor("Brass rune switch", PrimitiveType.Cylinder, p + Vector3.up * .735f,
                new Vector3(.23f, .021f, .23f), cryptGold);
            seal.SetParent(model, true);
            CryptSolid("Rune switch base", p + Vector3.up * .32f, new Vector3(.37f, .64f, .33f));
            var label = WorldLabel("Crypt seal", vault ? (value * 2 + 4).ToString() : SealNames[value], p + Vector3.up * 1f,
                new Vector2(200, 42), 23, Gold, true);
            cryptSwitches.Add(new CryptSwitch { Model = model, Label = label, Value = value, IsVault = vault });
        }

        private void Torch(Vector3 p, float yaw, Transform parent = null)
        {
            var torch = CryptModel("torch_mounted", p, yaw, parent: parent);
            Decor("Torch ember", PrimitiveType.Sphere, p + Vector3.up * .48f,
                new Vector3(.09f, .19f, .09f), cryptGold).SetParent(torch, true);
            var lamp = Lamp("Amber torch", p + Vector3.up * .46f, new Color(1, .62f, .30f), 4.8f, 6.8f);
            lamp.shadows = LightShadows.None;
            lamp.transform.SetParent(torch, true);
        }

        private Transform CryptModel(string name, Vector3 position, float yaw, float scale = 1, Transform parent = null)
        {
            if (!cryptModels.TryGetValue(name, out var asset))
                cryptModels[name] = asset = Resources.Load<GameObject>("TideCrypt/" + name);
            if (asset == null) throw new System.InvalidOperationException("Missing Tide Crypt model: " + name);
            var model = Instantiate(asset, transform);
            model.name = "Crypt " + name;
            model.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            model.transform.localScale = Vector3.one * scale;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterial = cryptStone;
            if (parent != null) model.transform.SetParent(parent, true);
            return model.transform;
        }

        private void CryptSolid(string title, Vector3 position, Vector3 size)
        {
            var solid = new GameObject(title, typeof(BoxCollider));
            solid.transform.SetParent(transform, false);
            solid.transform.position = position;
            solid.GetComponent<BoxCollider>().size = size;
        }

        private void ResetCrypt()
        {
            if (cryptSealGate == null) return;
            cryptOrder = 0;
            cryptFirstOpen = cryptSecondOpen = cryptCleared = cryptRelicClaimed = false;
            cryptGoldEarned = cryptXpEarned = 0;
            cryptFeedbackUntil = 0;
            cryptSealGate.SetActive(true);
            cryptVaultGate.SetActive(true);
            cryptRelic.localRotation = Quaternion.identity;
            foreach (var chest in cryptLoot)
            {
                chest.Claimed = false;
                chest.Lid.localRotation = Quaternion.identity;
            }
            foreach (var seal in cryptSwitches) seal.Label.color = Gold;
        }

        private bool CryptWaveReady(int wave)
        {
            float z = player.transform.position.z;
            return wave == 1 ? cryptFirstOpen && z < -17f : cryptSecondOpen && z < -37f;
        }

        private void UpdateCryptInteraction(Keyboard keys)
        {
            if (Time.time < cryptFeedbackUntil) { prompt.text = cryptFeedback; return; }
            prompt.text = "";
            bool pressed = keys != null && keys.eKey.wasPressedThisFrame;
            if (cryptCleared && !cryptRelicClaimed && CryptInReach(cryptRelic.position + Vector3.up * .85f))
            {
                prompt.text = "[E]  Claim the Tidekeeper";
                if (pressed)
                {
                    cryptRelicClaimed = true;
                    cryptRelic.Rotate(-45, 0, 0, Space.Self);
                    cryptGoldEarned += 75;
                    cryptXpEarned += 100;
                    raidEffects.Burst(RaidCue.Forge, cryptRelic.position + Vector3.up * .8f, Vector3.up);
                    ShowRaidResult(true);
                }
                return;
            }
            for (int i = 0; i < cryptLoot.Count; i++)
            {
                var chest = cryptLoot[i];
                if (chest.Claimed || !CryptInReach(chest.Lid.position + Vector3.up * .81f)) continue;
                prompt.text = "[E]  " + chest.Name;
                if (pressed) CollectCryptLoot(i);
                return;
            }
            foreach (var seal in cryptSwitches)
            {
                if (seal.IsVault ? cryptSecondOpen : cryptFirstOpen) continue;
                if (!CryptInReach(seal.Model.position + Vector3.up * .73f)) continue;
                prompt.text = "[E]  " + seal.Label.text;
                if (pressed) TryCryptSeal(seal.Value, seal.IsVault);
                return;
            }
        }

        private void CollectCryptLoot(int index)
        {
            var chest = cryptLoot[index];
            if (chest.Claimed) return;
            chest.Claimed = true;
            chest.Lid.Rotate(-42, 0, 0, Space.Self);
            cryptGoldEarned += chest.Gold;
            cryptXpEarned += chest.Xp;
            raidCombat.RecoverHealth(chest.Healing);
            raidEffects.Burst(RaidCue.Forge, chest.Lid.position + Vector3.up * .7f, Vector3.up, false);
            effectsAudio.PlayOneShot(keySound, .4f);
            CryptFeedback($"+{chest.Gold} gold   •   +{chest.Xp} XP");
        }

        private bool CryptInReach(Vector3 target)
        {
            var ray = target - view.position;
            float depth = Vector3.Dot(view.forward, ray);
            // Use an actual small reticle target instead of a wide forward cone.
            if (depth <= .08f || ray.sqrMagnitude > 2.15f * 2.15f ||
                (ray - view.forward * depth).sqrMagnitude > .28f * .28f) return false;
            int count = Physics.RaycastNonAlloc(view.position, ray.normalized, cryptSightHits, ray.magnitude - .025f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == cryptSightHits.Length) return false;
            for (int i = 0; i < count; i++)
                if (cryptSightHits[i].collider != null && !cryptSightHits[i].transform.IsChildOf(player.transform))
                    return false;
            return true;
        }

        private void TryCryptSeal(int value, bool vault)
        {
            if (value < 0 || value >= 3 || (vault ? cryptSecondOpen : cryptFirstOpen)) return;
            if (vault && (raidCombat.Wave != 1 || raidCombat.Remaining != 0))
            {
                CryptFeedback("Clear the burial hall first.");
                return;
            }
            if (vault ? value != 1 : value != SealOrder[cryptOrder])
            {
                if (!vault)
                {
                    cryptOrder = 0;
                    foreach (var seal in cryptSwitches) if (!seal.IsVault) seal.Label.color = Gold;
                }
                effectsAudio.PlayOneShot(errorSound, .35f);
                CryptFeedback(vault ? "The seal stays shut." : "The runes reset.");
                return;
            }
            var selected = cryptSwitches.Find(seal => seal.IsVault == vault && seal.Value == value);
            selected.Label.color = Teal;
            raidEffects.Burst(RaidCue.Forge, selected.Model.position + Vector3.up * .65f, Vector3.up);
            if (vault)
            {
                cryptSecondOpen = true;
                cryptVaultGate.SetActive(false);
                CryptFeedback("The vault opens.");
            }
            else if (++cryptOrder == SealOrder.Length)
            {
                cryptFirstOpen = true;
                cryptSealGate.SetActive(false);
                CryptFeedback("The seal opens.");
            }
        }

        private void CryptFeedback(string message)
        {
            cryptFeedback = message;
            cryptFeedbackUntil = Time.time + .75f;
        }
    }
}
