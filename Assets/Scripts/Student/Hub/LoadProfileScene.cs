// Script: LoadProfileScene
// Path: Assets/Scripts/Student/Hub/LoadProfileScene.cs
// Purpose: Loads the student profile scene from hub actions.

using UnityEngine;

public class LoadProfileScene : MonoBehaviour
{
    public void GoToProfile()
    {
        SceneTransition.LoadScene("StudentProfile");
    }
}


