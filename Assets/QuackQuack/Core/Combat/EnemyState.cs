using System;
using UnityEngine;

namespace QuackQuack.Core.Combat
{
    public readonly struct EnemyReward
    {
        public long LeaseId { get; }
        public string DefinitionId { get; }
        public int Experience { get; }
        public int Coins { get; }
        public Vector2 Position { get; }
        public EnemyReward(long leaseId, string definitionId, int experience, int coins, Vector2 position)
        { LeaseId = leaseId; DefinitionId = definitionId; Experience = experience; Coins = coins; Position = position; }
    }

    // One reusable state per pooled instance. No scene references or event subscribers.
    public sealed class EnemyState
    {
        private long lastLease;
        private int experience, coins;
        private bool encounterScaled;
        public bool TryScaleEncounter(long leaseId, float healthMultiplier)
        {
            double scaled = (double)MaxHealth * healthMultiplier;
            if (!IsActive || leaseId != LeaseId || encounterScaled || Health != MaxHealth ||
                float.IsNaN(healthMultiplier) || healthMultiplier <= 0 || scaled < float.Epsilon || scaled > float.MaxValue) return false;
            Health = MaxHealth = (float)scaled; encounterScaled = true; return true;
        }
        public long LeaseId { get; private set; }
        public string DefinitionId { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsDefeated => IsActive && Health <= 0;
        public bool RewardClaimed { get; private set; }
        public float MaxHealth { get; private set; }
        public float Health { get; private set; }
        public Vector2 Position { get; private set; }
        public Vector2 Knockback { get; private set; }
        public float InterruptionRemaining { get; private set; }
        public int TargetId { get; private set; }

        public bool TryActivate(long leaseId, string definitionId, float health, int experience, int coins, Vector2 position, int targetId)
        {
            if (IsActive || leaseId <= lastLease || string.IsNullOrWhiteSpace(definitionId) || !Finite(health) || health <= 0 ||
                experience < 0 || coins < 0 || !Finite(position)) return false;
            lastLease = LeaseId = leaseId; DefinitionId = definitionId; MaxHealth = Health = health;
            this.experience = experience; this.coins = coins; Position = position; TargetId = targetId;
            IsActive = true; RewardClaimed = false; Knockback = Vector2.zero; InterruptionRemaining = 0;
            return true;
        }

        public bool TrySetPosition(long leaseId, Vector2 position)
        { if (!Alive(leaseId) || !Finite(position)) return false; Position = position; return true; }

        public bool TrySetControl(long leaseId, Vector2 knockback, float interruptionSeconds)
        {
            if (!Alive(leaseId) || !Finite(knockback) || !Finite(interruptionSeconds) || interruptionSeconds < 0) return false;
            Knockback = knockback; InterruptionRemaining = interruptionSeconds; return true;
        }

        public bool TryDamage(long leaseId, float damage, out float applied, out EnemyReward? reward)
        {
            applied = 0; reward = null;
            if (!Alive(leaseId) || !Finite(damage) || damage <= 0) return false;
            applied = Math.Min(damage, Health); Health -= applied;
            if (Health == 0 && !RewardClaimed)
            {
                RewardClaimed = true; // Commit before any external observer can reenter.
                reward = new EnemyReward(LeaseId, DefinitionId, experience, coins, Position);
            }
            return true;
        }

        public bool Reset(long leaseId)
        {
            if (!IsActive || LeaseId != leaseId) return false;
            IsActive = false; LeaseId = 0; DefinitionId = null; MaxHealth = Health = 0;
            experience = coins = TargetId = 0; Position = Knockback = Vector2.zero;
            InterruptionRemaining = 0; RewardClaimed = false; encounterScaled = false; return true;
        }
        private bool Alive(long leaseId) => IsActive && LeaseId == leaseId && Health > 0;
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Finite(Vector2 value) => Finite(value.x) && Finite(value.y);
    }
}
