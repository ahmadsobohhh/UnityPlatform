using System.Collections.Generic;
using UnityEngine;

namespace ImagineQuest.Gameplay
{
    public sealed class AuroraCrewRoutine : MonoBehaviour
    {
        private static readonly Vector2[] Directions = {
            new Vector2(0, 1), new Vector2(0, -1), new Vector2(1, 0), new Vector2(-1, 0),
            new Vector2(.707f, .707f), new Vector2(-.707f, -.707f)
        };
        private readonly Collider[] blockers = new Collider[24];
        private Animator animator;
        private Transform head, forearm;
        private Quaternion restHead, restForearm, homeRotation;
        private Vector3 home, stop, visualOffset;
        private float pace, wait, activity, walkBlend;
        private bool hasRoute, goingOut;
        private int role;

        public void Configure(IReadOnlyList<Collider> surfaces, Vector3 support, int roleIndex)
        {
            role = roleIndex;
            animator = GetComponentInChildren<Animator>();
            home = stop = support;
            visualOffset = transform.position - support;
            homeRotation = transform.rotation;
            wait = 1.1f + role * .85f;
            goingOut = true;
            if (animator != null && animator.isHuman)
            {
                head = animator.GetBoneTransform(HumanBodyBones.Head);
                forearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
                if (head != null) restHead = head.localRotation;
                if (forearm != null) restForearm = forearm.localRotation;
            }
            if (role == 2) return; // The lookout stays on watch.
            Physics.SyncTransforms();
            foreach (float span in new[] { 1.65f, 1.15f, .82f })
            {
                for (int i = 0; i < Directions.Length; i++)
                {
                    Vector2 direction = Directions[(role + i) % Directions.Length];
                    Vector3 candidate = support + new Vector3(direction.x, 0, direction.y) * span;
                    bool clear = true;
                    for (int j = 1; j <= 7; j++)
                    {
                        var point = Vector3.Lerp(home, candidate, j / 7f);
                        if (!ArenaDeckPlacement.TrySurface(surfaces, point.x, point.z, out var ground) ||
                            Mathf.Abs(ground.y - home.y) > .035f || HasBlocker(ground, true))
                        { clear = false; break; }
                    }
                    if (!clear) continue;
                    stop = candidate;
                    hasRoute = true;
                    return;
                }
            }
        }

        private bool HasBlocker(Vector3 foot, bool planning)
        {
            int count = Physics.OverlapCapsuleNonAlloc(foot + Vector3.up * .23f, foot + Vector3.up * .77f,
                planning ? .24f : .2f, blockers, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == blockers.Length) return true;
            for (int i = 0; i < count; i++)
                if (blockers[i] != null && !blockers[i].transform.IsChildOf(transform)) return true;
            return false;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            activity += dt;
            float speed = 0f;
            if (wait > 0) wait -= dt;
            if (hasRoute && wait <= 0)
            {
                var foot = transform.position - visualOffset;
                var target = goingOut ? stop : home;
                var delta = target - foot;
                if (delta.sqrMagnitude <= .004f)
                {
                    goingOut = !goingOut;
                    wait = 1.8f + role * .6f;
                    pace = 0;
                }
                else
                {
                    Vector3 direction = delta.normalized;
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), dt * 190f);
                    pace = Mathf.MoveTowards(pace, .62f, dt * 1.8f);
                    var next = Vector3.MoveTowards(foot, target, pace * dt);
                    bool blocked = HasBlocker(next, false);
                    if (Vector3.Dot(transform.forward, direction) > .8f && !blocked)
                    {
                        transform.position = next + visualOffset;
                        speed = pace;
                    }
                    else if (blocked) pace = 0;
                }
            }
            else if (!hasRoute)
            {
                float turn = Mathf.Sin(activity * .52f + role * 1.7f) * (role == 2 ? 36f : 19f);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, homeRotation * Quaternion.Euler(0, turn, 0), dt * 39);
            }
            walkBlend = Mathf.MoveTowards(walkBlend, speed / .62f, dt * 5);
            if (animator != null && animator.runtimeAnimatorController != null)
                animator.SetFloat("Speed", walkBlend, .11f, dt);
        }

        private void LateUpdate()
        {
            float task = (1 - walkBlend) * Mathf.SmoothStep(0, 1, Mathf.Sin(activity * 1.1f + role * 2.1f) * .8f + .28f);
            if (head != null && walkBlend < .97f)
                head.localRotation = Quaternion.Slerp(head.localRotation,
                    restHead * Quaternion.Euler(role == 2 ? -8 : 15 * task, Mathf.Sin(activity * .85f + role) * 22, 0), 1 - walkBlend);
            if (forearm != null && role != 2 && walkBlend < .97f)
                forearm.localRotation = Quaternion.Slerp(forearm.localRotation,
                    restForearm * Quaternion.Euler(0, 20 * task, 31 * task), task * .65f);
        }
    }
}
