using System;
using System.Collections.Generic;
using QuackQuack.Content.Shared;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Terrain;
using UnityEngine;

namespace QuackQuack.Content.Weapons
{
    public enum WeaponTargetMode { NearestEnemy, Forward, Orbit, Self }

    [Serializable]
    public struct WeaponLevel
    {
        [SerializeField] private float damage, cooldown, range, areaRadius;
        [SerializeField] private int projectileCount;
        public float Damage => damage;
        public float Cooldown => cooldown;
        public float Range => range;
        public float AreaRadius => areaRadius;
        public int ProjectileCount => projectileCount;

        public WeaponLevel(float damage, float cooldown, float range, int projectileCount, float areaRadius)
        {
            this.damage = damage; this.cooldown = cooldown; this.range = range;
            this.projectileCount = projectileCount; this.areaRadius = areaRadius;
        }
    }

    [CreateAssetMenu(menuName = "QuackQuack/Weapon")]
    public sealed class WeaponDefinition : ContentDefinition
    {
        public const int RequiredLevelCount = 5;
        [SerializeField] private WeaponTargetMode targetMode;
        [SerializeField] private WeaponLevel[] levels = new WeaponLevel[RequiredLevelCount];
        [SerializeField] private string compatiblePassiveId, evolutionWeaponId;
        [SerializeField] private bool hasTerrainAffinity;
        [SerializeField] private float landDamageMultiplier = 1, waterDamageMultiplier = 1;
        [SerializeField] private float landCooldownMultiplier = 1, waterCooldownMultiplier = 1;
        public WeaponTargetMode TargetMode => targetMode;
        public int LevelCount => levels == null ? 0 : levels.Length;
        public string CompatiblePassiveId => compatiblePassiveId;
        public string EvolutionWeaponId => evolutionWeaponId;
        public bool HasTerrainAffinity => hasTerrainAffinity;

        public WeaponLevel GetLevel(int level)
        {
            if (level < 1 || level > RequiredLevelCount || levels == null || levels.Length != RequiredLevelCount)
                throw new ArgumentOutOfRangeException(nameof(level));
            return levels[level - 1];
        }

        public WeaponLevel GetLevel(int level, TerrainType terrain)
        {
            var authored = GetLevel(level);
            float damage = TerrainAffinity.Apply(authored.Damage, terrain,
                hasTerrainAffinity ? landDamageMultiplier : 1, hasTerrainAffinity ? waterDamageMultiplier : 1);
            float cooldown = TerrainAffinity.Apply(authored.Cooldown, terrain,
                hasTerrainAffinity ? landCooldownMultiplier : 1, hasTerrainAffinity ? waterCooldownMultiplier : 1);
            return new WeaponLevel(damage, cooldown, authored.Range, authored.ProjectileCount, authored.AreaRadius);
        }

        public override bool IsValid(out string reason)
        {
            if (!base.IsValid(out reason)) return false;
            if (!Enum.IsDefined(typeof(WeaponTargetMode), targetMode))
                reason = "Weapon target mode is invalid.";
            else if (levels == null || levels.Length != RequiredLevelCount)
                reason = "A weapon must define exactly five levels.";
            else if ((!string.IsNullOrEmpty(compatiblePassiveId) || !string.IsNullOrEmpty(evolutionWeaponId)) &&
                (string.IsNullOrWhiteSpace(compatiblePassiveId) || string.IsNullOrWhiteSpace(evolutionWeaponId)))
                reason = "Evolution requires both a compatible passive ID and a result weapon ID.";
            else if (!string.IsNullOrEmpty(evolutionWeaponId) && evolutionWeaponId == Id)
                reason = "A weapon cannot evolve into itself.";
            else if (hasTerrainAffinity && (!Positive(landDamageMultiplier) || !Positive(waterDamageMultiplier) ||
                !Positive(landCooldownMultiplier) || !Positive(waterCooldownMultiplier)))
                reason = "Enabled terrain multipliers must be finite and positive.";
            if (reason != null) return false;

            for (int i = 0; i < levels.Length; i++)
            {
                var value = levels[i];
                if (!Positive(value.Damage) || !Positive(value.Cooldown) || !Positive(value.Range) ||
                    !Finite(value.AreaRadius) || value.AreaRadius < 0 || value.ProjectileCount < 0 ||
                    (value.ProjectileCount == 0 && value.AreaRadius == 0))
                    reason = "Invalid weapon stats at level " + (i + 1) + ".";
                else if (hasTerrainAffinity &&
                    (!Positive(value.Damage * landDamageMultiplier) || !Positive(value.Damage * waterDamageMultiplier) ||
                    !Positive(value.Cooldown * landCooldownMultiplier) || !Positive(value.Cooldown * waterCooldownMultiplier)))
                    reason = "Terrain affinity produces invalid effective stats at level " + (i + 1) + ".";
                if (reason != null) return false;
            }
            return true;
        }

        // Pass only the passive catalog here; typed passive/evolution definitions arrive in T037/T038.
        public static bool IsValidCatalog(IEnumerable<WeaponDefinition> weapons,
            IEnumerable<ContentDefinition> passives, out string reason)
        {
            reason = null;
            if (weapons == null || passives == null)
            { reason = "Weapon and passive catalogs are required."; return false; }
            var weaponIds = new Dictionary<string, WeaponDefinition>(StringComparer.Ordinal);
            var passiveIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var passive in passives)
            {
                if (passive == null) { reason = "Missing passive definition."; return false; }
                if (!passive.IsValid(out reason)) return false;
                if (!passiveIds.Add(passive.Id)) { reason = "Duplicate passive ID: " + passive.Id; return false; }
            }
            foreach (var weapon in weapons)
            {
                if (weapon == null) { reason = "Missing weapon definition."; return false; }
                if (!weapon.IsValid(out reason)) return false;
                if (weaponIds.ContainsKey(weapon.Id)) { reason = "Duplicate weapon ID: " + weapon.Id; return false; }
                weaponIds.Add(weapon.Id, weapon);
            }
            if (weaponIds.Count == 0) { reason = "Weapon catalog must not be empty."; return false; }
            foreach (var weapon in weaponIds.Values)
            {
                if (string.IsNullOrEmpty(weapon.EvolutionWeaponId)) continue;
                if (!passiveIds.Contains(weapon.CompatiblePassiveId))
                { reason = "Unknown compatible passive: " + weapon.CompatiblePassiveId; return false; }
                if (!weaponIds.ContainsKey(weapon.EvolutionWeaponId))
                { reason = "Unknown evolution weapon: " + weapon.EvolutionWeaponId; return false; }
            }
            return true;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Positive(float value) => Finite(value) && value > 0;
    }
}
