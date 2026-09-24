using System;
using System.Collections.Generic;
using UnityEngine;

namespace ImagineQuest.QuestFramework
{
    /// <summary>
    /// The presentation role a node plays in a quest.  A node can still contain one or
    /// more challenges regardless of its role.
    /// </summary>
    public enum QuestNodeType
    {
        Briefing,
        Explore,
        Teaching,
        Practice,
        BossChallenge,
        Reflection,
        Reward
    }

    /// <summary>
    /// The answer interaction expected by a challenge.  Gameplay views decide how to
    /// render each type; the definition itself stays independent of a particular scene.
    /// </summary>
    public enum QuestChallengeType
    {
        MultipleChoice,
        NumericInput,
        TextInput,
        MatchPairs,
        Ordering,
        InteractiveObject
    }

    public enum QuestAttemptStatus
    {
        Started,
        Incorrect,
        Correct,
        Skipped
    }

    public enum QuestAssignmentState
    {
        Draft,
        Scheduled,
        Open,
        Closed,
        Archived
    }

    public enum QuestRunStatus
    {
        NotStarted,
        InProgress,
        Completed,
        Abandoned
    }

    /// <summary>
    /// A small, data-only description of one destination, portal, or encounter in a
    /// quest.  It is serializable so it can live directly inside a QuestDefinition asset.
    /// </summary>
    [Serializable]
    public sealed class QuestNodeDefinition
    {
        [SerializeField] private string nodeId = "arrival";
        [SerializeField] private string title = "New Quest Node";
        [SerializeField] private QuestNodeType nodeType = QuestNodeType.Explore;
        [SerializeField, TextArea(2, 5)] private string objectiveText;
        [SerializeField, TextArea(2, 6)] private string narrativeText;
        [SerializeField] private bool requiredForCompletion = true;
        [SerializeField] private List<QuestChallengeDefinition> challenges = new List<QuestChallengeDefinition>();

        public string NodeId => nodeId;
        public string Title => title;
        public QuestNodeType NodeType => nodeType;
        public string ObjectiveText => objectiveText;
        public string NarrativeText => narrativeText;
        public bool RequiredForCompletion => requiredForCompletion;
        public IReadOnlyList<QuestChallengeDefinition> Challenges => challenges;

        public QuestChallengeDefinition FindChallenge(string challengeId)
        {
            if (string.IsNullOrWhiteSpace(challengeId) || challenges == null)
                return null;

            for (var index = 0; index < challenges.Count; index++)
            {
                var challenge = challenges[index];
                if (challenge != null && string.Equals(challenge.ChallengeId, challengeId, StringComparison.Ordinal))
                    return challenge;
            }

            return null;
        }

        internal void Normalize()
        {
            nodeId = QuestIdentifier.Normalize(nodeId);
            if (challenges == null)
                challenges = new List<QuestChallengeDefinition>();

            for (var index = 0; index < challenges.Count; index++)
                challenges[index]?.Normalize();
        }
    }

    /// <summary>
    /// A challenge is deliberately content-first: UI, 3D interactables, and teacher
    /// assignments can all reuse the same math prompt and answer data.
    /// </summary>
    [Serializable]
    public sealed class QuestChallengeDefinition
    {
        [SerializeField] private string challengeId = "challenge";
        [SerializeField] private string conceptId;
        [SerializeField] private QuestChallengeType challengeType = QuestChallengeType.MultipleChoice;
        [SerializeField, TextArea(2, 6)] private string prompt;
        [SerializeField, TextArea(1, 4)] private string supportingText;
        [SerializeField] private List<QuestAnswerDefinition> answers = new List<QuestAnswerDefinition>();
        [SerializeField] private string correctAnswerId;
        [SerializeField, Min(1)] private int maximumAttempts = 3;
        [SerializeField] private bool requiredForCompletion = true;
        [SerializeField, TextArea(1, 4)] private string hintText;
        [SerializeField, TextArea(1, 4)] private string successFeedback;
        [SerializeField, TextArea(1, 4)] private string retryFeedback;

        public string ChallengeId => challengeId;
        public string ConceptId => conceptId;
        public QuestChallengeType ChallengeType => challengeType;
        public string Prompt => prompt;
        public string SupportingText => supportingText;
        public IReadOnlyList<QuestAnswerDefinition> Answers => answers;
        public string CorrectAnswerId => correctAnswerId;
        public int MaximumAttempts => Mathf.Max(1, maximumAttempts);
        public bool RequiredForCompletion => requiredForCompletion;
        public string HintText => hintText;
        public string SuccessFeedback => successFeedback;
        public string RetryFeedback => retryFeedback;

        public bool IsCorrectAnswer(string answerId)
        {
            return !string.IsNullOrWhiteSpace(correctAnswerId) &&
                   string.Equals(correctAnswerId, answerId, StringComparison.Ordinal);
        }

        public QuestAnswerDefinition FindAnswer(string answerId)
        {
            if (string.IsNullOrWhiteSpace(answerId) || answers == null)
                return null;

            for (var index = 0; index < answers.Count; index++)
            {
                var answer = answers[index];
                if (answer != null && string.Equals(answer.AnswerId, answerId, StringComparison.Ordinal))
                    return answer;
            }

            return null;
        }

        internal void Normalize()
        {
            challengeId = QuestIdentifier.Normalize(challengeId);
            correctAnswerId = QuestIdentifier.Normalize(correctAnswerId);
            maximumAttempts = Mathf.Max(1, maximumAttempts);

            if (answers == null)
                answers = new List<QuestAnswerDefinition>();

            for (var index = 0; index < answers.Count; index++)
                answers[index]?.Normalize();
        }
    }

    /// <summary>
    /// An answer option or selectable interaction result.  Correctness is intentionally
    /// stored on QuestChallengeDefinition so answer content remains reusable.
    /// </summary>
    [Serializable]
    public sealed class QuestAnswerDefinition
    {
        [SerializeField] private string answerId = "answer";
        [SerializeField] private string label = "Answer";
        [SerializeField, TextArea(1, 4)] private string detailText;

        public string AnswerId => answerId;
        public string Label => label;
        public string DetailText => detailText;

        internal void Normalize()
        {
            answerId = QuestIdentifier.Normalize(answerId);
        }
    }

    /// <summary>
    /// Reward values are a display policy and a server-validation contract, not a client
    /// wallet.  The client may show provisional experience; Cloud Functions should grant
    /// canonical XP, currency, and unlocks after validating the submitted run events.
    /// </summary>
    [Serializable]
    public sealed class QuestRewardPolicy
    {
        [SerializeField] private bool enabled = true;
        [SerializeField, Min(0)] private int experiencePerCorrectChallenge = 25;
        [SerializeField, Min(0)] private int completionExperience = 100;
        [SerializeField, Min(0)] private int perfectRunBonusExperience = 25;
        [SerializeField] private bool showProvisionalExperience = true;
        [SerializeField] private bool serverCalculatesCanonicalRewards = true;
        [SerializeField] private string rewardDescriptor = "Captain's commendation";

        public bool Enabled => enabled;
        public int ExperiencePerCorrectChallenge => Mathf.Max(0, experiencePerCorrectChallenge);
        public int CompletionExperience => Mathf.Max(0, completionExperience);
        public int PerfectRunBonusExperience => Mathf.Max(0, perfectRunBonusExperience);
        public bool ShowProvisionalExperience => showProvisionalExperience;
        public bool ServerCalculatesCanonicalRewards => serverCalculatesCanonicalRewards;
        public string RewardDescriptor => rewardDescriptor;

        public int CalculateDisplayExperience(int correctChallenges, bool completed, bool perfectRun)
        {
            if (!enabled || !showProvisionalExperience)
                return 0;

            var total = Mathf.Max(0, correctChallenges) * ExperiencePerCorrectChallenge;
            if (completed)
                total += CompletionExperience;
            if (perfectRun)
                total += PerfectRunBonusExperience;
            return total;
        }
    }

    internal static class QuestIdentifier
    {
        internal static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }
    }
}
