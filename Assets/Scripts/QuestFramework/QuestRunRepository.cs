using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;

namespace ImagineQuest.QuestFramework
{
    /// <summary>
    /// Small persistence seam so gameplay can be tested with an in-memory repository and
    /// production builds can use class- and assignment-scoped Firestore documents.
    /// </summary>
    public interface IQuestRunRepository
    {
        Task SaveRunAsync(QuestRunState state);
        Task SaveAttemptEventAsync(QuestRunState state, QuestChallengeAttemptRecord attempt);
        Task<QuestRunState> LoadRunAsync(string classId, string assignmentId, string runId);
    }

    /// <summary>
    /// Firebase implementation.  It writes only the signed-in learner's own run and
    /// event records.  Firestore rules and Cloud Functions remain responsible for access
    /// control and for calculating canonical wallet rewards.
    /// </summary>
    public sealed class FirebaseQuestRunRepository : IQuestRunRepository
    {
        private readonly FirebaseFirestore firestore;
        private readonly Func<string> currentUserIdProvider;

        public FirebaseQuestRunRepository(FirebaseFirestore firestore = null, Func<string> currentUserIdProvider = null)
        {
            this.firestore = firestore ?? FirebaseFirestore.DefaultInstance;
            this.currentUserIdProvider = currentUserIdProvider ?? GetCurrentUserId;
        }

        public Task SaveRunAsync(QuestRunState state)
        {
            ValidateState(state);
            return GetRunDocument(state).SetAsync(state.ToFirestoreDocument(), SetOptions.MergeAll);
        }

        public Task SaveAttemptEventAsync(QuestRunState state, QuestChallengeAttemptRecord attempt)
        {
            ValidateState(state);
            if (attempt == null)
                throw new ArgumentNullException(nameof(attempt));
            if (string.IsNullOrWhiteSpace(attempt.EventId))
                throw new ArgumentException("Attempt events require an event ID.", nameof(attempt));

            var payload = attempt.ToFirestoreDocument();
            payload["runId"] = state.RunId;
            payload["assignmentId"] = state.AssignmentId;
            payload["classId"] = state.ClassId;
            payload["questId"] = state.QuestId;

            return GetRunDocument(state).Collection("events").Document(attempt.EventId)
                .SetAsync(payload, SetOptions.MergeAll);
        }

        public async Task<QuestRunState> LoadRunAsync(string classId, string assignmentId, string runId)
        {
            if (string.IsNullOrWhiteSpace(classId))
                throw new ArgumentException("A class ID is required.", nameof(classId));
            if (string.IsNullOrWhiteSpace(assignmentId))
                throw new ArgumentException("An assignment ID is required.", nameof(assignmentId));
            if (string.IsNullOrWhiteSpace(runId))
                throw new ArgumentException("A run ID is required.", nameof(runId));

            var snapshot = await GetAssignmentDocument(classId, assignmentId).Collection("runs").Document(runId).GetSnapshotAsync();
            return snapshot.Exists ? QuestRunState.FromFirestoreDocument(snapshot.ToDictionary()) : null;
        }

        private DocumentReference GetRunDocument(QuestRunState state)
        {
            return GetAssignmentDocument(state.ClassId, state.AssignmentId).Collection("runs").Document(state.RunId);
        }

        private DocumentReference GetAssignmentDocument(string classId, string assignmentId)
        {
            var userId = currentUserIdProvider();
            if (string.IsNullOrWhiteSpace(userId))
                throw new InvalidOperationException("A signed-in learner is required to save quest progress.");

            return firestore.Collection("users").Document(userId)
                .Collection("classProgress").Document(classId)
                .Collection("assignments").Document(assignmentId);
        }

        private static string GetCurrentUserId()
        {
            return FirebaseAuth.DefaultInstance.CurrentUser == null
                ? string.Empty
                : FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        }

        private static void ValidateState(QuestRunState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (string.IsNullOrWhiteSpace(state.RunId))
                throw new ArgumentException("Quest runs require a run ID.", nameof(state));
            if (string.IsNullOrWhiteSpace(state.ClassId))
                throw new ArgumentException("Quest runs require a class ID.", nameof(state));
            if (string.IsNullOrWhiteSpace(state.AssignmentId))
                throw new ArgumentException("Quest runs require an assignment ID.", nameof(state));
        }
    }

    /// <summary>
    /// Useful for editor play mode, offline prototypes, and tests.  It mirrors the same
    /// repository contract without creating a Firebase dependency at the call site.
    /// </summary>
    public sealed class InMemoryQuestRunRepository : IQuestRunRepository
    {
        private readonly Dictionary<string, QuestRunState> runs = new Dictionary<string, QuestRunState>();

        public Task SaveRunAsync(QuestRunState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            runs[BuildKey(state.ClassId, state.AssignmentId, state.RunId)] = state;
            return Task.CompletedTask;
        }

        public Task SaveAttemptEventAsync(QuestRunState state, QuestChallengeAttemptRecord attempt)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (attempt == null)
                throw new ArgumentNullException(nameof(attempt));
            return Task.CompletedTask;
        }

        public Task<QuestRunState> LoadRunAsync(string classId, string assignmentId, string runId)
        {
            runs.TryGetValue(BuildKey(classId, assignmentId, runId), out var state);
            return Task.FromResult(state);
        }

        private static string BuildKey(string classId, string assignmentId, string runId)
        {
            return (classId ?? string.Empty) + "/" + (assignmentId ?? string.Empty) + "/" + (runId ?? string.Empty);
        }
    }

    internal static class QuestFirestoreValue
    {
        internal static string String(IDictionary<string, object> document, string fieldName, string fallback = "")
        {
            if (!TryGetValue(document, fieldName, out var value) || value == null)
                return fallback;

            return value as string ?? Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback;
        }

        internal static int Int(IDictionary<string, object> document, string fieldName, int fallback = 0)
        {
            if (!TryGetValue(document, fieldName, out var value) || value == null)
                return fallback;

            switch (value)
            {
                case int integer:
                    return integer;
                case long longInteger:
                    return longInteger > int.MaxValue ? int.MaxValue : longInteger < int.MinValue ? int.MinValue : (int)longInteger;
                case double decimalValue:
                    return decimalValue > int.MaxValue ? int.MaxValue : decimalValue < int.MinValue ? int.MinValue : (int)decimalValue;
                case float floatValue:
                    return floatValue > int.MaxValue ? int.MaxValue : floatValue < int.MinValue ? int.MinValue : (int)floatValue;
                default:
                    return int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
            }
        }

        internal static bool Bool(IDictionary<string, object> document, string fieldName, bool fallback = false)
        {
            if (!TryGetValue(document, fieldName, out var value) || value == null)
                return fallback;

            if (value is bool boolean)
                return boolean;

            return bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) ? parsed : fallback;
        }

        internal static long UnixSeconds(IDictionary<string, object> document, string fieldName)
        {
            if (!TryGetValue(document, fieldName, out var value) || value == null)
                return 0;

            return ToUnixSeconds(value);
        }

        internal static long ToUnixSeconds(DateTime utcTime)
        {
            return (long)(utcTime.ToUniversalTime() - DateTime.UnixEpoch).TotalSeconds;
        }

        internal static long ToUnixSeconds(object value)
        {
            switch (value)
            {
                case Timestamp timestamp:
                    return ToUnixSeconds(timestamp.ToDateTime());
                case DateTime dateTime:
                    return ToUnixSeconds(dateTime);
                case long longValue:
                    return longValue;
                case int intValue:
                    return intValue;
                case double doubleValue:
                    return (long)doubleValue;
                case float floatValue:
                    return (long)floatValue;
                default:
                    return long.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
            }
        }

        internal static T Enum<T>(IDictionary<string, object> document, string fieldName, T fallback) where T : struct
        {
            var raw = String(document, fieldName);
            return System.Enum.TryParse(raw, true, out T parsed) ? parsed : fallback;
        }

        internal static List<string> StringList(IDictionary<string, object> document, string fieldName)
        {
            var result = new List<string>();
            var values = ObjectList(document, fieldName);
            for (var index = 0; index < values.Count; index++)
            {
                var value = values[index];
                if (value == null)
                    continue;

                var text = value as string ?? Convert.ToString(value, CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(text))
                    result.Add(text);
            }
            return result;
        }

        internal static List<object> ObjectList(IDictionary<string, object> document, string fieldName)
        {
            var result = new List<object>();
            if (!TryGetValue(document, fieldName, out var value) || value == null)
                return result;

            if (value is IEnumerable<object> enumerable)
            {
                foreach (var item in enumerable)
                    result.Add(item);
            }
            return result;
        }

        private static bool TryGetValue(IDictionary<string, object> document, string fieldName, out object value)
        {
            value = null;
            return document != null && !string.IsNullOrWhiteSpace(fieldName) && document.TryGetValue(fieldName, out value);
        }
    }
}
