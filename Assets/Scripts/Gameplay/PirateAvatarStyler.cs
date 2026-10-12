using UnityEngine;

namespace ImagineQuest.Gameplay
{
    /// <summary>Portrait-inspired outfits using only the original modular pirate meshes.</summary>
    public sealed class PirateAvatarStyler : MonoBehaviour
    {
        public string SkinId { get; private set; } = "navigator";

        public void Apply(string avatarId, string colorHex = "FFFFFF")
        {
            // Remove only the placeholder attachments created by the previous art pass.
            RemovePrototypeAccessories();
            var id = (avatarId ?? "navigator").Trim().ToLowerInvariant();
            var primary = new Color(0.22f, 0.45f, 0.58f);
            var trim = new Color(0.8f, 0.62f, 0.25f);
            switch (id)
            {
                case "hero": primary = new Color(0.25f, 0.40f, 0.67f); trim = new Color(0.65f, 0.12f, 0.09f); break;
                case "mage": primary = new Color(0.18f, 0.25f, 0.73f); break;
                case "knight": primary = new Color(0.68f, 0.74f, 0.82f); break;
                case "bandit": primary = new Color(0.30f, 0.19f, 0.11f); trim = new Color(0.12f, 0.11f, 0.10f); break;
                case "monk": primary = new Color(0.95f, 0.49f, 0.08f); trim = new Color(0.28f, 0.43f, 0.22f); break;
                case "archer": primary = new Color(0.12f, 0.5f, 0.23f); trim = new Color(0.44f, 0.28f, 0.13f); break;
                case "samurai": primary = new Color(0.70f, 0.12f, 0.08f); break;
                case "scholar": primary = new Color(0.37f, 0.18f, 0.53f); trim = new Color(0.85f, 0.78f, 0.59f); break;
                default: id = "navigator"; break;
            }
            SkinId = id;
            if (ColorUtility.TryParseHtmlString("#" + (colorHex ?? "FFFFFF").TrimStart('#'), out var personalTint))
                primary = Color.Lerp(primary, primary * personalTint, 0.3f);

            var cloak = id == "mage" || id == "bandit" || id == "archer" || id == "scholar";
            var armored = id == "knight" || id == "samurai";
            foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var name = renderer.name;
                // The master prefab contains a complete body AND alternative split
                // body meshes. Rendering both makes coplanar skin surfaces flicker.
                if (name == "Pirate_Body" || name == "Pirate_Head" || name == "Pirate_Pants" ||
                    name == "Pirate_Legs" || name == "Pirate_Body_Cloth_Detail") renderer.gameObject.SetActive(false);
                // Torso includes the chest, head and arms. Head is the chest-cutout
                // alternative and must not be combined with (or replace) Torso here.
                if (name == "Pirate_Torso" || name == "Pirate_Jaw" || name == "Pirate_Eyes" || name == "Pirate_Shoes")
                    renderer.gameObject.SetActive(true);
                if (name.StartsWith("Pirate_Belt_")) renderer.gameObject.SetActive(name == "Pirate_Belt_01");
                if (name.StartsWith("Pirate_Ring_")) renderer.gameObject.SetActive(name == "Pirate_Ring_01");
                if (name.StartsWith("Pirate_Pistol_In_Belt")) renderer.gameObject.SetActive(false);
                if (name.StartsWith("Pirate_Cloth_Body_")) renderer.gameObject.SetActive(name == (armored ? "Pirate_Cloth_Body_02" : "Pirate_Cloth_Body_01"));
                if (name.StartsWith("Pirate_Cloth_Legs_")) renderer.gameObject.SetActive(name == "Pirate_Cloth_Legs_01");
                if (name.StartsWith("Pirate_Cloak_")) renderer.gameObject.SetActive(cloak && name == "Pirate_Cloak_Down");
                if (name == "Pirate_Hat") renderer.gameObject.SetActive(id == "navigator" || id == "hero" || id == "bandit" || id == "archer");
                if (name == "Pirate_Skull_Hat") renderer.gameObject.SetActive(armored);
                if (name == "Pirate_Beard" || name == "Pirate_Mustache") renderer.gameObject.SetActive(false);
                if (name == "Pirate_Sword_In_Belt") renderer.gameObject.SetActive(id == "hero" || armored || id == "bandit");
                if (name == "Pirate_Shoes_Sphere") renderer.gameObject.SetActive(false);

                var block = new MaterialPropertyBlock();
                var gearColor = name.Contains("Belt") || name.Contains("Ring") ? trim : primary;
                if (name.Contains("Cloth") || name.Contains("Cloak") || name.Contains("Belt"))
                {
                    block.SetColor("_BaseColor", gearColor);
                    block.SetColor("_Color", gearColor);
                    block.SetFloat("_Metallic", id == "knight" ? 0.6f : 0f);
                }
                renderer.SetPropertyBlock(block);
            }
        }

        private void RemovePrototypeAccessories()
        {
            foreach (var part in GetComponentsInChildren<Transform>(true))
            {
                switch (part.name)
                {
                    case "Mage hat brim": case "Mage pointed hat": case "Mage hat gold band":
                    case "Armored helmet": case "Red helmet crest": case "Shoulder armor":
                    case "Samurai gold crest": case "Portrait scarf": case "Archer quiver":
                    case "Quiver arrow": case "Scholar book": case "Scholar glasses bridge": case "Scholar spectacles":
                        part.gameObject.SetActive(false);
                        if (Application.isPlaying) Destroy(part.gameObject); else DestroyImmediate(part.gameObject);
                        break;
                }
            }
        }
    }
}
