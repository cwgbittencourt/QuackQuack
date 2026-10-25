using System;
using QuackQuack.Core.Run;

namespace QuackQuack.Core.Combat
{
    public sealed class BreathSystem
    {
        public float Capacity { get; }
        public float Current { get; private set; }
        public bool IsFull => Current >= Capacity;
        private readonly float landRecovery, waterRecovery;
        public BreathSystem(float capacity, float landRecovery, float waterRecovery)
        {
            if (capacity <= 0 || landRecovery < 0 || waterRecovery <= landRecovery) throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity; Current = capacity;
            this.landRecovery = landRecovery; this.waterRecovery = waterRecovery;
        }
        public bool TryConsume(float cost, out bool enhanced)
        {
            enhanced = false;
            if (cost <= 0 || cost > Capacity || Current <= 0) return false;
            enhanced = IsFull;
            Current = Math.Max(0, Current - cost);
            return true;
        }
        public void Recover(float seconds, TerrainType terrain)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            Current = Math.Min(Capacity, Current + seconds * (terrain == TerrainType.Water ? waterRecovery : landRecovery));
        }
    }
}
