using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

namespace ImagineQuest.Gameplay
{
    /// <summary>
    /// Persists the minimum viable quest state. Firestore rules must ensure learners can
    /// only write their own progress records; teacher-controlled rewards remain server-side.
    /// </summary>
    public sealed class QuestProgressRepository : MonoBehaviour
    {
        public static QuestProgressRepository Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SaveProgress(string questId, string classId, int completedChallenges, int earnedXp, bool completed)
        {
            if (string.IsNullOrWhiteSpace(questId))
                return;

            StartCoroutine(SaveProgressRoutine(questId, classId, completedChallenges, earnedXp, completed));
        }

        /// <summary>
        /// Writes a class-and-assignment scoped learner snapshot plus an immutable client
        /// event.  The displayed XP is deliberately called <c>provisionalXp</c>: a Cloud
        /// Function must validate and award real currency/rewards.  This avoids mixing a
        /// student-visible progress checkpoint with the authoritative reward ledger.
        /// </summary>
        public void SaveQuestRunSnapshot(string questId, string classId, string assignmentId,
            IReadOnlyList<string> completedNodeIds, int provisionalXp, bool completed)
        {
            if (string.IsNullOrWhiteSpace(questId) || string.IsNullOrWhiteSpace(classId))
                return;

            StartCoroutine(SaveQuestRunSnapshotRoutine(questId, classId, assignmentId,
                completedNodeIds, provisionalXp, completed));
        }

        private IEnumerator SaveProgressRoutine(string questId, string classId, int completedChallenges, int earnedXp, bool completed)
        {
            var user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user == null)
            {
                Debug.LogWarning("[QuestProgress] No signed-in learner; progress was kept only for this session.");
                yield break;
            }

            var payload = new Dictionary<string, object>
            {
                { "questId", questId },
                { "classId", classId ?? string.Empty },
                { "completedChallenges", completedChallenges },
                { "earnedXp", earnedXp },
                { "completed", completed },
                { "updatedAt", Timestamp.GetCurrentTimestamp() }
            };

            var write = FirebaseFirestore.DefaultInstance
                .Collection("users").Document(user.UserId)
                .Collection("questProgress").Document(questId)
                .SetAsync(payload, SetOptions.MergeAll);

            yield return new WaitUntil(() => write.IsCompleted);
            if (write.IsFaulted || write.IsCanceled)
                Debug.LogError("[QuestProgress] Firestore save failed: " + write.Exception);
        }

        private IEnumerator SaveQuestRunSnapshotRoutine(string questId, string classId, string assignmentId,
            IReadOnlyList<string> completedNodeIds, int provisionalXp, bool completed)
        {
            var user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user == null)
            {
                Debug.LogWarning("[QuestProgress] No signed-in learner; run snapshot was kept only for this session.");
                yield break;
            }

            var safeAssignmentId = string.IsNullOrWhiteSpace(assignmentId) ? questId : assignmentId;
            var nodes = new List<string>();
            if (completedNodeIds != null)
            {
                for (var i = 0; i < completedNodeIds.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(completedNodeIds[i]))
                        nodes.Add(completedNodeIds[i]);
                }
            }

            var firestore = FirebaseFirestore.DefaultInstance;
            var runRef = firestore.Collection("users").Document(user.UserId)
                .Collection("classProgress").Document(classId)
                .Collection("assignments").Document(safeAssignmentId);
            var eventRef = runRef.Collection("events").Document(System.Guid.NewGuid().ToString("N"));
            var now = Timestamp.GetCurrentTimestamp();
            var snapshot = new Dictionary<string, object>
            {
                { "questId", questId },
                { "classId", classId },
                { "assignmentId", safeAssignmentId },
                { "completedNodeIds", nodes },
                { "completedNodeCount", nodes.Count },
                { "provisionalXp", Mathf.Max(0, provisionalXp) },
                { "status", completed ? "completed" : "in_progress" },
                { "completed", completed },
                { "updatedAt", now }
            };
            var clientEvent = new Dictionary<string, object>
            {
                { "type", completed ? "quest_completed" : "checkpoint" },
                { "questId", questId },
                { "assignmentId", safeAssignmentId },
                { "completedNodeIds", nodes },
                { "provisionalXp", Mathf.Max(0, provisionalXp) },
                { "capturedAt", now },
                { "source", "unity-client" }
            };

            var batch = firestore.StartBatch();
            batch.Set(runRef, snapshot, SetOptions.MergeAll);
            batch.Set(eventRef, clientEvent);
            var write = batch.CommitAsync();

            yield return new WaitUntil(() => write.IsCompleted);
            if (write.IsFaulted || write.IsCanceled)
                Debug.LogError("[QuestProgress] Class-scoped run snapshot failed: " + write.Exception);
        }
    }
}
