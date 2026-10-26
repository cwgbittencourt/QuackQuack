using System;
using NUnit.Framework;
using QuackQuack.Content.Passives;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.EditMode
{
    public class PassiveDefinitionTests
    {
        private PassiveDefinition passive;

        [SetUp]
        public void SetUp()
        {
            passive = ScriptableObject.CreateInstance<PassiveDefinition>();
            Set("id", p => p.stringValue = "passive");
            Set("compatibleWeaponId", p => p.stringValue = "weapon");
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(passive);

        [Test]
        public void AuthoredBonusIsBoundedAtEveryLevelWithoutMutatingDefinition()
        {
            Set("modifierPerLevel", p => p.floatValue = 0.2f);
            Assert.That(passive.IsValid(out var reason), Is.True, reason);
            string before = EditorJsonUtility.ToJson(passive);
            Assert.That(passive.GetModifier(0), Is.Zero);
            Assert.That(passive.GetModifier(1), Is.EqualTo(0.2f).Within(0.00001f));
            Assert.That(passive.GetModifier(2), Is.EqualTo(0.4f).Within(0.00001f));
            Assert.That(passive.GetModifier(3), Is.EqualTo(0.5f));
            Assert.That(passive.GetModifier(5), Is.EqualTo(0.5f));
            Assert.That(passive.Attribute, Is.EqualTo(PassiveAttribute.Damage));
            Assert.That(passive.CompatibleWeaponId, Is.EqualTo("weapon"));
            Assert.That(EditorJsonUtility.ToJson(passive), Is.EqualTo(before));
        }

        [TestCase("modifierPerLevel")]
        [TestCase("modifierLimit")]
        public void ModifierFieldsRejectNonFiniteNegativeZeroAndNonNormalizedValues(string field)
        {
            foreach (float value in new[] { 0f, -0.1f, 1.01f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                Set(field, p => p.floatValue = value);
                Assert.That(passive.IsValid(out var reason), Is.False, field + ": " + value);
                Assert.That(reason, Is.Not.Empty);
            }
        }

        [Test]
        public void PerLevelBonusCannotExceedAuthoredLimit()
        {
            Set("modifierPerLevel", p => p.floatValue = 0.6f);
            Assert.That(passive.IsValid(out _), Is.False);
            Set("modifierLimit", p => p.floatValue = 1);
            Assert.That(passive.IsValid(out var reason), Is.True, reason);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void MaximumLevelMustBePositive(int level)
        {
            Set("maxLevel", p => p.intValue = level);
            Assert.That(passive.IsValid(out _), Is.False);
        }

        [TestCase(-1)]
        [TestCase(6)]
        public void UnownedLevelIsZeroButLevelsOutsideAuthoredRangeAreRejected(int level) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => passive.GetModifier(level));

        [TestCase("id", "")]
        [TestCase("id", " ")]
        [TestCase("compatibleWeaponId", "")]
        [TestCase("compatibleWeaponId", " ")]
        public void StableIdsAreRequired(string field, string value)
        {
            Set(field, p => p.stringValue = value);
            Assert.That(passive.IsValid(out _), Is.False);
        }

        [Test]
        public void UnknownAttributeIsRejected()
        {
            Set("attribute", p => p.intValue = 999);
            Assert.That(passive.IsValid(out _), Is.False);
        }

        [Test]
        public void LargeAuthoredLevelCountStillProducesFiniteCappedBonus()
        {
            Set("maxLevel", p => p.intValue = int.MaxValue);
            Assert.That(passive.IsValid(out var reason), Is.True, reason);
            Assert.That(passive.GetModifier(int.MaxValue), Is.EqualTo(passive.ModifierLimit));
        }

        private void Set(string field, Action<SerializedProperty> change)
        {
            var serialized = new SerializedObject(passive);
            change(serialized.FindProperty(field));
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
