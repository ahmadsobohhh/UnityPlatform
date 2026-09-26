// Script: ClassSelection
// Path: Assets/Scripts/Teacher/ClassSelect/ClassSelection.cs
// Purpose: Stores currently selected teacher class metadata between scenes.

using UnityEngine;

/* Static class to hold selected class info between scenes */
public static class ClassSelection
{
    public static string CurrentClassId;
    public static string CurrentClassName;
    public static string CurrentClassCode;

    public static void ClearForSignOut()
    {
        CurrentClassId = null;
        CurrentClassName = null;
        CurrentClassCode = null;

        PlayerPrefs.DeleteKey("SelectedClassId");
        PlayerPrefs.DeleteKey("SelectedClassName");
        PlayerPrefs.DeleteKey("SelectedClassCode");
        PlayerPrefs.DeleteKey("JoinedClassDocId");
        PlayerPrefs.DeleteKey("JoinedClassName");
        PlayerPrefs.DeleteKey("JoinedClassCode");
        PlayerPrefs.Save();

    }
}


