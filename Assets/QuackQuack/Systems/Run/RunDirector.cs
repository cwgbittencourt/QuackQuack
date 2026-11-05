using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using QuackQuack.Content.Catalogs;
using QuackQuack.Content.Encounters;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Bosses;
using QuackQuack.Gameplay.Enemies;
using QuackQuack.Gameplay.Pickups;
using QuackQuack.Gameplay.Player;
using QuackQuack.Gameplay.Upgrades;
using QuackQuack.Gameplay.Weapons;
using QuackQuack.Systems.Combat;
using QuackQuack.Systems.Pooling;
using QuackQuack.Systems.Save;
using QuackQuack.Systems.Spawning;
using QuackQuack.Systems.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace QuackQuack.Systems.Run
{
    public sealed class RunDirector : MonoBehaviour
    {
        [SerializeField] private LakeRunSettings settings;
        [SerializeField] private FrogKingSafeArea[] refuges;
        [SerializeField] private Button pauseButton, abandonButton;
        private struct Wave { public EncounterDefinition Definition; public string[] Ids; public int[] Quantities; }
        public static Func<string> SavePathProvider { get; set; } = () => Path.Combine(Application.persistentDataPath, "lake-relics.json");
        public bool AutomaticTick { get; set; } = true;
        public bool Ready { get; private set; }
        public SpawnDirector Spawns { get; private set; }
        public EnemyUpdateScheduler Scheduler { get; private set; }
        public FrogKingController Boss { get; private set; }
        public RunResult Result { get; private set; }
        public string RunId { get; } = Guid.NewGuid().ToString("N");
        public PrototypeBuildCatalog BuildCatalog => buildCatalog;
        public LakeRunSettings RunSettings => settings;
        public SaveOperationResult LastSave { get; private set; }
        public int BossRequests { get; private set; }
        public RewardLedger Rewards { get; private set; }
        public int PendingRewards => Rewards?.PendingCount ?? 0;
        // T028 supplies the healing view. Returning false preserves its obligation.
        public Func<RewardEntry, bool> HealingMaterializer { get; set; }
        public event Action<string> EncounterStarted;
        public event Action<RunResult> ResultAvailable;
        private RunState run;
        private PoolService pools;
        private GameplayEventBus events;
        private PlayerController player;
        private UpgradeSystem upgrades;
        private WeaponRunner weapons;
        private PrototypeBuildCatalog buildCatalog;
        private RelicLoadout relics;
        private JsonSaveService save;
        private RunResultSession results;
        private EncounterSchedule timeline;
        private readonly Dictionary<string, EncounterDefinition> encounters = new Dictionary<string, EncounterDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<int, string> sources = new Dictionary<int, string>();
        private readonly HashSet<string> knownUnlocks = new HashSet<string>(StringComparer.Ordinal);
        private Wave[] waves;
        private EncounterDefinition[] minis;
        private string[] miniIds;
        private QuackQuack.Content.Weapons.EvolutionDefinition[] recipes;
        private readonly bool[] requested = new bool[2], delivered = new bool[2];
        private readonly long[] retry = new long[2];
        private ExperiencePickup[] pickups;
        private HealingPickup[] healingPickups;
        private string healingKey;
        private readonly Dictionary<string, int> rewardSources = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<string> miniRewardSources = new HashSet<string>(StringComparer.Ordinal);
        private Func<int, bool> collectExperience;
        private static long nextRewardRun;
        private string xpKey, bossKey;
        private Camera view;
        private GameBootstrap bootstrap;
        private LakeRelics sceneRelics;
        private long tick, request;
        private double nextBatch;
        private int currentWave = -1, savedVersion;
        private int factsVersion = -1, buildVersion = -1;
        private bool ending, announced, dirty, disposed;

        private IEnumerator Start()
        {
            if (Ready || settings == null) yield break;
            bootstrap = GameBootstrap.Instance;
            if (bootstrap == null) { Debug.LogError("RunBootstrap is required for LakeRun.", this); yield break; }
            bootstrap.ExternalClock = true;
            LakeBuildProgression progression = null; LakeRelics loadout = null;
            for (int i = 0; i < 240; i++)
            {
                progression = UnityEngine.Object.FindAnyObjectByType<LakeBuildProgression>();
                loadout = UnityEngine.Object.FindAnyObjectByType<LakeRelics>();
                if (progression != null && progression.Ready && loadout != null && loadout.Ready) break;
                yield return null;
            }
            if (progression == null || !progression.Ready || loadout == null || !loadout.Ready)
            { Debug.LogError("LakeRun progression did not initialize.", this); yield break; }
            player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            sceneRelics = loadout;
            var ids = new List<string>();
            // Loadout knows the validated catalog; the authored IDs are supplied separately by scene composition.
            var relicCatalog = RelicCatalog;
            foreach (var definition in relicCatalog.Relics) ids.Add(definition.Id);
            Initialize(bootstrap.Run, bootstrap.Pools, bootstrap.Events, player, progression.Upgrades, progression.Runner,
                progression.Catalog, loadout.Loadout, settings, refuges, new JsonSaveService(SavePathProvider(), ids.ToArray()));
            var hud = UnityEngine.Object.FindAnyObjectByType<QuackQuack.Presentation.UI.GameplayHud>();
            if (hud != null) hud.Bind(player.Snapshot);
        }
        [SerializeField] private QuackQuack.Content.Relics.PrototypeRelicCatalog relicCatalog;
        public QuackQuack.Content.Relics.PrototypeRelicCatalog RelicCatalog => relicCatalog;

        public void Initialize(RunState state, PoolService owner, GameplayEventBus bus, PlayerController target,
            UpgradeSystem progression, WeaponRunner runner, PrototypeBuildCatalog catalog, RelicLoadout loadout,
            LakeRunSettings definition, FrogKingSafeArea[] safeAreas, JsonSaveService persistence)
        {
            if (Ready) throw new InvalidOperationException("Run director is already initialized.");
            if (state == null || state.Outcome.HasValue || owner == null || owner.IsDisposed || bus == null || target == null || target.State == null
                || progression == null || runner == null || catalog == null || !catalog.IsValid(out _) || loadout == null
                || definition == null || !definition.IsValid || safeAreas == null || persistence == null)
                throw new ArgumentException("Valid run composition is required.");
            run = state; pools = owner; events = bus; player = target; upgrades = progression; weapons = runner; buildCatalog = catalog;
            relics = loadout; settings = definition; refuges = (FrogKingSafeArea[])safeAreas.Clone(); save = persistence;
            view = Camera.main;
            var loaded = save.Load();
            foreach (string id in loaded.Data.UnlockedRelicIds) relics.Unlock(id);
            foreach (string id in loaded.Data.EquippedRelicIds) relics.Equip(id);
            foreach (string id in relics.UnlockedIds) knownUnlocks.Add(id);
            savedVersion = relics.Version; dirty = loaded.Status != SaveStatus.Success;
            results = new RunResultSession(run, 0, settings.VictoryBonus, settings.AbandonmentFactor, upgrades.Build, relics);
            sources.Add(player.GetInstanceID(), "quack");
            foreach (var d in catalog.GetAllWeapons()) sources[d.GetInstanceID()] = d.Id;
            var catalogWaves = settings.Catalog.Waves; waves = new Wave[catalogWaves.Count];
            for (int i = 0; i < waves.Length; i++)
            { var d = catalogWaves[i]; waves[i] = new Wave { Definition = d, Ids = new List<string>(d.EnemyIds).ToArray(), Quantities = new List<int>(d.Quantities).ToArray() }; }
            minis = new List<EncounterDefinition>(settings.Catalog.MiniBossEncounters).ToArray();
            miniIds = new[] { minis[0].EnemyIds[0], minis[1].EnemyIds[0] };
            foreach (string id in miniIds) miniRewardSources.Add(id);
            var rewardDefinitions = settings.Catalog.GetAllEnemies();
            for (int i = 0; i < rewardDefinitions.Length; i++) rewardSources.Add(rewardDefinitions[i].Id, i + 1);
            var dropRandom = new System.Random(unchecked(run.Seed ^ 0x52dce729));
            Rewards = settings.PickupPolicy.CreateLedger(Interlocked.Increment(ref nextRewardRun), dropRandom.NextDouble,
                settings.CalculateRewardDefeatCapacity());
            collectExperience = upgrades.TryCollectExperience;
            recipes = new List<QuackQuack.Content.Weapons.EvolutionDefinition>(catalog.Evolutions).ToArray();
            foreach (var d in settings.Catalog.GetAllEncounters()) encounters.Add(d.Id, d);
            timeline = settings.Catalog.CreateSchedule(run);
            settings.Catalog.RegisterPools(pools, transform);
            Spawns = settings.Catalog.CreateDirector(run, pools, events, player.transform, run.Seed, Reward);
            Scheduler = gameObject.AddComponent<EnemyUpdateScheduler>(); Scheduler.AutomaticTick = false;
            Scheduler.Initialize(run, Spawns, pools, events, player, settings.Catalog.BehaviorSettings, settings.Catalog.GetAllEnemies());
            xpKey = "run-xp/" + GetInstanceID(); bossKey = "run-boss/" + GetInstanceID();
            pools.Register(xpKey, PoolFamily.Pickup, () => Instantiate(settings.ExperiencePrefab, transform), settings.PickupCapacity);
            pickups = new ExperiencePickup[settings.PickupCapacity];
            healingKey = "run-healing/" + GetInstanceID();
            pools.Register(healingKey, PoolFamily.Pickup, () => Instantiate(settings.HealingPrefab, transform), settings.HealingCapacity);
            healingPickups = new HealingPickup[settings.HealingCapacity];
            HealingMaterializer = MaterializeHealingView;
            pools.Register(bossKey, PoolFamily.Enemy, () => Instantiate(settings.BossPrefab, transform).GetComponent<FrogKingController>(), 1);
            // Warm the boss's own hazard pools before gameplay, then return the actor.
            var warm = pools.Acquire<FrogKingController>(bossKey);
            warm.InitializeEncounter(run, events, pools, player, settings.BossSettings, warm.GetComponentInChildren<LineRenderer>(), warm.GetComponent<AudioSource>(), refuges);
            pools.Release(warm);
            weapons.AutomaticTick = false; weapons.Relics = relics; player.Relics = relics;
            events.Combat += OnCombat; run.Ended += OnEnded; run.Changed += PhaseChanged;
            if (pauseButton != null) pauseButton.onClick.AddListener(TogglePause);
            player.SetExternalPauseButton(pauseButton != null);
            if (abandonButton != null) abandonButton.onClick.AddListener(Abandon);
            Ready = true;
            GetComponent<QuackQuack.Systems.Diagnostics.PerformanceTelemetry>()?.Bind(pools, Scheduler);
            if (run.Phase == RunPhase.Preparing) run.TryTransition(RunPhase.Playing);
            PhaseChanged(run.Phase);
        }

        private void Update()
        {
            if (!Ready || !AutomaticTick) return;
            Advance(Time.deltaTime, VisibleArea());
        }
        public void Advance(float seconds, Rect visibleArea)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0 || !SpawnRequest.ValidRegion(visibleArea)) throw new ArgumentException("Valid timestep and camera bounds required.");
            if (!Ready || disposed) return;
            if (ending) { Finish(); return; }
            if (!run.IsSimulating) return;
            run.Advance(seconds); UpdateFacts(); tick++;
            if (player.State.Health <= 0) { run.TryTransition(RunPhase.Lost); Finish(); return; }
            if (run.Phase == RunPhase.Playing)
            {
                foreach (string id in timeline.CollectDue())
                {
                    var encounter = encounters[id]; EncounterStarted?.Invoke(id);
                    if (encounter.Kind == EncounterKind.Wave)
                    { for (int i = 0; i < waves.Length; i++) if (waves[i].Definition == encounter) { currentWave = i; nextBatch = run.ElapsedSeconds; } }
                    else if (encounter.Kind == EncounterKind.FrogKing) BeginBoss();
                    else requested[encounter.Kind == EncounterKind.AlphaDragonfly ? 0 : 1] = true;
                }
                if (run.Phase == RunPhase.Playing && currentWave >= 0)
                {
                    var wave = waves[currentWave]; Spawns.ActiveLimit = wave.Definition.MaxActive;
                    for (int i = 0; i < 2; i++) if (requested[i] && !delivered[i] && tick >= retry[i])
                    { Enqueue(minis[i].Id, miniIds[i], 1, EventPriority.Essential); retry[i] = tick + settings.RequestLifetimeTicks; }
                    if (run.ElapsedSeconds >= nextBatch)
                    {
                        for (int i = 0; i < wave.Ids.Length; i++) Enqueue(wave.Definition.Id, wave.Ids[i], wave.Quantities[i], EventPriority.Feedback);
                        nextBatch = run.ElapsedSeconds + wave.Definition.SpawnInterval;
                    }
                    Spawns.Process(tick, visibleArea);
                    for (int i = 0; i < Spawns.Capacity; i++) if (Spawns.TryGetActive(i, out var enemy, out _))
                    {
                        var modifier = wave.Definition;
                        for (int j = 0; j < 2; j++) if (!delivered[j] && enemy.Definition.Id == miniIds[j])
                        { delivered[j] = true; results.RecordBossReached(enemy.Definition.Id); }
                        for (int j = 0; j < 2; j++) if (enemy.Definition.Id == miniIds[j]) modifier = minis[j];
                        var binding = enemy.GetComponent<CombatTargetBinding>();
                        if (binding != null) binding.Configure(Scheduler, modifier.HealthMultiplier, modifier.DamageMultiplier);
                    }
                    Physics2D.SyncTransforms(); Scheduler.Advance(seconds);
                }
            }
            if (run.IsSimulating) { Physics2D.SyncTransforms(); weapons.Advance(seconds); }
            if (run.IsSimulating && Boss != null) Boss.Advance(seconds);
            if (run.IsSimulating) MaterializeRewards();
            if (run.IsSimulating && player.State.Health <= 0) run.TryTransition(RunPhase.Lost);
            if (ending) Finish();
        }
        private void Enqueue(string encounter, string enemy, int quantity, EventPriority priority) =>
            Spawns.TryEnqueue(new SpawnRequest(++request, encounter, enemy, quantity, settings.SpawnRegion,
                settings.MinimumSpawnDistance, priority, tick + settings.RequestLifetimeTicks));
        private void BeginBoss()
        {
            if (BossRequests != 0) return;
            BossRequests++; Spawns.Dispose(); Scheduler.enabled = false;
            if (!run.TryTransition(RunPhase.Boss)) return;
            results.RecordBossReached(LakeEncounterCatalog.FinalBossId);
            Boss = pools.Acquire<FrogKingController>(bossKey);
            Boss.transform.position = new Vector2(Mathf.Clamp(player.transform.position.x, -9, 9), Mathf.Clamp(player.transform.position.y + 3, -5, 5));
            Boss.InitializeEncounter(run, events, pools, player, settings.BossSettings, Boss.GetComponentInChildren<LineRenderer>(), Boss.GetComponent<AudioSource>(), refuges,
                settings.Catalog.FinalBossEncounter.HealthMultiplier, settings.Catalog.FinalBossEncounter.DamageMultiplier);
            Boss.AutomaticTick = false; Boss.AutomaticPatterns = true;
        }
        private void Reward(EnemyReward reward)
        {
            // The death was validated by PooledEnemy before combat callbacks. A callback may
            // pause/open a modal before the sink runs; retain that already-issued obligation.
            if (run.Outcome.HasValue || disposed) return;
            if (!rewardSources.TryGetValue(reward.DefinitionId, out int source)) return;
            var origin = miniRewardSources.Contains(reward.DefinitionId) ? RewardOrigin.MiniBoss : RewardOrigin.Common;
            if (!Rewards.ProcessDefeat(source, reward.LeaseId, origin, reward.Experience, reward.Position.x, reward.Position.y, true)) return;
            results.RecordIssuedDefeat(reward.DefinitionId, reward.Coins);
            MaterializeRewards();
        }
        private bool MaterializeHealingView(RewardEntry entry)
        {
            int commonActive = 0, free = -1;
            for (int i = 0; i < healingPickups.Length; i++)
            {
                if (healingPickups[i] != null && !pools.IsActive(healingPickups[i], PoolFamily.Pickup)) healingPickups[i] = null;
                if (healingPickups[i] == null) { if (free < 0) free = i; }
                else if (healingPickups[i].BoundRewardId.Kind == RewardKind.HealCommon) commonActive++;
            }
            if (free < 0 || (entry.Kind == RewardKind.HealCommon && commonActive >= settings.HealingCapacity - settings.MiniBossReservedHealingSlots)) return false;
            var pickup = pools.Acquire<HealingPickup>(healingKey);
            if (pickup == null) return false;
            try
            {
                pickup.Initialize(pools, Rewards, entry.Id, player, run);
                pickup.transform.position = new Vector2(entry.X, entry.Y); healingPickups[free] = pickup; return true;
            }
            catch { pools.Release(pickup); throw; }
        }
        private bool MaterializeHealing(RewardKind kind)
        {
            while (Rewards.TryGetPending(kind, out var entry))
            {
                bool available = HealingMaterializer != null && HealingMaterializer(entry);
                Rewards.TryMaterialize(entry.Id, available);
                if (!available) return false;
            }
            return true;
        }
        private void MaterializeRewards()
        {
            if (!run.IsSimulating || disposed || pools.IsDisposed) return;
            // Mini healing has first access to the future shared healing pool; XP is independent.
            if (MaterializeHealing(RewardKind.HealMiniBoss)) MaterializeHealing(RewardKind.HealCommon);
            int free = 0;
            for (int i = 0; i < pickups.Length; i++)
            {
                if (pickups[i] != null && !pools.IsActive(pickups[i], PoolFamily.Pickup)) pickups[i] = null;
                if (pickups[i] == null) free++;
            }
            int slot = 0;
            while (free > 0 && Rewards.TryGetPending(RewardKind.Experience, out var entry))
            {
                var pickup = ExperiencePickup.Spawn(pools, xpKey, upgrades, entry.Experience, new Vector2(entry.X, entry.Y));
                if (pickup == null) { Rewards.TryMaterialize(entry.Id, false); return; }
                while (pickups[slot] != null) slot++;
                pickup.BindReward(Rewards, entry.Id, collectExperience);
                Rewards.TryMaterialize(entry.Id, true);
                pickups[slot] = pickup; free--;
            }
            if (Rewards.TryGetPending(RewardKind.Experience, out var waiting)) Rewards.TryMaterialize(waiting.Id, false);
        }
        private void OnCombat(CombatEvent value)
        {
            if (!Ready || !run.IsSimulating) return;
            if (value.Type == CombatEventType.DamageApplied)
            {
                if (value.Target == player.GetInstanceID()) results.BreakStreak();
                else if (sources.TryGetValue(value.Source, out string id)) results.RecordWeaponDamage(id, value.Amount);
            }
            if (value.Type == CombatEventType.EntityDefeated && Boss != null && value.Target == Boss.GetInstanceID())
            { results.RecordDefeat(LakeEncounterCatalog.FinalBossId); results.AddCoins(settings.BossCoins); }
        }
        private void UpdateFacts()
        {
            if (sceneRelics != null) sceneRelics.RefreshUnlocks();
            if (upgrades.Build.History.Count != buildVersion)
            {
            buildVersion = upgrades.Build.History.Count;
            foreach (var recipe in recipes)
                if (upgrades.Build.Passives.ContainsKey(recipe.RequiredPassive.Id) &&
                    (upgrades.Build.Weapons.ContainsKey(recipe.BaseWeapon.Id) || upgrades.Build.Evolutions.ContainsKey(recipe.BaseWeapon.Id))) results.RecordSynergy(recipe.Id);
            }
            if (relics.Version == factsVersion) return;
            factsVersion = relics.Version;
            foreach (string id in relics.UnlockedIds) if (knownUnlocks.Add(id)) results.RecordItem(id, 1);
            if (relics.Version != savedVersion) dirty = true;
        }
        public void TogglePause() { if (!Ready || ending) return; if (run.IsSimulating) run.TryTransition(RunPhase.Paused); else if (run.Phase == RunPhase.Paused) run.TryResume(); }
        public void Abandon() { TryAbandon(); }
        public bool TryAbandon()
        {
            if (!Ready || run.Outcome.HasValue) return false;
            UpdateFacts();
            bool accepted = run.TryTransition(RunPhase.Abandoned); if (accepted) Finish(); return accepted;
        }
        private void OnEnded(RunPhase phase)
        {
            ending = true; Result = results.FinalizeResult(); dirty = true;
            Spawns.Dispose(); Scheduler.enabled = false; weapons.enabled = false;
            if (Boss != null) Boss.enabled = false;
            foreach (var pickup in pickups) pools.Release(pickup);
            foreach (var pickup in healingPickups) pools.Release(pickup);
            Array.Clear(pickups, 0, pickups.Length);
            Array.Clear(healingPickups, 0, healingPickups.Length);
        }
        private void Finish()
        {
            if (!ending || announced) return;
            announced = true;
            if (Boss != null) pools.Release(Boss);
            ResultAvailable?.Invoke(Result);
            events.Publish(new PresentationEvent(++tick, tick, PresentationEventType.ResultsAvailable, EventPriority.Essential, 5, player != null ? (Vector2)player.transform.position : Vector2.zero));
        }
        private void PhaseChanged(RunPhase phase)
        {
            if (!run.Outcome.HasValue) UpdateFacts();
            if (pauseButton != null)
            { pauseButton.interactable = run.IsSimulating || phase == RunPhase.Paused; pauseButton.GetComponentInChildren<Text>().text = phase == RunPhase.Paused ? "CONTINUAR" : "PAUSAR"; }
            if (abandonButton != null) abandonButton.interactable = !run.Outcome.HasValue;
        }
        private Rect VisibleArea()
        {
            if (view == null) return new Rect(-8, -4.5f, 16, 9);
            float height = view.orthographicSize * 2, width = height * view.aspect;
            return new Rect(view.transform.position.x - width / 2, view.transform.position.y - height / 2, width, height);
        }
        private void LateUpdate()
        {
            if (!Ready || disposed) return;
            if (view != null && player != null)
            {
                float halfHeight = view.orthographicSize, halfWidth = halfHeight * view.aspect; var area = settings.CameraBounds;
                float x = area.width <= halfWidth * 2 ? area.center.x : Mathf.Clamp(player.transform.position.x, area.xMin + halfWidth, area.xMax - halfWidth);
                float y = area.height <= halfHeight * 2 ? area.center.y : Mathf.Clamp(player.transform.position.y, area.yMin + halfHeight, area.yMax - halfHeight);
                view.transform.position = new Vector3(x, y, -10);
            }
            if (ending) Finish();
            if (relics.Version != savedVersion) dirty = true;
            if (!run.IsSimulating && dirty) Persist();
        }
        public SaveOperationResult Persist()
        {
            if (!Ready) return null;
            LastSave = save.Save(relics); savedVersion = relics.Version; dirty = false; return LastSave;
        }
        public RunProgressCommitResult CommitProgress(IEnumerable<ResultChallengeRule> rules)
        {
            if (Result == null) throw new InvalidOperationException("A terminal result is required.");
            var committed = save.CommitResult(RunId, Result, rules, relics);
            LastSave = committed.Save;
            if (LastSave.Status != SaveStatus.Failed) { savedVersion = relics.Version; dirty = false; }
            return committed;
        }
        private void OnApplicationPause(bool paused) { if (Ready && paused) { if (run.IsSimulating) TogglePause(); Persist(); } }
        private void OnDestroy()
        {
            if (!Ready || disposed) return;
            if (!run.Outcome.HasValue) TryAbandon();
            if (dirty || relics.Version != savedVersion || LastSave?.Status == SaveStatus.Failed) Persist();
            disposed = true; events.Combat -= OnCombat; run.Ended -= OnEnded; run.Changed -= PhaseChanged;
            results.Dispose(); Spawns.Dispose();
            if (bootstrap != null) bootstrap.ExternalClock = false;
            if (pauseButton != null) pauseButton.onClick.RemoveListener(TogglePause);
            if (abandonButton != null) abandonButton.onClick.RemoveListener(Abandon);
        }
    }
}
