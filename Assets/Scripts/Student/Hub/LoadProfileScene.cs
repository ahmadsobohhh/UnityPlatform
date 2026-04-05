using UnityEngine;

public class LoadProfileScene : MonoBehaviour
{
    public void GoToProfile()
    {
        SceneTransition.LoadScene("StudentProfile");
    }
}
