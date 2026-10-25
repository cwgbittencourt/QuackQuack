using QuackQuack.Core.Combat;
using System;

namespace QuackQuack.Core.Run
{
    public enum TerrainType { Land, Water }
    public sealed class PlayerState
    {
        public float MaxHealth { get; }
        public float Health { get; private set; }
        public double InvulnerableUntil { get; private set; }
        private TerrainType terrain;
        public TerrainType Terrain
        {
            get => terrain;
            set
            {
                if (value != TerrainType.Land && value != TerrainType.Water)
                    throw new ArgumentOutOfRangeException(nameof(value));
                terrain = value;
            }
        }
        public BreathSystem Breath { get; }
        public PlayerState(float health, float breathCapacity, float landRecovery, float waterRecovery)
        {
            if (float.IsNaN(health) || float.IsInfinity(health) || health <= 0)
                throw new ArgumentOutOfRangeException(nameof(health));
            MaxHealth = Health = health;
            Breath = new BreathSystem(breathCapacity, landRecovery, waterRecovery);
        }
        public bool TakeDamage(float amount, double time, float invulnerability)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) ||
                double.IsNaN(time) || double.IsInfinity(time) || time < 0 ||
                float.IsNaN(invulnerability) || float.IsInfinity(invulnerability) || invulnerability < 0)
                return false;
            if (amount <= 0 || Health <= 0 || time < InvulnerableUntil) return false;
            Health = Math.Max(0, Health - amount);
            InvulnerableUntil = time + invulnerability;
            return true;
        }
    }
    public sealed class RiskRewardState
    {
        public bool Claimed { get; private set; }
        public bool TryClaim() { if (Claimed) return false; Claimed = true; return true; }
    }
}
