using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ImagineQuest.Student
{
    [Serializable]
    public sealed class LocalStatusActivity
    {
        public string title, detail;
        public long xp, hearts, unixSeconds;
    }

    [Serializable]
    public sealed class LocalStudentStatus
    {
        public int schemaVersion = 1;
        public string ownerId;
        public long xp, epoch, crystals;
        public int hearts = 50;
        public List<string> receipts = new List<string>();
        public List<LocalStatusActivity> activity = new List<LocalStatusActivity>();
    }

    public sealed class LocalStatusReward
    {
        public string Receipt, Title, Detail;
        public int Xp;
    }

    /// <summary>Device-only progress. These files are never uploaded as a Firebase wallet.</summary>
    public sealed class LocalStudentStatusStore
    {
        public const int XpPerCrystal = 5;
        private static readonly object Gate = new object();
        private readonly string directory;
        public LocalStudentStatusStore(string directory) { this.directory = directory; }
        public string FilePath(string owner)
        {
            if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException("A local profile is required.");
            using (var hash = SHA256.Create())
                return Path.Combine(directory, string.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(owner)).Select(b => b.ToString("x2"))) + ".json");
        }
        public LocalStudentStatus Load(string owner)
        {
            lock (Gate) return Read(owner);
        }
        private LocalStudentStatus Read(string owner)
        {
            string path = FilePath(owner);
            if (!File.Exists(path))
            {
                var fresh = new LocalStudentStatus { ownerId = owner }; Write(fresh); return fresh;
            }
            LocalStudentStatus saved;
            try { saved = JsonUtility.FromJson<LocalStudentStatus>(File.ReadAllText(path)); }
            catch (Exception ex) { throw new InvalidDataException("Your local save could not be opened. The original file is preserved.", ex); }
            if (saved == null || saved.schemaVersion != 1 || saved.ownerId != owner || saved.xp < 0 || saved.crystals < 0 || saved.epoch < 0 ||
                saved.hearts < 0 || saved.hearts > 50 || saved.receipts == null || saved.activity == null ||
                saved.activity.Any(item => item == null || item.unixSeconds < 0 || item.unixSeconds > 253402300799L))
                throw new InvalidDataException("Your local save needs review. The original file is preserved.");
            return saved;
        }
        public LocalStudentStatus Award(string owner, IEnumerable<LocalStatusReward> rewards, out long awarded)
        {
            lock (Gate)
            {
                var saved = Read(owner); awarded = 0;
                if (saved.hearts == 0) return saved;
                foreach (var reward in rewards)
                {
                    if (reward == null || string.IsNullOrWhiteSpace(reward.Receipt) || reward.Xp <= 0 || reward.Xp > 10000)
                        throw new ArgumentException("Invalid local reward.");
                    string key = "reward/" + reward.Receipt;
                    if (saved.receipts.Contains(key)) continue;
                    saved.xp = checked(saved.xp + reward.Xp); awarded += reward.Xp;
                    saved.crystals = checked(saved.crystals + reward.Xp / XpPerCrystal);
                    saved.receipts.Add(key);
                    AddActivity(saved, reward.Title, reward.Detail, reward.Xp, 0);
                }
                if (awarded > 0) Write(saved);
                return saved;
            }
        }
        public LocalStudentStatus AdjustHearts(string owner, string eventId, int delta, string reason, out bool applied)
        {
            if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 140 || delta == 0 || Math.Abs((long)delta) > 50)
                throw new ArgumentException("Choose a valid Heart change and enter a reason.");
            lock (Gate)
            {
                var saved = Read(owner); string key = "standing/" + eventId;
                applied = !saved.receipts.Contains(key);
                if (!applied) return saved;
                int after = saved.hearts + delta;
                if (after < 0 || after > 50) throw new InvalidOperationException("Hearts must stay between 0 and 50.");
                bool reset = saved.hearts > 0 && after == 0; long previousXp = saved.xp;
                saved.hearts = after; saved.receipts.Add(key);
                if (reset) { saved.xp = 0; saved.crystals = 0; saved.epoch++; }
                AddActivity(saved, reset ? "A fresh start" : delta > 0 ? "Hearts restored" : "Classroom standing updated",
                    reason.Trim(), reset ? -previousXp : 0, delta);
                Write(saved); return saved;
            }
        }
        private static void AddActivity(LocalStudentStatus saved, string title, string detail, long xp, long hearts)
        {
            saved.activity.Insert(0, new LocalStatusActivity { title = title, detail = detail, xp = xp, hearts = hearts,
                unixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() });
            if (saved.activity.Count > 12) saved.activity.RemoveRange(12, saved.activity.Count - 12);
        }
        private void Write(LocalStudentStatus saved)
        {
            Directory.CreateDirectory(directory);
            string path = FilePath(saved.ownerId), temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(saved, true));
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
            else File.Move(temporary, path);
        }
    }
}
