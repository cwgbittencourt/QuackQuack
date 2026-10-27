using System;

namespace QuackQuack.Core.Run
{
    public enum RunPhase { Preparing, Playing, Paused, ChoosingUpgrade, Boss, Won, Lost, Abandoned, Results }

    public sealed class RunState
    {
        public int Seed { get; }
        public RunPhase Phase { get; private set; } = RunPhase.Preparing;
        public RunPhase? Outcome { get; private set; }
        public double ElapsedSeconds { get; private set; }
        public bool IsSimulating => Phase == RunPhase.Playing || Phase == RunPhase.Boss;
        public RunPhase ResumePhase { get; private set; } = RunPhase.Playing;
        public bool TryResume() => (Phase == RunPhase.Paused || Phase == RunPhase.ChoosingUpgrade) && TryTransition(ResumePhase);
        public event Action<RunPhase> Changed;
        public event Action<RunPhase> Ended;
        public event Action ResultsReady;

        public RunState(int seed) { Seed = seed; }

        public bool TryTransition(RunPhase next)
        {
            bool valid;
            switch (Phase)
            {
                case RunPhase.Preparing: valid = next == RunPhase.Playing; break;
                case RunPhase.Playing:
                    valid = next == RunPhase.Paused || next == RunPhase.ChoosingUpgrade || next == RunPhase.Boss || next == RunPhase.Lost || next == RunPhase.Abandoned;
                    break;
                case RunPhase.Paused:
                case RunPhase.ChoosingUpgrade: valid = next == ResumePhase || next == RunPhase.Abandoned; break;
                case RunPhase.Boss: valid = next == RunPhase.Paused || next == RunPhase.ChoosingUpgrade || next == RunPhase.Won || next == RunPhase.Lost || next == RunPhase.Abandoned; break;
                case RunPhase.Won:
                case RunPhase.Lost:
                case RunPhase.Abandoned: valid = next == RunPhase.Results; break;
                default: valid = false; break;
            }
            if (!valid) return false;
            if (IsSimulating && (next == RunPhase.Paused || next == RunPhase.ChoosingUpgrade)) ResumePhase = Phase;
            Phase = next;
            if (next == RunPhase.Won || next == RunPhase.Lost || next == RunPhase.Abandoned)
            {
                Outcome = next;
                Ended?.Invoke(next);
            }
            Changed?.Invoke(next);
            if (next == RunPhase.Results) ResultsReady?.Invoke();
            return true;
        }

        public void Advance(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (IsSimulating) ElapsedSeconds += seconds;
        }
    }
}
