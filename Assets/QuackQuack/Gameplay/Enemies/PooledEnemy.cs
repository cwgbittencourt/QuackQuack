using System;
using System.Threading;
using QuackQuack.Content.Enemies;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Run;
using QuackQuack.Systems.Combat;
using QuackQuack.Systems.Pooling;
using UnityEngine;

namespace QuackQuack.Gameplay.Enemies
{
    [RequireComponent(typeof(Rigidbody2D),typeof(CircleCollider2D))]
    public sealed class PooledEnemy : MonoBehaviour, IPoolLifecycle
    {
        private static long nextLease;
        private static long nextCombatCorrelation;
        private long lease;
        private bool acquired;
        private PoolService owner;
        private RunState run;
        private GameplayEventBus events;
        private Action<EnemyReward> rewardSink;
        private Rigidbody2D body;
        private CircleCollider2D shape;
        private LayerMask originalExcludedLayers;
        private bool contactFilterOverridden;
        public Rigidbody2D Body=>body;
        public CircleCollider2D Shape=>shape;
        public EnemyState State { get; } = new EnemyState();
        public EnemyDefinition Definition { get; private set; }
        public Transform Target { get; private set; }
        public long LeaseId => lease;
        public float DamageMultiplier { get; private set; } = 1;
        public bool ConfigureEncounter(float healthMultiplier, float damageMultiplier)
        {
            if (!IsReady || float.IsNaN(damageMultiplier) || float.IsInfinity(damageMultiplier) || damageMultiplier <= 0
                || !State.TryScaleEncounter(lease, healthMultiplier)) return false;
            DamageMultiplier = damageMultiplier; return true;
        }
        public bool IsReady => acquired && owner != null && !owner.IsDisposed && State.IsActive;

        // Enemy/enemy and enemy/XP pairs do not drive gameplay. Keep physical player
        // and terrain contacts, and keep this collider visible to combat queries.
        public void SuppressUnneededContacts()
        {
            if (!IsReady) throw new InvalidOperationException("An active enemy lease is required.");
            if (!contactFilterOverridden) originalExcludedLayers = shape.excludeLayers;
            shape.excludeLayers |= (1 << 9) | (1 << QuackQuack.Gameplay.Pickups.ExperiencePickup.PhysicsLayer);
            contactFilterOverridden = true;
        }

        private void Awake() { body = GetComponent<Rigidbody2D>(); shape=GetComponent<CircleCollider2D>(); body.gravityScale = 0; body.freezeRotation=true; }

        public static void RegisterPool(PoolService pools, EnemyDefinition definition, Transform parent = null)
        {
            if (pools == null) throw new ArgumentNullException(nameof(pools));
            if (definition == null || !definition.IsValid(out _) || definition.Prefab.GetComponent<PooledEnemy>() == null)
                throw new ArgumentException("A valid enemy definition with a root PooledEnemy prefab is required.");
            pools.Register(definition.PoolKey, PoolFamily.Enemy,
                () => Instantiate(definition.Prefab, parent).GetComponent<PooledEnemy>(), definition.PoolMaxSize);
        }

        public static PooledEnemy Spawn(PoolService pools, EnemyDefinition definition, RunState run,
            GameplayEventBus events, Transform target, Vector2 position, Action<EnemyReward> rewardSink)
            => SpawnCore(pools, definition, run, events, target, position, rewardSink, true);

        // SpawnDirector validates immutable definitions once at composition, outside the spawn loop.
        internal static PooledEnemy SpawnPrepared(PoolService pools, EnemyDefinition definition, RunState run,
            GameplayEventBus events, Transform target, Vector2 position, Action<EnemyReward> rewardSink)
            => SpawnCore(pools, definition, run, events, target, position, rewardSink, false);

        private static PooledEnemy SpawnCore(PoolService pools, EnemyDefinition definition, RunState run,
            GameplayEventBus events, Transform target, Vector2 position, Action<EnemyReward> rewardSink, bool validate)
        {
            if (pools == null || pools.IsDisposed || run == null || !run.IsSimulating) return null;
            if (definition == null || (validate && !definition.IsValid(out _))) throw new ArgumentException("Invalid enemy definition.");
            var instance = pools.Acquire<PooledEnemy>(definition.PoolKey);
            if (instance == null) return null;
            try { instance.InitializeCore(pools, definition, run, events, target, position, rewardSink, false); return instance; }
            catch { pools.Release(instance); throw; }
        }

        public void Initialize(PoolService pools, EnemyDefinition definition, RunState run, GameplayEventBus events,
            Transform target, Vector2 position, Action<EnemyReward> rewardSink)
            => InitializeCore(pools, definition, run, events, target, position, rewardSink, true);

        private void InitializeCore(PoolService pools, EnemyDefinition definition, RunState run, GameplayEventBus events,
            Transform target, Vector2 position, Action<EnemyReward> rewardSink, bool validate)
        {
            if (!acquired || pools == null || !pools.IsActive(this, PoolFamily.Enemy) || State.IsActive)
                throw new InvalidOperationException("Enemy must be acquired once from its owning enemy pool.");
            if (definition == null || (validate && !definition.IsValid(out _)) || run == null || !run.IsSimulating || events == null || rewardSink == null)
                throw new ArgumentException("Valid enemy services and reward consumer are required.");
            if (!State.TryActivate(lease, definition.Id, definition.Health, definition.ExperienceReward, definition.CoinReward,
                position, target == null ? 0 : target.GetInstanceID())) throw new ArgumentException("Invalid enemy lease data.");
            owner = pools; this.run = run; this.events = events; this.rewardSink = rewardSink;
            Definition = definition; Target = target; transform.position = position; body.position = position;
        }

        public bool TakeDamage(long expectedLease, float damage, int source, long tick)
        {
            if (!IsReady || expectedLease != lease || !run.IsSimulating || !owner.IsActive(this, PoolFamily.Enemy)) return false;
            State.TrySetPosition(lease, body.position);
            if (!State.TryDamage(expectedLease, damage, out float applied, out EnemyReward? reward)) return false;
            var bus = events; var sink = rewardSink; var position = State.Position;
            int targetId = GetInstanceID(); long correlation = Interlocked.Increment(ref nextCombatCorrelation);
            if (reward.HasValue) owner.Release(this); // Cleanup before notifications/reentrant callbacks.
            try
            {
                bus.Publish(new CombatEvent(tick, correlation, source, targetId, position, applied, CombatEventType.DamageApplied));
                if (reward.HasValue) bus.Publish(new CombatEvent(tick, correlation, source, targetId, position, 0, CombatEventType.EntityDefeated));
            }
            finally { if (reward.HasValue) sink(reward.Value); }
            return true;
        }

        public bool Despawn(long expectedLease) => expectedLease == lease && IsReady && owner.Release(this);
        public void OnAcquire() { acquired = true; lease = Interlocked.Increment(ref nextLease); }
        public void OnRelease() { acquired = false; }
        public void ResetState()
        {
            if (contactFilterOverridden && shape != null) shape.excludeLayers = originalExcludedLayers;
            contactFilterOverridden = false;
            if (State.IsActive) State.Reset(State.LeaseId);
            acquired = false; lease = 0; owner = null; run = null; events = null; rewardSink = null; DamageMultiplier = 1;
            Definition = null; Target = null;
            if (body == null) body = GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero; body.angularVelocity = 0;
            body.position = Vector2.zero; body.rotation = 0;
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }
}
