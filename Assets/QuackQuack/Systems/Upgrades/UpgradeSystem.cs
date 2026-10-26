using System;
using System.Collections.Generic;
using QuackQuack.Content.Passives;
using QuackQuack.Content.Weapons;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;

namespace QuackQuack.Systems.Upgrades
{
    public sealed class UpgradeOffer
    {
        public string OfferId { get; }
        public IReadOnlyList<string> Choices { get; }
        public bool IsClosed { get; }
        internal UpgradeOffer(string id, List<string> choices, bool closed = false)
        { OfferId = id; Choices = choices.AsReadOnly(); IsClosed = closed; }
        internal UpgradeOffer Close() => new UpgradeOffer(OfferId, new List<string>(Choices), true);
    }

    public sealed class UpgradeSystem : IDisposable
    {
        private readonly BuildState build;
        private readonly Dictionary<string, UpgradeOffer> offers = new Dictionary<string, UpgradeOffer>(StringComparer.Ordinal);
        private readonly HashSet<string> banished = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> confirmed = new List<string>();
        private readonly Dictionary<string, WeaponDefinition> weapons = new Dictionary<string, WeaponDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, PassiveDefinition> passives = new Dictionary<string, PassiveDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, EvolutionDefinition> evolutions = new Dictionary<string, EvolutionDefinition>(StringComparer.Ordinal);
        private readonly HashSet<string> evolutionResults = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> catalogIds = new List<string>();
        private string[] candidates = Array.Empty<string>();
        private uint random;
        private long sequence;
        private bool catalogBound, disposed;
        private RunState run;
        public BuildState Build => build;
        public UpgradeOffer CurrentOffer { get; private set; }
        public IReadOnlyList<string> ConfirmedChoices { get; }
        public int RerollsRemaining => build.RerollsRemaining;
        public int BanishRemaining => build.BanishRemaining;
        public event Action<UpgradeOffer> OfferChanged;
        public event Action<string> OfferResolved;

        // Prefiltered-ID boundary, also usable before authored catalogs exist.
        public UpgradeSystem(int seed) : this(seed, new BuildState()) { }
        public UpgradeSystem(int seed, BuildState build)
        {
            this.build = build ?? throw new ArgumentNullException(nameof(build));
            random = unchecked((uint)seed);
            if (random == 0) random = 0x9E3779B9u;
            ConfirmedChoices = confirmed.AsReadOnly();
        }

        public UpgradeSystem(int seed, BuildState build, IEnumerable<WeaponDefinition> weapons,
            IEnumerable<PassiveDefinition> passives, IEnumerable<EvolutionDefinition> evolutions) : this(seed, build)
        {
            if (weapons == null || passives == null || evolutions == null) throw new ArgumentNullException("catalog");
            var weaponList = new List<WeaponDefinition>(weapons);
            var passiveList = new List<PassiveDefinition>(passives);
            var evolutionList = new List<EvolutionDefinition>(evolutions);
            if (!WeaponDefinition.IsValidCatalog(weaponList, passiveList, out var reason) ||
                (evolutionList.Count > 0 && !EvolutionDefinition.IsValidCatalog(evolutionList, weaponList, passiveList, out reason)))
                throw new ArgumentException(reason);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var weapon in weaponList) { AddCatalogId(weapon.Id, ids); this.weapons.Add(weapon.Id, weapon); }
            foreach (var passive in passiveList) { AddCatalogId(passive.Id, ids); this.passives.Add(passive.Id, passive); }
            foreach (var evolution in evolutionList)
            {
                AddCatalogId(evolution.Id, ids);
                this.evolutions.Add(evolution.Id, evolution);
                evolutionResults.Add(evolution.ResultWeapon.Id);
            }
            // Also exclude result weapons declared by links if recipes are supplied separately later.
            foreach (var weapon in weaponList)
                if (!string.IsNullOrEmpty(weapon.EvolutionWeaponId)) evolutionResults.Add(weapon.EvolutionWeaponId);
            catalogBound = true;
        }

        private void AddCatalogId(string id, HashSet<string> ids)
        {
            if (!ids.Add(id)) throw new ArgumentException("Choice IDs must be unique across all content categories: " + id);
            catalogIds.Add(id);
        }

        public void BindRun(RunState state)
        {
            if (disposed || run != null || CurrentOffer != null) throw new InvalidOperationException("Bind the run once before opening offers.");
            run = state ?? throw new ArgumentNullException(nameof(state));
        }

        public UpgradeOffer GenerateOffer(string offerId) => GenerateOffer(offerId, catalogIds.ToArray());

        public UpgradeOffer GenerateOffer(string offerId, string[] eligibleIds)
        {
            if (disposed || string.IsNullOrWhiteSpace(offerId)) return null;
            if (offers.TryGetValue(offerId, out var previous)) return previous;
            if (!CanSelect()) return null;
            if (CurrentOffer != null && !CurrentOffer.IsClosed) return CurrentOffer;
            candidates = eligibleIds == null ? Array.Empty<string>() : (string[])eligibleIds.Clone();
            var choices = SelectChoices();
            if (choices.Count == 0) { offers.Add(offerId, null); CurrentOffer = null; return null; }
            if (run != null && run.Phase == RunPhase.Playing) run.TryTransition(RunPhase.ChoosingUpgrade);
            return Publish(offerId, choices);
        }

        public bool TryConfirm(string offerId, string choiceId)
        {
            if (!IsOpen(offerId) || !IsOffered(choiceId) || !Eligible(choiceId)) return false;
            if (catalogBound && !Apply(choiceId)) return false;
            confirmed.Add(choiceId);
            Resolve();
            return true;
        }

        public bool TrySkip(string offerId)
        {
            if (!IsOpen(offerId)) return false;
            Resolve();
            return true;
        }

        public bool TryReroll(string offerId)
        {
            if (!IsOpen(offerId) || RerollsRemaining == 0) return false;
            var choices = SelectChoices();
            if (choices.Count == 0 || !build.TryUseReroll()) return false;
            CloseCurrent();
            Publish(NextId(), choices);
            return true;
        }

        public bool TryBanish(string offerId, string choiceId)
        {
            if (!IsOpen(offerId) || !IsOffered(choiceId) || BanishRemaining == 0 || !build.TryUseBanish()) return false;
            banished.Add(choiceId);
            var choices = SelectChoices();
            if (choices.Count == 0) Resolve();
            else { CloseCurrent(); Publish(NextId(), choices); }
            return true;
        }

        private bool CanSelect() => run == null || run.Phase == RunPhase.Playing || run.Phase == RunPhase.ChoosingUpgrade;
        private bool IsOpen(string id) => !disposed && CanSelect() && id != null && CurrentOffer != null && !CurrentOffer.IsClosed && CurrentOffer.OfferId == id;
        private bool IsOffered(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            foreach (string choice in CurrentOffer.Choices) if (choice == id) return true;
            return false;
        }

        private bool Eligible(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || banished.Contains(id)) return false;
            if (!catalogBound) return true;
            if (weapons.ContainsKey(id))
            {
                if (evolutionResults.Contains(id) || build.Evolutions.ContainsKey(id)) return false;
                return build.Weapons.TryGetValue(id, out int level) ? level < BuildState.WeaponMaxLevel : build.Weapons.Count < BuildState.SlotLimit;
            }
            if (passives.TryGetValue(id, out var passive))
                return build.Passives.TryGetValue(id, out int level) ? level < passive.MaxLevel : build.Passives.Count < BuildState.SlotLimit;
            if (evolutions.TryGetValue(id, out var evolution))
                return build.Weapons.TryGetValue(evolution.BaseWeapon.Id, out int level) &&
                    evolution.CanEvolve(evolution.BaseWeapon.Id, level, build.Passives.Keys) && !build.Weapons.ContainsKey(evolution.ResultWeapon.Id);
            return false;
        }

        private bool Apply(string id)
        {
            if (weapons.ContainsKey(id)) return build.Weapons.ContainsKey(id) ? build.TryUpgradeWeapon(id) : build.TryAddWeapon(id);
            if (passives.TryGetValue(id, out var passive))
                return build.Passives.ContainsKey(id) ? build.TryUpgradePassive(id, passive.MaxLevel) : build.TryAddPassive(id);
            var evolution = evolutions[id];
            return build.TryEvolveWeapon(evolution.BaseWeapon.Id, evolution.RequiredPassive.Id, evolution.ResultWeapon.Id);
        }

        private List<string> SelectChoices()
        {
            var eligible = new List<string>();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in candidates) if (Eligible(id) && unique.Add(id)) eligible.Add(id);
            for (int i = eligible.Count - 1; i > 0; i--)
            {
                random ^= random << 13; random ^= random >> 17; random ^= random << 5;
                int other = (int)(random % (uint)(i + 1));
                string value = eligible[i]; eligible[i] = eligible[other]; eligible[other] = value;
            }
            if (eligible.Count > 3) eligible.RemoveRange(3, eligible.Count - 3);
            return eligible;
        }

        private string NextId()
        {
            string id;
            do { id = "upgrade-" + (++sequence); } while (offers.ContainsKey(id));
            return id;
        }
        private UpgradeOffer Publish(string id, List<string> choices)
        {
            CurrentOffer = new UpgradeOffer(id, choices);
            offers.Add(id, CurrentOffer);
            OfferChanged?.Invoke(CurrentOffer);
            return CurrentOffer;
        }
        private void CloseCurrent()
        {
            CurrentOffer = CurrentOffer.Close();
            offers[CurrentOffer.OfferId] = CurrentOffer;
        }
        private void Resolve()
        {
            CloseCurrent();
            try { OfferResolved?.Invoke(CurrentOffer.OfferId); }
            finally
            {
                if (run != null && run.Phase == RunPhase.ChoosingUpgrade && (CurrentOffer == null || CurrentOffer.IsClosed))
                    run.TryTransition(RunPhase.Playing);
            }
        }
        public void Dispose() { disposed = true; OfferChanged = null; OfferResolved = null; }
    }
}
