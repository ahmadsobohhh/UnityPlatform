using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class JointClassManager : MonoBehaviour
{
    public TMP_InputField codeInput;
    public string correctCode = "1234";
    public string sceneToLoad = "ClassroomScene"; // change this

    public void JoinClassByCode()
    {
        var entered = codeInput.text.Trim();

        if (entered == correctCode)
        {
            Debug.Log("Correct code!");
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.Log("Wrong code!");
        }
    }
}