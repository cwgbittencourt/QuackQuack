using System;

namespace QuackQuack.Core.Progression
{
    public sealed class ExperienceState
    {
        private readonly int[] thresholds;
        public int Experience { get; private set; }
        public int Level { get; private set; } = 1;

        public ExperienceState(int[] cumulativeThresholds)
        {
            if (cumulativeThresholds == null) throw new ArgumentNullException(nameof(cumulativeThresholds));
            thresholds = (int[])cumulativeThresholds.Clone();
            int previous = 0;
            foreach (int threshold in thresholds)
            {
                if (threshold <= previous) throw new ArgumentException("XP thresholds must be positive and strictly increasing.", nameof(cumulativeThresholds));
                previous = threshold;
            }
        }

        public bool TryAdd(int amount, out int levelsGained)
        {
            levelsGained = 0;
            if (amount <= 0 || amount > int.MaxValue - Experience) return false;
            Experience += amount;
            while (Level <= thresholds.Length && Experience >= thresholds[Level - 1])
            { Level++; levelsGained++; }
            return true;
        }
    }
}
