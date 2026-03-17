using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadJoinClassScene : MonoBehaviour
{
    public void GoToJoinClass()
    {
        SceneManager.LoadScene("StudentJoinClassWCode");
    }
}