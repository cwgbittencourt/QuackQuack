using System;
using QuackQuack.Gameplay.Player;
using QuackQuack.Systems.Pooling;
using QuackQuack.Systems.Upgrades;
using UnityEngine;

namespace QuackQuack.Gameplay.Pickups
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class ExperiencePickup : MonoBehaviour, IPoolLifecycle
    {
        private PoolService pools;
        private UpgradeSystem upgrades;
        private int amount;
        private bool acquired, claimed;

        private void Awake() => GetComponent<CircleCollider2D>().isTrigger = true;

        public void Initialize(PoolService owner, UpgradeSystem progression, int experienceAmount)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (progression == null) throw new ArgumentNullException(nameof(progression));
            if (experienceAmount <= 0) throw new ArgumentOutOfRangeException(nameof(experienceAmount));
            if (!acquired || !owner.IsActive(this, PoolFamily.Pickup))
                throw new InvalidOperationException("XP pickups must be acquired from their owning pickup pool.");
            if (upgrades != null) throw new InvalidOperationException("Pickup is already initialized for this acquisition.");
            pools = owner; upgrades = progression; amount = experienceAmount;
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
            if (!upgrades.TryCollectExperience(amount)) { claimed = false; return; }
            pools.Release(this);
        }

        public void OnAcquire() { acquired = true; claimed = false; }
        public void OnRelease() { acquired = false; }
        public void ResetState() { pools = null; upgrades = null; amount = 0; acquired = claimed = false; }

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
    }
}
