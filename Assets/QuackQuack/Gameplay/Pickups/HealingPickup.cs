using System;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Player;
using QuackQuack.Systems.Pooling;
using UnityEngine;

namespace QuackQuack.Gameplay.Pickups
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class HealingPickup : MonoBehaviour, IPoolLifecycle
    {
        private PoolService pools;
        private RewardLedger ledger;
        private RewardId rewardId;
        private PlayerController player;
        private RunState run;
        private bool acquired, claimed;
        private long acquisitionSequence, activeLease;
        public long ViewLeaseId => activeLease;
        public RewardId BoundRewardId => rewardId;
        public RewardState ViewState { get; private set; } = RewardState.Released;
        public Transform AttractionTarget { get; private set; }
        private void Awake() { gameObject.layer = ExperiencePickup.PhysicsLayer; GetComponent<CircleCollider2D>().isTrigger = true; }

        public void Initialize(PoolService owner, RewardLedger rewards, RewardId id, PlayerController target, RunState state)
        {
            if (owner == null || rewards == null || target == null || target.State == null || state == null ||
                !rewards.TryGetEntry(id, out var entry) ||
                (entry.Kind != RewardKind.HealCommon && entry.Kind != RewardKind.HealMiniBoss) ||
                (entry.State != RewardState.Due && entry.State != RewardState.WaitingForView))
                throw new ArgumentException("A valid pending healing obligation and player are required.");
            if (!acquired || !owner.IsActive(this, PoolFamily.Pickup) || ledger != null)
                throw new InvalidOperationException("Healing pickups must be acquired once from their owning pool.");
            pools = owner; ledger = rewards; rewardId = id; player = target; run = state; ViewState = RewardState.Grounded;
        }
        public bool TryMarkAttracted(Transform target)
        {
            if (ledger == null || target == null || player == null || target != player.transform || ViewState != RewardState.Grounded || !run.IsSimulating ||
                player.State.Health <= 0 || player.State.Health >= player.State.MaxHealth || !ledger.TryMarkAttracted(rewardId)) return false;
            AttractionTarget = target; ViewState = RewardState.Attracted; return true;
        }
        private void OnTriggerEnter2D(Collider2D other) => TryCollect(other);
        private void OnTriggerStay2D(Collider2D other) => TryCollect(other);
        private void TryCollect(Collider2D other)
        {
            if (!acquired || claimed || ledger == null || !run.IsSimulating || !pools.IsActive(this, PoolFamily.Pickup) ||
                player == null || !player.isActiveAndEnabled || other == null || other.GetComponentInParent<PlayerController>() != player) return;
            claimed = true;
            if (ledger.TryCollectHeal(rewardId, player.State) <= 0) { claimed = false; return; }
            ViewState = RewardState.Claimed; pools.Release(this);
        }
        public void OnAcquire() { acquired = true; claimed = false; activeLease = ++acquisitionSequence; ViewState = RewardState.Due; }
        public void OnRelease() { acquired = false; activeLease = 0; ViewState = RewardState.Released; }
        public void ResetState()
        {
            ledger?.ReturnToPending(rewardId);
            pools = null; ledger = null; rewardId = default; player = null; run = null;
            AttractionTarget = null; acquired = claimed = false; ViewState = RewardState.Released;
            activeLease = 0;
        }
    }
}
