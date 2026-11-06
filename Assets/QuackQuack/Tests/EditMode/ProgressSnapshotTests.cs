using System;
using NUnit.Framework;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Core.Shared;
using QuackQuack.Systems.Upgrades;

namespace QuackQuack.Tests.EditMode
{
    public class ProgressSnapshotTests
    {
        [Test]
        public void DefaultAndUninitializedProgressStaySafeWithoutInventingAnAuthoredCurve()
        {
            ProgressSnapshot empty = default;
            Assert.That(empty.Level, Is.EqualTo(1)); Assert.That(empty.Experience, Is.Zero);
            Assert.That(empty.PreviousThreshold, Is.Zero); Assert.That(empty.NextThreshold, Is.Zero);
            Assert.That(empty.Fraction, Is.Zero);
            using (var system = new UpgradeSystem(34))
            {
                Assert.That(system.Progress, Is.EqualTo(empty));
                var run = new RunState(34); run.TryTransition(RunPhase.Playing);
                system.InitializeProgression(run, Array.Empty<int>(), Array.Empty<string>());
                Assert.That(system.Progress.Level, Is.EqualTo(1));
                Assert.That(system.Progress.Fraction, Is.EqualTo(1), "An initialized curve with no remaining thresholds is complete.");
            }
        }

        [Test]
        public void HudTransportsProgressByValueAndNormalizesResourcesWithoutChangingRawData()
        {
            var state = new ExperienceState(new[] { 10, 30, 60 }); state.TryAdd(35, out _);
            var hud = new HudSnapshot(150, 200, 70, 140, "land", "paused", 9.5, true, "ready", state.Progress);
            state.TryAdd(25, out _);
            Assert.That(hud.Progress.Level, Is.EqualTo(3)); Assert.That(hud.Progress.Experience, Is.EqualTo(35));
            Assert.That(hud.Progress.Fraction, Is.EqualTo(1f / 6).Within(0.000001));
            Assert.That(state.Progress.Fraction, Is.EqualTo(1));
            Assert.That(hud.Health01, Is.EqualTo(0.75f)); Assert.That(hud.Breath01, Is.EqualTo(0.5f));
            Assert.That(hud.Health, Is.EqualTo(150)); Assert.That(hud.MaxHealth, Is.EqualTo(200));
            Assert.That(hud.Breath, Is.EqualTo(70)); Assert.That(hud.Capacity, Is.EqualTo(140));
            Assert.That(hud.Terrain, Is.EqualTo("land")); Assert.That(hud.Status, Is.EqualTo("paused"));
            Assert.That(hud.Time, Is.EqualTo(9.5)); Assert.That(hud.QuackReady, Is.True); Assert.That(hud.QuackState, Is.EqualTo("ready"));
        }

        [TestCase(float.NaN, 100f)] [TestCase(float.PositiveInfinity, 100f)] [TestCase(float.NegativeInfinity, 100f)]
        [TestCase(1f, float.NaN)] [TestCase(1f, float.PositiveInfinity)] [TestCase(1f, float.NegativeInfinity)]
        [TestCase(1f, 0f)] [TestCase(1f, -1f)]
        public void InvalidResourceReadingsCannotProduceNonFiniteFill(float current, float maximum)
        {
            var hud = new HudSnapshot(current, maximum, current, maximum, "", "", 0);
            Assert.That(hud.Health01, Is.Zero); Assert.That(hud.Breath01, Is.Zero);
        }

        [Test]
        public void ExtremeFiniteResourceReadingsClampWithoutOverflow()
        {
            var hud = new HudSnapshot(float.MaxValue, float.Epsilon, -float.MaxValue, float.Epsilon, "", "", 0);
            Assert.That(hud.Health01, Is.EqualTo(1)); Assert.That(hud.Breath01, Is.Zero);
        }

        [Test]
        public void WarmedProgressAndResourceProjectionReadsDoNotAllocateManagedMemory()
        {
            var state = new ExperienceState(new[] { 10, 30, 60 }); state.TryAdd(35, out _);
            var hud = new HudSnapshot(150, 200, 70, 140, "", "", 0, progress: state.Progress);
            double sum = 0;
            for (int i = 0; i < 100; i++) sum += state.Progress.Fraction + hud.Health01 + hud.Breath01;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) sum += state.Progress.Fraction + hud.Health01 + hud.Breath01;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(sum, Is.GreaterThan(0)); Assert.That(allocated, Is.Zero);
        }
    }
}
