using System;
using QuackQuack.Core.Shared;

namespace QuackQuack.Core.Progression
{
    public sealed class ExperienceState
    {
        private readonly int[] thresholds;
        public int Experience { get; private set; }
        public int Level { get; private set; } = 1;
        public ProgressSnapshot Progress
        {
            get
            {
                int previous = Level == 1 ? 0 : thresholds[Level - 2];
                if (Level > thresholds.Length)
                    return new ProgressSnapshot(Level, Experience, previous, previous, 1);
                int next = thresholds[Level - 1];
                return new ProgressSnapshot(Level, Experience, previous, next,
                    (float)((double)(Experience - previous) / (next - previous)));
            }
        }

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
