using System;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Systems.Upgrades;

namespace QuackQuack.Tests.EditMode
{
    public class ExperienceStateTests
    {
        [TestCase(0, 1, 0, 10, 0d)]
        [TestCase(5, 1, 0, 10, 0.5d)]
        [TestCase(10, 2, 10, 30, 0d)]
        [TestCase(25, 2, 10, 30, 0.75d)]
        [TestCase(30, 3, 30, 60, 0d)]
        [TestCase(45, 3, 30, 60, 0.5d)]
        [TestCase(60, 4, 60, 60, 1d)]
        [TestCase(99, 4, 60, 60, 1d)]
        public void ProgressUsesCurrentAuthoredIntervalAndFillsAtLastThreshold(int xp, int level, int previous, int next, double fraction)
        {
            var state = new ExperienceState(new[] { 10, 30, 60 });
            if (xp > 0) Assert.That(state.TryAdd(xp, out _), Is.True);
            AssertProgress(Read(state, "Progress"), level, xp, previous, next, fraction);
        }

        [Test]
        public void EmptyThresholdCurveIsAlreadyCompleteWithoutDivisionByZero()
        {
            var state = new ExperienceState(Array.Empty<int>());
            AssertProgress(Read(state, "Progress"), 1, 0, 0, 0, 1);
            state.TryAdd(7, out _);
            AssertProgress(Read(state, "Progress"), 1, 7, 0, 0, 1);
        }

        [Test]
        public void ProgressSnapshotCopiesValuesAndNeverUsesMutatedAuthoringArray()
        {
            var thresholds = new[] { 10, 30, 60 }; var state = new ExperienceState(thresholds);
            state.TryAdd(5, out _); object before = Read(state, "Progress");
            Assert.That(before.GetType().FullName, Is.EqualTo("QuackQuack.Core.Shared.ProgressSnapshot"));
            Assert.That(before.GetType().IsValueType, Is.True, "A snapshot must not become a live mutable state reference.");
            thresholds[0] = 1000; state.TryAdd(30, out int gained);
            Assert.That(gained, Is.EqualTo(2));
            AssertProgress(before, 1, 5, 0, 10, 0.5);
            AssertProgress(Read(state, "Progress"), 3, 35, 30, 60, 1d / 6);
        }

        [Test]
        public void RejectedRewardsLeaveEveryProgressValueUnchangedIncludingAtIntegerLimit()
        {
            var state = new ExperienceState(new[] { 10, int.MaxValue });
            state.TryAdd(5, out _); object before = Read(state, "Progress");
            foreach (int invalid in new[] { 0, -1, int.MinValue })
            { Assert.That(state.TryAdd(invalid, out _), Is.False); Assert.That(Read(state, "Progress"), Is.EqualTo(before)); }
            state.TryAdd(int.MaxValue - 5, out _);
            AssertProgress(Read(state, "Progress"), 3, int.MaxValue, int.MaxValue, int.MaxValue, 1);
            before = Read(state, "Progress"); Assert.That(state.TryAdd(1, out _), Is.False);
            Assert.That(Read(state, "Progress"), Is.EqualTo(before));
        }

        [Test]
        public void UpgradeProjectionReflectsAllGainedLevelsBeforeMandatoryOffersResolve()
        {
            var run = new RunState(33); run.TryTransition(RunPhase.Playing);
            using (var system = new UpgradeSystem(33))
            {
                system.InitializeProgression(run, new[] { 10, 30, 60 }, new[] { "choice" });
                system.TryCollectExperience(35);
                Assert.That(run.Phase, Is.EqualTo(RunPhase.ChoosingUpgrade));
                AssertProgress(Read(system, "Progress"), 3, 35, 30, 60, 1d / 6);
                Assert.That(system.TryConfirm(system.CurrentOffer.OfferId, "choice"), Is.True);
                AssertProgress(Read(system, "Progress"), 3, 35, 30, 60, 1d / 6);
                Assert.That(system.TryConfirm(system.CurrentOffer.OfferId, "choice"), Is.True);
                Assert.That(run.Phase, Is.EqualTo(RunPhase.Playing));
                AssertProgress(Read(system, "Progress"), 3, 35, 30, 60, 1d / 6);
            }
        }

        // Reflection keeps the red contract discoverable before T034 adds the production snapshot.
        private static object Read(object target, string name)
        {
            var type = target.GetType();
            var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property != null) return property.GetValue(target);
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(field, Is.Not.Null, "T034 missing public projection: " + type.Name + "." + name);
            return field.GetValue(target);
        }
        private static void AssertProgress(object snapshot, int level, int xp, int previous, int next, double fraction)
        {
            Assert.That(Convert.ToInt32(Read(snapshot, "Level")), Is.EqualTo(level));
            Assert.That(Convert.ToInt32(Read(snapshot, "Experience")), Is.EqualTo(xp));
            Assert.That(Convert.ToInt32(Read(snapshot, "PreviousThreshold")), Is.EqualTo(previous));
            Assert.That(Convert.ToInt32(Read(snapshot, "NextThreshold")), Is.EqualTo(next));
            double value = Convert.ToDouble(Read(snapshot, "Fraction"));
            Assert.That(double.IsNaN(value) || double.IsInfinity(value), Is.False);
            Assert.That(value, Is.InRange(0d, 1d)); Assert.That(value, Is.EqualTo(fraction).Within(0.000001));
        }

        [Test]
        public void CumulativeThresholdsAwardEachLevelOnceAndCopyAuthoredInput()
        {
            var thresholds = new[] { 10, 20, 30 };
            var state = new ExperienceState(thresholds);
            thresholds[0] = 1;
            Assert.That(state.TryAdd(9, out int gained), Is.True);
            Assert.That(gained, Is.Zero);
            Assert.That(state.Level, Is.EqualTo(1));
            Assert.That(state.TryAdd(11, out gained), Is.True);
            Assert.That(gained, Is.EqualTo(2));
            Assert.That(state.Experience, Is.EqualTo(20));
            Assert.That(state.Level, Is.EqualTo(3));
            Assert.That(state.TryAdd(20, out gained), Is.True);
            Assert.That(gained, Is.EqualTo(1));
            Assert.That(state.Level, Is.EqualTo(4));
            Assert.That(state.TryAdd(1, out gained), Is.True);
            Assert.That(gained, Is.Zero);
        }

        [Test]
        public void InvalidThresholdsAreRejectedBeforeStateExists()
        {
            Assert.Throws<ArgumentNullException>(() => new ExperienceState(null));
            foreach (var values in new[] { new[] { 0 }, new[] { -1 }, new[] { 10, 10 }, new[] { 20, 10 } })
                Assert.Throws<ArgumentException>(() => new ExperienceState(values));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void InvalidRewardsDoNotChangeExperienceOrLevel(int amount)
        {
            var state = new ExperienceState(new[] { 10 });
            state.TryAdd(5, out _);
            Assert.That(state.TryAdd(amount, out int gained), Is.False);
            Assert.That(gained, Is.Zero);
            Assert.That(state.Experience, Is.EqualTo(5));
            Assert.That(state.Level, Is.EqualTo(1));
        }

        [Test]
        public void OverflowIsRejectedWithoutWrappingOrRegrantingLevels()
        {
            var state = new ExperienceState(new[] { 10 });
            Assert.That(state.TryAdd(int.MaxValue, out int gained), Is.True);
            Assert.That(gained, Is.EqualTo(1));
            Assert.That(state.TryAdd(1, out gained), Is.False);
            Assert.That(gained, Is.Zero);
            Assert.That(state.Experience, Is.EqualTo(int.MaxValue));
            Assert.That(state.Level, Is.EqualTo(2));
        }

        [Test]
        public void ExhaustedChoicesConsumePendingLevelsWithoutLeavingRunFrozen()
        {
            var run = new RunState(42);
            run.TryTransition(RunPhase.Playing);
            using (var system = new UpgradeSystem(42))
            {
                system.InitializeProgression(run, new[] { 10, 20 }, Array.Empty<string>());
                Assert.That(system.TryCollectExperience(20), Is.True);
                Assert.That(system.Level, Is.EqualTo(3));
                Assert.That(system.PendingLevels, Is.Zero);
                Assert.That(system.CurrentOffer, Is.Null);
                Assert.That(run.Phase, Is.EqualTo(RunPhase.Playing));
            }
        }

        [Test]
        public void ProgressionInitializationIsAtomicAndCannotResetAwardedExperience()
        {
            var run = new RunState(42);
            run.TryTransition(RunPhase.Playing);
            using (var system = new UpgradeSystem(42))
            {
                Assert.Throws<ArgumentException>(() => system.InitializeProgression(run, new[] { 0 }, new[] { "a" }));
                system.InitializeProgression(run, new[] { 10 }, new[] { "a" });
                system.TryCollectExperience(4);
                Assert.Throws<InvalidOperationException>(() => system.InitializeProgression(run, new[] { 10 }, new[] { "a" }));
                Assert.That(system.Experience, Is.EqualTo(4));
                system.Dispose();
                Assert.That(system.TryCollectExperience(10), Is.False);
                Assert.That(system.Experience, Is.EqualTo(4));
            }
        }
    }
}
