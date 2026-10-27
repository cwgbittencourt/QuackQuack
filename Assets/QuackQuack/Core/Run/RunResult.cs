using System;
using System.Collections.Generic;
using QuackQuack.Core.Progression;

namespace QuackQuack.Core.Run
{
    public readonly struct RunRecordProgress
    {
        public double Previous { get; }
        public double Current { get; }
        public bool IsNewRecord => Current > Previous;
        internal RunRecordProgress(double previous, double current) { Previous = previous; Current = current; }
    }

    public readonly struct RunChallengeProgress
    {
        public int Previous { get; }
        public int Current { get; }
        public int Target { get; }
        public int Gained => Current - Previous;
        public bool IsCompleted => Current >= Target;
        internal RunChallengeProgress(int previous, int current, int target)
        { Previous = previous; Current = current; Target = target; }
    }

    // Copies values, never retains mutable gameplay state or ScriptableObjects.
    public sealed class RunBuildSnapshot
    {
        public IReadOnlyDictionary<string, int> Weapons { get; }
        public IReadOnlyDictionary<string, int> Passives { get; }
        public IReadOnlyDictionary<string, string> Evolutions { get; }
        public IReadOnlyList<BuildChange> History { get; }
        public IReadOnlyList<string> EquippedRelicIds { get; }
        public int RerollsRemaining { get; }
        public int BanishRemaining { get; }

        internal RunBuildSnapshot(BuildState build, RelicLoadout relics)
        {
            Weapons = RunResult.CopyMap(build?.Weapons);
            Passives = RunResult.CopyMap(build?.Passives);
            Evolutions = RunResult.CopyMap(build?.Evolutions);
            History = new List<BuildChange>(build?.History ?? Array.Empty<BuildChange>()).AsReadOnly();
            EquippedRelicIds = new List<string>(relics?.EquippedIds ?? Array.Empty<string>()).AsReadOnly();
            RerollsRemaining = build?.RerollsRemaining ?? 0;
            BanishRemaining = build?.BanishRemaining ?? 0;
        }
    }

    public sealed class RunResult
    {
        public int Seed { get; }
        public RunPhase Outcome { get; }
        public bool IsVictory => Outcome == RunPhase.Won;
        public double DurationSeconds { get; }
        public bool BossReached { get; }
        public string BossId { get; }
        public int EnemiesDefeated { get; }
        public IReadOnlyDictionary<string, int> DefeatsByEnemy { get; }
        public IReadOnlyDictionary<string, double> DamageByWeapon { get; }
        public int LongestStreak { get; }
        public RunBuildSnapshot Build { get; }
        public IReadOnlyList<string> Synergies { get; }
        public int EarnedCoins { get; }
        public int VictoryBonus { get; }
        public int Coins { get; }
        public IReadOnlyDictionary<string, int> Items { get; }
        public IReadOnlyDictionary<string, RunRecordProgress> Records { get; }
        public IReadOnlyDictionary<string, RunChallengeProgress> Challenges { get; }

        internal RunResult(RunState run, bool bossReached, string bossId, int defeats, int longestStreak,
            BuildState build, RelicLoadout relics, IEnumerable<string> synergies,
            IDictionary<string, int> defeatsByEnemy, IDictionary<string, double> damageByWeapon,
            int earnedCoins, int victoryBonus, double abandonmentFactor, IDictionary<string, int> items,
            IDictionary<string, RunRecordProgress> records, IDictionary<string, RunChallengeProgress> challenges)
        {
            Seed = run.Seed; Outcome = run.Outcome.Value; DurationSeconds = run.ElapsedSeconds;
            BossReached = bossReached; BossId = bossId; EnemiesDefeated = defeats; LongestStreak = longestStreak;
            Build = new RunBuildSnapshot(build, relics);
            var sortedSynergies = new List<string>(synergies); sortedSynergies.Sort(StringComparer.Ordinal);
            Synergies = sortedSynergies.AsReadOnly();
            DefeatsByEnemy = CopyMap(defeatsByEnemy); DamageByWeapon = CopyMap(damageByWeapon);
            EarnedCoins = earnedCoins; VictoryBonus = IsVictory ? victoryBonus : 0;
            Coins = Outcome == RunPhase.Abandoned ? (int)Math.Floor(earnedCoins * abandonmentFactor)
                : (int)Math.Min(int.MaxValue, (long)earnedCoins + VictoryBonus);
            Items = CopyMap(items); Records = CopyMap(records); Challenges = CopyMap(challenges);
        }

        internal static IReadOnlyDictionary<string, T> CopyMap<T>(IEnumerable<KeyValuePair<string, T>> values)
        {
            var copy = new SortedDictionary<string, T>(StringComparer.Ordinal);
            if (values != null) foreach (var pair in values) copy.Add(pair.Key, pair.Value);
            return new System.Collections.ObjectModel.ReadOnlyDictionary<string, T>(copy);
        }
    }
}
