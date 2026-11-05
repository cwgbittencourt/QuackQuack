using System;
using System.Collections.Generic;
using QuackQuack.Core.Run;

namespace QuackQuack.Core.Progression
{
    public enum RewardOrigin { Common, MiniBoss, FinalBoss }
    public enum RewardKind { Experience, HealCommon, HealMiniBoss }
    public enum RewardState { Due, WaitingForView, Grounded, Claimed }

    public readonly struct RewardId : IEquatable<RewardId>
    {
        public long RunId { get; }
        public int SourceId { get; }
        public long LeaseId { get; }
        public RewardKind Kind { get; }
        public int Ordinal { get; }
        public RewardId(long runId, int sourceId, long leaseId, RewardKind kind, int ordinal = 0)
        { RunId = runId; SourceId = sourceId; LeaseId = leaseId; Kind = kind; Ordinal = ordinal; }
        public bool Equals(RewardId other) => RunId == other.RunId && SourceId == other.SourceId &&
            LeaseId == other.LeaseId && Kind == other.Kind && Ordinal == other.Ordinal;
        public override bool Equals(object obj) => obj is RewardId other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = RunId.GetHashCode(); hash = hash * 397 ^ SourceId;
                hash = hash * 397 ^ LeaseId.GetHashCode(); hash = hash * 397 ^ (int)Kind;
                return hash * 397 ^ Ordinal;
            }
        }
    }

    public sealed class RewardEntry
    {
        public RewardId Id { get; internal set; }
        public RewardOrigin Origin { get; internal set; }
        public RewardKind Kind => Id.Kind;
        public int Experience { get; internal set; }
        public float Fraction { get; internal set; }
        public float X { get; internal set; }
        public float Y { get; internal set; }
        public RewardState State { get; internal set; }
        internal int Slot, Next = -1, Previous = -1;
        internal bool Collecting;
    }

    // Per-run obligations, independent of GameObjects, pools, proximity and offer RNG.
    public sealed class RewardLedger
    {
        private readonly long runId;
        private readonly Func<double> nextDropRoll;
        private readonly double commonChance;
        private readonly float commonFraction, miniBossFraction;
        private readonly int defeatCapacity;
        private int defeats;
        private readonly RewardEntry[] storage;
        private readonly List<RewardEntry> entries;
        private readonly Dictionary<RewardId, RewardEntry> byId;
        private readonly int[] heads = { -1, -1, -1 }, tails = { -1, -1, -1 };
        public IReadOnlyList<RewardEntry> Entries { get; }
        public int DefeatCapacity => defeatCapacity;
        public int DefeatCount => defeats;
        public int PendingCount { get; private set; }

        public RewardLedger(long runId, Func<double> nextDropRoll, double commonChance,
            float commonFraction, float miniBossFraction, int defeatCapacity)
        {
            if (runId <= 0) throw new ArgumentOutOfRangeException(nameof(runId));
            if (nextDropRoll == null) throw new ArgumentNullException(nameof(nextDropRoll));
            if (double.IsNaN(commonChance) || double.IsInfinity(commonChance) || commonChance < 0 || commonChance > 1)
                throw new ArgumentOutOfRangeException(nameof(commonChance));
            if (!ValidFraction(commonFraction)) throw new ArgumentOutOfRangeException(nameof(commonFraction));
            if (!ValidFraction(miniBossFraction)) throw new ArgumentOutOfRangeException(nameof(miniBossFraction));
            if (defeatCapacity <= 0 || defeatCapacity > int.MaxValue / 2)
                throw new ArgumentOutOfRangeException(nameof(defeatCapacity));
            this.runId = runId; this.nextDropRoll = nextDropRoll; this.commonChance = commonChance;
            this.commonFraction = commonFraction; this.miniBossFraction = miniBossFraction;
            this.defeatCapacity = defeatCapacity;
            int capacity = defeatCapacity * 2;
            storage = new RewardEntry[capacity];
            for (int i = 0; i < capacity; i++) storage[i] = new RewardEntry { Slot = i };
            entries = new List<RewardEntry>(capacity);
            Entries = entries.AsReadOnly();
            byId = new Dictionary<RewardId, RewardEntry>(capacity);
        }

        public bool ProcessDefeat(int sourceId, long lease, RewardOrigin origin, int experience,
            float x, float y, bool isDefeat)
        {
            if (!isDefeat || sourceId == 0 || lease <= 0 || experience <= 0 || !Finite(x) || !Finite(y) ||
                origin < RewardOrigin.Common || origin > RewardOrigin.FinalBoss) return false;
            var xpId = new RewardId(runId, sourceId, lease, RewardKind.Experience);
            if (byId.ContainsKey(xpId)) return false;
            // Fail before RNG or mutation: technical exhaustion must never silently suppress a drop.
            // The host must size this for the entire run; production sizing/integration is T027.
            if (defeats == defeatCapacity) throw new InvalidOperationException("Reward ledger defeat capacity exhausted.");
            bool commonHeal = false;
            if (origin == RewardOrigin.Common)
            {
                double roll = nextDropRoll();
                if (double.IsNaN(roll) || double.IsInfinity(roll) || roll < 0 || roll >= 1)
                    throw new InvalidOperationException("Drop RNG must return a finite value in [0,1).");
                commonHeal = roll < commonChance;
            }
            Add(xpId, origin, experience, 0, x, y);
            if (commonHeal)
                Add(new RewardId(runId, sourceId, lease, RewardKind.HealCommon), origin, 0, commonFraction, x, y);
            else if (origin == RewardOrigin.MiniBoss)
                Add(new RewardId(runId, sourceId, lease, RewardKind.HealMiniBoss), origin, 0, miniBossFraction, x, y);
            defeats++;
            return true;
        }

        private void Add(RewardId id, RewardOrigin origin, int xp, float fraction, float x, float y)
        {
            var entry = storage[entries.Count];
            entry.Id = id; entry.Origin = origin; entry.Experience = xp; entry.Fraction = fraction;
            entry.X = x; entry.Y = y; entry.State = RewardState.Due;
            entries.Add(entry); byId.Add(id, entry);
            Enqueue(entry);
        }

        private void Enqueue(RewardEntry entry)
        {
            int kind = (int)entry.Kind;
            entry.Previous = tails[kind]; entry.Next = -1;
            if (tails[kind] >= 0) storage[tails[kind]].Next = entry.Slot;
            else heads[kind] = entry.Slot;
            tails[kind] = entry.Slot; PendingCount++;
        }
        private void RemovePending(RewardEntry entry)
        {
            int kind = (int)entry.Kind;
            if (entry.Previous >= 0) storage[entry.Previous].Next = entry.Next; else heads[kind] = entry.Next;
            if (entry.Next >= 0) storage[entry.Next].Previous = entry.Previous; else tails[kind] = entry.Previous;
            entry.Next = entry.Previous = -1; PendingCount--;
        }
        public bool TryGetPending(RewardKind kind, out RewardEntry entry)
        {
            int index = (int)kind;
            entry = index >= 0 && index < heads.Length && heads[index] >= 0 ? storage[heads[index]] : null;
            return entry != null;
        }
        public bool ReturnToPending(RewardId id)
        {
            if (!byId.TryGetValue(id, out var entry) || entry.State != RewardState.Grounded || entry.Collecting) return false;
            entry.State = RewardState.WaitingForView; Enqueue(entry); return true;
        }

        // Pure availability acknowledgement. The caller must actually secure a view first;
        // binding and retry scheduling with real pools belong to T027/T028.
        public bool TryMaterialize(RewardId id, bool hasViewSlot)
        {
            if (!byId.TryGetValue(id, out var entry) ||
                (entry.State != RewardState.Due && entry.State != RewardState.WaitingForView)) return false;
            entry.State = hasViewSlot ? RewardState.Grounded : RewardState.WaitingForView;
            if (hasViewSlot) RemovePending(entry);
            return hasViewSlot;
        }

        // Uses the real UpgradeSystem consumer so level offers remain in its existing flow.
        public bool TryClaimExperience(RewardId id, Func<int, bool> collect)
        {
            if (collect == null || !byId.TryGetValue(id, out var entry) || entry.Kind != RewardKind.Experience ||
                entry.State != RewardState.Grounded || entry.Collecting) return false;
            entry.Collecting = true;
            try
            {
                if (!collect(entry.Experience)) return false;
                entry.State = RewardState.Claimed; return true;
            }
            finally { entry.Collecting = false; }
        }

        public float TryCollectHeal(RewardId id, PlayerState player)
        {
            if (player == null || !byId.TryGetValue(id, out var entry) || entry.State != RewardState.Grounded ||
                (entry.Kind != RewardKind.HealCommon && entry.Kind != RewardKind.HealMiniBoss)) return 0;
            float applied = player.Heal(entry.Fraction);
            if (applied > 0) entry.State = RewardState.Claimed;
            return applied;
        }

        public bool TryCollectExperience(RewardId id, ExperienceState experience)
        {
            if (experience == null || !byId.TryGetValue(id, out var entry) || entry.Kind != RewardKind.Experience ||
                entry.State != RewardState.Grounded || !experience.TryAdd(entry.Experience, out _)) return false;
            entry.State = RewardState.Claimed;
            return true;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool ValidFraction(float value) => Finite(value) && value > 0 && value <= 1;
    }
}
