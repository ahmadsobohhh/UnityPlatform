using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ImagineQuest.Gameplay
{
    public sealed class QuestArenaOutline : MonoBehaviour
    {
        private readonly List<Renderer> shells = new List<Renderer>();
        private MaterialPropertyBlock properties;
        private Color color;
        private static bool revealed;

        public static void SetRevealed(bool value) => revealed = value;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetReveal() => revealed = false;

        public void Configure(Material outlineMaterial, Color edgeColor)
        {
            if (outlineMaterial == null || shells.Count > 0) return;
            color = edgeColor;
            var originals = GetComponentsInChildren<Renderer>();
            foreach (var original in originals)
            {
                if (!original.enabled || !original.gameObject.activeInHierarchy) continue;
                Mesh mesh = null;
                var skinned = original as SkinnedMeshRenderer;
                if (skinned != null) mesh = skinned.sharedMesh;
                else if (original.TryGetComponent<MeshFilter>(out var filter)) mesh = filter.sharedMesh;
                if (mesh == null) continue;
                var shell = new GameObject("Interaction edge");
                shell.transform.SetParent(original.transform, false);
                Renderer renderer;
                if (skinned != null)
                {
                    var skin = shell.AddComponent<SkinnedMeshRenderer>();
                    skin.sharedMesh = mesh;
                    skin.bones = skinned.bones;
                    skin.rootBone = skinned.rootBone;
                    skin.localBounds = skinned.localBounds;
                    renderer = skin;
                }
                else
                {
                    shell.AddComponent<MeshFilter>().sharedMesh = mesh;
                    renderer = shell.AddComponent<MeshRenderer>();
                }
                var materials = new Material[Mathf.Max(1, mesh.subMeshCount)];
                for (var index = 0; index < materials.Length; index++) materials[index] = outlineMaterial;
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                shells.Add(renderer);
            }
            SetFocused(false);
        }

        public void SetFocused(bool focused)
        {
            if (properties == null) properties = new MaterialPropertyBlock();
            properties.SetColor("_OutlineColor", color * (focused ? 1.65f : 1.1f));
            properties.SetFloat("_OutlineWidth", focused ? 2.1f : 1.3f);
            foreach (var shell in shells)
                if (shell != null)
                {
                    shell.SetPropertyBlock(properties);
                    shell.enabled = revealed;
                }
        }

        private void LateUpdate()
        {
            foreach (var shell in shells)
                if (shell != null) shell.enabled = revealed && isActiveAndEnabled;
        }

        private void OnDisable()
        {
            foreach (var shell in shells)
                if (shell != null) shell.enabled = false;
        }
    }
}
