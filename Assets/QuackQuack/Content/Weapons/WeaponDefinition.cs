using System;
using System.Collections.Generic;
using QuackQuack.Content.Shared;
using QuackQuack.Content.Passives;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Terrain;
using UnityEngine;

namespace QuackQuack.Content.Weapons
{
    public enum WeaponTargetMode { NearestEnemy, Forward, Orbit, Self }
    public enum WeaponBehavior { Boomerang, Eggs, FrontalQuack, EnergyAura, Rockets, Bubbles }

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
        [SerializeField] private WeaponBehavior behavior;
        [SerializeField] private float projectileSpeed = 6, projectileLifetime = 2, hitRadius = 0.2f, turnSpeed = 180, coneAngle = 60;
        [SerializeField] private Sprite projectileSprite;
        [SerializeField] private Material projectileMaterial;
        [SerializeField] private WeaponLevel[] levels = new WeaponLevel[RequiredLevelCount];
        [SerializeField] private string compatiblePassiveId, evolutionWeaponId;
        [SerializeField] private bool hasTerrainAffinity;
        [SerializeField] private float landDamageMultiplier = 1, waterDamageMultiplier = 1;
        [SerializeField] private float landCooldownMultiplier = 1, waterCooldownMultiplier = 1;
        public WeaponTargetMode TargetMode => targetMode;
        public WeaponBehavior Behavior => behavior;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileLifetime => projectileLifetime;
        public float HitRadius => hitRadius;
        public float TurnSpeed => turnSpeed;
        public float ConeAngle => coneAngle;
        public Sprite ProjectileSprite => projectileSprite;
        public Material ProjectileMaterial => projectileMaterial;
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
            else if (!Enum.IsDefined(typeof(WeaponBehavior), behavior)) reason = "Weapon behavior is invalid.";
            else if (!Positive(projectileSpeed) || !Positive(projectileLifetime) || !Positive(hitRadius) ||
                !Positive(turnSpeed) || !Positive(coneAngle) || coneAngle > 360 || !Positive(projectileSpeed * projectileLifetime))
                reason = "Weapon motion and collision parameters must be finite and positive; cone angle must not exceed 360.";
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
                    (value.ProjectileCount == 0 && value.AreaRadius == 0) ||
                    (value.ProjectileCount == 0 && behavior != WeaponBehavior.EnergyAura && behavior != WeaponBehavior.FrontalQuack))
                    reason = "Invalid weapon stats at level " + (i + 1) + ".";
                else if (hasTerrainAffinity &&
                    (!Positive(value.Damage * landDamageMultiplier) || !Positive(value.Damage * waterDamageMultiplier) ||
                    !Positive(value.Cooldown * landCooldownMultiplier) || !Positive(value.Cooldown * waterCooldownMultiplier)))
                    reason = "Terrain affinity produces invalid effective stats at level " + (i + 1) + ".";
                if (reason != null) return false;
            }
            return true;
        }

        public static bool IsValidCatalog(IEnumerable<WeaponDefinition> weapons,
            IEnumerable<PassiveDefinition> passives, out string reason)
        {
            reason = null;
            if (weapons == null || passives == null)
            { reason = "Weapon and passive catalogs are required."; return false; }
            var weaponIds = new Dictionary<string, WeaponDefinition>(StringComparer.Ordinal);
            var passiveIds = new Dictionary<string, PassiveDefinition>(StringComparer.Ordinal);
            foreach (var passive in passives)
            {
                if (passive == null) { reason = "Missing passive definition."; return false; }
                if (!passive.IsValid(out reason)) return false;
                if (passiveIds.ContainsKey(passive.Id)) { reason = "Duplicate passive ID: " + passive.Id; return false; }
                passiveIds.Add(passive.Id, passive);
            }
            foreach (var weapon in weapons)
            {
                if (weapon == null) { reason = "Missing weapon definition."; return false; }
                if (!weapon.IsValid(out reason)) return false;
                if (weaponIds.ContainsKey(weapon.Id)) { reason = "Duplicate weapon ID: " + weapon.Id; return false; }
                weaponIds.Add(weapon.Id, weapon);
            }
            if (weaponIds.Count == 0) { reason = "Weapon catalog must not be empty."; return false; }
            foreach (var passive in passiveIds.Values)
            {
                if (!weaponIds.ContainsKey(passive.CompatibleWeaponId))
                { reason = "Unknown compatible weapon: " + passive.CompatibleWeaponId; return false; }
                var compatible = weaponIds[passive.CompatibleWeaponId];
                if (!string.IsNullOrEmpty(compatible.CompatiblePassiveId) && compatible.CompatiblePassiveId != passive.Id)
                { reason = "Inconsistent compatible passive for weapon: " + compatible.Id; return false; }
            }
            foreach (var weapon in weaponIds.Values)
            {
                if (string.IsNullOrEmpty(weapon.EvolutionWeaponId)) continue;
                if (!passiveIds.ContainsKey(weapon.CompatiblePassiveId))
                { reason = "Unknown compatible passive: " + weapon.CompatiblePassiveId; return false; }
                if (passiveIds[weapon.CompatiblePassiveId].CompatibleWeaponId != weapon.Id)
                { reason = "Compatible weapon/passive references must be reciprocal: " + weapon.Id; return false; }
                if (!weaponIds.ContainsKey(weapon.EvolutionWeaponId))
                { reason = "Unknown evolution weapon: " + weapon.EvolutionWeaponId; return false; }
            }
            return true;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Positive(float value) => Finite(value) && value > 0;
    }
}
