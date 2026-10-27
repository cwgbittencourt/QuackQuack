using System;
using System.Collections.Generic;
using QuackQuack.Core.Run;

namespace QuackQuack.Core.Progression
{
    public enum RelicAttribute { Damage, MovementSpeed, AttackSpeed, Range, Area, BreathRecovery }
    public enum RelicTerrain { Any, Land, Water }
    public readonly struct RelicModifier
    {
        public string Id { get; }
        public RelicAttribute Attribute { get; }
        public RelicTerrain Terrain { get; }
        public float Bonus { get; }
        public RelicModifier(string id, RelicAttribute attribute, RelicTerrain terrain, float bonus)
        { Id = id; Attribute = attribute; Terrain = terrain; Bonus = bonus; }
    }
    public sealed class RelicLoadout
    {
        public const int SlotLimit = 3;
        private readonly Dictionary<string, RelicModifier> definitions = new Dictionary<string, RelicModifier>(StringComparer.Ordinal);
        private readonly List<string> unlocked = new List<string>(), equipped = new List<string>();
        public IReadOnlyCollection<string> UnlockedIds { get; }
        public IReadOnlyCollection<string> EquippedIds { get; }
        public int Version { get; private set; }
        public RelicLoadout(string[] knownIds, float[] damageBonuses) : this(Convert(knownIds, damageBonuses)) { }
        public RelicLoadout(IEnumerable<RelicModifier> modifiers)
        {
            if (modifiers == null) throw new ArgumentNullException(nameof(modifiers));
            foreach (var value in modifiers)
            {
                if (string.IsNullOrWhiteSpace(value.Id) || definitions.ContainsKey(value.Id) ||
                    !Enum.IsDefined(typeof(RelicAttribute), value.Attribute) || !Enum.IsDefined(typeof(RelicTerrain), value.Terrain) ||
                    float.IsNaN(value.Bonus) || float.IsInfinity(value.Bonus) || value.Bonus <= 0 || value.Bonus > 1)
                    throw new ArgumentException("Relic IDs must be unique, with valid attributes and bonuses in (0,1].");
                definitions.Add(value.Id, value);
            }
            if (definitions.Count != 10) throw new ArgumentException("Prototype requires ten relics.");
            UnlockedIds = unlocked.AsReadOnly(); EquippedIds = equipped.AsReadOnly();
        }
        public bool IsUnlocked(string id) => id != null && unlocked.Contains(id);
        public bool IsEquipped(string id) => id != null && equipped.Contains(id);
        public bool Unlock(string id)
        {
            if (id == null || !definitions.ContainsKey(id) || IsUnlocked(id)) return false;
            unlocked.Add(id); Version++; return true;
        }
        public bool Equip(string id)
        {
            if (!IsUnlocked(id) || IsEquipped(id) || equipped.Count == SlotLimit) return false;
            equipped.Add(id); Version++; return true;
        }
        public bool Unequip(string id)
        { if (!equipped.Remove(id)) return false; Version++; return true; }
        public float GetBonus(RelicAttribute attribute, TerrainType terrain)
        {
            float sum = 0;
            foreach (string id in equipped)
            {
                var value = definitions[id];
                if (value.Attribute == attribute && (value.Terrain == RelicTerrain.Any ||
                    (value.Terrain == RelicTerrain.Water && terrain == TerrainType.Water) ||
                    (value.Terrain == RelicTerrain.Land && terrain == TerrainType.Land))) sum += value.Bonus;
            }
            return sum;
        }
        public float ApplyEffects(float baseDamage) => ApplyEffects(baseDamage, RelicAttribute.Damage, TerrainType.Land);
        public float ApplyEffects(float baseline, RelicAttribute attribute, TerrainType terrain)
        {
            if (float.IsNaN(baseline) || float.IsInfinity(baseline) || baseline < 0) throw new ArgumentOutOfRangeException(nameof(baseline));
            double result = (double)baseline * (1 + GetBonus(attribute, terrain));
            if (result > float.MaxValue) throw new ArgumentOutOfRangeException(nameof(baseline));
            return (float)result;
        }
        private static IEnumerable<RelicModifier> Convert(string[] ids, float[] bonuses)
        {
            if (ids == null || bonuses == null || ids.Length != bonuses.Length) throw new ArgumentException("Matching IDs and bonuses are required.");
            var result = new RelicModifier[ids.Length];
            for (int i = 0; i < result.Length; i++) result[i] = new RelicModifier(ids[i], RelicAttribute.Damage, RelicTerrain.Any, bonuses[i]);
            return result;
        }
    }
}
