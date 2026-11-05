using System;
using QuackQuack.Core.Run;

namespace QuackQuack.Core.Combat
{
    public sealed class BreathSystem
    {
        public float Capacity { get; private set; }
        public float Current { get; private set; }
        public bool IsFull => Current >= Capacity;
        private readonly float landRecovery, waterRecovery;
        public BreathSystem(float capacity, float landRecovery, float waterRecovery)
        {
            if (float.IsNaN(capacity) || float.IsInfinity(capacity) || capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (float.IsNaN(landRecovery) || float.IsInfinity(landRecovery) || landRecovery < 0) throw new ArgumentOutOfRangeException(nameof(landRecovery));
            if (float.IsNaN(waterRecovery) || float.IsInfinity(waterRecovery) || waterRecovery <= landRecovery) throw new ArgumentOutOfRangeException(nameof(waterRecovery));
            Capacity = capacity; Current = capacity;
            this.landRecovery = landRecovery; this.waterRecovery = waterRecovery;
        }
        /// <summary>
        /// Accepts a positive reserve even when it cannot cover the full cost.
        /// Enhanced reflects the charge before consumption; rejected requests do not mutate it.
        /// </summary>
        public bool TryConsume(float cost, out bool enhanced)
        {
            enhanced = false;
            if (float.IsNaN(cost) || float.IsInfinity(cost) || cost <= 0 || cost > Capacity || Current <= 0) return false;
            enhanced = IsFull;
            Current = Math.Max(0, Current - cost);
            return true;
        }
        /// <summary>
        /// Advances recovery using simulation seconds supplied by the caller, capped at capacity.
        /// Invalid time or terrain is rejected before changing the reserve.
        /// </summary>
        public void Recover(float seconds, TerrainType terrain)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (terrain != TerrainType.Land && terrain != TerrainType.Water) throw new ArgumentOutOfRangeException(nameof(terrain));
            RecoverAtRate(seconds, terrain == TerrainType.Water ? waterRecovery : landRecovery);
        }

        // Increasing capacity does not grant breath; shrinking clamps excess reserve.
        public void SetCapacity(float capacity)
        {
            if (float.IsNaN(capacity) || float.IsInfinity(capacity) || capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity;
            Current = Math.Min(Current, capacity);
        }

        public void RecoverAtRate(float seconds, float rate)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (float.IsNaN(rate) || float.IsInfinity(rate) || rate < 0) throw new ArgumentOutOfRangeException(nameof(rate));
            Current = (float)Math.Min(Capacity, (double)Current + (double)seconds * rate);
        }
    }
}
