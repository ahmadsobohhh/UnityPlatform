using System;
using System.Collections;
using System.IO;
using Firebase;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.Networking;

namespace ImagineQuest.Gameplay
{
    /// <summary>Local checkpoints plus ordered, idempotent server verification.</summary>
    public sealed class BlacksmithQuestService : MonoBehaviour
    {
        public BlacksmithQuestState State { get; private set; }
        public string Status { get; private set; } = "Loading progress...";
        public bool Busy { get; private set; }
        public bool Ready => State != null;
        public bool LocalSaved { get; private set; }
        public bool IsPractice => State != null && State.classId == "local-practice";
        public event Action Changed;
        private BlacksmithQuestStore store;
        private string owner;
        private float nextRetryAt;
        private bool cloudLoaded;

        private void Update()
        {
            if (!ImagineQuest.Student.StudentCloudConfig.Enabled || !Ready || IsPractice || Busy || Time.realtimeSinceStartup < nextRetryAt) return;
            nextRetryAt = Time.realtimeSinceStartup + 15;
            if (!cloudLoaded || State.pending.Count > 0) RetrySync();
        }

        [Serializable] private sealed class Payload
        {
            public string classId, assignmentId, contentVersion = BlacksmithQuestContent.Version;
            public string action, eventId;
            public int station = -1, choice = -1;
        }
        [Serializable] private sealed class Request { public Payload data; }
        [Serializable] private sealed class Result
        {
            public int lessonMask, completedMask, verifiedXp;
            public bool turnedIn;
        }
        [Serializable] private sealed class Response { public Result result; }

        public void Initialize(string classId, string assignmentId)
        {
            // Opening this scene directly must work without Firebase initialization.
            owner = "local-guest";
            try
            {
                var app = FirebaseApp.GetInstance(FirebaseApp.DefaultName);
                if (app != null) owner = FirebaseAuth.GetAuth(app).CurrentUser?.UserId ?? owner;
            }
            catch (Exception) { }
            if (owner == "local-guest") classId = "local-practice";
            store = new BlacksmithQuestStore(Path.Combine(Application.persistentDataPath, "BlacksmithQuest"));
            try
            {
                State = store.Load(owner, string.IsNullOrWhiteSpace(classId) ? "local-practice" : classId,
                    string.IsNullOrWhiteSpace(assignmentId) ? BlacksmithQuestContent.Id : assignmentId);
                Save();
                RetrySync();
            }
            catch (Exception)
            {
                State = null;
                Status = "Your saved progress could not be opened. The file has been preserved.";
                Changed?.Invoke();
            }
        }

        public bool Save()
        {
            if (State == null) return false;
            try
            {
                store.Save(State);
                LocalSaved = true;
                Status = IsPractice || !ImagineQuest.Student.StudentCloudConfig.Enabled ? string.Empty
                    : "Saved on this PC • Awaiting cloud verification";
            }
            catch (Exception)
            {
                LocalSaved = false;
                Status = "Save failed on this PC. Keep the workshop open and retry.";
            }
            Changed?.Invoke();
            return LocalSaved;
        }

        public void RetrySync()
        {
            if (!ImagineQuest.Student.StudentCloudConfig.Enabled || Busy || State == null || !Save() || IsPractice) return;
            nextRetryAt = Time.realtimeSinceStartup + 15;
            StartCoroutine(Sync());
        }

        private bool SameUser => FirebaseAuth.DefaultInstance.CurrentUser?.UserId == owner;

        private IEnumerator Sync()
        {
            Busy = true;
            if (!SameUser)
            {
                Status = "Saved on this PC. Sign in with the same student to sync.";
                Busy = false;
                Changed?.Invoke();
                yield break;
            }
            Status = "Saved on this PC • Syncing...";
            Changed?.Invoke();
            var token = FirebaseAuth.DefaultInstance.CurrentUser.TokenAsync(false);
            var deadline = Time.realtimeSinceStartup + 15;
            while (!token.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
            if (!token.IsCompleted || token.IsCanceled || token.IsFaulted || !SameUser)
            {
                SyncFailed();
                yield break;
            }
            var project = FirebaseApp.DefaultInstance.Options.ProjectId;
            var endpoint = "https://northamerica-northeast1-" + project + ".cloudfunctions.net/blacksmithQuest";
            // First load server progress, then replay only unacknowledged events in order.
            bool loading = true;
            while (SameUser && (loading || State.pending.Count > 0))
            {
                var next = loading ? null : State.pending[0];
                var payload = new Payload {
                    classId = State.classId, assignmentId = State.assignmentId,
                    action = next?.action ?? "load", eventId = next?.eventId ?? "",
                    station = next?.station ?? -1, choice = next?.choice ?? -1
                };
                using (var request = new UnityWebRequest(endpoint, "POST"))
                {
                    request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(
                        JsonUtility.ToJson(new Request { data = payload })));
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Authorization", "Bearer " + token.Result);
                    request.timeout = 15;
                    yield return request.SendWebRequest();
                    if (!SameUser) { SyncFailed(); yield break; }
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        string cloudMessage = null;
                        try { cloudMessage = JsonUtility.FromJson<CloudErrorReply>(request.downloadHandler.text)?.error?.message; } catch (Exception) { }
                        SyncFailed(cloudMessage ?? (request.responseCode == 403
                            ? "Cloud access denied. Ask your teacher to check the assignment."
                            : "Cloud sync unavailable. Your progress is saved; reconnect to sync."));
                        yield break;
                    }
                    Response response = null;
                    try { response = JsonUtility.FromJson<Response>(request.downloadHandler.text); }
                    catch (Exception) { }
                    if (response?.result == null) { SyncFailed(); yield break; }
                    var result = response.result;
                    State.MergeVerified(result.lessonMask, result.completedMask, result.turnedIn, result.verifiedXp);
                    if (next != null) State.pending.RemoveAt(0);
                    if (!Save()) { Busy = false; yield break; }
                }
                loading = false;
            }
            Busy = false;
            cloudLoaded = true;
            Status = SameUser ? "Progress synced • " + State.verifiedXp + " XP verified"
                : "Saved on this PC. Sign in with the same student to sync.";
            Changed?.Invoke();
        }

        private void SyncFailed(string message = "Cloud sync unavailable. Progress is saved on this PC; use Retry sync.")
        {
            Busy = false;
            Status = message;
            Changed?.Invoke();
        }
        [Serializable] private sealed class CloudErrorReply { public CloudError error; }
        [Serializable] private sealed class CloudError { public string message; }
    }
}
