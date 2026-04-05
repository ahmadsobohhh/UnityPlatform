using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;

public class StudentProfilePage : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_Text profileTitleText;
    [SerializeField] private TMP_Text classNameText;
    [SerializeField] private TMP_Text memberSinceText;
    [SerializeField] private TMP_Text classesJoinedText;
    [SerializeField] private TMP_Text totalXpText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image xpBarFill;
    [SerializeField] private Button backButton;
    [SerializeField] private Button signOutButton;
    [SerializeField] private CanvasGroup uiGroup;

    [Header("Avatar Sprites")]
    [SerializeField] private Sprite[] avatarSprites;
    [SerializeField] private string[] avatarIds;

    private void Start()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.EnsureMusicPlaying();

        EnsureProfileTitleLayout();

        if (backButton != null)
            backButton.onClick.AddListener(() => SceneTransition.LoadScene("StudentHub"));
        if (signOutButton != null)
            signOutButton.onClick.AddListener(SignOut);

        StartCoroutine(LoadProfile());
    }

    private void EnsureProfileTitleLayout()
    {
        if (profileTitleText == null)
        {
            foreach (var tmp in GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp != null && (tmp.name == "TitleTxt" || tmp.text == "Your Profile"))
                {
                    profileTitleText = tmp;
                    break;
                }
            }
        }

        if (profileTitleText == null)
            return;

        var rt = profileTitleText.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(36f, -28f);
        rt.sizeDelta = new Vector2(480f, 70f);

        profileTitleText.alignment = TextAlignmentOptions.TopLeft;
        profileTitleText.enableWordWrapping = false;
        profileTitleText.transform.SetAsLastSibling();
    }

    private IEnumerator LoadProfile()
    {
        if (uiGroup != null) uiGroup.alpha = 0f;

        var auth = FirebaseAuth.DefaultInstance;
        var db = FirebaseFirestore.DefaultInstance;
        var user = auth.CurrentUser;

        if (user == null) { Debug.LogError("No user signed in."); yield break; }

        var userDocTask = db.Collection("users").Document(user.UserId).GetSnapshotAsync();
        yield return new WaitUntil(() => userDocTask.IsCompleted);

        if (userDocTask.IsFaulted || !userDocTask.Result.Exists)
        {
            Debug.LogError("Failed to load profile.");
            yield break;
        }

        var snap = userDocTask.Result;

        string username = snap.ContainsField("username") ? snap.GetValue<string>("username") : "Player";
        string charClass = snap.ContainsField("characterClass") ? snap.GetValue<string>("characterClass") : "";
        string charColor = snap.ContainsField("characterColor") ? snap.GetValue<string>("characterColor") : "FFFFFF";
        string avatarId = snap.ContainsField("avatarId") ? snap.GetValue<string>("avatarId") : "";

        if (usernameText != null) usernameText.text = username;
        if (classNameText != null) classNameText.text = string.IsNullOrEmpty(charClass) ? "No Class" : charClass;

        if (snap.ContainsField("createdAt"))
        {
            var ts = snap.GetValue<Timestamp>("createdAt");
            if (memberSinceText != null)
                memberSinceText.text = "Member since " + ts.ToDateTime().ToString("MMM yyyy");
        }
        else if (memberSinceText != null)
        {
            memberSinceText.text = "";
        }

        SetAvatar(avatarId, charColor);

        // Fetch classes joined + aggregate stats
        var classesTask = db.Collection("users").Document(user.UserId)
            .Collection("classes").GetSnapshotAsync();
        yield return new WaitUntil(() => classesTask.IsCompleted);

        int classCount = 0;
        var classIds = new List<string>();
        if (!classesTask.IsFaulted && classesTask.Result != null)
        {
            classCount = classesTask.Result.Count;
            foreach (var doc in classesTask.Result.Documents)
                classIds.Add(doc.Id);
        }

        if (classesJoinedText != null)
            classesJoinedText.text = classCount.ToString();

        int totalXp = 0;
        int maxLevel = 1;
        foreach (string classId in classIds)
        {
            var memberTask = db.Collection("classes").Document(classId)
                .Collection("members").Document(user.UserId).GetSnapshotAsync();
            yield return new WaitUntil(() => memberTask.IsCompleted);

            if (!memberTask.IsFaulted && memberTask.Result != null && memberTask.Result.Exists)
            {
                var m = memberTask.Result;
                if (m.ContainsField("xp")) totalXp += m.GetValue<int>("xp");
                if (m.ContainsField("level"))
                {
                    int lvl = m.GetValue<int>("level");
                    if (lvl > maxLevel) maxLevel = lvl;
                }
            }
        }

        if (totalXpText != null) totalXpText.text = totalXp.ToString();
        if (levelText != null) levelText.text = maxLevel.ToString();

        if (xpBarFill != null)
        {
            int xpForNext = maxLevel * 100;
            int currentLevelXp = totalXp % xpForNext;
            xpBarFill.fillAmount = Mathf.Clamp01((float)currentLevelXp / xpForNext);
        }

        // Fade in
        if (uiGroup != null)
        {
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                uiGroup.alpha = Mathf.Clamp01(t / 0.5f);
                yield return null;
            }
            uiGroup.alpha = 1f;
        }
    }

    private void SetAvatar(string avatarId, string hexColor)
    {
        if (avatarImage == null) return;

        if (avatarIds != null && avatarSprites != null)
        {
            for (int i = 0; i < avatarIds.Length && i < avatarSprites.Length; i++)
            {
                if (avatarIds[i] == avatarId)
                {
                    avatarImage.sprite = avatarSprites[i];
                    break;
                }
            }
        }

        if (ColorUtility.TryParseHtmlString("#" + hexColor, out Color tint))
            avatarImage.color = tint;

        avatarImage.preserveAspect = true;
    }

    private void SignOut()
    {
        FirebaseAuth.DefaultInstance.SignOut();
        Debug.Log("User signed out");
        SceneTransition.LoadScene("WelcomePage");
    }
}
