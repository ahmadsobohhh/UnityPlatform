using Firebase.Auth;
using UnityEngine;

namespace ImagineQuest.Gameplay
{
    /// <summary>Account-scoped cache populated from signup and the saved login profile.</summary>
    public static class StudentAvatarIdentity
    {
        public static string AvatarId => OwnerId=="local-guest" ? PlayerPrefs.GetString("GuestPirateAppearance", ImagineQuest.Characters.StudentAppearance.Default.AvatarId) : IsCurrentUser ? PlayerPrefs.GetString("SelectedAvatarId", "navigator") : "navigator";
        public static string ColorHex => IsCurrentUser ? PlayerPrefs.GetString("SelectedAvatarColor", "FFFFFF") : "FFFFFF";
        public static event System.Action Changed;
        public static string OwnerId
        {
            get
            {
                if(System.Environment.GetEnvironmentVariable("IMAGINEQUEST_OFFLINE_TESTS")=="1") return "local-guest";
                try {var app=Firebase.FirebaseApp.GetInstance(Firebase.FirebaseApp.DefaultName);return app==null?"local-guest":FirebaseAuth.GetAuth(app).CurrentUser?.UserId??"local-guest";}
                catch(System.Exception) {return "local-guest";}
            }
        }
        private static bool IsCurrentUser
        {
            get
            {
                string owner=OwnerId;
                return owner!="local-guest" && PlayerPrefs.GetString("AvatarProfileUserId", "") == owner;
            }
        }
        public static void Cache(string userId, string avatarId, string colorHex)
        {
            PlayerPrefs.SetString("AvatarProfileUserId", userId ?? "");
            PlayerPrefs.SetString("SelectedAvatarId", string.IsNullOrWhiteSpace(avatarId) ? "navigator" : avatarId);
            PlayerPrefs.SetString("SelectedAvatarColor", string.IsNullOrWhiteSpace(colorHex) ? "FFFFFF" : colorHex);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
        public static void SaveGuest(string avatarId)
        {
            if(!ImagineQuest.Characters.StudentAppearance.TryParse(avatarId,out _)) throw new System.ArgumentException("Invalid guest appearance.");
            PlayerPrefs.SetString("GuestPirateAppearance",avatarId);PlayerPrefs.Save();Changed?.Invoke();
        }
        public static void Clear()
        {
            PlayerPrefs.DeleteKey("AvatarProfileUserId");
            PlayerPrefs.DeleteKey("SelectedAvatarId");
            PlayerPrefs.DeleteKey("SelectedAvatarColor");
            Changed?.Invoke();
        }
    }
}
