using System;
using System.IO;
using ImagineQuest.Gameplay;
using NUnit.Framework;

public class BlacksmithWorkshopTests
{
    [Test]
    public void AnswerRequiresLessonAndCorrectChoice()
    {
        var state = new BlacksmithQuestState();
        Assert.That(state.Answer(0, 0), Is.False);
        state.ReadLesson(0);
        Assert.That(state.Answer(0, 2), Is.False);
        Assert.That(state.HasKey, Is.False);
        Assert.That(state.Answer(0, 0), Is.True);
        Assert.That(state.attempts[0], Is.EqualTo(2));
        Assert.That(state.CompletedCount, Is.EqualTo(1));
    }

    [Test]
    public void KeyRequiresEveryCraftAndTurnInIsIdempotent()
    {
        var state = new BlacksmithQuestState();
        Assert.That(state.TurnIn(), Is.False);
        foreach (int station in new[] { 2, 0, 1 })
        {
            state.ReadLesson(station);
            state.Answer(station, station);
        }
        Assert.That(state.HasKey, Is.True);
        Assert.That(state.CompletedCount, Is.EqualTo(3));
        Assert.That(state.TurnIn(), Is.False, "Completing crafts alone must not skip the Forge Stone.");
        Assert.That(state.TryOpenPortal(), Is.True);
        Assert.That(state.TurnIn(), Is.True);
        int events = state.pending.Count;
        Assert.That(state.TurnIn(), Is.True);
        state.ReadLesson(0);
        state.Answer(0, 0);
        Assert.That(state.pending.Count, Is.EqualTo(events));
        Assert.That(state.verifiedXp, Is.Zero, "Local completion must not award wallet XP.");
    }

    [Test]
    public void CheckpointRestoresProgressAndSeparatesStudentsAndAssignments()
    {
        string directory = Path.Combine(Path.GetTempPath(), "WorkshopTests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new BlacksmithQuestStore(directory);
            var state = store.Load("student-a", "class", "assignment-a");
            state.ReadLesson(1); state.Answer(1, 1); state.introSeen = true;
            store.Save(state);
            var restored = store.Load("student-a", "class", "assignment-a");
            Assert.That(restored.HasAnswer(1), Is.True);
            Assert.That(restored.introSeen, Is.True);
            Assert.That(restored.pending.Count, Is.EqualTo(2));
            Assert.That(store.Load("student-b", "class", "assignment-a").CompletedCount, Is.Zero);
            Assert.That(store.Load("student-a", "class", "assignment-b").CompletedCount, Is.Zero);
            restored.ReadLesson(2); store.Save(restored);
            Assert.That(store.Load("student-a", "class", "assignment-a").HasLesson(2), Is.True);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Test]
    public void PortalActivationIsSavedWithoutCreatingRewardEvents()
    {
        var state = new BlacksmithQuestState { userId = "portal-student", classId = "portal-class" };
        Assert.That(state.TryOpenPortal(), Is.False);
        for (int i = 0; i < 3; i++) { state.ReadLesson(i); state.Answer(i, i); }
        int events = state.pending.Count;
        Assert.That(state.TryOpenPortal(), Is.True);
        Assert.That(state.TryOpenPortal(), Is.True);
        Assert.That(state.pending.Count, Is.EqualTo(events));
        string directory = Path.Combine(Path.GetTempPath(), "PortalTests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new BlacksmithQuestStore(directory);
            store.Save(state);
            Assert.That(store.Load("portal-student", "portal-class", BlacksmithQuestContent.Id).portalOpened, Is.True);
            Assert.That(state.verifiedXp, Is.Zero);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Test]
    public void CorruptCheckpointIsPreserved()
    {
        string directory = Path.Combine(Path.GetTempPath(), "WorkshopTests-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(directory);
            var store = new BlacksmithQuestStore(directory);
            string path = store.FilePath("a", "b", "c");
            File.WriteAllText(path, "{}");
            Assert.Throws<InvalidDataException>(() => store.Load("a", "b", "c"));
            Assert.That(File.ReadAllText(path), Is.EqualTo("{}"));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [TestCase(-1, 0)]
    [TestCase(3, 0)]
    [TestCase(0, -1)]
    [TestCase(0, 3)]
    public void InvalidAnswersCannotUnlockCraft(int station, int choice)
    {
        var state = new BlacksmithQuestState();
        state.ReadLesson(0);
        Assert.That(state.Answer(station, choice), Is.False);
        Assert.That(state.CompletedCount, Is.Zero);
    }
}
