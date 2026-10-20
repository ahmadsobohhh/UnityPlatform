using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ImagineQuest.Gameplay
{
    public static class BlacksmithQuestContent
    {
        public const string Id = "blacksmith-workshop";
        public const string Version = "1";
        public const string Title = "The Aurora's Repair";
        public const string Mission = "A storm has damaged the Aurora. Captain Esme needs a repair kit before the next voyage.\n\nVisit the anvil, workbench and forge. Learn each craft, answer its question, then bring the workshop key back through the portal.";
        public static readonly string[] Names = { "ANVIL", "WORKBENCH", "FORGE" };
        public static readonly string[] Lessons = {
            "An anvil is a heavy, solid metal surface. A blacksmith supports heated metal on it and shapes that metal with a hammer.",
            "Equal groups can be counted with multiplication. Three batches with four nails in each batch make 3 × 4 = 12 nails.",
            "A forge heats metal until it becomes easier to shape. The smith then shapes it on the anvil. Heating comes before shaping."
        };
        public static readonly string[] Questions = {
            "Which tool gives a blacksmith a solid surface for shaping metal?",
            "A smith makes 3 batches of 4 nails. How many nails is that?",
            "What does the forge do before metal is shaped?"
        };
        public static readonly string[][] Answers = {
            new[] { "An anvil", "A barrel", "A compass" },
            new[] { "7 nails", "12 nails", "16 nails" },
            new[] { "Cools the metal", "Paints the metal", "Heats the metal" }
        };
        // Local practice feedback only. The server independently verifies submitted answers.
        public static bool IsCorrect(int station, int choice) => station >= 0 && station < 3 && choice == station;
    }

    [Serializable]
    public sealed class BlacksmithQuestEvent
    {
        public string eventId = Guid.NewGuid().ToString("N");
        public string action;
        public int station = -1;
        public int choice = -1;
    }

    [Serializable]
    public sealed class BlacksmithQuestState
    {
        public string contentVersion = BlacksmithQuestContent.Version;
        public string userId;
        public string classId;
        public string assignmentId = BlacksmithQuestContent.Id;
        public bool introSeen;
        public int lessonMask;
        public int completedMask;
        public int[] attempts = new int[3];
        public bool turnedIn;
        public int verifiedXp;
        public List<BlacksmithQuestEvent> pending = new List<BlacksmithQuestEvent>();
        public int CompletedCount => CountBits(completedMask & 7);
        public bool HasKey => (completedMask & 7) == 7;
        public bool HasLesson(int station) => station >= 0 && station < 3 && (lessonMask & (1 << station)) != 0;
        public bool HasAnswer(int station) => station >= 0 && station < 3 && (completedMask & (1 << station)) != 0;

        public void ReadLesson(int station)
        {
            if (station < 0 || station >= 3 || HasLesson(station)) return;
            lessonMask |= 1 << station;
            pending.Add(new BlacksmithQuestEvent { action = "lesson", station = station });
        }

        public bool Answer(int station, int choice)
        {
            if (!HasLesson(station) || choice < 0 || choice >= 3) return false;
            if (HasAnswer(station)) return BlacksmithQuestContent.IsCorrect(station, choice);
            attempts[station]++;
            pending.Add(new BlacksmithQuestEvent { action = "answer", station = station, choice = choice });
            if (!BlacksmithQuestContent.IsCorrect(station, choice)) return false;
            completedMask |= 1 << station;
            return true;
        }

        public bool TurnIn()
        {
            if (!HasKey) return false;
            if (!turnedIn)
            {
                turnedIn = true;
                pending.Add(new BlacksmithQuestEvent { action = "complete" });
            }
            return true;
        }

        public void MergeVerified(int lessons, int completed, bool finished, int xp)
        {
            lessonMask |= lessons & 7;
            completedMask |= completed & 7;
            turnedIn |= finished && HasKey;
            verifiedXp = Math.Max(0, xp);
        }

        private static int CountBits(int value) => (value & 1) + ((value >> 1) & 1) + ((value >> 2) & 1);
    }

    /// <summary>Device checkpoints are not authoritative grades or wallet balances.</summary>
    public sealed class BlacksmithQuestStore
    {
        private readonly string directory;
        public BlacksmithQuestStore(string directory) { this.directory = directory; }

        public string FilePath(string user, string classId, string assignment)
        {
            using (var hash = SHA256.Create())
            {
                var key = user + "\n" + classId + "\n" + assignment + "\n" + BlacksmithQuestContent.Version;
                var name = string.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(key)).Select(b => b.ToString("x2")));
                return Path.Combine(directory, name + ".json");
            }
        }

        public BlacksmithQuestState Load(string user, string classId, string assignment)
        {
            var path = FilePath(user, classId, assignment);
            if (File.Exists(path))
            {
                var state = JsonUtility.FromJson<BlacksmithQuestState>(File.ReadAllText(path));
                if (state == null || state.userId != user || state.classId != classId ||
                    state.assignmentId != assignment || state.contentVersion != BlacksmithQuestContent.Version ||
                    state.attempts == null || state.attempts.Length != 3 || state.pending == null)
                    throw new InvalidDataException("The workshop checkpoint cannot be read. It has been preserved.");
                return state;
            }
            return new BlacksmithQuestState { userId = user, classId = classId, assignmentId = assignment };
        }

        public void Save(BlacksmithQuestState state)
        {
            Directory.CreateDirectory(directory);
            var path = FilePath(state.userId, state.classId, state.assignmentId);
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(state, true));
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }
    }
}
