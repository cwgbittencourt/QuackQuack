using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using QuackQuack.Content.Catalogs;
using QuackQuack.Content.Passives;
using QuackQuack.Content.Weapons;
using QuackQuack.Core.Run;
using QuackQuack.Core.Progression;
using QuackQuack.Gameplay.Player;
using QuackQuack.Core.Shared;
using QuackQuack.Presentation.UI;
using QuackQuack.Systems.Upgrades;
using UnityEngine;

namespace QuackQuack.Gameplay.Upgrades
{
    public sealed class UpgradeSelectionBridge : MonoBehaviour
    {
        private readonly Dictionary<string, WeaponDefinition> weapons = new Dictionary<string, WeaponDefinition>();
        private readonly Dictionary<string, PassiveDefinition> passives = new Dictionary<string, PassiveDefinition>();
        private readonly Dictionary<string, EvolutionDefinition> evolutions = new Dictionary<string, EvolutionDefinition>();
        private UpgradeSystem system;
        private RunState run;
        private UpgradeSelectionView view;
        private UpgradeSelectionSnapshot snapshot;
        private UpgradeOffer lastOffer;
        private int lastHistory = -1, lastRerolls = -1, lastBanishes = -1, lastLevel = -1;
        private RunPhase lastPhase;
        private PlayerController player;
        private TerrainType lastTerrain;
        private RelicLoadout lastRelics;
        private int lastRelicVersion = -1;

        public void Initialize(PrototypeBuildCatalog catalog, UpgradeSystem system, RunState run, UpgradeSelectionView view, PlayerController player = null)
        {
            if (this.system != null) throw new InvalidOperationException("Selection bridge already initialized.");
            if (catalog == null || !catalog.IsValid(out _)) throw new ArgumentException("A valid build catalog is required.");
            this.system = system ?? throw new ArgumentNullException(nameof(system));
            this.run = run ?? throw new ArgumentNullException(nameof(run));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.player = player;
            foreach (var weapon in catalog.GetAllWeapons()) weapons.Add(weapon.Id, weapon);
            foreach (var passive in catalog.Passives) passives.Add(passive.Id, passive);
            foreach (var evolution in catalog.Evolutions) evolutions.Add(evolution.Id, evolution);
            Attach();
        }
        private void Attach()
        {
            if (system == null || !isActiveAndEnabled) return;
            view.Bind(Snapshot, system.TryConfirm, system.TryBanish, system.TryReroll, system.TrySkip);
        }
        private void OnEnable() { Attach(); }
        private void OnDisable() { if (view != null) view.Unbind(); }
        private void OnDestroy() { if (view != null) view.Unbind(); }

        public UpgradeSelectionSnapshot Snapshot()
        {
            var offer = system.CurrentOffer;
            var terrain = player != null && player.State != null ? player.State.Terrain : TerrainType.Land;
            var relics = player != null ? player.Relics : null;
            if (snapshot != null && ReferenceEquals(lastOffer, offer) && lastHistory == system.Build.History.Count &&
                lastRerolls == system.RerollsRemaining && lastBanishes == system.BanishRemaining && lastLevel == system.Level && lastPhase == run.Phase &&
                lastTerrain == terrain && ReferenceEquals(lastRelics, relics) && lastRelicVersion == (relics?.Version ?? 0))
                return snapshot;
            lastOffer = offer; lastHistory = system.Build.History.Count; lastRerolls = system.RerollsRemaining;
            lastBanishes = system.BanishRemaining; lastLevel = system.Level; lastPhase = run.Phase;
            lastTerrain = terrain; lastRelics = relics; lastRelicVersion = relics?.Version ?? 0;
            bool visible = run.Phase == RunPhase.ChoosingUpgrade && offer != null && !offer.IsClosed;
            var cards = new UpgradeCardSnapshot[visible ? offer.Choices.Count : 0];
            for (int i = 0; i < cards.Length; i++) cards[i] = Describe(offer.Choices[i]);
            snapshot = new UpgradeSelectionSnapshot(offer == null ? null : offer.OfferId, visible, system.Level,
                system.RerollsRemaining, system.BanishRemaining, cards);
            return snapshot;
        }
        private UpgradeCardSnapshot Describe(string id)
        {
            if (weapons.TryGetValue(id, out var weapon))
            {
                system.Build.Weapons.TryGetValue(id, out int current);
                string synergy = "Sem receita no protótipo";
                string requirement = "Sem evolução no protótipo";
                if (!string.IsNullOrEmpty(weapon.CompatiblePassiveId))
                {
                    var passive = passives[weapon.CompatiblePassiveId];
                    synergy = passive.DisplayName + " → " + weapons[weapon.EvolutionWeaponId].DisplayName;
                    requirement = "Arma N5 + " + passive.DisplayName + (system.Build.Passives.ContainsKey(passive.Id) ? " (presente)" : " (ausente)");
                }
                return new UpgradeCardSnapshot(id, weapon.DisplayName, current == 0 ? "NOVA ARMA" : "MELHORIA DE ARMA",
                    current == 0 ? "Não adquirida" : Stats(weapon, current), Stats(weapon, Math.Min(5, current + 1)), synergy, requirement,
                    activeEffects: ActiveEffects());
            }
            if (passives.TryGetValue(id, out var definition))
            {
                system.Build.Passives.TryGetValue(id, out int current);
                var compatible = weapons[definition.CompatibleWeaponId];
                string requirement = string.IsNullOrEmpty(compatible.EvolutionWeaponId) ? "Sem evolução no protótipo" : compatible.DisplayName + " N5 + esta passiva";
                if (definition.Attribute == PassiveAttribute.AreaRadius && compatible.Behavior == WeaponBehavior.Bubbles)
                {
                    bool owned = system.Build.Weapons.TryGetValue(compatible.Id, out int weaponLevel);
                    if (!owned) weaponLevel = 1;
                    int proposed = Math.Min(definition.MaxLevel, current + 1);
                    float before = system.ResolveWeaponAttributes(compatible, weaponLevel, lastTerrain, lastRelics).HitRadius;
                    float after = system.PreviewWeaponAttributes(compatible, weaponLevel, lastTerrain, lastRelics, definition.Id, proposed).HitRadius;
                    return new UpgradeCardSnapshot(id, definition.DisplayName, current == 0 ? "NOVA PASSIVA" : "MELHORIA DE PASSIVA",
                        "N" + current + " · +" + Percent(definition.GetModifier(current)) + " no raio de Bolhas",
                        "N" + proposed + " · +" + Percent(definition.GetModifier(proposed)) + " no raio de Bolhas",
                        owned ? "Somente Bolhas" : "Ao adquirir Bolhas · prévia N1",
                        requirement, before, after, ActiveEffects());
                }
                return new UpgradeCardSnapshot(id, definition.DisplayName, current == 0 ? "NOVA PASSIVA" : "MELHORIA DE PASSIVA",
                    "N" + current + " · +" + Percent(definition.GetModifier(current)) + " " + Attribute(definition.Attribute),
                    "N" + (current + 1) + " · +" + Percent(definition.GetModifier(Math.Min(definition.MaxLevel, current + 1))) + " " + Attribute(definition.Attribute),
                    "Compatível com " + compatible.DisplayName, requirement, activeEffects: ActiveEffects());
            }
            var evolution = evolutions[id];
            return new UpgradeCardSnapshot(id, evolution.ResultWeapon.DisplayName, "EVOLUÇÃO",
                Stats(evolution.BaseWeapon, 5), Stats(evolution.ResultWeapon, 5),
                evolution.BaseWeapon.DisplayName + " + " + evolution.RequiredPassive.DisplayName,
                "Arma N5 e passiva presentes · substitui a arma no mesmo slot", activeEffects: ActiveEffects());
        }
        private string ActiveEffects()
        {
            var text = new StringBuilder("PASSIVAS ATIVAS: ");
            bool any = false;
            foreach (PassiveAttribute attribute in Enum.GetValues(typeof(PassiveAttribute)))
            {
                float bonus = system.GetPassiveBonus(attribute);
                if (bonus <= 0) continue;
                if (any) text.Append(" · ");
                string label = attribute == PassiveAttribute.Damage ? "Dano" :
                    attribute == PassiveAttribute.AttackSpeed ? "Cadência" :
                    attribute == PassiveAttribute.MovementSpeed ? "Movimento" :
                    attribute == PassiveAttribute.BreathCapacity ? "Capacidade" :
                    attribute == PassiveAttribute.BreathRecovery ? "Recuperação" : "Raio Bolhas";
                text.Append(label).Append(" +").Append(Percent(bonus));
                if (attribute == PassiveAttribute.AreaRadius && !system.Build.Weapons.ContainsKey("bolhas")) text.Append(" (aguarda Bolhas)");
                any = true;
            }
            return any ? text.ToString() : "Sem passivas ativas.";
        }
        private string Stats(WeaponDefinition definition, int level)
        {
            var stats = definition.GetLevel(level);
            var effective = system.ResolveWeaponAttributes(definition, level, lastTerrain, lastRelics);
            return "N" + level + " · Dano " + Number(effective.Damage) + " · Intervalo " + Number(effective.AttackInterval) + "s\nAlcance " + Number(effective.Range) +
                (stats.ProjectileCount > 0 ? " · " + stats.ProjectileCount + " projétil(is)" : " · Área " + Number(effective.AreaRadius));
        }
        private static string Number(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        private static string Percent(float value) => Number(value * 100) + "%";
        private static string Attribute(PassiveAttribute value)
        {
            switch (value)
            {
                case PassiveAttribute.Damage: return "dano";
                case PassiveAttribute.AttackSpeed: return "velocidade de ataque";
                case PassiveAttribute.MovementSpeed: return "velocidade de movimento";
                case PassiveAttribute.BreathCapacity: return "capacidade de Fôlego";
                case PassiveAttribute.BreathRecovery: return "recuperação de Fôlego";
                default: return "raio de Bolhas";
            }
        }
    }
}
