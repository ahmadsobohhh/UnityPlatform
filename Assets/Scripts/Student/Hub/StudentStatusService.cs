using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

namespace ImagineQuest.Student
{
    public sealed class StudentStatusSnapshot
    {
        public long Xp { get; }
        public long Hearts { get; }
        public long Level { get; }
        public long Epoch { get; }
        public long Crystals { get; }
        public long Gold { get; }
        public bool IsCached { get; }
        public StudentStatusSnapshot(IDictionary<string, object> wallet, IDictionary<string, object> standing, bool cached)
        {
            Xp = Read(wallet, "xp"); Level = Read(wallet, "level", 1); Epoch = Read(wallet, "epoch");
            Crystals = Read(wallet, "crystals"); Gold = Read(wallet, "gold");
            Hearts = Read(standing, "hearts", 50); IsCached = cached;
            if (Hearts > 50 || Level < 1) throw new InvalidOperationException("Saved status needs administrator review.");
        }
        private static long Read(IDictionary<string, object> source, string key, long fallback = 0)
        {
            if (source == null) throw new InvalidOperationException("Waiting for saved status.");
            if (!source.TryGetValue(key, out var value)) return fallback;
            long number = value is long l ? l : value is int i ? i : -1;
            if (number < 0) throw new InvalidOperationException("Saved status needs administrator review.");
            return number;
        }
    }

    public sealed class StudentActivity
    {
        public string Title, Detail;
        public long Xp, Hearts;
        public DateTime? Time;
    }

    /// <summary>One authenticated, scene-independent subscription; no client balance writes.</summary>
    public sealed class StudentStatusService : MonoBehaviour
    {
        private static StudentStatusService instance;
        public static StudentStatusService GetOrCreate()
        {
            if (instance == null) instance = new GameObject("Live student status").AddComponent<StudentStatusService>();
            return instance;
        }
        public event Action Changed;
        public StudentStatusSnapshot Snapshot { get; private set; }
        public bool Ready => Snapshot != null;
        public string UserId { get; private set; }
        public string Connection { get; private set; } = "Connecting your progress...";
        public string HistoryStatus { get; private set; } = "Loading recent activity...";
        public long SessionXp { get; private set; }
        public readonly List<StudentActivity> Activity = new List<StudentActivity>();
        private FirebaseAuth auth;
        private ListenerRegistration walletListener, standingListener, historyListener;
        private IDictionary<string, object> wallet, standing;
        private bool walletCached, standingCached, dirty, baseline;
        private int generation;
        private readonly ConcurrentQueue<Action> callbacks = new ConcurrentQueue<Action>();

        private async void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this; DontDestroyOnLoad(gameObject);
            // Isolated UI/play tests must never create real cloud balances or use a saved login.
            if (Environment.GetEnvironmentVariable("IMAGINEQUEST_OFFLINE_TESTS") == "1")
            { Connection = "Sign in to connect your progress."; return; }
            if (!StudentCloudConfig.Enabled)
            {
                Connection = "Cloud progress is not enabled yet.";
                HistoryStatus = "Your progress will appear here when cloud updates are enabled.";
                return;
            }
            try
            {
                if (await FirebaseApp.CheckAndFixDependenciesAsync() != DependencyStatus.Available)
                    throw new InvalidOperationException("Cloud services are unavailable on this device.");
                if (this == null) return;
                auth = FirebaseAuth.DefaultInstance; auth.StateChanged += AuthChanged;
                Reconnect();
            }
            catch (Exception) { Connection = "Cloud connection unavailable. Select Retry in your recap."; Changed?.Invoke(); }
        }

        private void AuthChanged(object sender, EventArgs args)
        {
            if (auth.CurrentUser?.UserId != UserId) Reconnect();
        }

        public async void Reconnect()
        {
            if (auth == null) return;
            string nextUser = auth.CurrentUser?.UserId;
            bool newAccount = nextUser != UserId;
            StopListeners(); int version = ++generation; UserId = nextUser;
            wallet = standing = null;
            if (newAccount) { Snapshot = null; SessionXp = 0; baseline = false; Activity.Clear(); }
            Connection = UserId == null ? "Sign in to connect your progress." : "Connecting your progress...";
            HistoryStatus = "Loading recent activity..."; Changed?.Invoke();
            if (UserId == null) return;
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                var profile = await db.Collection("users").Document(UserId).GetSnapshotAsync(Source.Server);
                if (!Current(version)) return;
                if (!profile.Exists || !profile.ContainsField("role") || profile.GetValue<string>("role") != "student")
                { Snapshot = null; Connection = "A student account is required."; Changed?.Invoke(); return; }
                await GameCloudClient.CallAsync<GameCloudClient.Empty, GameCloudClient.Acknowledgement>("ensureStudentStatus", new GameCloudClient.Empty());
                if (!Current(version)) return;
                walletListener = db.Collection("wallets").Document(UserId).Listen(MetadataChanges.Include, snapshot => callbacks.Enqueue(() => {
                    if (!Current(version)) return;
                    wallet = snapshot.Exists ? snapshot.ToDictionary() : null; walletCached = snapshot.Metadata.IsFromCache; dirty = true;
                }));
                standingListener = db.Collection("studentStanding").Document(UserId).Listen(MetadataChanges.Include, snapshot => callbacks.Enqueue(() => {
                    if (!Current(version)) return;
                    standing = snapshot.Exists ? snapshot.ToDictionary() : null; standingCached = snapshot.Metadata.IsFromCache; dirty = true;
                }));
                historyListener = db.Collection("studentActivity").Document(UserId).Collection("entries")
                    .OrderByDescending("occurredAt").Limit(6).Listen(snapshot => callbacks.Enqueue(() => {
                        if (!Current(version)) return;
                        Activity.Clear();
                        foreach (var doc in snapshot.Documents)
                        {
                            var data = doc.ToDictionary();
                            Activity.Add(new StudentActivity {
                                Title = data.TryGetValue("title", out var title) ? title as string ?? "Progress updated" : "Progress updated",
                                Detail = data.TryGetValue("detail", out var detail) ? detail as string ?? "" : "",
                                Xp = data.TryGetValue("deltaXp", out var xp) && xp is long x ? x : 0,
                                Hearts = data.TryGetValue("deltaHearts", out var hearts) && hearts is long h ? h : 0,
                                Time = data.TryGetValue("occurredAt", out var time) && time is Timestamp stamp ? stamp.ToDateTime() : (DateTime?)null,
                            });
                        }
                        HistoryStatus = Activity.Count == 0 ? "Your next adventure starts your story." : "Your six most recent updates";
                        Changed?.Invoke();
                    }));
                WatchFailure(walletListener.ListenerTask, version, false);
                WatchFailure(standingListener.ListenerTask, version, false);
                WatchFailure(historyListener.ListenerTask, version, true);
            }
            catch (Exception ex)
            {
                if (!Current(version)) return;
                Connection = Ready ? "Connection interrupted • showing last saved totals" : ex is InvalidOperationException
                    ? ex.Message : "Could not load saved progress. Select Reconnect to try again.";
                Changed?.Invoke();
            }
        }
        private bool Current(int version) => this != null && version == generation && auth?.CurrentUser?.UserId == UserId;
        private void WatchFailure(Task task, int version, bool history)
        {
            task.ContinueWith(done => {
                if (!done.IsFaulted) return;
                _ = done.Exception;
                callbacks.Enqueue(() => {
                    if (!Current(version)) return;
                    if (history) HistoryStatus = "Recent activity is unavailable. Retry to reconnect.";
                    else Connection = "Connection interrupted • open your recap to retry";
                    Changed?.Invoke();
                });
            });
        }
        private void Update()
        {
            while (callbacks.TryDequeue(out var callback)) callback();
            if (!dirty) return;
            dirty = false;
            if (wallet == null || standing == null) return;
            try
            {
                var next = new StudentStatusSnapshot(wallet, standing, walletCached || standingCached);
                if (!next.IsCached)
                {
                    if (baseline && Snapshot != null && next.Epoch == Snapshot.Epoch && next.Xp > Snapshot.Xp)
                        SessionXp += next.Xp - Snapshot.Xp;
                    if (Snapshot != null && next.Epoch != Snapshot.Epoch) SessionXp = 0;
                    baseline = true;
                }
                Snapshot = next;
                Connection = next.IsCached ? "Offline • last saved totals" : "Live • progress saved";
                Changed?.Invoke();
            }
            catch (Exception ex) { Snapshot = null; Connection = ex.Message; Changed?.Invoke(); }
        }
        private void OnApplicationFocus(bool focused) { if (focused && auth != null && walletListener == null) Reconnect(); }
        private void StopListeners()
        {
            walletListener?.Stop(); standingListener?.Stop(); historyListener?.Stop();
            walletListener = standingListener = historyListener = null;
        }
        private void OnDestroy()
        {
            ++generation; StopListeners(); if (auth != null) auth.StateChanged -= AuthChanged;
            if (instance == this) instance = null;
        }
    }
}
