using System;
using System.Collections.Generic;

namespace QuackQuack.Core.Run
{
    public enum ResultMetric { EnemiesDefeated, SurvivalSeconds, Victories, Evolutions }
    public readonly struct ResultChallengeRule
    {
        public string Id { get; }
        public ResultMetric Metric { get; }
        public int Target { get; }
        public bool Cumulative { get; }
        public ResultChallengeRule(string id, ResultMetric metric, int target, bool cumulative)
        {
            if (string.IsNullOrWhiteSpace(id) || !Enum.IsDefined(typeof(ResultMetric), metric) || target <= 0) throw new ArgumentException("Invalid challenge rule.");
            Id = id; Metric = metric; Target = target; Cumulative = cumulative;
        }
    }
    public sealed class RunProgressProfile
    {
        public int Coins { get; }
        public int Runs { get; }
        public int Victories { get; }
        public IReadOnlyDictionary<string, double> Records { get; }
        public IReadOnlyDictionary<string, int> Challenges { get; }
        public IReadOnlyList<string> AppliedRunIds { get; }
        public RunProgressProfile(int coins = 0, int runs = 0, int victories = 0,
            IEnumerable<KeyValuePair<string, double>> records = null, IEnumerable<KeyValuePair<string, int>> challenges = null,
            IEnumerable<string> appliedRunIds = null)
        {
            if (coins < 0 || runs < 0 || victories < 0 || victories > runs) throw new ArgumentException("Invalid progress counters.");
            Coins = coins; Runs = runs; Victories = victories;
            Records = RunResult.CopyMap(records); Challenges = RunResult.CopyMap(challenges);
            foreach (var pair in Records) if (string.IsNullOrWhiteSpace(pair.Key) || double.IsNaN(pair.Value) || double.IsInfinity(pair.Value) || pair.Value < 0) throw new ArgumentException("Invalid record.");
            foreach (var pair in Challenges) if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value < 0) throw new ArgumentException("Invalid challenge progress.");
            var ids = new List<string>(); var seen = new HashSet<string>(StringComparer.Ordinal);
            if (appliedRunIds != null) foreach (string id in appliedRunIds)
            { if (string.IsNullOrWhiteSpace(id) || !seen.Add(id)) throw new ArgumentException("Invalid result receipts."); ids.Add(id); }
            AppliedRunIds = ids.AsReadOnly();
            if (ids.Count != runs) throw new ArgumentException("Every counted run requires a unique receipt.");
        }
    }
    public sealed class RunProgressReceipt
    {
        public RunProgressProfile Profile { get; }
        public IReadOnlyDictionary<string, RunRecordProgress> Records { get; }
        public IReadOnlyDictionary<string, RunChallengeProgress> Challenges { get; }
        public int CoinsAdded { get; }
        public bool AlreadyApplied { get; }
        internal RunProgressReceipt(RunProgressProfile profile, Dictionary<string, RunRecordProgress> records,
            Dictionary<string, RunChallengeProgress> challenges, int added, bool duplicate)
        { Profile = profile; Records = RunResult.CopyMap(records); Challenges = RunResult.CopyMap(challenges); CoinsAdded = added; AlreadyApplied = duplicate; }
    }
    public static class RunProgression
    {
        public static RunProgressReceipt Apply(RunProgressProfile before, string runId, RunResult result, IEnumerable<ResultChallengeRule> rules)
        {
            if (before == null || result == null || rules == null || string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("Result progress dependencies required.");
            bool duplicate = false; foreach (string id in before.AppliedRunIds) if (id == runId) duplicate = true;
            var records = new Dictionary<string, double>(); foreach (var pair in before.Records) records.Add(pair.Key, pair.Value);
            var challenges = new Dictionary<string, int>(); foreach (var pair in before.Challenges) challenges.Add(pair.Key, pair.Value);
            var recordChanges = new Dictionary<string, RunRecordProgress>(); var challengeChanges = new Dictionary<string, RunChallengeProgress>();
            double damage = 0; foreach (var pair in result.DamageByWeapon) damage = Math.Min(double.MaxValue - pair.Value, damage) + pair.Value;
            var metrics = new Dictionary<string, double> { { "survival-seconds", Math.Floor(result.DurationSeconds) }, { "enemies-defeated", result.EnemiesDefeated }, { "longest-streak", result.LongestStreak }, { "total-damage", damage } };
            foreach (var pair in metrics)
            {
                records.TryGetValue(pair.Key, out double previous);
                recordChanges.Add(pair.Key, new RunRecordProgress(previous, duplicate ? previous : pair.Value));
                if (!duplicate) records[pair.Key] = Math.Max(previous, pair.Value);
            }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var rule in rules)
            {
                if (string.IsNullOrWhiteSpace(rule.Id) || rule.Target <= 0 || !seen.Add(rule.Id) || !Enum.IsDefined(typeof(ResultMetric), rule.Metric)) throw new ArgumentException("Invalid or duplicate challenge.");
                challenges.TryGetValue(rule.Id, out int previous); previous = Math.Min(previous, rule.Target);
                double amount = rule.Metric == ResultMetric.EnemiesDefeated ? result.EnemiesDefeated : rule.Metric == ResultMetric.SurvivalSeconds ? result.DurationSeconds
                    : rule.Metric == ResultMetric.Victories ? (result.IsVictory ? 1 : 0) : result.Build.Evolutions.Count;
                long value = (long)Math.Min(int.MaxValue, Math.Floor(amount));
                int current = duplicate ? previous : (int)Math.Min(rule.Target, rule.Cumulative ? previous + value : Math.Max(previous, value));
                challenges[rule.Id] = current; challengeChanges.Add(rule.Id, new RunChallengeProgress(previous, current, rule.Target));
            }
            int coins = duplicate ? before.Coins : (int)Math.Min(int.MaxValue, (long)before.Coins + result.Coins);
            var receipts = new List<string>(before.AppliedRunIds); if (!duplicate) receipts.Add(runId);
            var after = duplicate ? before : new RunProgressProfile(coins, (int)Math.Min(int.MaxValue, (long)before.Runs + 1),
                (int)Math.Min(int.MaxValue, (long)before.Victories + (result.IsVictory ? 1 : 0)), records, challenges, receipts);
            return new RunProgressReceipt(after, recordChanges, challengeChanges, coins - before.Coins, duplicate);
        }
    }
}
