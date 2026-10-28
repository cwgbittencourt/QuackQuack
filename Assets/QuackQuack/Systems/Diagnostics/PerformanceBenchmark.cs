using System;
using QuackQuack.Content.Catalogs;
using QuackQuack.Content.Characters;
using QuackQuack.Content.Enemies;
using QuackQuack.Content.Weapons;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Run;
using QuackQuack.Core.Shared;
using QuackQuack.Gameplay.Enemies;
using QuackQuack.Gameplay.Pickups;
using QuackQuack.Gameplay.Player;
using QuackQuack.Gameplay.Weapons;
using QuackQuack.Platform.Input;
using QuackQuack.Systems.Combat;
using QuackQuack.Systems.Pooling;
using QuackQuack.Systems.Spawning;
using QuackQuack.Systems.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace QuackQuack.Systems.Diagnostics
{
    // An isolated workload, not a run director or performance acceptance result.
    public sealed class PerformanceBenchmark : MonoBehaviour
    {
        private sealed class NeutralInput : IPlayerInput { public PlayerCommand Read() => default; }
        public const float StepSeconds = 0.02f;
        public const int ProjectileCapacity = 512, PickupCapacity = 512, VfxCapacity = 128;
        public const int WarmupTicks = 250, StageTicks = 1500;
        [SerializeField] private int seed = 71;
        [SerializeField] private CharacterDefinition character;
        [SerializeField] private LakeEncounterCatalog enemies;
        [SerializeField] private WeaponDefinition projectile;
        [SerializeField] private ExperiencePickup pickupPrefab;
        [SerializeField] private PooledQuackFeedback feedbackPrefab;
        [SerializeField] private Text status;
        private PoolService pools;
        private SpawnDirector director;
        private EnemyUpdateScheduler scheduler;
        private UpgradeSystem upgrades;
        private RunState run;
        private PlayerController player;
        private EnemyDefinition[] definitions;
        private WeaponProjectile[] shots;
        private ExperiencePickup[] pickups;
        private PooledQuackFeedback[] effects;
        private uint random;
        private long request;
        private Transform instances;
        public bool AutomaticTick { get; set; } = true;
        public bool Ready { get; private set; }
        public bool Completed => Tick >= WarmupTicks + StageTicks * 2;
        public int Tick { get; private set; }
        public int Seed => seed;
        public int EnemyTarget { get; private set; }
        public int CreatedObjects { get; private set; }
        public int RecycledObjects { get; private set; }
        public int SaturationChecks { get; private set; }
        public PoolService Pools => pools;
        public SpawnDirector Director => director;
        public EnemyUpdateScheduler Scheduler => scheduler;

        private void Start() => Initialize();
        private void FixedUpdate() { if (AutomaticTick) Step(); }

        public void Initialize()
        {
            if (Ready) return;
            if (character == null || enemies == null || projectile == null || pickupPrefab == null || feedbackPrefab == null)
                throw new InvalidOperationException("Benchmark scene references are incomplete.");
            random = unchecked((uint)seed); if (random == 0) random = 1;
            instances = new GameObject("Prewarmed benchmark objects").transform;
            instances.SetParent(transform, false);
            pools = new PoolService();
            run = new RunState(seed); run.TryTransition(RunPhase.Playing);
            var events = new GameplayEventBus();
            var playerRoot = new GameObject("Benchmark target"); playerRoot.transform.SetParent(instances, false);
            player = playerRoot.AddComponent<PlayerController>();
            player.Initialize(character, new NeutralInput(), run, events);
            player.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            player.GetComponent<CircleCollider2D>().enabled = false;
            // Keep scheduler attacks active without allowing a death or XP modal to end the workload.
            player.State.TakeDamage(1, 0, float.MaxValue);
            var common = enemies.CommonEnemies;
            definitions = new EnemyDefinition[common.Count];
            for (int i = 0; i < common.Count; i++)
            {
                var definition = common[i]; definitions[i] = definition;
                pools.Register(definition.PoolKey, PoolFamily.Enemy, () =>
                {
                    CreatedObjects++;
                    return Instantiate(definition.Prefab, instances).GetComponent<PooledEnemy>();
                }, 400 / common.Count);
            }
            pools.Register("benchmark/projectiles", PoolFamily.Projectile, () =>
            {
                CreatedObjects++;
                var item = new GameObject("Benchmark projectile"); item.SetActive(false); item.transform.SetParent(instances, false);
                var shot = item.AddComponent<WeaponProjectile>();
                shot.GetComponent<SpriteRenderer>().sortingOrder = 8;
                return shot;
            }, ProjectileCapacity);
            pools.Register("benchmark/pickups", PoolFamily.Pickup, () => { CreatedObjects++; return Instantiate(pickupPrefab, instances); }, PickupCapacity);
            pools.Register("benchmark/vfx", PoolFamily.Vfx, () =>
            {
                CreatedObjects++; var item = Instantiate(feedbackPrefab, instances); item.Initialize(pools); return item;
            }, VfxCapacity);
            upgrades = new UpgradeSystem(seed);
            shots = new WeaponProjectile[ProjectileCapacity]; pickups = new ExperiencePickup[PickupCapacity]; effects = new PooledQuackFeedback[VfxCapacity];
            director = new SpawnDirector(run, pools, events, player.transform, definitions, 400, 16, 400, 256, 0, seed, _ => { });
            var schedulerRoot = new GameObject("Production enemy scheduler"); schedulerRoot.transform.SetParent(instances, false);
            scheduler = schedulerRoot.AddComponent<EnemyUpdateScheduler>(); scheduler.AutomaticTick = false;
            scheduler.Initialize(run, director, pools, events, player, enemies.BehaviorSettings, definitions);
            AddEnemies(200);
            FillSaturation();
            Ready = true;
            ShowStage("WARMUP / 200");
        }

        private void AddEnemies(int count)
        {
            foreach (var d in definitions)
                if (!director.TryEnqueue(new SpawnRequest(++request, "benchmark", d.Id, count / definitions.Length,
                    new Rect(-12, -6, 24, 12), 2, EventPriority.Feedback, long.MaxValue)))
                    throw new InvalidOperationException("Benchmark spawn queue rejected a request.");
            director.Process(Tick, new Rect(-1, -1, 2, 2));
            EnemyTarget += count;
            if (director.ActiveCount != EnemyTarget) throw new InvalidOperationException("Benchmark could not reach its enemy target.");
        }

        public void Step()
        {
            if (!Ready || Completed) return;
            Tick++;
            if (Tick == WarmupTicks) ShowStage("MEASURE / 200");
            if (Tick == WarmupTicks + StageTicks) { AddEnemies(200); ShowStage("MEASURE / 400"); }
            run.Advance(StepSeconds);
            scheduler.Advance(StepSeconds);
            for (int i = 0; i < shots.Length; i++)
            {
                var shot = shots[i]; shot.Age += StepSeconds;
                shot.transform.position += (Vector3)(shot.Direction * projectile.ProjectileSpeed * StepSeconds);
                if (shot.Age >= 1)
                {
                    pools.Release(shot); shots[i] = null; RecycledObjects++;
                }
            }
            for (int i = 0; i < effects.Length; i++) effects[i].View.Advance(StepSeconds);
            // A bounded pickup batch is returned/reacquired every tick, without granting XP.
            for (int i = 0; i < 16; i++)
            {
                int index = (Tick * 16 + i) % pickups.Length;
                pools.Release(pickups[index]); pickups[index] = null; RecycledObjects++;
            }
            FillSaturation();
            if (Completed) ShowStage("COMPLETE / 400 (reload scene to repeat)");
        }

        private void FillSaturation()
        {
            for (int i = 0; i < shots.Length; i++)
                if (shots[i] == null)
                {
                    var shot = pools.Acquire<WeaponProjectile>("benchmark/projectiles"); shots[i] = shot;
                    shot.Begin(projectile, projectile.GetLevel(1), ProjectileMotion.Straight, Position(),
                        new Vector2(Unit() - 0.5f, Unit() - 0.5f).normalized, null);
                }
            for (int i = 0; i < pickups.Length; i++)
                if (pickups[i] == null) pickups[i] = ExperiencePickup.Spawn(pools, "benchmark/pickups", upgrades, 1, Position());
            // Clear every expired reference before acquiring: LIFO reuse can return
            // an instance still referenced by a later slot from its previous lease.
            for (int i = 0; i < effects.Length; i++)
                if (effects[i] != null && !pools.IsActive(effects[i], PoolFamily.Vfx)) { effects[i] = null; RecycledObjects++; }
            for (int i = 0; i < effects.Length; i++)
                if (effects[i] == null)
                {
                    effects[i] = pools.Acquire<PooledQuackFeedback>("benchmark/vfx");
                    effects[i].View.Play(Position(), 0.5f, new Color(0.4f, 0.9f, 1), 0.5f);
                }
            // Explicit exhaustion must return null, never grow a pool.
            if (pools.Acquire<WeaponProjectile>("benchmark/projectiles") != null ||
                pools.Acquire<ExperiencePickup>("benchmark/pickups") != null || pools.Acquire<PooledQuackFeedback>("benchmark/vfx") != null)
                throw new InvalidOperationException("Benchmark pools grew beyond their configured saturation.");
            SaturationChecks++;
        }

        private Vector2 Position() => new Vector2((Unit() - 0.5f) * 24, (Unit() - 0.5f) * 12);
        private float Unit() { random ^= random << 13; random ^= random >> 17; random ^= random << 5; return (random >> 8) * (1f / 16777216f); }
        private void ShowStage(string stage)
        {
            if (status != null) status.text = "BENCHMARK T071 / seed " + seed + " / tick " + Tick + "\n" + stage +
                "\n512 projectiles / 512 pickups / 128 VFX + enemy fields\nWorkload only: no FPS acceptance (T072/T074).";
            Debug.Log("BENCHMARK_STAGE seed=" + seed + " tick=" + Tick + " enemies=" + EnemyTarget + " stage=" + stage);
        }

        private void OnDestroy()
        {
            Ready = false;
            if (scheduler != null) scheduler.enabled = false;
            director?.Dispose(); upgrades?.Dispose(); pools?.Dispose();
        }
    }
}
