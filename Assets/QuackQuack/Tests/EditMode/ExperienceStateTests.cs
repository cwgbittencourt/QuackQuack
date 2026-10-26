using System;
using NUnit.Framework;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Systems.Upgrades;

namespace QuackQuack.Tests.EditMode
{
    public class ExperienceStateTests
    {
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
