using System;
using System.Collections.Generic;
using QuackQuack.Content.Shared;
using QuackQuack.Core.Run;
using UnityEngine;

namespace QuackQuack.Content.Encounters
{
    public enum EncounterKind { Wave, AlphaDragonfly, CururuToad, FrogKing }
    public enum EncounterCompletion { WindowElapsed, TargetDefeated }

    [CreateAssetMenu(menuName = "QuackQuack/Encounter")]
    public sealed class EncounterDefinition : ContentDefinition
    {
        public const double LakeDurationSeconds = 900;
        [SerializeField] private EncounterKind kind;
        [SerializeField] private double startSeconds, endSeconds = 300;
        [SerializeField] private string[] enemyIds = Array.Empty<string>();
        [SerializeField] private int[] quantities = Array.Empty<int>();
        [SerializeField] private float spawnInterval = 2, healthMultiplier = 1, damageMultiplier = 1;
        [SerializeField] private int maxActive = 64;
        [SerializeField] private string arenaId = "lake";

        public EncounterKind Kind => kind;
        public double StartSeconds => startSeconds;
        public double EndSeconds => endSeconds;
        public IReadOnlyList<string> EnemyIds => Array.AsReadOnly((string[])enemyIds.Clone());
        public IReadOnlyList<int> Quantities => Array.AsReadOnly((int[])quantities.Clone());
        public float SpawnInterval => spawnInterval;
        public int MaxActive => maxActive;
        public string ArenaId => arenaId;
        public float HealthMultiplier => healthMultiplier;
        public float DamageMultiplier => damageMultiplier;
        public EncounterCompletion Completion => kind == EncounterKind.Wave ? EncounterCompletion.WindowElapsed : EncounterCompletion.TargetDefeated;

        public override bool IsValid(out string reason)
        {
            if (!base.IsValid(out reason)) return false;
            if (string.IsNullOrWhiteSpace(DisplayName) || arenaId != "lake") reason = "Encounter requires a display name and the lake arena.";
            else if (!Enum.IsDefined(typeof(EncounterKind), kind)) reason = "Unknown encounter kind.";
            else if (!Finite(startSeconds) || !Finite(endSeconds) || startSeconds < 0 || endSeconds > LakeDurationSeconds || endSeconds < startSeconds)
                reason = "Encounter window must be finite and inside the lake timeline.";
            else if (kind == EncounterKind.Wave && endSeconds <= startSeconds) reason = "Wave window must have positive duration.";
            else if (kind != EncounterKind.Wave && (startSeconds != Milestone(kind) || endSeconds != startSeconds))
                reason = "Lake milestones must occur at minutes 5, 10 and 15, with a single request time.";
            else if (!Positive(spawnInterval) || maxActive <= 0 || !Positive(healthMultiplier) || !Positive(damageMultiplier))
                reason = "Spawn interval, capacity and stat multipliers must be positive and finite.";
            else if (enemyIds == null || quantities == null || enemyIds.Length == 0 || enemyIds.Length != quantities.Length)
                reason = "Encounter requires matching enemy IDs and quantities.";
            else
            {
                var unique = new HashSet<string>(StringComparer.Ordinal);
                long total = 0;
                for (int i = 0; i < enemyIds.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(enemyIds[i]) || !unique.Add(enemyIds[i]) || quantities[i] <= 0)
                    { reason = "Enemy IDs must be unique/nonempty and quantities positive."; return false; }
                    total += quantities[i];
                }
                if (total > maxActive) reason = "Encounter batch exceeds its active limit.";
                else if (kind != EncounterKind.Wave && (enemyIds.Length != 1 || quantities[0] != 1))
                    reason = "A milestone must request exactly one target.";
                else if (kind != EncounterKind.Wave && enemyIds[0] != MilestoneTarget(kind))
                    reason = "Lake milestone must reference its designated target ID.";
            }
            return reason == null;
        }

        public static bool IsValidTimeline(IReadOnlyList<EncounterDefinition> definitions,
            IReadOnlyCollection<string> knownEnemyIds, out string reason)
        {
            reason = null;
            if (definitions == null || definitions.Count == 0 || knownEnemyIds == null || knownEnemyIds.Count == 0)
            { reason = "Timeline and known enemy IDs are required."; return false; }
            var known = new HashSet<string>(knownEnemyIds, StringComparer.Ordinal);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var milestones = new HashSet<EncounterKind>();
            var waves = new List<EncounterDefinition>();
            foreach (var definition in definitions)
            {
                if (definition == null) { reason = "Missing encounter definition."; return false; }
                if (!definition.IsValid(out reason)) return false;
                if (!ids.Add(definition.Id)) { reason = "Duplicate encounter ID."; return false; }
                foreach (string enemyId in definition.enemyIds)
                    if (!known.Contains(enemyId)) { reason = "Unknown encounter enemy: " + enemyId; return false; }
                if (definition.kind == EncounterKind.Wave) waves.Add(definition);
                else if (!milestones.Add(definition.kind)) { reason = "Duplicate lake milestone."; return false; }
            }
            if (milestones.Count != 3) { reason = "Lake requires both minibosses and the final boss."; return false; }
            waves.Sort((a, b) => a.startSeconds.CompareTo(b.startSeconds));
            double coveredUntil = 0;
            foreach (var wave in waves)
            {
                if (wave.startSeconds > coveredUntil) { reason = "Gap in lake wave progression."; return false; }
                coveredUntil = Math.Max(coveredUntil, wave.endSeconds);
            }
            if (coveredUntil != LakeDurationSeconds) { reason = "Wave progression must cover the full 15 minutes."; return false; }
            return true;
        }

        public static EncounterSchedule CreateSchedule(RunState run, IReadOnlyList<EncounterDefinition> definitions,
            IReadOnlyCollection<string> knownEnemyIds)
        {
            if (!IsValidTimeline(definitions, knownEnemyIds, out string reason)) throw new ArgumentException(reason, nameof(definitions));
            var ordered = new List<EncounterDefinition>(definitions);
            // At equal times, start the wave before requesting the milestone.
            ordered.Sort((a, b) => { int time = a.startSeconds.CompareTo(b.startSeconds);
                return time != 0 ? time : a.kind != b.kind ? a.kind.CompareTo(b.kind) : StringComparer.Ordinal.Compare(a.Id, b.Id); });
            var ids = new string[ordered.Count]; var times = new double[ordered.Count];
            for (int i = 0; i < ordered.Count; i++) { ids[i] = ordered[i].Id; times[i] = ordered[i].startSeconds; }
            return new EncounterSchedule(run, ids, times);
        }

        private static double Milestone(EncounterKind value) => value == EncounterKind.AlphaDragonfly ? 300 : value == EncounterKind.CururuToad ? 600 : 900;
        private static string MilestoneTarget(EncounterKind value) => value == EncounterKind.AlphaDragonfly ? "libelula-alfa" : value == EncounterKind.CururuToad ? "sapo-cururu" : "sapo-rei";
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static bool Positive(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value > 0;
    }
}
