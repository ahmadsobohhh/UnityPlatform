using UnityEngine;
using System.Collections.Generic;

namespace ImagineQuest.Gameplay
{
    /// <summary>Keeps retargeted landing poses above the capsule's real support surface.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class PirateVisualGrounding : MonoBehaviour
    {
        private Transform supportRoot;
        private CharacterController controller;
        private SkinnedMeshRenderer[] visuals;
        private readonly List<Vector3> vertices = new List<Vector3>(256);
        private Mesh sample;
        private Vector3 restingLocalPosition;

        public void Configure(Transform root, CharacterController capsule)
        {
            supportRoot = root;
            controller = capsule;
            restingLocalPosition = transform.localPosition;
            visuals = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (sample == null) sample = new Mesh { name = "Navigator sole sample" };
        }

        private void LateUpdate()
        {
            CorrectGroundedPose(controller != null && controller.enabled && controller.isGrounded);
        }

        public void CorrectGroundedPose(bool grounded)
        {
            if (supportRoot == null || visuals == null || sample == null) return;
            // Reset the last correction before evaluating this frame's animated pose.
            transform.localPosition = restingLocalPosition;
            if (!grounded) return;
            var lowest = float.PositiveInfinity;
            foreach (var renderer in visuals)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                renderer.BakeMesh(sample, true);
                sample.GetVertices(vertices);
                foreach (var vertex in vertices)
                    lowest = Mathf.Min(lowest, renderer.transform.TransformPoint(vertex).y);
            }
            if (!float.IsInfinity(lowest))
                transform.position += Vector3.up * Mathf.Max(0f, supportRoot.position.y - lowest);
        }

        private void OnDestroy()
        {
            if (sample == null) return;
            if (Application.isPlaying) Destroy(sample); else DestroyImmediate(sample);
        }
    }
}
