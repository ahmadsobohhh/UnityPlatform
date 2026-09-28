using System;
using System.Threading.Tasks;
using Firebase.Firestore;

namespace ImagineQuest.Student
{
    public interface IStudentDashboardRepository
    {
        Task<StudentDashboardSnapshot> LoadAsync(string userId);
    }

    /// <summary>
    /// Reads identity, the server-owned wallet, and teacher-controlled standing separately.
    /// No rewards, deductions, resets, or client writes are performed by the dashboard.
    /// </summary>
    public sealed class StudentDashboardRepository : IStudentDashboardRepository
    {
        private readonly FirebaseFirestore firestore;

        public StudentDashboardRepository(FirebaseFirestore firestore)
        {
            this.firestore = firestore ?? throw new ArgumentNullException(nameof(firestore));
        }

        public async Task<StudentDashboardSnapshot> LoadAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId) || userId.Contains("/"))
                throw new ArgumentException("A student user ID is required.", nameof(userId));

            var profile = firestore.Collection("users").Document(userId).GetSnapshotAsync(Source.Server);
            var wallet = firestore.Collection("wallets").Document(userId).GetSnapshotAsync(Source.Server);
            var standing = firestore.Collection("studentStanding").Document(userId).GetSnapshotAsync(Source.Server);
            await Task.WhenAll(profile, wallet, standing);

            return StudentDashboardSnapshot.FromDocuments(
                profile.Result.Exists ? profile.Result.ToDictionary() : null,
                wallet.Result.Exists ? wallet.Result.ToDictionary() : null,
                standing.Result.Exists ? standing.Result.ToDictionary() : null);
        }
    }
}
