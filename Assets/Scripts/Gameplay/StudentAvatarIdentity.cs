using Firebase.Auth;
using UnityEngine;

namespace ImagineQuest.Gameplay
{
    /// <summary>Account-scoped cache populated from signup and the saved login profile.</summary>
    public static class StudentAvatarIdentity
    {
        public static string AvatarId => IsCurrentUser ? PlayerPrefs.GetString("SelectedAvatarId", "navigator") : "navigator";
        public static string ColorHex => IsCurrentUser ? PlayerPrefs.GetString("SelectedAvatarColor", "FFFFFF") : "FFFFFF";
        private static bool IsCurrentUser
        {
            get
            {
                var user = FirebaseAuth.DefaultInstance.CurrentUser;
                return user != null && PlayerPrefs.GetString("AvatarProfileUserId", "") == user.UserId;
            }
        }
        public static void Cache(string userId, string avatarId, string colorHex)
        {
            PlayerPrefs.SetString("AvatarProfileUserId", userId ?? "");
            PlayerPrefs.SetString("SelectedAvatarId", string.IsNullOrWhiteSpace(avatarId) ? "navigator" : avatarId);
            PlayerPrefs.SetString("SelectedAvatarColor", string.IsNullOrWhiteSpace(colorHex) ? "FFFFFF" : colorHex);
            PlayerPrefs.Save();
        }
        public static void Clear()
        {
            PlayerPrefs.DeleteKey("AvatarProfileUserId");
            PlayerPrefs.DeleteKey("SelectedAvatarId");
            PlayerPrefs.DeleteKey("SelectedAvatarColor");
        }
    }
}
