using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Firestore;

public class StudentCharacterSelect : MonoBehaviour
{
    [Header("Character Slots (in order: Character1 → Character8)")]
    public List<GameObject> characterSlots;  // Each character GameObject (with Image component)
    public GameObject joinGUI;               // Shown when selecting empty slot
    public GameObject classInfo;             // Shown when selecting existing class slot

    [Header("Colors")]
    public Color normalColor = Color.white; 
    public Color dimColor = new Color(0.3f, 0.3f, 0.3f); // darker for unused

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    // Tracks how many classroom slots are filled
    private int classroomCount = 0;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        joinGUI.SetActive(false);
        classInfo.SetActive(false);

        StartCoroutine(CheckStudentClassrooms());
    }

    private IEnumerator CheckStudentClassrooms()
    {
        var user = auth.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No signed-in user found!");
            yield break;
        }

        // Example path: users/{uid}/classrooms
        var classroomsRef = db.Collection("users").Document(user.UserId).Collection("classrooms");
        var task = classroomsRef.GetSnapshotAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted || task.IsCanceled)
        {
            Debug.LogError("Failed to load classrooms: " + task.Exception);
            yield break;
        }

        QuerySnapshot snapshot = task.Result;
        classroomCount = snapshot.Count;

        Debug.Log($"Student has {classroomCount} classrooms.");

        ApplySlotColors();
        AssignSlotListeners();
    }

    private void ApplySlotColors()
    {
        // Darken unused slots
        for (int i = 0; i < characterSlots.Count; i++)
        {
            Image slotImage = characterSlots[i].GetComponent<Image>();
            if (slotImage == null) continue;

            // If there are no classrooms and this isn’t Character1, darken it
            if (classroomCount == 0)
            {
                slotImage.color = (i == 0) ? normalColor : dimColor;
            }
            else
            {
                // Example: 3 classrooms -> slots 1,2,3 bright, rest dark
                slotImage.color = (i < classroomCount) ? normalColor : dimColor;
            }
        }
    }

    private void AssignSlotListeners()
    {
        // Clear previous listeners
        foreach (GameObject slot in characterSlots)
        {
            Button b = slot.GetComponent<Button>();
            if (b != null) b.onClick.RemoveAllListeners();
        }

        // Assign new ones
        for (int i = 0; i < characterSlots.Count; i++)
        {
            int index = i; // local copy for closure
            Button button = characterSlots[index].GetComponent<Button>();
            if (button == null) continue;

            button.onClick.AddListener(() => OnSlotSelected(index));
        }
    }

    private void OnSlotSelected(int index)
    {
        Debug.Log($"Clicked Character {index + 1}");

        // If the clicked slot index < classroomCount → it's a used slot
        if (index < classroomCount && classroomCount > 0)
        {
            classInfo.SetActive(true);
            joinGUI.SetActive(false);
            Debug.Log("Opened ClassInfo (existing classroom).");
        }
        else
        {
            joinGUI.SetActive(true);
            classInfo.SetActive(false);
            Debug.Log("Opened JoinGUI (empty slot).");
        }
    }
}
