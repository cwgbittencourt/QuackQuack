using QuackQuack.Core.Combat;
using UnityEngine;

namespace QuackQuack.Core.Run
{
    public enum TerrainType { Land, Water }
    public sealed class PlayerState
    {
        public float MaxHealth { get; }
        public float Health { get; private set; }
        public double InvulnerableUntil { get; private set; }
        public TerrainType Terrain { get; set; }
        public BreathSystem Breath { get; }
        public PlayerState(float health, float breathCapacity, float landRecovery, float waterRecovery)
        {
            if (health <= 0) throw new System.ArgumentOutOfRangeException(nameof(health));
            MaxHealth = Health = health;
            Breath = new BreathSystem(breathCapacity, landRecovery, waterRecovery);
        }
        public bool TakeDamage(float amount, double time, float invulnerability)
        {
            if (amount <= 0 || Health <= 0 || time < InvulnerableUntil) return false;
            Health = Mathf.Max(0, Health - amount);
            InvulnerableUntil = time + Mathf.Max(0, invulnerability);
            return true;
        }
    }
    public sealed class RiskRewardState
    {
        public bool Claimed { get; private set; }
        public bool TryClaim() { if (Claimed) return false; Claimed = true; return true; }
    }
}
