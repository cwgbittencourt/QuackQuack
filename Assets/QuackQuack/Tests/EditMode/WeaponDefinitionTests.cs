using System;
using NUnit.Framework;
using QuackQuack.Content.Passives;
using QuackQuack.Content.Weapons;
using QuackQuack.Core.Run;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.EditMode
{
    public class WeaponDefinitionTests
    {
        private WeaponDefinition weapon, evolved;
        private PassiveDefinition passiveFixture;

        [SetUp]
        public void SetUp()
        {
            weapon = CreateWeapon("weapon");
            evolved = CreateWeapon("evolved");
            passiveFixture = ScriptableObject.CreateInstance<PassiveDefinition>();
            Set(passiveFixture, "id", p => p.stringValue = "passive");
            Set(passiveFixture, "compatibleWeaponId", p => p.stringValue = "weapon");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(weapon);
            Object.DestroyImmediate(evolved);
            Object.DestroyImmediate(passiveFixture);
        }

        [Test]
        public void FiveAuthoredLevelsAreReadAsValuesAndCannotBeMutatedThroughTheApi()
        {
            Assert.That(weapon.IsValid(out var reason), Is.True, reason);
            Assert.That(weapon.LevelCount, Is.EqualTo(5));
            for (int i = 1; i <= 5; i++) Assert.That(weapon.GetLevel(i).Damage, Is.EqualTo(i));
            var copy = weapon.GetLevel(1);
            copy = new WeaponLevel(999, 1, 1, 1, 0);
            Assert.That(copy.Damage, Is.EqualTo(999));
            Assert.That(weapon.GetLevel(1).Damage, Is.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(4)]
        [TestCase(6)]
        public void ExactlyFiveLevelsAreRequired(int count)
        {
            Set(weapon, "levels", p => p.arraySize = count);
            Assert.That(weapon.IsValid(out var reason), Is.False);
            Assert.That(reason, Is.Not.Empty);
        }

        [TestCase(0)]
        [TestCase(6)]
        [TestCase(-1)]
        public void OutOfRangeLevelsAreRejected(int level) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => weapon.GetLevel(level));

        [TestCase("damage")]
        [TestCase("cooldown")]
        [TestCase("range")]
        [TestCase("areaRadius")]
        public void EveryLevelRejectsNegativeAndNonFiniteNumbers(string field)
        {
            for (int level = 0; level < 5; level++)
            {
                foreach (float value in new[] { -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                {
                    SetLevel(level, field, value);
                    Assert.That(weapon.IsValid(out var reason), Is.False, field + " level " + (level + 1));
                    Assert.That(reason, Is.Not.Empty);
                }
                SetLevel(level, field, field == "areaRadius" ? 0 : 1);
            }
        }

        [TestCase("damage")]
        [TestCase("cooldown")]
        [TestCase("range")]
        public void ZeroDamageCooldownAndRangeAreRejected(string field)
        {
            SetLevel(0, field, 0);
            Assert.That(weapon.IsValid(out _), Is.False);
        }

        [Test]
        public void AreaWeaponMayHaveNoProjectileButMustHaveAnArea()
        {
            Set(weapon, "behavior", p => p.enumValueIndex = (int)WeaponBehavior.EnergyAura);
            Set(weapon, "levels", p => p.GetArrayElementAtIndex(0).FindPropertyRelative("projectileCount").intValue = 0);
            Assert.That(weapon.IsValid(out _), Is.False);
            SetLevel(0, "areaRadius", 2);
            Assert.That(weapon.IsValid(out var reason), Is.True, reason);
            Set(weapon, "levels", p => p.GetArrayElementAtIndex(0).FindPropertyRelative("projectileCount").intValue = -1);
            Assert.That(weapon.IsValid(out _), Is.False);
        }

        [Test]
        public void InvalidIdAndTargetModeAreRejected()
        {
            Set(weapon, "id", p => p.stringValue = " ");
            Assert.That(weapon.IsValid(out _), Is.False);
            Set(weapon, "id", p => p.stringValue = "weapon");
            Set(weapon, "targetMode", p => p.intValue = 99);
            Assert.That(weapon.IsValid(out _), Is.False);
        }

        [Test]
        public void EvolutionRequiresBothIdsAndCannotPointToItself()
        {
            Set(weapon, "compatiblePassiveId", p => p.stringValue = "passive");
            Assert.That(weapon.IsValid(out _), Is.False);
            Set(weapon, "evolutionWeaponId", p => p.stringValue = "weapon");
            Assert.That(weapon.IsValid(out _), Is.False);
            Set(weapon, "evolutionWeaponId", p => p.stringValue = "evolved");
            Assert.That(weapon.IsValid(out var reason), Is.True, reason);
            Set(weapon, "compatiblePassiveId", p => p.stringValue = "");
            Assert.That(weapon.IsValid(out _), Is.False);
        }

        [Test]
        public void CatalogResolvesEvolutionAndRejectsMissingOrDuplicateReferences()
        {
            Set(weapon, "compatiblePassiveId", p => p.stringValue = "passive");
            Set(weapon, "evolutionWeaponId", p => p.stringValue = "evolved");
            var passives = new[] { passiveFixture };
            Assert.That(WeaponDefinition.IsValidCatalog(new[] { weapon, evolved }, passives, out var reason), Is.True, reason);
            Assert.That(WeaponDefinition.IsValidCatalog(new[] { weapon }, passives, out _), Is.False);
            Assert.That(WeaponDefinition.IsValidCatalog(new[] { weapon, evolved }, Array.Empty<PassiveDefinition>(), out _), Is.False);
            Assert.That(WeaponDefinition.IsValidCatalog(new[] { weapon, weapon, evolved }, passives, out _), Is.False);
            Assert.That(WeaponDefinition.IsValidCatalog(new[] { weapon, evolved }, new[] { passiveFixture, passiveFixture }, out _), Is.False);
            Assert.That(WeaponDefinition.IsValidCatalog(new WeaponDefinition[] { null }, passives, out _), Is.False);
            Assert.That(WeaponDefinition.IsValidCatalog(null, passives, out _), Is.False);
            Assert.That(WeaponDefinition.IsValidCatalog(new[] { weapon, evolved }, null, out _), Is.False);
        }

        [Test]
        public void PassiveCatalogRequiresExistingAndReciprocalWeaponReference()
        {
            Set(weapon, "compatiblePassiveId", p => p.stringValue = "passive");
            Set(weapon, "evolutionWeaponId", p => p.stringValue = "evolved");
            Set(passiveFixture, "compatibleWeaponId", p => p.stringValue = "missing");
            Assert.That(WeaponDefinition.IsValidCatalog(new[] { weapon, evolved }, new[] { passiveFixture }, out _), Is.False);
            Set(passiveFixture, "compatibleWeaponId", p => p.stringValue = "evolved");
            Assert.That(WeaponDefinition.IsValidCatalog(new[] { weapon, evolved }, new[] { passiveFixture }, out _), Is.False);
            Set(passiveFixture, "compatibleWeaponId", p => p.stringValue = "weapon");
            Assert.That(WeaponDefinition.IsValidCatalog(new[] { weapon, evolved }, new[] { passiveFixture }, out var reason), Is.True, reason);
            Assert.That(WeaponDefinition.IsValidCatalog(new[] { weapon, evolved }, new PassiveDefinition[] { null }, out _), Is.False);
        }

        [Test]
        public void OptionalAffinityRecomputesFromBaseWithoutAccumulationOrAssetMutation()
        {
            string before = EditorJsonUtility.ToJson(weapon);
            Assert.That(weapon.GetLevel(2, TerrainType.Water).Damage, Is.EqualTo(2));
            Set(weapon, "hasTerrainAffinity", p => p.boolValue = true);
            Set(weapon, "waterDamageMultiplier", p => p.floatValue = 2);
            Set(weapon, "waterCooldownMultiplier", p => p.floatValue = 0.5f);
            Assert.That(weapon.IsValid(out var reason), Is.True, reason);
            string authored = EditorJsonUtility.ToJson(weapon);
            for (int i = 0; i < 50; i++)
            {
                Assert.That(weapon.GetLevel(2, TerrainType.Water).Damage, Is.EqualTo(4));
                Assert.That(weapon.GetLevel(2, TerrainType.Water).Cooldown, Is.EqualTo(0.5f));
                Assert.That(weapon.GetLevel(2, TerrainType.Land).Damage, Is.EqualTo(2));
            }
            Assert.That(EditorJsonUtility.ToJson(weapon), Is.EqualTo(authored));
            Assert.That(authored, Is.Not.EqualTo(before));
            Assert.Throws<ArgumentOutOfRangeException>(() => weapon.GetLevel(1, (TerrainType)99));
        }

        [TestCase("landDamageMultiplier")]
        [TestCase("waterDamageMultiplier")]
        [TestCase("landCooldownMultiplier")]
        [TestCase("waterCooldownMultiplier")]
        public void EnabledAffinityRejectsZeroNegativeNonFiniteAndOverflow(string field)
        {
            Set(weapon, "hasTerrainAffinity", p => p.boolValue = true);
            foreach (float value in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                Set(weapon, field, p => p.floatValue = value);
                Assert.That(weapon.IsValid(out _), Is.False, field + ": " + value);
            }
            SetLevel(0, "cooldown", 2);
            Set(weapon, field, p => p.floatValue = float.MaxValue);
            Assert.That(weapon.IsValid(out _), Is.False, "Effective values must not overflow.");
        }

        [TestCase("projectileSpeed")]
        [TestCase("projectileLifetime")]
        [TestCase("hitRadius")]
        [TestCase("turnSpeed")]
        [TestCase("coneAngle")]
        public void MotionParametersMustBeFiniteAndPositive(string field)
        {
            foreach (float value in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                Set(weapon, field, p => p.floatValue = value);
                Assert.That(weapon.IsValid(out _), Is.False);
            }
        }

        [Test]
        public void BehaviorConeAngleAndTravelProductMustBeValid()
        {
            Set(weapon, "behavior", p => p.intValue = 99);
            Assert.That(weapon.IsValid(out _), Is.False);
            Set(weapon, "behavior", p => p.intValue = (int)WeaponBehavior.Boomerang);
            Set(weapon, "coneAngle", p => p.floatValue = 361);
            Assert.That(weapon.IsValid(out _), Is.False);
            Set(weapon, "coneAngle", p => p.floatValue = 60);
            Set(weapon, "projectileSpeed", p => p.floatValue = float.MaxValue);
            Assert.That(weapon.IsValid(out _), Is.False);
        }

        private WeaponDefinition CreateWeapon(string id)
        {
            var result = ScriptableObject.CreateInstance<WeaponDefinition>();
            Set(result, "id", p => p.stringValue = id);
            Set(result, "levels", p =>
            {
                p.arraySize = 5;
                for (int i = 0; i < 5; i++)
                {
                    var level = p.GetArrayElementAtIndex(i);
                    level.FindPropertyRelative("damage").floatValue = i + 1;
                    level.FindPropertyRelative("cooldown").floatValue = 1;
                    level.FindPropertyRelative("range").floatValue = 4;
                    level.FindPropertyRelative("projectileCount").intValue = 1;
                    level.FindPropertyRelative("areaRadius").floatValue = 0;
                }
            });
            return result;
        }

        private void SetLevel(int index, string field, float value) =>
            Set(weapon, "levels", p => p.GetArrayElementAtIndex(index).FindPropertyRelative(field).floatValue = value);

        private static void Set(Object target, string field, Action<SerializedProperty> change)
        {
            var data = new SerializedObject(target);
            change(data.FindProperty(field));
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
