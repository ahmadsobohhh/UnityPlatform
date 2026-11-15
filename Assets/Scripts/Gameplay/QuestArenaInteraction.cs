using System.Collections.Generic;
using UnityEngine;

namespace ImagineQuest.Gameplay
{
    /// <summary>
    /// Common contract for every object the third-person player can use aboard the ship.
    /// Keeping this independent from a particular quest lets future lessons reuse the arena.
    /// </summary>
    public interface IQuestArenaInteractable
    {
        Transform InteractionTransform { get; }
        string InteractionPrompt { get; }
        bool CanInteract { get; }
        void Interact();
    }

    public static class QuestArenaInteractionRegistry
    {
        private static readonly List<IQuestArenaInteractable> Items = new List<IQuestArenaInteractable>();
        private static readonly RaycastHit[] SightHits = new RaycastHit[24];

        public static void Register(IQuestArenaInteractable interactable)
        {
            if (interactable != null && !Items.Contains(interactable))
                Items.Add(interactable);
        }

        public static void Unregister(IQuestArenaInteractable interactable) => Items.Remove(interactable);

        public static IQuestArenaInteractable FindNearest(Vector3 position, float radius, Transform observer = null)
        {
            IQuestArenaInteractable nearest = null;
            var nearestSquaredDistance = radius * radius;

            for (var index = Items.Count - 1; index >= 0; index--)
            {
                var candidate = Items[index];
                if (candidate == null || candidate.InteractionTransform == null)
                {
                    Items.RemoveAt(index);
                    continue;
                }

                if (!candidate.CanInteract)
                    continue;

                var squaredDistance = (candidate.InteractionTransform.position - position).sqrMagnitude;
                if (squaredDistance >= nearestSquaredDistance)
                    continue;
                if (candidate is QuestArenaShipInteractable fixture && squaredDistance >= fixture.Reach * fixture.Reach)
                    continue;
                if (observer != null && !HasClearReach(position, candidate.InteractionTransform, observer))
                    continue;

                nearestSquaredDistance = squaredDistance;
                nearest = candidate;
            }

            return nearest;
        }

        private static bool HasClearReach(Vector3 origin, Transform target, Transform observer)
        {
            var delta = target.position - origin;
            float length = delta.magnitude;
            if (length < .01f) return true;
            int count = Physics.RaycastNonAlloc(origin, delta / length, SightHits, length - .01f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var obstacle = SightHits[i].transform;
                if (obstacle != null && !obstacle.IsChildOf(observer) && !obstacle.IsChildOf(target))
                    return false;
            }
            return true;
        }
    }

    public enum QuestArenaShipAction
    {
        Captain,
        ChartTable,
        TreasureChest,
        Helm,
        Telescope,
        ShipBell,
        Crew
    }

    /// <summary>Reusable interaction endpoint for the non-quest fixtures aboard the Aurora.</summary>
    public sealed class QuestArenaShipInteractable : MonoBehaviour, IQuestArenaInteractable
    {
        private CelestialClockQuestBootstrap owner;
        private QuestArenaShipAction action;
        private string prompt;

        public Transform InteractionTransform => transform;
        public string InteractionPrompt => prompt;
        public bool CanInteract => enabled && gameObject.activeInHierarchy;
        internal float Reach => action == QuestArenaShipAction.Helm ? 1.08f : 1.65f;

        public void Configure(CelestialClockQuestBootstrap arenaOwner, QuestArenaShipAction shipAction, string interactionPrompt)
        {
            owner = arenaOwner;
            action = shipAction;
            prompt = interactionPrompt;
        }

        private void OnEnable() => QuestArenaInteractionRegistry.Register(this);
        private void OnDisable() => QuestArenaInteractionRegistry.Unregister(this);
        private void OnDestroy() => QuestArenaInteractionRegistry.Unregister(this);

        public void Interact()
        {
            if (CanInteract)
                owner?.HandleShipInteraction(action);
        }
    }
}
