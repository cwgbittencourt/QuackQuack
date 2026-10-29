using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;

namespace QuackQuack.Systems.Diagnostics
{
    // Added only to the isolated profiling scene, never to the product build.
    [DefaultExecutionOrder(10000)]
    public sealed class PerformanceProfileCapture : MonoBehaviour
    {
        [Serializable] private sealed class Metadata
        {
            public string Unity, Platform, OS, Model, CPU, GPU, GraphicsAPI, BuildGuid;
            public int SystemMemoryMB, GraphicsMemoryMB, Width, Height, TargetFrameRate, Samples, Overwritten, FocusLosses;
            public bool FrameTimingEnabled, Development, Il2Cpp;
            public string[] NativeMarkers;
            public string[] NativeMarkerUnits;
        }
        private struct Timing { public int Frame, Tick; public ulong Timestamp; public double Cpu, Gpu; }
        private readonly FrameTiming[] latest = new FrameTiming[1];
        private Timing[] timings;
        private int timingCount, focusLosses;
        private PerformanceBenchmark benchmark;
        private PerformanceTelemetry telemetry;
        private string output;
        private bool finished;
        private ProfilerRecorder[] probes;
        private string[] probeNames;
        private string[] probeUnits;
        private long[] probeSamples;
        private void Start()
        {
            Application.runInBackground = true; QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60;
            if (Application.isMobilePlatform) Screen.sleepTimeout = SleepTimeout.NeverSleep;
            benchmark = GetComponent<PerformanceBenchmark>(); telemetry = GetComponent<PerformanceTelemetry>();
            timings = new Timing[8192];
            output = Path.Combine(Application.temporaryCachePath, "T074-profile.csv");
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-profileOutput") output = Path.GetFullPath(args[i + 1]);
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-profileTargetFps" && int.TryParse(args[i + 1], out int fps) && fps > 0 && fps <= 240) Application.targetFrameRate = fps;
            var handles = new List<ProfilerRecorderHandle>(); ProfilerRecorderHandle.GetAvailable(handles);
            var names = new List<string>(); var units = new List<string>(); var recorders = new List<ProfilerRecorder>();
            foreach (var handle in handles)
            {
                var d = ProfilerRecorderHandle.GetDescription(handle);
                if (d.Name.Contains("Physics2D") || d.Name == "GPU Frame Time" || d.Name == "RenderLoop.Draw" || d.Name == "Gfx.PresentFrame")
                {
                    names.Add(d.Name); units.Add(d.UnitType.ToString()); recorders.Add(ProfilerRecorder.StartNew(d.Category, d.Name, 1));
                }
            }
            probeNames = names.ToArray(); probeUnits = units.ToArray(); probes = recorders.ToArray(); probeSamples = new long[8192 * probes.Length];
        }
        private void OnApplicationFocus(bool focus) { if (!focus) focusLosses++; }
        private void LateUpdate()
        {
            if (finished || benchmark == null || !benchmark.Ready) return;
            if (benchmark.Completed) { Finish(); return; }
            FrameTimingManager.CaptureFrameTimings();
            if (timingCount < timings.Length && FrameTimingManager.GetLatestTimings(1, latest) > 0)
            {
                timings[timingCount] = new Timing { Frame = Time.frameCount, Tick = benchmark.Tick,
                    Timestamp = latest[0].frameStartTimestamp, Cpu = latest[0].cpuFrameTime, Gpu = latest[0].gpuFrameTime };
                for (int p = 0; p < probes.Length; p++) probeSamples[timingCount * probes.Length + p] = probes[p].Valid && probes[p].Count > 0 ? probes[p].LastValue : -1;
                timingCount++;
            }
        }
        private void Finish()
        {
            finished = true; telemetry.AutomaticSampling = false;
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            telemetry.ExportCsv(output);
            using (var writer = new StreamWriter(output + ".gpu.csv"))
            {
                writer.WriteLine("capture_frame,benchmark_tick,timing_timestamp,cpu_ms,gpu_ms");
                for (int i = 0; i < timingCount; i++)
                {
                    var t = timings[i];
                    writer.WriteLine(string.Join(",", t.Frame.ToString(CultureInfo.InvariantCulture), t.Tick.ToString(CultureInfo.InvariantCulture),
                        t.Timestamp.ToString(CultureInfo.InvariantCulture), t.Cpu.ToString("R", CultureInfo.InvariantCulture), t.Gpu.ToString("R", CultureInfo.InvariantCulture)));
                }
            }
            var metadata = new Metadata { Unity = Application.unityVersion, Platform = Application.platform.ToString(), OS = SystemInfo.operatingSystem,
                Model = SystemInfo.deviceModel, CPU = SystemInfo.processorType, GPU = SystemInfo.graphicsDeviceName,
                GraphicsAPI = SystemInfo.graphicsDeviceType.ToString(), BuildGuid = Application.buildGUID,
                SystemMemoryMB = SystemInfo.systemMemorySize, GraphicsMemoryMB = SystemInfo.graphicsMemorySize,
                Width = Screen.width, Height = Screen.height, TargetFrameRate = Application.targetFrameRate,
                Samples = telemetry.Count, Overwritten = (int)telemetry.Overwritten, FocusLosses = focusLosses,
                FrameTimingEnabled = FrameTimingManager.IsFeatureEnabled(), Development = Debug.isDebugBuild, NativeMarkers = probeNames, NativeMarkerUnits = probeUnits,
#if ENABLE_IL2CPP
                Il2Cpp = true
#else
                Il2Cpp = false
#endif
            };
            File.WriteAllText(output + ".json", JsonUtility.ToJson(metadata, true));
            using (var writer = new StreamWriter(output + ".markers.csv"))
            {
                writer.Write("frame,tick"); foreach (var name in probeNames) writer.Write(",\"" + name.Replace("\"", "\"\"") + "\""); writer.WriteLine();
                for (int i = 0; i < timingCount; i++)
                {
                    writer.Write(timings[i].Frame.ToString(CultureInfo.InvariantCulture) + "," + timings[i].Tick.ToString(CultureInfo.InvariantCulture));
                    for (int p = 0; p < probes.Length; p++) writer.Write("," + probeSamples[i * probes.Length + p].ToString(CultureInfo.InvariantCulture));
                    writer.WriteLine();
                }
            }
            Debug.Log("T074_PROFILE_COMPLETE " + output);
            Application.Quit(0);
        }
        private void OnDestroy() { if (probes != null) foreach (var probe in probes) if (probe.Valid) probe.Dispose(); }
    }
}
