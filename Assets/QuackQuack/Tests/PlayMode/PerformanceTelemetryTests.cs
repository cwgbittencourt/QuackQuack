using System;
using System.Collections;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using QuackQuack.Gameplay.Enemies;
using QuackQuack.Gameplay.Pickups;
using QuackQuack.Systems.Diagnostics;
using QuackQuack.Systems.Pooling;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.PlayMode
{
    [Category("PerformanceTelemetryRuntime")]
    public class PerformanceTelemetryTests
    {
        private GameObject root;
        private PoolService pools;
        private EnemyUpdateScheduler scheduler;
        private PerformanceTelemetry telemetry;
        [SetUp]
        public void SetUp()
        {
            root = new GameObject("T072 telemetry fixture");
            scheduler = root.AddComponent<EnemyUpdateScheduler>(); scheduler.AutomaticTick = false;
            pools = new PoolService();
            ExperiencePickup.RegisterPool(pools, "probe", 4, root.transform);
            telemetry = root.AddComponent<PerformanceTelemetry>(); telemetry.AutomaticSampling = false;
            telemetry.Bind(pools, scheduler, capacity: 4);
        }
        [TearDown]
        public void Cleanup() { Object.DestroyImmediate(root); pools.Dispose(); }

        [Test]
        public void PoolsExposeOccupancyCapacityAndExhaustionWithoutChangingLeaseBehavior()
        {
            var items = new ExperiencePickup[4];
            for (int i = 0; i < 4; i++) items[i] = pools.Acquire<ExperiencePickup>("probe");
            Assert.That(pools.Acquire<ExperiencePickup>("probe"), Is.Null);
            var full = pools.GetMetrics(PoolFamily.Pickup);
            Assert.That(full.Active, Is.EqualTo(4)); Assert.That(full.Capacity, Is.EqualTo(4)); Assert.That(full.Exhaustions, Is.EqualTo(1));
            Assert.That(pools.Release(items[0]), Is.True); Assert.That(pools.Release(items[0]), Is.False);
            Assert.That(pools.GetMetrics(PoolFamily.Pickup).Active, Is.EqualTo(3));
            Assert.That(pools.GetMetrics(PoolFamily.Enemy).Capacity, Is.Zero);
        }

        [Test]
        public void RingKeepsLatestSamplesInOrderAndReturnsValueCopies()
        {
            for (int i = 0; i < 10; i++) Assert.That(telemetry.Capture(i), Is.True);
            Assert.That(telemetry.Count, Is.EqualTo(4)); Assert.That(telemetry.Written, Is.EqualTo(10)); Assert.That(telemetry.Overwritten, Is.EqualTo(6));
            for (int i = 0; i < 4; i++) { Assert.That(telemetry.TryGet(i, out var s), Is.True); Assert.That(s.FrameMilliseconds, Is.EqualTo(i + 6)); }
            telemetry.TryGet(0, out var copy); copy.FrameMilliseconds = 99;
            telemetry.TryGet(0, out var unchanged); Assert.That(unchanged.FrameMilliseconds, Is.EqualTo(6));
            Assert.That(telemetry.TryGet(-1, out _), Is.False); Assert.That(telemetry.TryGet(4, out _), Is.False);
        }

        [Test]
        public void WarmedSamplingAndPoolSnapshotsAllocateNoManagedBytes()
        {
            for (int i = 0; i < 64; i++) telemetry.Capture(16.6);
            GC.GetAllocatedBytesForCurrentThread();
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 2048; i++) telemetry.Capture(16.6);
            long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(bytes, Is.Zero, "Only sampler + pool metrics are measured, not the whole game or Editor.");
        }

        [UnityTest]
        public IEnumerator NativeCountersHaveExplicitAvailabilityAndRecordersFollowComponentLifetime()
        {
            yield return null; yield return null; yield return null;
            telemetry.Capture(20);
            var sample = telemetry.Latest;
            AssertMetric(sample.Available, AvailableCounters.MainThread, sample.MainThreadMilliseconds);
            AssertMetric(sample.Available, AvailableCounters.GcAllocated, sample.GcAllocatedBytes);
            AssertMetric(sample.Available, AvailableCounters.UnityUsedMemory, sample.UnityUsedBytes);
            AssertMetric(sample.Available, AvailableCounters.GcUsedMemory, sample.GcUsedBytes);
#if UNITY_EDITOR
            Assert.That(sample.Available & AvailableCounters.UnityUsedMemory, Is.Not.EqualTo(AvailableCounters.None));
            Assert.That(sample.UnityUsedBytes, Is.GreaterThan(0));
#endif
            telemetry.enabled = false;
            Assert.That(telemetry.RecordersStarted, Is.False); Assert.That(telemetry.Capture(20), Is.False);
            telemetry.enabled = true; yield return null;
            Assert.That(telemetry.Capture(20), Is.True);
            pools.Dispose(); Assert.That(telemetry.Capture(20), Is.False);
        }

        [Test]
        public void ExportIsExplicitChronologicalAndCultureIndependent()
        {
            telemetry.Capture(6.5); telemetry.Capture(7.5);
#if UNITY_EDITOR
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../TestResults/t072-telemetry.csv"));
#else
            string path = Path.Combine(Application.temporaryCachePath, "QuackQuack-T072-telemetry.csv");
#endif
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var culture = CultureInfo.CurrentCulture;
            try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR"); telemetry.ExportCsv(path); }
            finally { CultureInfo.CurrentCulture = culture; }
            var lines = File.ReadAllLines(path);
            Assert.That(lines.Length, Is.EqualTo(3));
            Assert.That(lines[1].Split(',').Length, Is.EqualTo(lines[0].Split(',').Length));
            Assert.That(lines[1].Split(',')[5], Is.EqualTo("6.5"));
            Assert.That(lines[2].Split(',')[5], Is.EqualTo("7.5"));
        }

        [Test]
        public void InvalidConfigurationAndTimingsAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => telemetry.Bind(pools, scheduler, capacity: 0));
            Assert.Throws<ArgumentException>(() => telemetry.Bind(null, scheduler));
            Assert.Throws<ArgumentOutOfRangeException>(() => telemetry.Capture(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => telemetry.Capture(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => pools.GetMetrics((PoolFamily)99));
        }
        private static void AssertMetric(AvailableCounters available, AvailableCounters flag, double value)
        {
            if ((available & flag) == 0) Assert.That(value, Is.EqualTo(-1));
            else Assert.That(value, Is.GreaterThanOrEqualTo(0));
        }
    }
}
