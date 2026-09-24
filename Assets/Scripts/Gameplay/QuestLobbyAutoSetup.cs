using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ImagineQuest.Gameplay
{
    /// <summary>
    /// Keeps the class-lobby feature attached even when a designer replaces a Canvas in
    /// one of the existing lobby scenes. It is intentionally a separate source file so
    /// Unity can reliably associate the lobby MonoBehaviour script assets in scenes.
    /// </summary>
    internal static class QuestLobbyAutoSetup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= Attach;
            SceneManager.sceneLoaded += Attach;
            Attach(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void Attach(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "StudentHub" && scene.name != "ClassroomScene" && scene.name != "TeacherClass")
                return;

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[Quest Lobby] No Canvas was found in " + scene.name + ".");
                return;
            }

            if (scene.name == "TeacherClass")
            {
                if (canvas.GetComponent<TeacherQuestLobby>() == null)
                    canvas.gameObject.AddComponent<TeacherQuestLobby>();
                return;
            }

            if (canvas.GetComponent<QuestLauncher>() == null)
                canvas.gameObject.AddComponent<QuestLauncher>();
            if (canvas.GetComponent<StudentQuestLobby>() == null)
                canvas.gameObject.AddComponent<StudentQuestLobby>();
        }
    }
}
