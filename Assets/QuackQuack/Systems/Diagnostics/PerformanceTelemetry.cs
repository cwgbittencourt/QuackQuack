using System;
using System.Globalization;
using System.IO;
using QuackQuack.Gameplay.Enemies;
using QuackQuack.Systems.Pooling;
using Unity.Profiling;
using UnityEngine;

namespace QuackQuack.Systems.Diagnostics
{
    [Flags]
    public enum AvailableCounters { None = 0, MainThread = 1, GcAllocated = 2, UnityUsedMemory = 4, GcUsedMemory = 8 }

    public struct PerformanceSample
    {
        public int Frame, Seed, BenchmarkTick, EnemyTarget, Steering;
        public bool Warmup;
        public double FrameMilliseconds, MainThreadMilliseconds, SchedulerMilliseconds;
        public long SchedulerSteps, GcAllocatedBytes, UnityUsedBytes, GcUsedBytes;
        public PoolMetrics Enemies, Projectiles, Pickups, Vfx;
        public AvailableCounters Available;
    }

    // Sampling uses value types and a bounded ring; formatting and I/O are explicit operations.
    [DisallowMultipleComponent]
    public sealed class PerformanceTelemetry : MonoBehaviour
    {
        private static readonly ProfilerMarker CaptureMarker = new ProfilerMarker("QuackQuack.Telemetry.Capture");
        private PoolService pools;
        private EnemyUpdateScheduler scheduler;
        private PerformanceBenchmark benchmark;
        private PerformanceSample[] samples;
        private int next;
        private double previousSchedulerTime;
        private long previousSchedulerSteps;
        private ProfilerRecorder mainThread, gcAllocated, unityUsed, gcUsed;
        public bool AutomaticSampling { get; set; } = true;
        public int Count { get; private set; }
        public int Capacity => samples == null ? 0 : samples.Length;
        public long Written { get; private set; }
        public long Overwritten => Written - Count;
        public bool RecordersStarted => mainThread.Valid || gcAllocated.Valid || unityUsed.Valid || gcUsed.Valid;
        public PerformanceSample Latest => Count == 0 ? default : samples[(next + samples.Length - 1) % samples.Length];

        public void Bind(PoolService owner, EnemyUpdateScheduler source, PerformanceBenchmark context = null, int capacity = 8192)
        {
            if (owner == null || owner.IsDisposed || source == null) throw new ArgumentException("Live pools and scheduler are required.");
            if (capacity < 1 || capacity > 65536) throw new ArgumentOutOfRangeException(nameof(capacity));
            CloseRecorders();
            pools = owner; scheduler = source; benchmark = context;
            samples = new PerformanceSample[capacity]; next = Count = 0; Written = 0;
            ResetSchedulerBaseline();
            if (isActiveAndEnabled) OpenRecorders();
        }

        private void LateUpdate()
        {
            if (!AutomaticSampling || (benchmark != null && (!benchmark.Ready || benchmark.Completed))) return;
            Capture(Time.unscaledDeltaTime * 1000.0);
        }

        public bool Capture(double frameMilliseconds)
        {
            if (!isActiveAndEnabled || samples == null || pools.IsDisposed || scheduler == null) return false;
            if (double.IsNaN(frameMilliseconds) || double.IsInfinity(frameMilliseconds) || frameMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(frameMilliseconds));
            using var scope = CaptureMarker.Auto();
            var available = AvailableCounters.None;
            long cpu = Read(ref mainThread, AvailableCounters.MainThread, ref available);
            var sample = new PerformanceSample
            {
                Frame = Time.frameCount, FrameMilliseconds = frameMilliseconds,
                MainThreadMilliseconds = cpu < 0 ? -1 : cpu * 0.000001,
                SchedulerMilliseconds = scheduler.TotalAdvanceMilliseconds - previousSchedulerTime,
                SchedulerSteps = scheduler.AdvanceCount - previousSchedulerSteps,
                Steering = scheduler.AdvanceCount == previousSchedulerSteps ? 0 : scheduler.LastSteeringCount,
                Enemies = pools.GetMetrics(PoolFamily.Enemy), Projectiles = pools.GetMetrics(PoolFamily.Projectile),
                Pickups = pools.GetMetrics(PoolFamily.Pickup), Vfx = pools.GetMetrics(PoolFamily.Vfx),
                GcAllocatedBytes = Read(ref gcAllocated, AvailableCounters.GcAllocated, ref available),
                UnityUsedBytes = Read(ref unityUsed, AvailableCounters.UnityUsedMemory, ref available),
                GcUsedBytes = Read(ref gcUsed, AvailableCounters.GcUsedMemory, ref available),
                Available = available, BenchmarkTick = -1
            };
            if (benchmark != null)
            {
                sample.Seed = benchmark.Seed; sample.BenchmarkTick = benchmark.Tick; sample.EnemyTarget = benchmark.EnemyTarget;
                sample.Warmup = benchmark.Tick < PerformanceBenchmark.WarmupTicks;
            }
            samples[next] = sample; next = (next + 1) % samples.Length;
            if (Count < samples.Length) Count++;
            Written++;
            ResetSchedulerBaseline();
            return true;
        }

        // Index zero is the oldest retained sample. Returned structs cannot mutate the buffer.
        public bool TryGet(int index, out PerformanceSample sample)
        {
            sample = default;
            if (index < 0 || index >= Count) return false;
            sample = samples[(next - Count + samples.Length + index) % samples.Length];
            return true;
        }

        private static long Read(ref ProfilerRecorder recorder, AvailableCounters flag, ref AvailableCounters available)
        {
            if (!recorder.Valid || recorder.Count == 0) return -1;
            available |= flag;
            return recorder.LastValue;
        }

        private void ResetSchedulerBaseline()
        {
            if (scheduler == null) return;
            previousSchedulerTime = scheduler.TotalAdvanceMilliseconds;
            previousSchedulerSteps = scheduler.AdvanceCount;
        }

        private void OpenRecorders()
        {
            mainThread = Open(ProfilerCategory.Internal, "Main Thread");
            gcAllocated = Open(ProfilerCategory.Memory, "GC Allocated In Frame");
            unityUsed = Open(ProfilerCategory.Memory, "Total Used Memory");
            gcUsed = Open(ProfilerCategory.Memory, "GC Used Memory");
        }
        private static ProfilerRecorder Open(ProfilerCategory category, string name)
        {
            try { return ProfilerRecorder.StartNew(category, name, 1); }
            catch (ArgumentException) { return default; }
            catch (InvalidOperationException) { return default; }
        }
        private void OnEnable()
        {
            if (samples == null || pools.IsDisposed) return;
            ResetSchedulerBaseline(); OpenRecorders();
        }
        private void OnDisable() => CloseRecorders();
        private void OnDestroy() => CloseRecorders();
        private void CloseRecorders()
        {
            Close(ref mainThread); Close(ref gcAllocated); Close(ref unityUsed); Close(ref gcUsed);
        }
        private static void Close(ref ProfilerRecorder recorder) { if (recorder.Valid) recorder.Dispose(); recorder = default; }

        public void ExportCsv(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("CSV output path is required.", nameof(path));
            using var writer = new StreamWriter(path, false);
            writer.WriteLine("frame,seed,benchmark_tick,enemy_target,warmup,frame_ms,main_thread_ms,scheduler_ms,scheduler_steps,steering,enemies_active,enemies_capacity,enemies_exhaustions,projectiles_active,projectiles_capacity,projectiles_exhaustions,pickups_active,pickups_capacity,pickups_exhaustions,vfx_active,vfx_capacity,vfx_exhaustions,gc_allocated_bytes,unity_used_bytes,gc_used_bytes,available_mask");
            for (int i = 0; i < Count; i++)
            {
                TryGet(i, out var s);
                writer.WriteLine(string.Join(",", new[] {
                    Number(s.Frame), Number(s.Seed), Number(s.BenchmarkTick), Number(s.EnemyTarget), s.Warmup ? "1" : "0",
                    Number(s.FrameMilliseconds), Number(s.MainThreadMilliseconds), Number(s.SchedulerMilliseconds), Number(s.SchedulerSteps), Number(s.Steering),
                    Number(s.Enemies.Active), Number(s.Enemies.Capacity), Number(s.Enemies.Exhaustions),
                    Number(s.Projectiles.Active), Number(s.Projectiles.Capacity), Number(s.Projectiles.Exhaustions),
                    Number(s.Pickups.Active), Number(s.Pickups.Capacity), Number(s.Pickups.Exhaustions),
                    Number(s.Vfx.Active), Number(s.Vfx.Capacity), Number(s.Vfx.Exhaustions),
                    Number(s.GcAllocatedBytes), Number(s.UnityUsedBytes), Number(s.GcUsedBytes), Number((int)s.Available) }));
            }
        }
        private static string Number(long value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
        [ContextMenu("Export telemetry CSV to temporary cache")]
        private void ExportToCache()
        {
            string path = Path.Combine(Application.temporaryCachePath, "QuackQuackTelemetry.csv");
            ExportCsv(path); Debug.Log("TELEMETRY_CSV " + path);
        }
    }
}
