using System.Collections.Generic;
using UnityEngine;

namespace ImagineQuest.Gameplay
{
    public static class ArenaDeckPlacement
    {
        public static bool TrySurface(IReadOnlyList<Collider> surfaces, float x, float z, out Vector3 position)
        {
            Physics.SyncTransforms();
            var ray = new Ray(new Vector3(x, 15f, z), Vector3.down);
            var highest = float.NegativeInfinity;
            foreach (var surface in surfaces)
                if (surface != null && surface.enabled && surface.Raycast(ray, out var hit, 25f) && hit.normal.y > 0.92f)
                    highest = Mathf.Max(highest, hit.point.y);
            position = new Vector3(x, highest + 0.015f, z);
            return !float.IsNegativeInfinity(highest);
        }

        public static bool TryClearArea(IReadOnlyList<Collider> surfaces, Vector2[] candidates,
            Vector2 footprint, out Vector3 position)
        {
            foreach (var candidate in candidates)
            {
                if (!TrySurface(surfaces, candidate.x, candidate.y, out var centre)) continue;
                var flat = true;
                for (var corner = 0; corner < 4; corner++)
                {
                    var x = candidate.x + (corner % 2 == 0 ? -1f : 1f) * footprint.x * 0.5f;
                    var z = candidate.y + (corner < 2 ? -1f : 1f) * footprint.y * 0.5f;
                    if (!TrySurface(surfaces, x, z, out var contact) || Mathf.Abs(contact.y - centre.y) > 0.035f)
                    { flat = false; break; }
                }
                if (!flat) continue;
                var obstacles = Physics.OverlapBox(centre + Vector3.up * 0.35f,
                    new Vector3(footprint.x * 0.5f, 0.32f, footprint.y * 0.5f), Quaternion.identity,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                if (obstacles.Length != 0) continue;
                position = centre;
                return true;
            }
            position = default;
            return false;
        }
    }
}
