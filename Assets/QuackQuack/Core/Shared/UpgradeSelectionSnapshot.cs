using System;
using System.Collections.Generic;

namespace QuackQuack.Core.Shared
{
    public sealed class UpgradeCardSnapshot
    {
        public string Id { get; }
        public string Title { get; }
        public string Kind { get; }
        public string Current { get; }
        public string Next { get; }
        public string Synergy { get; }
        public string Requirement { get; }
        public UpgradeCardSnapshot(string id, string title, string kind, string current, string next, string synergy, string requirement)
        { Id = id; Title = title; Kind = kind; Current = current; Next = next; Synergy = synergy; Requirement = requirement; }
    }
    public sealed class UpgradeSelectionSnapshot
    {
        public string OfferId { get; }
        public bool Visible { get; }
        public int Level { get; }
        public int Rerolls { get; }
        public int Banishes { get; }
        public IReadOnlyList<UpgradeCardSnapshot> Cards { get; }
        public UpgradeSelectionSnapshot(string offerId, bool visible, int level, int rerolls, int banishes, UpgradeCardSnapshot[] cards)
        {
            OfferId = offerId; Visible = visible; Level = level; Rerolls = rerolls; Banishes = banishes;
            if (cards == null || cards.Length > 3) throw new ArgumentException("Selection supports at most three cards.");
            Cards = Array.AsReadOnly((UpgradeCardSnapshot[])cards.Clone());
        }
    }
}
