using System;
using System.Collections.Generic;
using QuackQuack.Core.Progression;

namespace QuackQuack.Core.Run
{
    // Receives accepted gameplay facts. Event attribution/deduplication belongs to the producer.
    // A terminal transition freezes the snapshot even if the UI asks for it much later.
    public sealed class RunResultSession : IDisposable
    {
        private readonly RunState run;
        private readonly BuildState build;
        private readonly RelicLoadout relics;
        private readonly int victoryBonus;
        private readonly double abandonmentFactor;
        private readonly Dictionary<string, int> defeatsByEnemy = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, double> damageByWeapon = new Dictionary<string, double>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> items = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, RunRecordProgress> records = new Dictionary<string, RunRecordProgress>(StringComparer.Ordinal);
        private readonly Dictionary<string, RunChallengeProgress> challenges = new Dictionary<string, RunChallengeProgress>(StringComparer.Ordinal);
        private readonly HashSet<string> synergies = new HashSet<string>(StringComparer.Ordinal);
        private int coins, defeats, streak, longestStreak;
        private bool bossReached, disposed;
        private string bossId = string.Empty;
        private RunResult result;

        public RunResultSession(RunState run, int earnedCoins, int victoryBonus, double abandonmentFactor)
            : this(run, earnedCoins, victoryBonus, abandonmentFactor, null, null) { }

        public RunResultSession(RunState run, int earnedCoins, int victoryBonus, double abandonmentFactor,
            BuildState build, RelicLoadout relics = null)
        {
            this.run = run ?? throw new ArgumentNullException(nameof(run));
            if (earnedCoins < 0) throw new ArgumentOutOfRangeException(nameof(earnedCoins));
            if (victoryBonus < 0) throw new ArgumentOutOfRangeException(nameof(victoryBonus));
            if (!Finite(abandonmentFactor) || abandonmentFactor < 0 || abandonmentFactor > 1)
                throw new ArgumentOutOfRangeException(nameof(abandonmentFactor));
            coins = earnedCoins; this.victoryBonus = victoryBonus; this.abandonmentFactor = abandonmentFactor;
            this.build = build; this.relics = relics;
            bossReached = run.Phase == RunPhase.Boss || run.Outcome == RunPhase.Won;
            run.Ended += OnEnded; run.Changed += OnPhaseChanged;
            if (run.Outcome.HasValue) FinalizeResult();
        }

        private bool CanRecord => !disposed && result == null && run.IsSimulating;
        private bool CanRecordSummary => !disposed && result == null && !run.Outcome.HasValue && run.Phase != RunPhase.Preparing;

        public bool RecordBossReached(string id)
        {
            if (!CanRecord || !ValidId(id)) return false;
            bossReached = true; bossId = id; return true;
        }

        public bool RecordDefeat(string enemyId)
        {
            if (!CanRecord || !ValidId(enemyId) || defeats == int.MaxValue) return false;
            defeatsByEnemy.TryGetValue(enemyId, out int count);
            defeatsByEnemy[enemyId] = count + 1; defeats++;
            streak++; longestStreak = Math.Max(longestStreak, streak);
            return true;
        }

        // The caller decides what breaks a streak (e.g. accepted damage to the player).
        public bool BreakStreak()
        {
            if (!CanRecord) return false;
            streak = 0; return true;
        }

        public bool RecordWeaponDamage(string weaponId, double appliedDamage)
        {
            if (!CanRecord || !ValidId(weaponId) || !Finite(appliedDamage) || appliedDamage <= 0) return false;
            damageByWeapon.TryGetValue(weaponId, out double previous);
            double total = previous + appliedDamage;
            if (!Finite(total)) return false;
            damageByWeapon[weaponId] = total; return true;
        }

        public bool AddCoins(int amount)
        {
            if (!CanRecord || amount <= 0 || coins > int.MaxValue - amount) return false;
            coins += amount; return true;
        }

        public bool RecordItem(string itemId, int amount)
        {
            if (!CanRecordSummary || !ValidId(itemId) || amount <= 0) return false;
            items.TryGetValue(itemId, out int previous);
            if (previous > int.MaxValue - amount) return false;
            items[itemId] = previous + amount; return true;
        }

        public bool RecordSynergy(string synergyId) => CanRecordSummary && ValidId(synergyId) && synergies.Add(synergyId);

        // These carry an evaluated result; challenge definitions and record evaluation are not duplicated here.
        public bool SetRecord(string recordId, double previous, double current)
        {
            if (!CanRecord || !ValidId(recordId) || !Finite(previous) || !Finite(current) || previous < 0 || current < 0) return false;
            if (records.TryGetValue(recordId, out var existing) && (previous != existing.Previous || current < existing.Current)) return false;
            records[recordId] = new RunRecordProgress(previous, current); return true;
        }

        public bool SetChallengeProgress(string challengeId, int previous, int current, int target)
        {
            if (!CanRecord || !ValidId(challengeId) || target <= 0 || previous < 0 || current < previous || current > target) return false;
            if (challenges.TryGetValue(challengeId, out var existing)
                && (previous != existing.Previous || target != existing.Target || current < existing.Current)) return false;
            challenges[challengeId] = new RunChallengeProgress(previous, current, target); return true;
        }

        public RunResult FinalizeResult()
        {
            if (result != null) return result;
            if (disposed || !run.Outcome.HasValue) return null;
            result = new RunResult(run, bossReached, bossId, defeats, longestStreak, build, relics,
                synergies, defeatsByEnemy, damageByWeapon, coins, victoryBonus, abandonmentFactor, items, records, challenges);
            Unsubscribe();
            return result;
        }

        private void OnEnded(RunPhase _) { FinalizeResult(); }
        private void OnPhaseChanged(RunPhase phase) { if (phase == RunPhase.Boss) bossReached = true; }
        private void Unsubscribe() { run.Ended -= OnEnded; run.Changed -= OnPhaseChanged; }
        public void Dispose() { Unsubscribe(); disposed = true; }
        private static bool ValidId(string id) => !string.IsNullOrWhiteSpace(id);
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
