using System;
using UnityEngine;

namespace ImagineQuest.Gameplay
{
    /// <summary>
    /// Carries the selected quest context through scene transitions. This is deliberately
    /// small: persistent learner data belongs in Firestore, not in scene objects.
    /// </summary>
    public sealed class QuestSession : MonoBehaviour
    {
        public static QuestSession Instance { get; private set; }

        public string QuestId { get; private set; }
        public string ClassId { get; private set; }
        public string AssignmentId { get; private set; }
        public string QuestTitle { get; private set; }
        public string IntroVideoUrl { get; private set; }

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

        public static QuestSession Begin(string questId, string classId, string introVideoUrl = null,
            string assignmentId = null, string questTitle = null)
        {
            if (Instance == null)
            {
                var sessionObject = new GameObject("QuestSession");
                sessionObject.AddComponent<QuestSession>();
            }

            Instance.QuestId = string.IsNullOrWhiteSpace(questId) ? "celestial-clock" : questId;
            Instance.ClassId = classId ?? string.Empty;
            // Older assignment documents used the quest id as the document id. Keep that
            // convention as the compatibility fallback while new assignments can supply
            // their own stable id.
            Instance.AssignmentId = string.IsNullOrWhiteSpace(assignmentId) ? Instance.QuestId : assignmentId;
            Instance.QuestTitle = string.IsNullOrWhiteSpace(questTitle)
                ? "The Riddle of the Celestial Clock"
                : questTitle;
            Instance.IntroVideoUrl = introVideoUrl ?? string.Empty;
            return Instance;
        }

        public void Clear()
        {
            QuestId = string.Empty;
            ClassId = string.Empty;
            AssignmentId = string.Empty;
            QuestTitle = string.Empty;
            IntroVideoUrl = string.Empty;
        }
    }
}
