using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using ImagineQuest.Gameplay;
using UnityEngine;

namespace ImagineQuest.Characters
{
    public interface IAppearanceRepository
    {
        bool IsLocal {get;}
        bool IsCurrent {get;}
        Task<string> LoadAsync();
        Task SaveAsync(StudentAppearance appearance);
    }
    public sealed class GuestAppearanceRepository : IAppearanceRepository
    {
        public bool IsLocal=>true;
        public bool IsCurrent=>StudentAvatarIdentity.OwnerId=="local-guest";
        public Task<string> LoadAsync()=>Task.FromResult(StudentAvatarIdentity.AvatarId);
        public Task SaveAsync(StudentAppearance appearance)
        {
            if(!IsCurrent || !appearance.Valid) throw new InvalidOperationException("The profile changed.");
            StudentAvatarIdentity.SaveGuest(appearance.AvatarId); return Task.CompletedTask;
        }
    }
    public sealed class FirebaseAppearanceRepository : IAppearanceRepository
    {
        private readonly FirebaseAuth auth;
        private readonly FirebaseFirestore db;
        private readonly string owner;
        private bool loaded;
        public FirebaseAppearanceRepository(FirebaseAuth auth,FirebaseFirestore db) {this.auth=auth;this.db=db;owner=auth.CurrentUser?.UserId;}
        public bool IsLocal=>false;
        public bool IsCurrent=>owner!=null && auth.CurrentUser?.UserId==owner;
        public async Task<string> LoadAsync()
        {
            if(!IsCurrent) throw new InvalidOperationException("Sign in to your student profile.");
            var profile=await db.Collection("users").Document(owner).GetSnapshotAsync();
            if(!IsCurrent || !profile.Exists || !profile.ContainsField("role") || profile.GetValue<string>("role")!="student")
                throw new InvalidOperationException("A student profile is required.");
            loaded=true;
            string avatarId=profile.ContainsField("avatarId")?profile.GetValue<string>("avatarId"):"";
            StudentAvatarIdentity.Cache(owner,avatarId,"FFFFFF");
            return avatarId;
        }
        public async Task SaveAsync(StudentAppearance appearance)
        {
            if(!loaded || !IsCurrent || !appearance.Valid) throw new InvalidOperationException("Reload your student profile first.");
            // Reuse the existing allowed profile fields. No rules deployment, wallet write, or billing setup.
            await db.Collection("users").Document(owner).UpdateAsync(new Dictionary<string,object> {
                {"avatarId",appearance.AvatarId},{"characterColor","FFFFFF"},{"characterClass","Deckhand"},
                {"avatarChosen",true},{"updatedAt",FieldValue.ServerTimestamp}
            });
            if(!IsCurrent) throw new InvalidOperationException("Your account changed while saving.");
            StudentAvatarIdentity.Cache(owner,appearance.AvatarId,"FFFFFF");
        }
        public static async Task<IAppearanceRepository> Connect()
        {
            if(await FirebaseApp.CheckAndFixDependenciesAsync()!=DependencyStatus.Available)
                throw new InvalidOperationException("Your profile could not connect.");
            var auth=FirebaseAuth.DefaultInstance;
            return auth.CurrentUser==null ? (IAppearanceRepository)new GuestAppearanceRepository() : new FirebaseAppearanceRepository(auth,FirebaseFirestore.DefaultInstance);
        }
    }
    public static class AppearanceEditorRoute
    {
        private static string returnScene;
        public static void Open(string origin) {returnScene=origin;SceneTransition.LoadScene("StudentAvatarSelect");}
        public static string Consume() {string origin=returnScene;returnScene=null;return origin;}
    }
}
