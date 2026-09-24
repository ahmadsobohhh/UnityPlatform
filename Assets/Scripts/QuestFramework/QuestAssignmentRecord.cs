using System;
using System.Collections.Generic;
using Firebase.Firestore;
using UnityEngine;

namespace ImagineQuest.QuestFramework
{
    /// <summary>
    /// A class-scoped reference to a quest definition.  It contains release controls and
    /// the version learners should play, but never learner-owned progress or wallet data.
    /// </summary>
    [Serializable]
    public sealed class QuestAssignmentRecord
    {
        [SerializeField] private string assignmentId;
        [SerializeField] private string classId;
        [SerializeField] private string questId;
        [SerializeField] private string questContentVersion;
        [SerializeField] private string title;
        [SerializeField] private QuestAssignmentState state = QuestAssignmentState.Draft;
        [SerializeField] private long releaseAtUnixSeconds;
        [SerializeField] private long dueAtUnixSeconds;
        [SerializeField] private List<string> targetConceptIds = new List<string>();
        [SerializeField] private string assignedByUid;
        [SerializeField] private long createdAtUnixSeconds;
        [SerializeField] private long updatedAtUnixSeconds;

        public string AssignmentId => assignmentId;
        public string ClassId => classId;
        public string QuestId => questId;
        public string QuestContentVersion => questContentVersion;
        public string Title => title;
        public QuestAssignmentState State => state;
        public long ReleaseAtUnixSeconds => releaseAtUnixSeconds;
        public long DueAtUnixSeconds => dueAtUnixSeconds;
        public IReadOnlyList<string> TargetConceptIds => targetConceptIds;
        public string AssignedByUid => assignedByUid;
        public long CreatedAtUnixSeconds => createdAtUnixSeconds;
        public long UpdatedAtUnixSeconds => updatedAtUnixSeconds;

        public static QuestAssignmentRecord Create(string newAssignmentId, string newClassId, string newQuestId,
            string newQuestContentVersion, string newTitle, string teacherUid,
            QuestAssignmentState newState = QuestAssignmentState.Draft)
        {
            var now = QuestFirestoreValue.ToUnixSeconds(DateTime.UtcNow);
            return new QuestAssignmentRecord
            {
                assignmentId = QuestIdentifier.Normalize(newAssignmentId),
                classId = newClassId ?? string.Empty,
                questId = QuestIdentifier.Normalize(newQuestId),
                questContentVersion = string.IsNullOrWhiteSpace(newQuestContentVersion) ? "1.0.0" : newQuestContentVersion.Trim(),
                title = newTitle ?? string.Empty,
                state = newState,
                assignedByUid = teacherUid ?? string.Empty,
                createdAtUnixSeconds = now,
                updatedAtUnixSeconds = now
            };
        }

        public bool IsAvailableAt(DateTime utcNow)
        {
            if (state != QuestAssignmentState.Open)
                return false;

            var nowUnixSeconds = QuestFirestoreValue.ToUnixSeconds(utcNow);
            if (releaseAtUnixSeconds > 0 && nowUnixSeconds < releaseAtUnixSeconds)
                return false;
            if (dueAtUnixSeconds > 0 && nowUnixSeconds > dueAtUnixSeconds)
                return false;
            return true;
        }

        public Dictionary<string, object> ToFirestoreDocument()
        {
            var document = new Dictionary<string, object>
            {
                { "assignmentId", assignmentId ?? string.Empty },
                { "classId", classId ?? string.Empty },
                { "questId", questId ?? string.Empty },
                { "questDefinitionId", questId ?? string.Empty },
                { "questContentVersion", questContentVersion ?? string.Empty },
                { "contentVersion", questContentVersion ?? string.Empty },
                { "title", title ?? string.Empty },
                { "state", state.ToString() },
                { "targetConceptIds", targetConceptIds ?? new List<string>() },
                { "assignedByUid", assignedByUid ?? string.Empty }
            };

            AddTimestampIfPresent(document, "releaseAt", releaseAtUnixSeconds);
            AddTimestampIfPresent(document, "dueAt", dueAtUnixSeconds);
            AddTimestampIfPresent(document, "createdAt", createdAtUnixSeconds);
            AddTimestampIfPresent(document, "updatedAt", updatedAtUnixSeconds);
            return document;
        }

        public static QuestAssignmentRecord FromFirestoreDocument(string documentId, IDictionary<string, object> document)
        {
            var legacyState = QuestFirestoreValue.Bool(document, "isUnlocked")
                ? QuestAssignmentState.Open
                : QuestAssignmentState.Draft;
            var record = new QuestAssignmentRecord
            {
                assignmentId = QuestFirestoreValue.String(document, "assignmentId", documentId),
                classId = QuestFirestoreValue.String(document, "classId"),
                questId = QuestFirestoreValue.String(document, "questDefinitionId",
                    QuestFirestoreValue.String(document, "questId")),
                questContentVersion = QuestFirestoreValue.String(document, "contentVersion",
                    QuestFirestoreValue.String(document, "questContentVersion")),
                title = QuestFirestoreValue.String(document, "title"),
                state = QuestFirestoreValue.Enum(document, "state", legacyState),
                releaseAtUnixSeconds = QuestFirestoreValue.UnixSeconds(document, "releaseAt"),
                dueAtUnixSeconds = QuestFirestoreValue.UnixSeconds(document, "dueAt"),
                targetConceptIds = QuestFirestoreValue.StringList(document, "targetConceptIds"),
                assignedByUid = QuestFirestoreValue.String(document, "assignedByUid"),
                createdAtUnixSeconds = QuestFirestoreValue.UnixSeconds(document, "createdAt"),
                updatedAtUnixSeconds = QuestFirestoreValue.UnixSeconds(document, "updatedAt")
            };
            return record;
        }

        public static QuestAssignmentRecord FromFirestoreSnapshot(DocumentSnapshot snapshot)
        {
            if (snapshot == null || !snapshot.Exists)
                return null;

            return FromFirestoreDocument(snapshot.Id, snapshot.ToDictionary());
        }

        private static void AddTimestampIfPresent(Dictionary<string, object> document, string fieldName, long unixSeconds)
        {
            if (unixSeconds <= 0)
                return;

            document[fieldName] = Timestamp.FromDateTime(DateTime.UnixEpoch.AddSeconds(unixSeconds).ToUniversalTime());
        }
    }
}
