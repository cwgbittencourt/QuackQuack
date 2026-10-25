using NUnit.Framework;
using QuackQuack.Content.Characters;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Run;
using UnityEditor;
using UnityEngine;

namespace QuackQuack.Tests.EditMode
{
    public class QuackDefinitionTests
    {
        private QuackDefinition definition;

        [SetUp]
        public void SetUp()
        {
            definition = ScriptableObject.CreateInstance<QuackDefinition>();
            var data = new SerializedObject(definition);
            data.FindProperty("id").stringValue = "quack";
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(definition);

        [TestCase("capacity")]
        [TestCase("cost")]
        [TestCase("damage")]
        [TestCase("range")]
        [TestCase("push")]
        [TestCase("interruption")]
        [TestCase("fullMultiplier")]
        [TestCase("landRecovery")]
        [TestCase("waterRecovery")]
        [TestCase("cooldown")]
        [TestCase("reflectionSpeed")]
        public void NonFiniteBalanceIsRejected(string field)
        {
            foreach (float value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                SetNumber(field, value);
                Assert.That(definition.IsValid(out var reason), Is.False, field + ": " + value);
                Assert.That(reason, Is.Not.Empty);
            }
        }

        [TestCase("capacity", 0)]
        [TestCase("cost", 0)]
        [TestCase("cost", 101)]
        [TestCase("range", 0)]
        [TestCase("damage", -1)]
        [TestCase("push", -1)]
        [TestCase("interruption", -1)]
        [TestCase("fullMultiplier", 1)]
        [TestCase("landRecovery", -1)]
        [TestCase("waterRecovery", 12)]
        [TestCase("cooldown", 0)]
        [TestCase("reflectionSpeed", 0)]
        public void InvalidBoundariesAreRejected(string field, float value)
        {
            SetNumber(field, value);
            Assert.That(definition.IsValid(out var reason), Is.False);
            Assert.That(reason, Is.Not.Empty);
        }

        [Test]
        public void ZeroOptionalEffectsAndFullCapacityCostAreValid()
        {
            SetNumber("damage", 0);
            SetNumber("push", 0);
            SetNumber("interruption", 0);
            SetNumber("landRecovery", 0);
            SetNumber("cost", definition.Capacity);
            Assert.That(definition.IsValid(out var reason), Is.True, reason);
        }

        [Test]
        public void AuthoredValuesDriveBreathAndEnhancedHitWithoutMutatingDefinition()
        {
            Assert.That(definition.IsValid(out var reason), Is.True, reason);
            string before = EditorJsonUtility.ToJson(definition);
            var breath = new BreathSystem(definition.Capacity, definition.LandRecovery, definition.WaterRecovery);
            Assert.That(breath.TryConsume(definition.Cost, out var enhanced), Is.True);
            var rules = new QuackSystem();
            rules.BeginWave(1);
            Assert.That(rules.TryHit(1, true, false, enhanced, definition.Damage, definition.Push,
                definition.Interruption, definition.FullMultiplier, out var hit), Is.True);
            Assert.That(hit.Critical, Is.True);
            Assert.That(hit.Damage, Is.EqualTo(definition.Damage * definition.FullMultiplier));
            Assert.That(hit.Knockback, Is.EqualTo(definition.Push * definition.FullMultiplier));
            Assert.That(hit.InterruptSeconds, Is.EqualTo(definition.Interruption * definition.FullMultiplier));
            Assert.That(rules.TryHit(1, true, false, enhanced, definition.Damage, definition.Push,
                definition.Interruption, definition.FullMultiplier, out _), Is.False);
            breath.Recover(1, TerrainType.Water);
            Assert.That(breath.Current, Is.InRange(0f, definition.Capacity));
            Assert.That(EditorJsonUtility.ToJson(definition), Is.EqualTo(before));
        }

        [Test]
        public void MissingIdIsRejected()
        {
            var data = new SerializedObject(definition);
            data.FindProperty("id").stringValue = " ";
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(definition.IsValid(out var reason), Is.False);
            Assert.That(reason, Is.Not.Empty);
        }

        private void SetNumber(string field, float value)
        {
            var data = new SerializedObject(definition);
            data.FindProperty(field).floatValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        [TestCase(true, true, true)]
        [TestCase(false, true, false)]
        [TestCase(true, false, false)]
        [TestCase(false, false, false)]
        public void ReflectionRequiresBothProjectileFlags(bool light, bool reflectable, bool expected)
        {
            Assert.That(definition.CanReflect(light, reflectable), Is.EqualTo(expected));
            var velocity = QuackSystem.ReflectedVelocity(Vector2.right, Vector2.zero, definition.ReflectionSpeed);
            Assert.That(velocity, Is.EqualTo(Vector2.right * definition.ReflectionSpeed));
        }
    }
}
