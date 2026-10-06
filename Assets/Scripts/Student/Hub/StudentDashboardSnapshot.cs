using System;
using System.Collections.Generic;

namespace ImagineQuest.Student
{
    /// <summary>Read-only dashboard data. Economy and classroom standing have separate owners.</summary>
    public sealed class StudentDashboardSnapshot
    {
        public string DisplayName { get; }
        public string AvatarId { get; }
        public string CharacterColor { get; }
        public StudentEconomy Economy { get; }
        public StudentStanding Standing { get; }

        private StudentDashboardSnapshot(string displayName, string avatarId, string characterColor,
            StudentEconomy economy, StudentStanding standing)
        {
            DisplayName = displayName;
            AvatarId = avatarId;
            CharacterColor = characterColor;
            Economy = economy;
            Standing = standing;
        }

        // Missing documents represent a new student; failed reads must never call this factory.
        public static StudentDashboardSnapshot FromDocuments(IDictionary<string, object> profile,
            IDictionary<string, object> wallet, IDictionary<string, object> standing)
        {
            if (profile == null)
                throw new InvalidOperationException("The student profile is missing.");

            string name = Text(profile, "username");
            if (string.IsNullOrWhiteSpace(name))
                name = (Text(profile, "firstName") + " " + Text(profile, "lastName")).Trim();
            if (string.IsNullOrWhiteSpace(name)) name = "Student";

            return new StudentDashboardSnapshot(name, Text(profile, "avatarId"),
                Text(profile, "characterColor"),
                new StudentEconomy(Number(wallet, "level", 1, 1), Number(wallet, "xp", 0),
                    Number(wallet, "crystals", 0), Number(wallet, "gold", 0)),
                new StudentStanding(Number(standing, "hearts", StudentStanding.StartingHearts)));
        }

        private static string Text(IDictionary<string, object> data, string key)
        {
            return data.TryGetValue(key, out object value) && value is string text ? text.Trim() : "";
        }

        private static long Number(IDictionary<string, object> data, string key, long fallback, long minimum = 0)
        {
            if (data == null || !data.TryGetValue(key, out object value)) return fallback;
            // Firestore integers are Int64. Reject malformed data rather than show a fake balance.
            long number;
            if (value is long longValue) number = longValue;
            else if (value is int intValue) number = intValue;
            else throw new InvalidOperationException("Invalid student status field: " + key);
            if (number < minimum) throw new InvalidOperationException("Invalid student status field: " + key);
            return number;
        }
    }

    public sealed class StudentEconomy
    {
        public long Level { get; }
        public long Xp { get; }
        public long Crystals { get; }
        public long Gold { get; }

        internal StudentEconomy(long level, long xp, long crystals, long gold)
        {
            Level = level;
            Xp = xp;
            Crystals = crystals;
            Gold = gold;
        }
    }

    public sealed class StudentStanding
    {
        public const long StartingHearts = 50;
        public long Hearts { get; }

        internal StudentStanding(long hearts) { Hearts = hearts; }
    }
}
