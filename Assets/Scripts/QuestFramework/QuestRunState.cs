using System;
using System.Collections.Generic;
using Firebase.Firestore;
using UnityEngine;

namespace ImagineQuest.QuestFramework
{
    /// <summary>
    /// Mutable, learner-owned state for one attempt at a class assignment.  Its
    /// provisional experience is intentionally display-only; a trusted service must
    /// validate event history before it grants any permanent reward.
    /// </summary>
    [Serializable]
    public sealed class QuestRunState
    {
        [SerializeField] private string runId;
        [SerializeField] private string assignmentId;
        [SerializeField] private string classId;
        [SerializeField] private string questId;
        [SerializeField] private string questContentVersion;
        [SerializeField] private QuestRunStatus status = QuestRunStatus.NotStarted;
        [SerializeField] private string currentNodeId;
        [SerializeField] private long startedAtUnixSeconds;
        [SerializeField] private long updatedAtUnixSeconds;
        [SerializeField] private long completedAtUnixSeconds;
        [SerializeField] private int provisionalExperience;
        [SerializeField] private List<string> completedChallengeIds = new List<string>();
        [SerializeField] private List<QuestChallengeAttemptRecord> challengeAttempts = new List<QuestChallengeAttemptRecord>();

        public string RunId => runId;
        public string AssignmentId => assignmentId;
        public string ClassId => classId;
        public string QuestId => questId;
        public string QuestContentVersion => questContentVersion;
        public QuestRunStatus Status => status;
        public string CurrentNodeId => currentNodeId;
        public long StartedAtUnixSeconds => startedAtUnixSeconds;
        public long UpdatedAtUnixSeconds => updatedAtUnixSeconds;
        public long CompletedAtUnixSeconds => completedAtUnixSeconds;
        public int ProvisionalExperience => provisionalExperience;
        public IReadOnlyList<string> CompletedChallengeIds => completedChallengeIds;
        public IReadOnlyList<QuestChallengeAttemptRecord> ChallengeAttempts => challengeAttempts;

        public static QuestRunState StartNew(QuestDefinition definition, QuestAssignmentRecord assignment)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (assignment == null)
                throw new ArgumentNullException(nameof(assignment));
            if (string.IsNullOrWhiteSpace(assignment.AssignmentId))
                throw new ArgumentException("An assignment ID is required to start a quest run.", nameof(assignment));
            if (string.IsNullOrWhiteSpace(assignment.ClassId))
                throw new ArgumentException("A class ID is required to start a quest run.", nameof(assignment));

            var now = QuestFirestoreValue.ToUnixSeconds(DateTime.UtcNow);
            return new QuestRunState
            {
                runId = Guid.NewGuid().ToString("N"),
                assignmentId = assignment.AssignmentId,
                classId = assignment.ClassId,
                questId = string.IsNullOrWhiteSpace(assignment.QuestId) ? definition.QuestId : assignment.QuestId,
                questContentVersion = string.IsNullOrWhiteSpace(assignment.QuestContentVersion)
                    ? definition.ContentVersion
                    : assignment.QuestContentVersion,
                status = QuestRunStatus.InProgress,
                startedAtUnixSeconds = now,
                updatedAtUnixSeconds = now
            };
        }

        public void SetCurrentNode(string nodeId)
        {
            currentNodeId = QuestIdentifier.Normalize(nodeId);
            Touch();
        }

        public QuestChallengeAttemptRecord RecordAttempt(string nodeId, QuestChallengeDefinition challenge,
            string selectedAnswerId, QuestAttemptStatus attemptStatus, int provisionalExperienceDelta = 0)
        {
            if (challenge == null)
                throw new ArgumentNullException(nameof(challenge));
            if (string.IsNullOrWhiteSpace(challenge.ChallengeId))
                throw new ArgumentException("Challenge definitions need stable challenge IDs.", nameof(challenge));
            if (status != QuestRunStatus.InProgress)
                throw new InvalidOperationException("Only an in-progress quest run can record an attempt.");

            var normalizedNodeId = QuestIdentifier.Normalize(nodeId);
            var normalizedAnswerId = QuestIdentifier.Normalize(selectedAnswerId);
            var attempt = new QuestChallengeAttemptRecord
            {
                eventId = Guid.NewGuid().ToString("N"),
                nodeId = normalizedNodeId,
                challengeId = challenge.ChallengeId,
                selectedAnswerId = normalizedAnswerId,
                status = attemptStatus,
                attemptNumber = CountAttemptsFor(challenge.ChallengeId) + 1,
                occurredAtUnixSeconds = QuestFirestoreValue.ToUnixSeconds(DateTime.UtcNow)
            };

            challengeAttempts.Add(attempt);
            if (attemptStatus == QuestAttemptStatus.Correct && !completedChallengeIds.Contains(challenge.ChallengeId))
                completedChallengeIds.Add(challenge.ChallengeId);

            provisionalExperience = Mathf.Max(0, provisionalExperience + Mathf.Max(0, provisionalExperienceDelta));
            currentNodeId = normalizedNodeId;
            updatedAtUnixSeconds = attempt.OccurredAtUnixSeconds;
            return attempt;
        }

        public bool HasCompletedChallenge(string challengeId)
        {
            return !string.IsNullOrWhiteSpace(challengeId) && completedChallengeIds.Contains(challengeId);
        }

        public bool HasCompletedAllRequiredChallenges(QuestDefinition definition)
        {
            if (definition == null)
                return false;

            for (var nodeIndex = 0; nodeIndex < definition.Nodes.Count; nodeIndex++)
            {
                var node = definition.Nodes[nodeIndex];
                if (node == null || !node.RequiredForCompletion)
                    continue;

                for (var challengeIndex = 0; challengeIndex < node.Challenges.Count; challengeIndex++)
                {
                    var challenge = node.Challenges[challengeIndex];
                    if (challenge != null && challenge.RequiredForCompletion && !HasCompletedChallenge(challenge.ChallengeId))
                        return false;
                }
            }

            return true;
        }

        public void Complete(int completionExperienceDelta = 0)
        {
            if (status != QuestRunStatus.InProgress)
                throw new InvalidOperationException("Only an in-progress quest run can be completed.");

            status = QuestRunStatus.Completed;
            provisionalExperience = Mathf.Max(0, provisionalExperience + Mathf.Max(0, completionExperienceDelta));
            completedAtUnixSeconds = QuestFirestoreValue.ToUnixSeconds(DateTime.UtcNow);
            updatedAtUnixSeconds = completedAtUnixSeconds;
        }

        public void Abandon()
        {
            if (status == QuestRunStatus.Completed)
                return;

            status = QuestRunStatus.Abandoned;
            Touch();
        }

        public Dictionary<string, object> ToFirestoreDocument()
        {
            var attempts = new List<object>();
            if (challengeAttempts != null)
            {
                for (var index = 0; index < challengeAttempts.Count; index++)
                {
                    var attempt = challengeAttempts[index];
                    if (attempt != null)
                        attempts.Add(attempt.ToFirestoreDocument());
                }
            }

            var document = new Dictionary<string, object>
            {
                { "runId", runId ?? string.Empty },
                { "assignmentId", assignmentId ?? string.Empty },
                { "classId", classId ?? string.Empty },
                { "questId", questId ?? string.Empty },
                { "questContentVersion", questContentVersion ?? string.Empty },
                { "status", status.ToString() },
                { "currentNodeId", currentNodeId ?? string.Empty },
                { "provisionalExperience", provisionalExperience },
                { "completedChallengeIds", completedChallengeIds ?? new List<string>() },
                { "attempts", attempts }
            };

            AddTimestampIfPresent(document, "startedAt", startedAtUnixSeconds);
            AddTimestampIfPresent(document, "updatedAt", updatedAtUnixSeconds);
            AddTimestampIfPresent(document, "completedAt", completedAtUnixSeconds);
            return document;
        }

        public static QuestRunState FromFirestoreDocument(IDictionary<string, object> document)
        {
            var state = new QuestRunState
            {
                runId = QuestFirestoreValue.String(document, "runId"),
                assignmentId = QuestFirestoreValue.String(document, "assignmentId"),
                classId = QuestFirestoreValue.String(document, "classId"),
                questId = QuestFirestoreValue.String(document, "questId"),
                questContentVersion = QuestFirestoreValue.String(document, "questContentVersion"),
                status = QuestFirestoreValue.Enum(document, "status", QuestRunStatus.NotStarted),
                currentNodeId = QuestFirestoreValue.String(document, "currentNodeId"),
                provisionalExperience = QuestFirestoreValue.Int(document, "provisionalExperience"),
                completedChallengeIds = QuestFirestoreValue.StringList(document, "completedChallengeIds"),
                startedAtUnixSeconds = QuestFirestoreValue.UnixSeconds(document, "startedAt"),
                updatedAtUnixSeconds = QuestFirestoreValue.UnixSeconds(document, "updatedAt"),
                completedAtUnixSeconds = QuestFirestoreValue.UnixSeconds(document, "completedAt"),
                challengeAttempts = new List<QuestChallengeAttemptRecord>()
            };

            var attempts = QuestFirestoreValue.ObjectList(document, "attempts");
            for (var index = 0; index < attempts.Count; index++)
            {
                var attemptDocument = attempts[index] as IDictionary<string, object>;
                if (attemptDocument != null)
                    state.challengeAttempts.Add(QuestChallengeAttemptRecord.FromFirestoreDocument(attemptDocument));
            }

            return state;
        }

        private int CountAttemptsFor(string challengeId)
        {
            if (challengeAttempts == null)
                return 0;

            var count = 0;
            for (var index = 0; index < challengeAttempts.Count; index++)
            {
                var attempt = challengeAttempts[index];
                if (attempt != null && string.Equals(attempt.ChallengeId, challengeId, StringComparison.Ordinal))
                    count++;
            }

            return count;
        }

        private void Touch()
        {
            updatedAtUnixSeconds = QuestFirestoreValue.ToUnixSeconds(DateTime.UtcNow);
        }

        private static void AddTimestampIfPresent(Dictionary<string, object> document, string fieldName, long unixSeconds)
        {
            if (unixSeconds <= 0)
                return;

            document[fieldName] = Timestamp.FromDateTime(DateTime.UnixEpoch.AddSeconds(unixSeconds).ToUniversalTime());
        }
    }

    /// <summary>
    /// One immutable interaction event.  These events give trusted backend code enough
    /// context to validate a run without trusting a client-supplied reward total.
    /// </summary>
    [Serializable]
    public sealed class QuestChallengeAttemptRecord
    {
        [SerializeField] internal string eventId;
        [SerializeField] internal string nodeId;
        [SerializeField] internal string challengeId;
        [SerializeField] internal string selectedAnswerId;
        [SerializeField] internal QuestAttemptStatus status;
        [SerializeField] internal int attemptNumber;
        [SerializeField] internal long occurredAtUnixSeconds;

        public string EventId => eventId;
        public string NodeId => nodeId;
        public string ChallengeId => challengeId;
        public string SelectedAnswerId => selectedAnswerId;
        public QuestAttemptStatus Status => status;
        public int AttemptNumber => attemptNumber;
        public long OccurredAtUnixSeconds => occurredAtUnixSeconds;

        public Dictionary<string, object> ToFirestoreDocument()
        {
            var document = new Dictionary<string, object>
            {
                { "eventId", eventId ?? string.Empty },
                { "nodeId", nodeId ?? string.Empty },
                { "challengeId", challengeId ?? string.Empty },
                { "selectedAnswerId", selectedAnswerId ?? string.Empty },
                { "status", status.ToString() },
                { "attemptNumber", attemptNumber }
            };

            if (occurredAtUnixSeconds > 0)
                document["occurredAt"] = Timestamp.FromDateTime(DateTime.UnixEpoch.AddSeconds(occurredAtUnixSeconds).ToUniversalTime());
            return document;
        }

        public static QuestChallengeAttemptRecord FromFirestoreDocument(IDictionary<string, object> document)
        {
            return new QuestChallengeAttemptRecord
            {
                eventId = QuestFirestoreValue.String(document, "eventId"),
                nodeId = QuestFirestoreValue.String(document, "nodeId"),
                challengeId = QuestFirestoreValue.String(document, "challengeId"),
                selectedAnswerId = QuestFirestoreValue.String(document, "selectedAnswerId"),
                status = QuestFirestoreValue.Enum(document, "status", QuestAttemptStatus.Started),
                attemptNumber = QuestFirestoreValue.Int(document, "attemptNumber"),
                occurredAtUnixSeconds = QuestFirestoreValue.UnixSeconds(document, "occurredAt")
            };
        }
    }
}
