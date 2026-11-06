namespace QuackQuack.Core.Shared
{
    // Value projection of the owned XP curve. A default snapshot represents uninitialized progress.
    public readonly struct ProgressSnapshot
    {
        private readonly int level;
        public int Level => level == 0 ? 1 : level;
        public readonly int Experience, PreviousThreshold, NextThreshold;
        public readonly float Fraction;

        internal ProgressSnapshot(int level, int experience, int previousThreshold, int nextThreshold, float fraction)
        {
            this.level = level; Experience = experience;
            PreviousThreshold = previousThreshold; NextThreshold = nextThreshold; Fraction = fraction;
        }
    }
}
