using System;
using System.Collections.Generic;
using NUnit.Framework;
using QuackQuack.Content.Encounters;
using QuackQuack.Core.Run;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.EditMode
{
    public class EncounterDefinitionTests
    {
        private readonly List<EncounterDefinition> created = new List<EncounterDefinition>();
        private readonly string[] known = { "ant", "libelula-alfa", "sapo-cururu", "sapo-rei" };
        private EncounterDefinition[] timeline;

        [SetUp]
        public void SetUp() => timeline = new[] {
            Make("wave-a", EncounterKind.Wave, 0, 300, "ant"),
            Make("wave-b", EncounterKind.Wave, 300, 600, "ant"),
            Make("wave-c", EncounterKind.Wave, 600, 900, "ant"),
            Make("alpha", EncounterKind.AlphaDragonfly, 300, 300, "libelula-alfa"),
            Make("cururu", EncounterKind.CururuToad, 600, 600, "sapo-cururu"),
            Make("king", EncounterKind.FrogKing, 900, 900, "sapo-rei") };

        [TearDown]
        public void Cleanup() { foreach (var value in created) Object.DestroyImmediate(value); created.Clear(); }

        [Test]
        public void ValidTimelineConnectsAuthoredWavesAndMilestonesInOrderWithoutMutatingAssets()
        {
            Assert.That(Valid(out string reason), Is.True, reason);
            string before = EditorJsonUtility.ToJson(timeline[0]);
            var run = new RunState(51);
            Array.Reverse(timeline); // Authoring order must not change request order.
            var schedule = EncounterDefinition.CreateSchedule(run, timeline, known);
            run.TryTransition(RunPhase.Playing);
            CollectionAssert.AreEqual(new[] { "wave-a" }, schedule.CollectDue());
            run.Advance(300);
            CollectionAssert.AreEqual(new[] { "wave-b", "alpha" }, schedule.CollectDue());
            run.Advance(300);
            CollectionAssert.AreEqual(new[] { "wave-c", "cururu" }, schedule.CollectDue());
            run.Advance(300);
            CollectionAssert.AreEqual(new[] { "king" }, schedule.CollectDue());
            Assert.That(schedule.CollectDue(), Is.Empty);
            Assert.That(EditorJsonUtility.ToJson(timeline[5]), Is.EqualTo(before));
        }

        [TestCase(0, "startSeconds", 1d)]
        [TestCase(1, "startSeconds", 301d)]
        [TestCase(2, "endSeconds", 899d)]
        public void InitialInternalAndFinalProgressionGapsAreRejected(int index, string field, double time)
        {
            Set(timeline[index], field, p => p.doubleValue = time);
            Assert.That(Valid(out _), Is.False);
        }

        [Test]
        public void OverlappingWavesCanCoverTimelineWithoutGap()
        {
            Set(timeline[0], "endSeconds", p => p.doubleValue = 400);
            Assert.That(Valid(out _), Is.True);
        }

        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void EveryMilestoneIsRequiredExactlyOnceAtItsMinute(int index)
        {
            var missing = new List<EncounterDefinition>(timeline); missing.RemoveAt(index);
            Assert.That(EncounterDefinition.IsValidTimeline(missing, known, out _), Is.False);
            Set(timeline[index], "startSeconds", p => p.doubleValue += 1);
            Assert.That(Valid(out _), Is.False);
        }

        [Test]
        public void DuplicateMilestoneAndDuplicateIdAreRejected()
        {
            var extra = Make("extra", EncounterKind.FrogKing, 900, 900, "sapo-rei");
            var duplicate = new List<EncounterDefinition>(timeline) { extra };
            Assert.That(EncounterDefinition.IsValidTimeline(duplicate, known, out _), Is.False);
            Set(timeline[1], "id", p => p.stringValue = timeline[0].Id);
            Assert.That(Valid(out _), Is.False);
        }

        [Test]
        public void MissingUnknownOrEmptyReferencesAreRejected()
        {
            Assert.That(EncounterDefinition.IsValidTimeline(null, known, out _), Is.False);
            Assert.That(EncounterDefinition.IsValidTimeline(Array.Empty<EncounterDefinition>(), known, out _), Is.False);
            Assert.That(EncounterDefinition.IsValidTimeline(timeline, null, out _), Is.False);
            Assert.That(EncounterDefinition.IsValidTimeline(new EncounterDefinition[] { null }, known, out _), Is.False);
            Assert.That(EncounterDefinition.IsValidTimeline(timeline, new[] { "ant" }, out _), Is.False);
        }

        [TestCase("startSeconds")]
        [TestCase("endSeconds")]
        public void WindowsRejectNonfiniteNegativeAndOutOfRunValues(string field)
        {
            foreach (double value in new[] { -1d, 901d, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            { Set(timeline[0], field, p => p.doubleValue = value); Assert.That(timeline[0].IsValid(out _), Is.False); }
        }

        [TestCase("spawnInterval")]
        [TestCase("healthMultiplier")]
        [TestCase("damageMultiplier")]
        public void SpawnAndModifiersMustBePositiveFinite(string field)
        {
            foreach (float value in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            { Set(timeline[0], field, p => p.floatValue = value); Assert.That(timeline[0].IsValid(out _), Is.False); }
        }

        [Test]
        public void CompositionRejectsEmptyMismatchedDuplicateAndOversizedBatches()
        {
            var wave = timeline[0];
            Set(wave, "enemyIds", p => p.arraySize = 0); Assert.That(wave.IsValid(out _), Is.False);
            Set(wave, "enemyIds", p => { p.arraySize = 2; p.GetArrayElementAtIndex(0).stringValue = "ant"; p.GetArrayElementAtIndex(1).stringValue = "ant"; });
            Set(wave, "quantities", p => { p.arraySize = 2; p.GetArrayElementAtIndex(0).intValue = 1; p.GetArrayElementAtIndex(1).intValue = 1; });
            Assert.That(wave.IsValid(out _), Is.False);
            Set(wave, "enemyIds", p => p.arraySize = 1);
            Assert.That(wave.IsValid(out _), Is.False); // Length mismatch.
            Set(wave, "quantities", p => { p.arraySize = 1; p.GetArrayElementAtIndex(0).intValue = int.MaxValue; });
            Assert.That(wave.IsValid(out _), Is.False);
            Set(wave, "quantities", p => p.GetArrayElementAtIndex(0).intValue = 0);
            Assert.That(wave.IsValid(out _), Is.False);
        }

        [Test]
        public void MilestoneIsSingleTargetAndCollectionsAreImmutableSnapshots()
        {
            var boss = timeline[5];
            Assert.That(boss.Completion, Is.EqualTo(EncounterCompletion.TargetDefeated));
            Assert.That(timeline[0].Completion, Is.EqualTo(EncounterCompletion.WindowElapsed));
            var snapshot = boss.EnemyIds;
            Assert.Throws<NotSupportedException>(() => ((IList<string>)snapshot)[0] = "changed");
            Set(boss, "quantities", p => p.GetArrayElementAtIndex(0).intValue = 2);
            Assert.That(boss.IsValid(out _), Is.False);
            CollectionAssert.AreEqual(new[] { "sapo-rei" }, snapshot);
        }

        [Test]
        public void MilestoneCannotSubstituteAnotherKnownEnemyForItsTarget()
        {
            foreach (int index in new[] { 3, 4, 5 })
            {
                Set(timeline[index], "enemyIds", p => p.GetArrayElementAtIndex(0).stringValue = "ant");
                Assert.That(timeline[index].IsValid(out _), Is.False);
            }
        }

        [Test]
        public void InvalidTimelineCannotCreateSchedule()
        {
            Set(timeline[0], "arenaId", p => p.stringValue = "other");
            Assert.That(Valid(out _), Is.False);
            Assert.Throws<ArgumentException>(() => EncounterDefinition.CreateSchedule(new RunState(51), timeline, known));
        }

        [Test]
        public void CoreScheduleRejectsInvalidIdsTimesOrDependencies()
        {
            var run = new RunState(51);
            Assert.Throws<ArgumentNullException>(() => new EncounterSchedule(null, new[] { "a" }, new[] { 1d }));
            Assert.Throws<ArgumentException>(() => new EncounterSchedule(run, null, new[] { 1d }));
            Assert.Throws<ArgumentException>(() => new EncounterSchedule(run, new[] { "a" }, Array.Empty<double>()));
            Assert.Throws<ArgumentException>(() => new EncounterSchedule(run, new[] { "a", "a" }, new[] { 1d, 2d }));
            Assert.Throws<ArgumentException>(() => new EncounterSchedule(run, new[] { " " }, new[] { 1d }));
            Assert.Throws<ArgumentException>(() => new EncounterSchedule(run, new[] { "a", "b" }, new[] { 2d, 1d }));
            foreach (double time in new[] { -1d, double.NaN, double.PositiveInfinity })
                Assert.Throws<ArgumentException>(() => new EncounterSchedule(run, new[] { "a" }, new[] { time }));
        }

        private bool Valid(out string reason) => EncounterDefinition.IsValidTimeline(timeline, known, out reason);
        private EncounterDefinition Make(string id, EncounterKind kind, double start, double end, string enemy)
        {
            var value = ScriptableObject.CreateInstance<EncounterDefinition>(); created.Add(value);
            Set(value, "id", p => p.stringValue = id); Set(value, "displayName", p => p.stringValue = id);
            Set(value, "kind", p => p.intValue = (int)kind);
            Set(value, "startSeconds", p => p.doubleValue = start); Set(value, "endSeconds", p => p.doubleValue = end);
            Set(value, "enemyIds", p => { p.arraySize = 1; p.GetArrayElementAtIndex(0).stringValue = enemy; });
            Set(value, "quantities", p => { p.arraySize = 1; p.GetArrayElementAtIndex(0).intValue = 1; });
            return value;
        }
        private static void Set(EncounterDefinition value, string name, Action<SerializedProperty> change)
        { var serialized = new SerializedObject(value); change(serialized.FindProperty(name)); serialized.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
