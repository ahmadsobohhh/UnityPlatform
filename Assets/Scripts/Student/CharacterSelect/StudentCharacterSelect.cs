using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Firestore;

public class StudentCharacterSelect : MonoBehaviour
{
    [Header("Character Slots (Character1 → Character8)")]
    public List<GameObject> characterSlots;  // Drag all Character GameObjects
    public GameObject joinGUI;
    public GameObject classInfo;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color dimColor = new Color(55, 55, 0); // darker for unused

    private FirebaseAuth auth;
    private FirebaseFirestore db;
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

        // 🔍 Fetch the student's classrooms
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
        for (int i = 0; i < characterSlots.Count; i++)
        {
            Image slotImage = characterSlots[i].GetComponent<Image>();
            if (slotImage == null) continue;

            // Set color brightness based on number of classrooms
            if (classroomCount == 0)
            {
                // No classrooms: only Character1 bright, others dim
                slotImage.color = (i == 0) ? normalColor : dimColor;
            }
            else
            {
                // Example: 3 classrooms → 1–3 bright, rest dim
                slotImage.color = (i < classroomCount) ? normalColor : dimColor;
            }
        }
    }

    private void AssignSlotListeners()
    {
        // Clear existing listeners
        foreach (GameObject slot in characterSlots)
        {
            Button btn = slot.GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveAllListeners();
        }

        for (int i = 0; i < characterSlots.Count; i++)
        {
            int index = i;
            Button btn = characterSlots[index].GetComponent<Button>();
            if (btn == null) continue;

            // Determine if this slot is active (bright)
            bool isActiveSlot = (classroomCount == 0 && index == 0) || (classroomCount > 0 && index < classroomCount);

            if (isActiveSlot)
                btn.onClick.AddListener(() => OnSlotSelected(index));
            else
                btn.interactable = false; // disable completely for dimmed slots
        }
    }

    private void OnSlotSelected(int index)
    {
        Debug.Log($"Clicked Character {index + 1}");

        // Existing class
        if (index < classroomCount && classroomCount > 0)
        {
            classInfo.SetActive(true);
            joinGUI.SetActive(false);
            Debug.Log("Opened ClassInfo (existing classroom).");
        }
        // Empty slot (only allowed when index == 0 and no classrooms yet)
        else if (classroomCount == 0 && index == 0)
        {
            joinGUI.SetActive(true);
            classInfo.SetActive(false);
            Debug.Log("Opened JoinGUI (first character creation).");
        }
    }

    void Update()
    {
        // If JoinGUI is active, detect click outside
        if (joinGUI.activeSelf && Input.GetMouseButtonDown(0))
        {
            // Convert mouse position to world point
            Vector3 mousePos = Input.mousePosition;

            // Check if click was inside the JoinGUI rect
            RectTransform rect = joinGUI.GetComponent<RectTransform>();
            if (rect != null && !RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, null))
            {
                // Hide JoinGUI when clicked outside
                joinGUI.SetActive(false);
            }
        }
    }
}
