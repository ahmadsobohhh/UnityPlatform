using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Firestore;

public class TeacherClassEditor : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text titleLabel;       // "Edit class:"
    [SerializeField] private TMP_InputField nameInput;  // input field for class name
    [SerializeField] private Button editBtn;            // confirm edit
    [SerializeField] private Button deleteBtn;          // delete class

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    private string classId;
    private string ownerUid;

    void Awake()
    {
        auth = FirebaseAuth.DefaultInstance;
        db   = FirebaseFirestore.DefaultInstance;
    }

    void Start()
    {
        classId = ClassSelection.CurrentClassId;
        ownerUid = auth.CurrentUser != null ? auth.CurrentUser.UserId : null;

        if (string.IsNullOrEmpty(classId) || string.IsNullOrEmpty(ownerUid))
        {
            Debug.LogWarning("No selected class or missing user auth.");
            return;
        }

        // Setup UI with selected class
        string currentName = ClassSelection.CurrentClassName ?? "";
        if (titleLabel) titleLabel.text = $"Edit class: {currentName}";
        if (nameInput)  nameInput.text  = currentName;

        // Wire up buttons
        if (editBtn)   editBtn.onClick.AddListener(OnClick_Edit);
        if (deleteBtn) deleteBtn.onClick.AddListener(OnClick_Delete);
    }

    // --- Rename class ---
    private void OnClick_Edit()
    {
        string newName = nameInput ? (nameInput.text ?? "").Trim() : "";
        if (string.IsNullOrEmpty(newName))
        {
            Debug.LogWarning("Class name cannot be empty.");
            return;
        }

        StartCoroutine(RenameRoutine(classId, ownerUid, newName));
    }

    // --- Delete class ---
    private void OnClick_Delete()
    {
        StartCoroutine(DeleteRoutine(classId, ownerUid));
    }

    // --- Firestore update routines ---
    private IEnumerator RenameRoutine(string classId, string uid, string newName)
    {
        var globalRef = db.Collection("classes").Document(classId);
        var idxRef    = db.Collection("users").Document(uid).Collection("classes").Document(classId);

        var t1 = globalRef.UpdateAsync(new Dictionary<string, object> {
            { "name", newName },
            { "updatedAt", Firebase.Firestore.Timestamp.GetCurrentTimestamp() }
        });
        var t2 = idxRef.UpdateAsync(new Dictionary<string, object> {
            { "name", newName }
        });

        yield return new WaitUntil(() => t1.IsCompleted && t2.IsCompleted);

        if (t1.IsFaulted || t2.IsFaulted)
        {
            Debug.LogError("Rename failed: " + (t1.Exception ?? t2.Exception));
            yield break;
        }

        // Update title and stored name
        ClassSelection.CurrentClassName = newName;
        if (titleLabel) titleLabel.text = $"Edit class: {newName}";
        Debug.Log("Class renamed successfully.");
    }

    private IEnumerator DeleteRoutine(string classId, string uid)
    {
        var globalRef = db.Collection("classes").Document(classId);
        var idxRef    = db.Collection("users").Document(uid).Collection("classes").Document(classId);

        var del1 = globalRef.DeleteAsync();
        var del2 = idxRef.DeleteAsync();

        yield return new WaitUntil(() => del1.IsCompleted && del2.IsCompleted);

        if (del1.IsFaulted || del2.IsFaulted)
        {
            Debug.LogError("Delete failed: " + (del1.Exception ?? del2.Exception));
            yield break;
        }

        Debug.Log("Class deleted successfully.");

        // You can now hide the edit tab via Unity (UI.SetActive(false))
        // or trigger your own animation/UI transition here.
    }
}
