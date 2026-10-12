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

        public static void Register(IQuestArenaInteractable interactable)
        {
            if (interactable != null && !Items.Contains(interactable))
                Items.Add(interactable);
        }

        public static void Unregister(IQuestArenaInteractable interactable) => Items.Remove(interactable);

        public static IQuestArenaInteractable FindNearest(Vector3 position, float radius)
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

                nearestSquaredDistance = squaredDistance;
                nearest = candidate;
            }

            return nearest;
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
