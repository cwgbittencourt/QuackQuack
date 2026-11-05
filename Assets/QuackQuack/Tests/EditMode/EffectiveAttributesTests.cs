using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Content.Characters;
using QuackQuack.Content.Passives;
using QuackQuack.Content.Weapons;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using UnityEditor;
using UnityEngine;

namespace QuackQuack.Tests.EditMode
{
    // T015 is deliberately red until T016. Reflection keeps the existing assemblies
    // compilable without a production stub or a dependency from Core to Content.
    public class EffectiveAttributesTests
    {
        private static readonly string[] PassiveIds = {
            "ninho-reforcado", "oculos-aviador", "pena-aerodinamica",
            "pulmao-poderoso", "amuleto-lago", "sabao-magico"
        };
        private const string WeaponPath = "Assets/QuackQuack/Content/Weapons/Prototype/";
        private readonly Dictionary<UnityEngine.Object, string> originals = new Dictionary<UnityEngine.Object, string>();

        [TearDown]
        public void AssetsRemainUnchanged()
        {
            foreach (var pair in originals)
                Assert.That(EditorJsonUtility.ToJson(pair.Key), Is.EqualTo(pair.Value), pair.Key.name);
            originals.Clear();
        }

        public static IEnumerable PassiveLevels
        {
            get
            {
                foreach (string id in PassiveIds)
                    for (int level = 0; level <= 5; level++)
                        yield return new TestCaseData(id, level).SetName("Passive_" + id + "_Level" + level);
            }
        }

        [TestCaseSource(nameof(PassiveLevels))]
        public void SixAuthoredPassivesAffectOnlyTheirAttributeAtEveryLevel(string id, int level)
        {
            var passive = Passive(id);
            var weapon = Weapon("bolhas");
            var stats = weapon.GetLevel(1, TerrainType.Land);
            var result = Resolve(weapon, 1, TerrainType.Land, new[] { passive }, new[] { level });
            float bonus = Math.Min(level * 0.08f, 0.40f);
            AssertValue(result, "Damage", stats.Damage * (passive.Attribute == PassiveAttribute.Damage ? 1 + bonus : 1));
            AssertValue(result, "AttackInterval", stats.Cooldown / (passive.Attribute == PassiveAttribute.AttackSpeed ? 1 + bonus : 1));
            AssertValue(result, "MovementSpeed", 4 * (passive.Attribute == PassiveAttribute.MovementSpeed ? 1 + bonus : 1));
            AssertValue(result, "BreathCapacity", 100 * (passive.Attribute == PassiveAttribute.BreathCapacity ? 1 + bonus : 1));
            AssertValue(result, "BreathRecovery", 12 * (passive.Attribute == PassiveAttribute.BreathRecovery ? 1 + bonus : 1));
            AssertValue(result, "HitRadius", weapon.HitRadius * (passive.Attribute == PassiveAttribute.AreaRadius ? 1 + bonus : 1));
            AssertValue(result, "Range", stats.Range);
            AssertValue(result, "AreaRadius", stats.AreaRadius);
            AssertValue(result, "ProjectileSpeed", weapon.ProjectileSpeed);
            Assert.That(passive.GetModifier(level), Is.EqualTo(bonus).Within(0.00001f));
        }

        [TestCase(1, 1.08f)]
        [TestCase(5, 1.40f)]
        public void SoapUsesBubbleHitRadiusDespiteZeroArea(int level, float multiplier)
        {
            var weapon = Weapon("bolhas");
            Assert.That(weapon.GetLevel(1).AreaRadius, Is.Zero);
            var result = Resolve(weapon, 1, TerrainType.Land, new[] { Passive("sabao-magico") }, new[] { level });
            AssertValue(result, "HitRadius", weapon.HitRadius * multiplier);
            AssertValue(result, "ProjectileSpeed", weapon.ProjectileSpeed);
            AssertValue(result, "Damage", weapon.GetLevel(1).Damage);
            AssertValue(result, "Range", weapon.GetLevel(1).Range);
        }

        [TestCase("bumerangue")]
        [TestCase("ovos")]
        [TestCase("quack-frontal")]
        [TestCase("aura-energia")]
        [TestCase("foguetes")]
        [TestCase("tornado-penas")]
        [TestCase("incubadora-combate")]
        [TestCase("quack-supersonico")]
        public void SoapDoesNotChangeOtherWeaponsOrEvolutions(string id)
        {
            var weapon = Weapon(id);
            var baseline = Resolve(weapon, 5, TerrainType.Water);
            var soap = Resolve(weapon, 5, TerrainType.Water, new[] { Passive("sabao-magico") }, new[] { 5 });
            foreach (string field in Fields) AssertValue(soap, field, Value(baseline, field));
        }

        [TestCase(TerrainType.Land)]
        [TestCase(TerrainType.Water)]
        public void RecomputeUsesBaseAndCurrentTerrainWithoutAccumulating(TerrainType terrain)
        {
            var weapon = Weapon("bolhas");
            var passives = new[] { Passive("ninho-reforcado"), Passive("sabao-magico") };
            var levels = new[] { 5, 5 };
            for (int i = 0; i < 100; i++)
            {
                var result = Resolve(weapon, 3, terrain, passives, levels);
                AssertValue(result, "Damage", weapon.GetLevel(3, terrain).Damage * 1.4f);
                AssertValue(result, "HitRadius", weapon.HitRadius * 1.4f);
                AssertValue(result, "MovementSpeed", terrain == TerrainType.Water ? 6 : 4);
                AssertValue(result, "BreathRecovery", terrain == TerrainType.Water ? 24 : 12);
            }
            Resolve(weapon, 3, terrain == TerrainType.Land ? TerrainType.Water : TerrainType.Land, passives, levels);
            var cleared = Resolve(weapon, 3, terrain);
            AssertValue(cleared, "Damage", weapon.GetLevel(3, terrain).Damage);
            AssertValue(cleared, "HitRadius", weapon.HitRadius);
            CollectionAssert.AreEqual(new[] { 5, 5 }, levels);
        }

        [TestCase(TerrainType.Land)]
        [TestCase(TerrainType.Water)]
        public void RelicsKeepAdditiveInternalBonusAndComposeAfterPassives(TerrainType terrain)
        {
            var relics = Relics();
            Equip(relics, "damage-any", "damage-water", "area");
            var weapon = Weapon("bolhas");
            var result = Resolve(weapon, 2, terrain,
                new[] { Passive("ninho-reforcado"), Passive("sabao-magico") }, new[] { 1, 5 }, relics);
            float relicDamage = terrain == TerrainType.Water ? 0.3f : 0.1f;
            AssertValue(result, "Damage", weapon.GetLevel(2, terrain).Damage * 1.08f * (1 + relicDamage));
            AssertValue(result, "HitRadius", weapon.HitRadius * 1.4f * 1.1f);
            Assert.That(relics.EquippedIds.Count, Is.EqualTo(3));
            relics.Unequip("area");
            AssertValue(Resolve(weapon, 2, terrain, relics: relics), "HitRadius", weapon.HitRadius);
        }

        [Test]
        public void AttackSpeedMovementRecoveryAndRangeRelicsKeepExistingComposition()
        {
            var relics = Relics();
            Equip(relics, "speed", "move", "recovery");
            var weapon = Weapon("bolhas");
            var result = Resolve(weapon, 1, TerrainType.Water,
                new[] { Passive("pena-aerodinamica"), Passive("amuleto-lago") }, new[] { 5, 5 }, relics);
            AssertValue(result, "AttackInterval", weapon.GetLevel(1, TerrainType.Water).Cooldown / 1.4f / 1.1f);
            AssertValue(result, "MovementSpeed", 6 * 1.1f);
            AssertValue(result, "BreathRecovery", 24 * 1.4f * 1.1f);
            relics.Unequip("move");
            Equip(relics, "range");
            AssertValue(Resolve(weapon, 1, TerrainType.Water, relics: relics), "Range", weapon.GetLevel(1).Range * 1.1f);
        }

        [TestCase("bumerangue", "pena-aerodinamica", "tornado-penas")]
        [TestCase("ovos", "ninho-reforcado", "incubadora-combate")]
        [TestCase("quack-frontal", "pulmao-poderoso", "quack-supersonico")]
        public void EvolutionResolvesReplacementBaseAndPreservesPassive(string baseId, string passiveId, string evolvedId)
        {
            var build = new BuildState();
            Assert.That(build.TryAddWeapon(baseId), Is.True);
            for (int i = 1; i < 5; i++) Assert.That(build.TryUpgradeWeapon(baseId), Is.True);
            Assert.That(build.TryAddPassive(passiveId), Is.True);
            Assert.That(build.TryEvolveWeapon(baseId, passiveId, evolvedId), Is.True);
            var weapon = Weapon(evolvedId);
            var passive = Passive(passiveId);
            var result = Resolve(weapon, build.Weapons[evolvedId], TerrainType.Water,
                new[] { passive }, new[] { build.Passives[passiveId] });
            var stats = weapon.GetLevel(5, TerrainType.Water);
            AssertValue(result, "Damage", stats.Damage * (passive.Attribute == PassiveAttribute.Damage ? 1.08f : 1));
            AssertValue(result, "AttackInterval", stats.Cooldown / (passive.Attribute == PassiveAttribute.AttackSpeed ? 1.08f : 1));
            AssertValue(result, "BreathCapacity", passive.Attribute == PassiveAttribute.BreathCapacity ? 108 : 100);
            Assert.That(build.Weapons.ContainsKey(baseId), Is.False);
        }

        [Test]
        public void AuthoredCapIsUsedInsteadOfHardcodedFortyPercent()
        {
            var clone = UnityEngine.Object.Instantiate(Passive("sabao-magico"));
            try
            {
                var serialized = new SerializedObject(clone);
                serialized.FindProperty("modifierPerLevel").floatValue = 0.12f;
                serialized.FindProperty("modifierLimit").floatValue = 0.25f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var weapon = Weapon("bolhas");
                AssertValue(Resolve(weapon, 1, TerrainType.Land, new[] { clone }, new[] { 5 }), "HitRadius", weapon.HitRadius * 1.25f);
            }
            finally { UnityEngine.Object.DestroyImmediate(clone); }
        }

        private static readonly string[] Fields = { "Damage", "AttackInterval", "MovementSpeed", "BreathCapacity", "BreathRecovery", "Range", "AreaRadius", "HitRadius", "ProjectileSpeed" };

        // Proposed pure T016 seam: nine base scalars, bubble eligibility, six
        // authored bonuses in PassiveAttribute order, current relics and terrain.
        // Content/BuildState extraction stays on this side of the Core boundary.
        private object Resolve(WeaponDefinition weapon, int level, TerrainType terrain,
            PassiveDefinition[] passives = null, int[] levels = null, RelicLoadout relics = null)
        {
            var bonuses = new float[6];
            if (passives != null)
                for (int i = 0; i < passives.Length; i++) bonuses[(int)passives[i].Attribute] += passives[i].GetModifier(levels[i]);
            var stats = weapon.GetLevel(level, terrain);
            var character = Load<CharacterDefinition>("Assets/QuackQuack/Content/Characters/Pato.asset");
            if (!originals.ContainsKey(character.Quack)) originals.Add(character.Quack, EditorJsonUtility.ToJson(character.Quack));
            float speed = terrain == TerrainType.Water ? character.WaterSpeed : character.LandSpeed;
            float recovery = terrain == TerrainType.Water ? character.Quack.WaterRecovery : character.Quack.LandRecovery;
            var type = typeof(BuildState).Assembly.GetType("QuackQuack.Core.Combat.EffectiveAttributes");
            Assert.That(type, Is.Not.Null, "T016 pending: pure EffectiveAttributes resolver is missing.");
            var method = type.GetMethod("Resolve", BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(float), typeof(float), typeof(float), typeof(float), typeof(float), typeof(float),
                    typeof(float), typeof(float), typeof(float), typeof(bool), typeof(float[]), typeof(RelicLoadout), typeof(TerrainType) }, null);
            Assert.That(method, Is.Not.Null, "T016 pending: implement the scalar Resolve seam documented in T015.");
            return method.Invoke(null, new object[] { stats.Damage, stats.Cooldown, speed, character.Quack.Capacity, recovery,
                stats.Range, stats.AreaRadius, weapon.HitRadius, weapon.ProjectileSpeed,
                weapon.Behavior == WeaponBehavior.Bubbles, bonuses, relics, terrain });
        }

        private T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, path);
            if (!originals.ContainsKey(asset)) originals.Add(asset, EditorJsonUtility.ToJson(asset));
            return asset;
        }
        private PassiveDefinition Passive(string id) => Load<PassiveDefinition>("Assets/QuackQuack/Content/Passives/Prototype/" + id + ".asset");
        private WeaponDefinition Weapon(string id) => Load<WeaponDefinition>(WeaponPath + id + ".asset");
        private static float Value(object result, string field)
        {
            Assert.That(result, Is.Not.Null);
            var property = result.GetType().GetProperty(field);
            Assert.That(property, Is.Not.Null, "Missing effective attribute: " + field);
            return (float)property.GetValue(result);
        }
        private static void AssertValue(object result, string field, float expected)
        {
            float actual = Value(result, field);
            Assert.That(float.IsNaN(actual) || float.IsInfinity(actual), Is.False, field);
            Assert.That(actual, Is.GreaterThanOrEqualTo(0), field);
            Assert.That(actual, Is.EqualTo(expected).Within(0.00001f), field);
        }
        private static RelicLoadout Relics()
        {
            return new RelicLoadout(new[] {
                new RelicModifier("damage-any", RelicAttribute.Damage, RelicTerrain.Any, 0.1f),
                new RelicModifier("damage-water", RelicAttribute.Damage, RelicTerrain.Water, 0.2f),
                new RelicModifier("area", RelicAttribute.Area, RelicTerrain.Any, 0.1f),
                new RelicModifier("speed", RelicAttribute.AttackSpeed, RelicTerrain.Any, 0.1f),
                new RelicModifier("move", RelicAttribute.MovementSpeed, RelicTerrain.Any, 0.1f),
                new RelicModifier("recovery", RelicAttribute.BreathRecovery, RelicTerrain.Any, 0.1f),
                new RelicModifier("range", RelicAttribute.Range, RelicTerrain.Any, 0.1f),
                new RelicModifier("unused1", RelicAttribute.Damage, RelicTerrain.Land, 0.1f),
                new RelicModifier("unused2", RelicAttribute.Damage, RelicTerrain.Land, 0.1f),
                new RelicModifier("unused3", RelicAttribute.Damage, RelicTerrain.Land, 0.1f)
            });
        }
        private static void Equip(RelicLoadout relics, params string[] ids)
        {
            foreach (string id in ids) { relics.Unlock(id); Assert.That(relics.Equip(id), Is.True, id); }
        }
    }
}
