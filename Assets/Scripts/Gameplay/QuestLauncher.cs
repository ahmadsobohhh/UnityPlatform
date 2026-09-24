using UnityEngine;

namespace ImagineQuest.Gameplay
{
    /// <summary>
    /// Optional entry point for a future dedicated quest menu. It does not attach
    /// itself to any legacy student, teacher, or character-selection page.
    /// </summary>
    public sealed class QuestLauncher : MonoBehaviour
    {
        [SerializeField] private string defaultQuestId = "celestial-clock";
        // The existing teacher UI stores its migration-bridge document at this ID.
        // The playable quest itself has the stable content ID celestial-clock.
        [SerializeField] private string defaultAssignmentId = "pirate-voyage";
        [SerializeField] private string questSceneName = "CelestialClockQuest";
        [TextArea]
        [SerializeField] private string introVideoUrl;

        public void LaunchPirateQuest()
        {
            // Character selection carries its active class through ClassSelection, while
            // the hub path also persists it in PlayerPrefs. Support both entry routes.
            var classId = ClassSelection.CurrentClassId;
            if (string.IsNullOrWhiteSpace(classId))
                classId = PlayerPrefs.GetString("SelectedClassId", string.Empty);
            if (string.IsNullOrWhiteSpace(classId))
            {
                Debug.LogWarning("[QuestLauncher] Choose a class before starting a class quest.");
                return;
            }

            Launch(defaultQuestId, classId, introVideoUrl, defaultAssignmentId);
        }

        public void Launch(string questId, string classId, string videoUrl = null)
        {
            Launch(questId, classId, videoUrl, questId);
        }

        public void Launch(string questId, string classId, string videoUrl, string assignmentId)
        {
            QuestSession.Begin(questId, classId, videoUrl, assignmentId, "The Riddle of the Celestial Clock");

            // Existing scenes may have serialized the former proof-of-concept scene name.
            // Route those old buttons into the complete quest instead of forcing every
            // designer to manually rewire a UnityEvent before the next migration pass.
            var sceneName = string.IsNullOrWhiteSpace(questSceneName) || questSceneName == "PirateQuest"
                ? "CelestialClockQuest"
                : questSceneName;
            SceneTransition.LoadScene(sceneName);
        }
    }
}
