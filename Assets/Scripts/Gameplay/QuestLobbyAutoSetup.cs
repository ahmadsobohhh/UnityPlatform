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

            RemoveStalePersistentLobbyComponents(scene);

            // Do not use FindFirstObjectByType here. SceneTransition owns a
            // DontDestroyOnLoad Canvas and may be returned first. Adding a lobby to
            // that Canvas made student warnings survive sign-out and appear in the
            // teacher UI. Resolve the Canvas from this scene's roots only.
            Canvas canvas = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                canvas = root.GetComponentInChildren<Canvas>(true);
                if (canvas != null)
                    break;
            }

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

        private static void RemoveStalePersistentLobbyComponents(Scene activeScene)
        {
            foreach (var lobby in Object.FindObjectsByType<StudentQuestLobby>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (lobby != null && lobby.gameObject.scene != activeScene)
                    Object.Destroy(lobby);
            }

            foreach (var lobby in Object.FindObjectsByType<TeacherQuestLobby>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (lobby != null && lobby.gameObject.scene != activeScene)
                    Object.Destroy(lobby);
            }
        }
    }
}
