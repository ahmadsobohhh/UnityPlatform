using System;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

namespace ImagineQuest.Gameplay
{
    public static class WorkshopAssignmentLauncher
    {
        private static bool launching;
        public static async void Launch(Action<string> feedback)
        {
            if (launching) return;
            launching = true;
            try
            {
                if (!ImagineQuest.Student.StudentCloudConfig.Enabled)
                    throw new InvalidOperationException("This class workshop isn't available yet. Your teacher can open it when cloud progress is ready.");
                var user = FirebaseAuth.DefaultInstance.CurrentUser;
                if (user == null) throw new InvalidOperationException("Sign in as a student and open your class to play an assigned workshop.");
                string owner = user.UserId;
                int scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
                string classId = QuestSession.Instance?.ClassId;
                if (string.IsNullOrWhiteSpace(classId)) classId = ClassSelection.CurrentClassId;
                if (string.IsNullOrWhiteSpace(classId)) classId = PlayerPrefs.GetString("SelectedClassId", "");
                if (string.IsNullOrWhiteSpace(classId)) throw new InvalidOperationException("Open your class before starting this workshop.");
                var assignment = await FirebaseFirestore.DefaultInstance.Collection("classes").Document(classId)
                    .Collection("assignments").Document(BlacksmithQuestContent.Id).GetSnapshotAsync(Source.Server);
                if (FirebaseAuth.DefaultInstance.CurrentUser?.UserId != owner || UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle != scene) return;
                if (!assignment.Exists || !assignment.ContainsField("state") || assignment.GetValue<string>("state") != "open" ||
                    assignment.GetValue<string>("questDefinitionId") != BlacksmithQuestContent.Id)
                    throw new InvalidOperationException("Your teacher hasn't assigned The Aurora's Repair yet. Ask them to select Assign Workshop in Hearts & Progress.");
                QuestSession.Begin(BlacksmithQuestContent.Id, classId, null, BlacksmithQuestContent.Id, BlacksmithQuestContent.Title);
                SceneTransition.LoadScene("BlacksmithRoom");
            }
            catch (Exception ex) { feedback?.Invoke(ex is InvalidOperationException ? ex.Message : "The workshop could not connect. Check your connection and try again."); }
            finally { launching = false; }
        }
    }
}
