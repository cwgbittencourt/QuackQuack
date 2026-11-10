using System;
using System.Collections;
using System.Globalization;
using System.IO;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Player;
using QuackQuack.Gameplay.Upgrades;
using QuackQuack.Systems.Run;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuackQuack.Systems.Diagnostics
{
    // Opt-in profiling entry only. Never added to either production scene.
    [DefaultExecutionOrder(12000)]
    public sealed class PolishPerformanceCapture : MonoBehaviour
    {
        [Serializable] private sealed class Metadata
        {
            public string Scenario, Unity, Platform, OS, CPU, GPU, GraphicsAPI, BuildGuid;
            public int Width, Height, TargetFps, Samples, Overwritten, FocusLosses, Defeats, Rewards, Pending, MaxPending;
            public int LedgerCapacity, GuardKills, Materialized, Unresolved, ClaimedExperience, AttractedSamples;
            public double SimulationSeconds, WallSeconds, MaxObservedWaitMs, MaxOutstandingWaitMs;
            public bool Development, Il2Cpp, FrameTimingEnabled;
        }
        private struct Extra
        {
            public double Simulation, CpuMs, GpuMs;
            public int Frame, Target, Pending, Rewards, Defeats;
        }
        private string scenario, output;
        private int fps, focusLosses, count, guardKills, maxPending, seen, waiting, materialized, attractedSamples;
        private bool ready, done, stress;
        private double started, maxWait, nextQuack;
        private RunDirector director;
        private GameBootstrap bootstrap;
        private PlayerController player;
        private Rigidbody2D body;
        private Camera view;
        private QuackQuack.Content.Enemies.EnemyDefinition[] stressDefinitions;
        private LakeBuildProgression progression;
        private PerformanceTelemetry telemetry;
        private Extra[] extras;
        private RewardEntry[] pending;
        private double[] firstSeen;
        private readonly FrameTiming[] timing = new FrameTiming[1];
        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
        private IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            scenario = "benchmark60";
            string config = Path.Combine(Application.persistentDataPath, "t049-mode.txt");
            if (File.Exists(config)) scenario = File.ReadAllText(config).Trim();
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-profileScenario") scenario = args[i + 1];
            if (scenario != "benchmark60" && scenario != "benchmark30" && scenario != "stress60" && scenario != "stress30" && scenario != "run60" && scenario != "run30")
                throw new InvalidOperationException("Unknown profiling scenario: " + scenario);
            fps = scenario.EndsWith("30", StringComparison.Ordinal) ? 30 : 60;
            Application.runInBackground = true; QualitySettings.vSyncCount = 0; Application.targetFrameRate = fps;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            output = Path.Combine(Application.temporaryCachePath, "T049-" + scenario + ".csv");
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-profileOutput") output = Path.GetFullPath(args[i + 1]);
            if (scenario.StartsWith("benchmark", StringComparison.Ordinal))
            {
                yield return SceneManager.LoadSceneAsync("T049Benchmark");
                yield break;
            }
            stress = scenario.StartsWith("stress", StringComparison.Ordinal);
            RunDirector.SavePathProvider = () => Path.Combine(Application.temporaryCachePath, "T049-profile-save.json");
            yield return SceneManager.LoadSceneAsync("RunBootstrap");
            for (int i = 0; i < 600; i++)
            {
                director = FindAnyObjectByType<RunDirector>();
                if (director != null && director.Ready) break;
                yield return null;
            }
            if (director == null || !director.Ready) throw new InvalidOperationException("Production director did not initialize.");
            director.AutomaticTick = false;
            bootstrap = GameBootstrap.Instance; player = FindAnyObjectByType<PlayerController>();
            body = player.GetComponent<Rigidbody2D>();
            view = Camera.main;
            progression = FindAnyObjectByType<LakeBuildProgression>();
            if (stress)
            {
                float h = view.orthographicSize, w = h * view.aspect;
                director.Advance(0, new Rect(view.transform.position.x - w, view.transform.position.y - h, w * 2, h * 2));
                // Consume the initial authored wave, then replace its 32-enemy producer
                // only in this opt-in synthetic workload. Keep director, rewards,
                // scheduler, collision and weapons; never modify the production scene.
                var wave = typeof(RunDirector).GetField("currentWave", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (wave == null) throw new InvalidOperationException("Diagnostic wave binding is unavailable.");
                wave.SetValue(director, -1);
                var original = director.Rewards;
                if (original.DefeatCount != 0) throw new InvalidOperationException("Stress ledger must be replaced before any defeat.");
                var randomField = typeof(RewardLedger).GetField("nextDropRoll", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (randomField == null) throw new InvalidOperationException("Dedicated drop RNG is unavailable.");
                var random = (Func<double>)randomField.GetValue(original);
                // Artificial replenishment exceeds the entire authored 15-minute
                // defeat budget. Keep its policy/RNG/identity and increase storage
                // only for this bounded diagnostic (8 spawns/frame at target 60).
                var ledger = director.RunSettings.PickupPolicy.CreateLedger(original.RunId, random, 32768);
                typeof(RunDirector).GetProperty("Rewards").SetValue(director, ledger);
            }
            var common = director.RunSettings.Catalog.CommonEnemies;
            stressDefinitions = new QuackQuack.Content.Enemies.EnemyDefinition[common.Count];
            for (int i = 0; i < common.Count; i++) stressDefinitions[i] = common[i];
            // Explicit synthetic survival guard; damage/reward policies are otherwise unchanged.
            player.State.TakeDamage(1, 0, float.MaxValue);
            player.SetMenuInputBlocked(true);
            foreach (var weapon in progression.Catalog.Weapons)
            {
                progression.Upgrades.Build.TryAddWeapon(weapon.Id);
                for (int i = 1; i < 5; i++) progression.Upgrades.Build.TryUpgradeWeapon(weapon.Id);
            }
            foreach (var passive in progression.Catalog.Passives)
            {
                progression.Upgrades.Build.TryAddPassive(passive.Id);
                for (int i = 1; i < 5; i++) progression.Upgrades.Build.TryUpgradePassive(passive.Id, 5);
            }
            telemetry = gameObject.AddComponent<PerformanceTelemetry>();
            telemetry.AutomaticSampling = false;
            telemetry.Bind(bootstrap.Pools, director.Scheduler, null, 65536);
            extras = new Extra[65536];
            int capacity = checked(director.Rewards.DefeatCapacity * 2);
            pending = new RewardEntry[capacity]; firstSeen = new double[capacity];
            started = Time.realtimeSinceStartupAsDouble; ready = true;
            Debug.Log("T049_CAPTURE_STARTED " + scenario);
        }
        private void OnApplicationFocus(bool focus) { if (!focus) focusLosses++; }
        private void FixedUpdate()
        {
            if (!ready || done) return;
            if (bootstrap.Run.Phase == RunPhase.Paused) bootstrap.Run.TryResume();
            if (progression.Upgrades.CurrentOffer != null && !progression.Upgrades.CurrentOffer.IsClosed)
                progression.Upgrades.TrySkip(progression.Upgrades.CurrentOffer.OfferId);
            if (!bootstrap.Run.IsSimulating) return;
            double elapsed = bootstrap.Run.ElapsedSeconds;
            // Long exposure leaves every kill outside pickup capture radius. Stress moves
            // through drops to exercise attraction; neither mode suppresses a reward.
            body.position = stress ? new Vector2((float)Math.Sin(elapsed * 0.35) * 6, (float)Math.Cos(elapsed * 0.35) * 3) : new Vector2(-3, 0);
            if (stress && elapsed >= nextQuack) { player.ActivateQuack(); nextQuack = elapsed + 1; }
            for (int i = 0; i < director.Spawns.Capacity; i++)
                if (director.Spawns.TryGetActive(i, out var enemy, out long lease) && Vector2.Distance(enemy.Body.position, body.position) < 3)
                {
                    if (enemy.TakeDamage(lease, float.MaxValue, GetInstanceID(), Time.frameCount)) guardKills++;
                }
        }
        private void LateUpdate()
        {
            Application.targetFrameRate = fps;
            if (!ready || done) return;
            float height = view.orthographicSize, width = height * view.aspect;
            var area = new Rect(view.transform.position.x - width, view.transform.position.y - height, width * 2, height * 2);
            if (bootstrap.Run.IsSimulating) director.Advance(Time.deltaTime, area);
            double sim = bootstrap.Run.ElapsedSeconds, now = Time.realtimeSinceStartupAsDouble;
            if (sim >= (stress ? 65 : 900)) { Finish(); return; }
            int target = stress ? (sim < 35 ? 200 : 400) : 0;
            if (stress && bootstrap.Run.IsSimulating)
            {
                director.Spawns.ActiveLimit = target;
                int missing = target - director.Spawns.ActiveCount;
                if (missing > 0 && director.Spawns.PendingCount == 0)
                {
                    var definitions = stressDefinitions;
                    for (int i = 0; i < definitions.Length; i++)
                        director.Spawns.TryEnqueue(new SpawnRequest(Time.frameCount * 10L + i,
                            "T049-stress", definitions[i].Id, (missing + definitions.Length - 1) / definitions.Length,
                            director.RunSettings.SpawnRegion, 4, QuackQuack.Core.Combat.EventPriority.Feedback, long.MaxValue));
                }
                director.Spawns.Process(director.SimulationTick, area);
                for (int i = 0; i < director.Spawns.Capacity; i++)
                    if (director.Spawns.TryGetActive(i, out var enemy, out _))
                        enemy.GetComponent<QuackQuack.Gameplay.Enemies.CombatTargetBinding>()?.Configure(director.Scheduler, 1, 1);
                director.Scheduler.Advance(Time.deltaTime);
            }
            ObserveRewards(now);
            FrameTimingManager.CaptureFrameTimings();
            bool hasTiming = FrameTimingManager.GetLatestTimings(1, timing) > 0;
            if (count == extras.Length) throw new InvalidOperationException("Capture buffer full; no samples may be overwritten.");
            telemetry.Capture(Time.unscaledDeltaTime * 1000);
            extras[count++] = new Extra { Simulation = sim, Frame = Time.frameCount, Target = target,
                Pending = director.PendingRewards, Rewards = director.Rewards.Entries.Count, Defeats = director.Rewards.DefeatCount,
                CpuMs = hasTiming ? timing[0].cpuFrameTime : -1, GpuMs = hasTiming ? timing[0].gpuFrameTime : -1 };
        }
        private void ObserveRewards(double now)
        {
            var entries = director.Rewards.Entries;
            while (seen < entries.Count)
            {
                var entry = entries[seen++];
                if (entry.State == RewardState.Due || entry.State == RewardState.WaitingForView)
                { pending[waiting] = entry; firstSeen[waiting++] = now; }
                else { materialized++; maxWait = Math.Max(maxWait, Time.unscaledDeltaTime * 1000); }
            }
            for (int i = waiting - 1; i >= 0; i--)
            {
                var state = pending[i].State;
                if (state == RewardState.Due || state == RewardState.WaitingForView) continue;
                if (state != RewardState.Cancelled && state != RewardState.Discarded)
                { materialized++; maxWait = Math.Max(maxWait, (now - firstSeen[i] + Time.unscaledDeltaTime) * 1000); }
                pending[i] = pending[--waiting]; firstSeen[i] = firstSeen[waiting];
            }
            maxPending = Math.Max(maxPending, director.PendingRewards);
            if (Time.frameCount % fps == 0)
                for (int i = 0; i < entries.Count; i++)
                    if (entries[i].State == RewardState.Attracted) { attractedSamples++; break; }
        }
        private void Finish()
        {
            ObserveRewards(Time.realtimeSinceStartupAsDouble);
            done = true;
            Directory.CreateDirectory(Path.GetDirectoryName(output)); telemetry.ExportCsv(output);
            using (var writer = new StreamWriter(output + ".run.csv"))
            {
                writer.WriteLine("frame,simulation_seconds,enemy_target,pending,rewards,defeats,cpu_ms,gpu_ms");
                for (int i = 0; i < count; i++) { var s = extras[i]; writer.WriteLine(string.Join(",", s.Frame.ToString(Culture), s.Simulation.ToString("R", Culture), s.Target.ToString(Culture), s.Pending.ToString(Culture), s.Rewards.ToString(Culture), s.Defeats.ToString(Culture), s.CpuMs.ToString("R", Culture), s.GpuMs.ToString("R", Culture))); }
            }
            double outstanding = 0;
            for (int i = 0; i < waiting; i++) outstanding = Math.Max(outstanding, (Time.realtimeSinceStartupAsDouble - firstSeen[i]) * 1000);
            var metadata = new Metadata { Scenario = scenario, Unity = Application.unityVersion, Platform = Application.platform.ToString(), OS = SystemInfo.operatingSystem,
                CPU = SystemInfo.processorType, GPU = SystemInfo.graphicsDeviceName, GraphicsAPI = SystemInfo.graphicsDeviceType.ToString(), BuildGuid = Application.buildGUID,
                Width = Screen.width, Height = Screen.height, TargetFps = fps, Samples = count, Overwritten = (int)telemetry.Overwritten, FocusLosses = focusLosses,
                SimulationSeconds = bootstrap.Run.ElapsedSeconds, WallSeconds = Time.realtimeSinceStartupAsDouble - started,
                Defeats = director.Rewards.DefeatCount, Rewards = director.Rewards.Entries.Count, Pending = director.PendingRewards, MaxPending = maxPending,
                LedgerCapacity = director.Rewards.DefeatCapacity, GuardKills = guardKills, Materialized = materialized, Unresolved = waiting,
                ClaimedExperience = progression.Upgrades.Experience, AttractedSamples = attractedSamples, MaxObservedWaitMs = maxWait, MaxOutstandingWaitMs = outstanding,
                Development = Debug.isDebugBuild, FrameTimingEnabled = FrameTimingManager.IsFeatureEnabled(),
#if ENABLE_IL2CPP
                Il2Cpp = true
#endif
            };
            File.WriteAllText(output + ".json", JsonUtility.ToJson(metadata, true));
            Debug.Log("T049_CAPTURE_COMPLETE " + output); Application.Quit(0);
        }
    }
}
