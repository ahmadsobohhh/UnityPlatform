using System;
using System.Collections.Generic;
using UnityEngine;

namespace ImagineQuest.QuestFramework
{
    /// <summary>
    /// The versioned, authorable source of truth for a reusable learning quest.  Teacher
    /// assignments reference its quest ID and content version instead of duplicating it.
    /// </summary>
    [CreateAssetMenu(fileName = "QuestDefinition", menuName = "Imagine Quest/Quest Definition", order = 10)]
    public sealed class QuestDefinition : ScriptableObject
    {
        [SerializeField] private string questId = "new-quest";
        [SerializeField] private string displayName = "New Imagine Quest";
        [SerializeField] private string contentVersion = "1.0.0";
        [SerializeField, TextArea(2, 6)] private string summary;
        [SerializeField] private QuestRewardPolicy rewardPolicy = new QuestRewardPolicy();
        [SerializeField] private List<QuestNodeDefinition> nodes = new List<QuestNodeDefinition>();

        public string QuestId => questId;
        public string DisplayName => displayName;
        public string ContentVersion => contentVersion;
        public string Summary => summary;
        public QuestRewardPolicy RewardPolicy => rewardPolicy;
        public IReadOnlyList<QuestNodeDefinition> Nodes => nodes;

        public QuestNodeDefinition FindNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || nodes == null)
                return null;

            for (var index = 0; index < nodes.Count; index++)
            {
                var node = nodes[index];
                if (node != null && string.Equals(node.NodeId, nodeId, StringComparison.Ordinal))
                    return node;
            }

            return null;
        }

        public QuestChallengeDefinition FindChallenge(string challengeId, out QuestNodeDefinition containingNode)
        {
            containingNode = null;
            if (string.IsNullOrWhiteSpace(challengeId) || nodes == null)
                return null;

            for (var index = 0; index < nodes.Count; index++)
            {
                var node = nodes[index];
                var challenge = node?.FindChallenge(challengeId);
                if (challenge == null)
                    continue;

                containingNode = node;
                return challenge;
            }

            return null;
        }

        public int CountRequiredChallenges()
        {
            if (nodes == null)
                return 0;

            var count = 0;
            for (var nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
            {
                var node = nodes[nodeIndex];
                if (node == null || !node.RequiredForCompletion || node.Challenges == null)
                    continue;

                for (var challengeIndex = 0; challengeIndex < node.Challenges.Count; challengeIndex++)
                {
                    if (node.Challenges[challengeIndex] != null && node.Challenges[challengeIndex].RequiredForCompletion)
                        count++;
                }
            }

            return count;
        }

        private void OnValidate()
        {
            questId = QuestIdentifier.Normalize(questId);
            contentVersion = string.IsNullOrWhiteSpace(contentVersion) ? "1.0.0" : contentVersion.Trim();

            if (rewardPolicy == null)
                rewardPolicy = new QuestRewardPolicy();
            if (nodes == null)
                nodes = new List<QuestNodeDefinition>();

            for (var index = 0; index < nodes.Count; index++)
                nodes[index]?.Normalize();
        }
    }
}
