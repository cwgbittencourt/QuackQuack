using System;
using System.Threading;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Progression;
using QuackQuack.Systems.Combat;
using QuackQuack.Gameplay.Player;
using QuackQuack.Systems.Pooling;
using QuackQuack.Systems.Upgrades;
using UnityEngine;

namespace QuackQuack.Gameplay.Pickups
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class ExperiencePickup : MonoBehaviour, IPoolLifecycle
    {
        public const int PhysicsLayer = 10;
        private PoolService pools;
        private UpgradeSystem upgrades;
        private int amount;
        private RewardLedger ledger;
        private RewardId rewardId;
        private Func<int, bool> collect;
        private long acquisitionSequence, activeLease;
        public long ViewLeaseId => activeLease;
        public RewardId BoundRewardId => rewardId;
        public RewardState ViewState { get; private set; } = RewardState.Released;
        public Transform AttractionTarget { get; private set; }
        public bool TryMarkAttracted(Transform target)
        {
            if (target == null || ViewState != RewardState.Grounded || upgrades == null || !upgrades.CanCollectExperience) return false;
            if (ledger != null && !ledger.TryMarkAttracted(rewardId)) return false;
            AttractionTarget = target; ViewState = RewardState.Attracted; return true;
        }
        public void BindReward(RewardLedger owner, RewardId id, Func<int, bool> consumer)
        {
            if (owner == null || consumer == null || upgrades == null || ledger != null || id.Kind != RewardKind.Experience ||
                !owner.TryGetEntry(id, out var entry) || entry.Experience != amount ||
                (entry.State != RewardState.Due && entry.State != RewardState.WaitingForView))
                throw new ArgumentException("An initialized XP view and unique ledger binding are required.");
            ledger = owner; rewardId = id; collect = consumer;
        }
        private bool acquired, claimed;
        private GameplayEventBus rareEvents;
        private long rareId;
        private static long nextRareId;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetIds() { nextRareId = 0; }

        private void Awake() { gameObject.layer = PhysicsLayer; GetComponent<CircleCollider2D>().isTrigger = true; }

        public void Initialize(PoolService owner, UpgradeSystem progression, int experienceAmount)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (progression == null) throw new ArgumentNullException(nameof(progression));
            if (experienceAmount <= 0) throw new ArgumentOutOfRangeException(nameof(experienceAmount));
            if (!acquired || !owner.IsActive(this, PoolFamily.Pickup))
                throw new InvalidOperationException("XP pickups must be acquired from their owning pickup pool.");
            if (upgrades != null) throw new InvalidOperationException("Pickup is already initialized for this acquisition.");
            pools = owner; upgrades = progression; amount = experienceAmount;
            ViewState = RewardState.Grounded;
        }

        public void InitializeRare(PoolService owner, UpgradeSystem progression, int experienceAmount, GameplayEventBus events)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));
            Initialize(owner, progression, experienceAmount);
            rareEvents = events; rareId = Interlocked.Increment(ref nextRareId);
        }

        private void OnTriggerEnter2D(Collider2D other) => TryCollect(other);
        private void OnTriggerStay2D(Collider2D other) => TryCollect(other);

        private void TryCollect(Collider2D other)
        {
            if (!acquired || claimed || upgrades == null || !upgrades.CanCollectExperience ||
                !pools.IsActive(this, PoolFamily.Pickup)) return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.isActiveAndEnabled || player.State == null || player.State.Health <= 0) return;
            claimed = true;
            long acceptingLease = activeLease;
            var acceptingPool = pools;
            var acceptedEvents = rareEvents; long acceptedId = rareId; Vector2 acceptedPosition = transform.position;
            bool accepted = ledger == null ? upgrades.TryCollectExperience(amount) : ledger.TryClaimExperience(rewardId, collect);
            if (!accepted) { if (activeLease == acceptingLease) claimed = false; return; }
            if (activeLease == acceptingLease)
            { ViewState = RewardState.Claimed; acceptingPool.Release(this); }
            acceptedEvents?.Publish(new PresentationEvent(acceptedId, acceptedId, PresentationEventType.RareReward,
                EventPriority.Feedback, 2, acceptedPosition));
        }

        public void OnAcquire() { acquired = true; claimed = false; activeLease = ++acquisitionSequence; ViewState = RewardState.Due; }
        public void OnRelease() { acquired = false; activeLease = 0; ViewState = RewardState.Released; }
        public void ResetState()
        {
            ledger?.ReturnToPending(rewardId);
            ledger = null; rewardId = default; collect = null;
            AttractionTarget = null; ViewState = RewardState.Released;
            activeLease = 0;
            pools = null; upgrades = null; rareEvents = null; rareId = 0; amount = 0; acquired = claimed = false;
        }

        // Composition helpers: creation happens only inside the pool's initial warmup.
        public static void RegisterPool(PoolService owner, string key, int capacity, Transform parent = null)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            owner.Register(key, PoolFamily.Pickup, () =>
            {
                var instance = new GameObject("Experience pickup");
                instance.SetActive(false);
                if (parent != null) instance.transform.SetParent(parent, false);
                return instance.AddComponent<ExperiencePickup>();
            }, capacity);
        }

        public static ExperiencePickup Spawn(PoolService owner, string key, UpgradeSystem progression, int amount, Vector3 position)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            var pickup = owner.Acquire<ExperiencePickup>(key);
            if (pickup == null) return null;
            try { pickup.Initialize(owner, progression, amount); pickup.transform.position = position; }
            catch { owner.Release(pickup); throw; }
            return pickup;
        }

        public static ExperiencePickup SpawnRare(PoolService owner, string key, UpgradeSystem progression, int amount, Vector3 position, GameplayEventBus events)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            var pickup = owner.Acquire<ExperiencePickup>(key);
            if (pickup == null) return null;
            try { pickup.InitializeRare(owner, progression, amount, events); pickup.transform.position = position; }
            catch { owner.Release(pickup); throw; }
            return pickup;
        }
    }
}
