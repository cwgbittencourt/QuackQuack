using System;
using System.Collections.Generic;
using QuackQuack.Content.Passives;
using QuackQuack.Content.Shared;
using UnityEngine;

namespace QuackQuack.Content.Weapons
{
    [CreateAssetMenu(menuName = "QuackQuack/Evolution")]
    public sealed class EvolutionDefinition : ContentDefinition
    {
        [SerializeField] private WeaponDefinition baseWeapon, resultWeapon;
        [SerializeField] private PassiveDefinition requiredPassive;
        public WeaponDefinition BaseWeapon => baseWeapon;
        public WeaponDefinition ResultWeapon => resultWeapon;
        public PassiveDefinition RequiredPassive => requiredPassive;
        public int RequiredWeaponLevel => WeaponDefinition.RequiredLevelCount;

        public override bool IsValid(out string reason)
        {
            if (!base.IsValid(out reason)) return false;
            if (baseWeapon == null || resultWeapon == null || requiredPassive == null)
            { reason = "Evolution requires a base weapon, passive and result weapon."; return false; }
            if (!baseWeapon.IsValid(out reason) || !requiredPassive.IsValid(out reason) || !resultWeapon.IsValid(out reason))
                return false;
            if (baseWeapon.Id == resultWeapon.Id) reason = "Evolution must replace the base with a different weapon.";
            else if (baseWeapon.CompatiblePassiveId != requiredPassive.Id || requiredPassive.CompatibleWeaponId != baseWeapon.Id)
                reason = "Evolution weapon/passive links must match the recipe.";
            else if (baseWeapon.EvolutionWeaponId != resultWeapon.Id)
                reason = "Evolution result must match the base weapon's declared result.";
            return reason == null;
        }

        // Eligibility only. Applying/replacing inventory and emitting events belong to progression.
        public bool CanEvolve(string ownedWeaponId, int weaponLevel, IEnumerable<string> ownedPassiveIds)
        {
            if (!IsValid(out _) || ownedWeaponId != baseWeapon.Id || weaponLevel != RequiredWeaponLevel || ownedPassiveIds == null)
                return false;
            foreach (string passiveId in ownedPassiveIds)
                if (passiveId == requiredPassive.Id) return true;
            return false;
        }

        public static bool IsValidCatalog(IEnumerable<EvolutionDefinition> evolutions,
            IEnumerable<WeaponDefinition> weapons, IEnumerable<PassiveDefinition> passives, out string reason)
        {
            reason = null;
            if (evolutions == null || weapons == null || passives == null)
            { reason = "Evolution, weapon and passive catalogs are required."; return false; }
            // Materialize once so single-pass enumerables work across catalog validation and lookup.
            var weaponList = new List<WeaponDefinition>(weapons);
            var passiveList = new List<PassiveDefinition>(passives);
            if (!WeaponDefinition.IsValidCatalog(weaponList, passiveList, out reason)) return false;
            var weaponIds = new Dictionary<string, WeaponDefinition>(StringComparer.Ordinal);
            var passiveIds = new Dictionary<string, PassiveDefinition>(StringComparer.Ordinal);
            foreach (var weapon in weaponList) weaponIds.Add(weapon.Id, weapon);
            foreach (var passive in passiveList) passiveIds.Add(passive.Id, passive);
            var evolutionIds = new HashSet<string>(StringComparer.Ordinal);
            var combinations = new HashSet<(string weapon, string passive)>();
            foreach (var evolution in evolutions)
            {
                if (evolution == null) { reason = "Missing evolution definition."; return false; }
                if (!evolution.IsValid(out reason)) return false;
                if (!evolutionIds.Add(evolution.Id)) { reason = "Duplicate evolution ID: " + evolution.Id; return false; }
                if (!combinations.Add((evolution.BaseWeapon.Id, evolution.RequiredPassive.Id)))
                { reason = "Duplicate evolution combination: " + evolution.BaseWeapon.Id + " / " + evolution.RequiredPassive.Id; return false; }
                if (!weaponIds.TryGetValue(evolution.BaseWeapon.Id, out var baseAsset) || baseAsset != evolution.BaseWeapon ||
                    !weaponIds.TryGetValue(evolution.ResultWeapon.Id, out var resultAsset) || resultAsset != evolution.ResultWeapon ||
                    !passiveIds.TryGetValue(evolution.RequiredPassive.Id, out var passiveAsset) || passiveAsset != evolution.RequiredPassive)
                { reason = "Evolution references must be the registered catalog assets: " + evolution.Id; return false; }
            }
            if (evolutionIds.Count == 0) { reason = "Evolution catalog must not be empty."; return false; }
            return true;
        }
    }
}
