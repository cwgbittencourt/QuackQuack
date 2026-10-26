using System;
using System.Collections.Generic;
using QuackQuack.Content.Shared;
using QuackQuack.Content.Passives;
using QuackQuack.Content.Weapons;
using QuackQuack.Core.Progression;
using QuackQuack.Systems.Upgrades;
using UnityEngine;

namespace QuackQuack.Content.Catalogs
{
    [CreateAssetMenu(menuName = "QuackQuack/Prototype build catalog")]
    public sealed class PrototypeBuildCatalog : ContentDefinition
    {
        [SerializeField] private WeaponDefinition[] weapons;
        [SerializeField] private PassiveDefinition[] passives;
        [SerializeField] private EvolutionDefinition[] evolutions;
        public IReadOnlyList<WeaponDefinition> Weapons => Array.AsReadOnly(weapons ?? Array.Empty<WeaponDefinition>());
        public IReadOnlyList<PassiveDefinition> Passives => Array.AsReadOnly(passives ?? Array.Empty<PassiveDefinition>());
        public IReadOnlyList<EvolutionDefinition> Evolutions => Array.AsReadOnly(evolutions ?? Array.Empty<EvolutionDefinition>());

        // Includes the three result definitions, but only the six basic weapons are selectable.
        // This startup query returns a snapshot; no catalog enumeration belongs in the combat loop.
        public IReadOnlyList<WeaponDefinition> GetAllWeapons()
        {
            var all = new List<WeaponDefinition>();
            if (weapons != null) all.AddRange(weapons);
            if (evolutions != null)
                foreach (var evolution in evolutions) if (evolution != null) all.Add(evolution.ResultWeapon);
            return all.AsReadOnly();
        }

        public override bool IsValid(out string reason)
        {
            if (!base.IsValid(out reason)) return false;
            if (weapons == null || weapons.Length != 6 || passives == null || passives.Length != 6 ||
                evolutions == null || evolutions.Length != 3)
            { reason = "Prototype build requires six basic weapons, six passives and three evolutions."; return false; }
            var behaviors = new HashSet<WeaponBehavior>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var weapon in weapons)
            {
                if (weapon == null || !weapon.IsValid(out reason))
                { reason = reason ?? "Missing basic weapon."; return false; }
                if (!behaviors.Add(weapon.Behavior) || !ids.Add(weapon.Id))
                { reason = "Basic weapons must have unique IDs and cover all six behaviors."; return false; }
            }
            var baseIds = new HashSet<string>(ids, StringComparer.Ordinal);
            foreach (var passive in passives)
            {
                if (passive == null || !passive.IsValid(out reason))
                { reason = reason ?? "Missing passive."; return false; }
                if (!ids.Add(passive.Id) || !baseIds.Contains(passive.CompatibleWeaponId))
                { reason = "Passive IDs must be unique and reference a basic weapon."; return false; }
            }
            foreach (var evolution in evolutions)
            {
                if (evolution == null || !evolution.IsValid(out reason))
                { reason = reason ?? "Missing evolution."; return false; }
                if (!baseIds.Contains(evolution.BaseWeapon.Id) || !ids.Add(evolution.Id) || !ids.Add(evolution.ResultWeapon.Id))
                { reason = "Evolution IDs/results must be distinct and reference a basic weapon."; return false; }
            }
            return EvolutionDefinition.IsValidCatalog(evolutions, GetAllWeapons(), passives, out reason);
        }

        public UpgradeSystem CreateUpgradeSystem(int seed, BuildState build)
        {
            if (!IsValid(out var reason)) throw new InvalidOperationException(reason);
            return new UpgradeSystem(seed, build, GetAllWeapons(), passives, evolutions);
        }
    }
}
