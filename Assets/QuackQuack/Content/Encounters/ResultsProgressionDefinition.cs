using System;
using QuackQuack.Core.Run;
using UnityEngine;
namespace QuackQuack.Content.Encounters
{
    [Serializable] public sealed class ResultsChallengeDefinition
    {
        public string Id, Title;
        public ResultMetric Metric;
        public int Target;
        public bool Cumulative;
    }
    [CreateAssetMenu(menuName = "QuackQuack/Results progression")]
    public sealed class ResultsProgressionDefinition : ScriptableObject
    {
        public ResultsChallengeDefinition[] Challenges = Array.Empty<ResultsChallengeDefinition>();
        public ResultChallengeRule[] CreateRules()
        {
            if (Challenges == null || Challenges.Length == 0) throw new ArgumentException("Challenge definitions required.");
            var seen = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal); var rules = new ResultChallengeRule[Challenges.Length];
            for (int i = 0; i < rules.Length; i++)
            {
                var d = Challenges[i]; if (d == null || string.IsNullOrWhiteSpace(d.Title) || !seen.Add(d.Id)) throw new ArgumentException("Invalid challenge content.");
                rules[i] = new ResultChallengeRule(d.Id, d.Metric, d.Target, d.Cumulative);
            }
            return rules;
        }
    }
}
