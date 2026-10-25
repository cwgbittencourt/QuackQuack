using NUnit.Framework;
using QuackQuack.Content.Characters;
using UnityEditor;
using UnityEngine;

namespace QuackQuack.Tests.EditMode
{
    public class CharacterDefinitionTests
    {
        private CharacterDefinition character;
        private QuackDefinition quack;

        [SetUp]
        public void SetUp()
        {
            character = ScriptableObject.CreateInstance<CharacterDefinition>();
            quack = ScriptableObject.CreateInstance<QuackDefinition>();
            SetString(quack, "id", "quack");
            SetString(character, "id", "pato");
            var data = new SerializedObject(character);
            data.FindProperty("quack").objectReferenceValue = quack;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(character);
            Object.DestroyImmediate(quack);
        }

        [Test]
        public void ValidPatoExposesAuthoredValuesWithoutMutation()
        {
            Assert.That(character.IsValid(out var reason), Is.True, reason);
            Assert.That(character.Id, Is.EqualTo("pato"));
            Assert.That(character.Health, Is.EqualTo(100));
            Assert.That(character.WaterSpeed, Is.GreaterThan(character.LandSpeed));
            Assert.That(character.Quack, Is.SameAs(quack));
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase("outro")]
        [TestCase("Pato")]
        public void OnlyCanonicalPatoIdIsAccepted(string id)
        {
            SetString(character, "id", id);
            Assert.That(character.IsValid(out var reason), Is.False);
            Assert.That(reason, Is.Not.Empty);
        }

        [TestCase("health")]
        [TestCase("landSpeed")]
        [TestCase("waterSpeed")]
        [TestCase("invulnerability")]
        public void InvalidNumbersAreRejected(string field)
        {
            foreach (float value in new[] { -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                var data = new SerializedObject(character);
                data.FindProperty(field).floatValue = value;
                data.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(character.IsValid(out var reason), Is.False, field + ": " + value);
                Assert.That(reason, Is.Not.Empty);
            }
        }

        [Test]
        public void ZeroHealthAndSpeedsAreRejectedButZeroInvulnerabilityIsAllowed()
        {
            foreach (string field in new[] { "health", "landSpeed", "waterSpeed" })
            {
                var data = new SerializedObject(character);
                float original = data.FindProperty(field).floatValue;
                data.FindProperty(field).floatValue = 0;
                data.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(character.IsValid(out _), Is.False, field);
                data.FindProperty(field).floatValue = original;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            var serialized = new SerializedObject(character);
            serialized.FindProperty("invulnerability").floatValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(character.IsValid(out var reason), Is.True, reason);
        }

        [Test]
        public void MissingQuackAndWaterWithoutSpeedAdvantageAreRejected()
        {
            var data = new SerializedObject(character);
            data.FindProperty("waterSpeed").floatValue = character.LandSpeed;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(character.IsValid(out _), Is.False);
            data.FindProperty("waterSpeed").floatValue = 6;
            data.FindProperty("quack").objectReferenceValue = null;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(character.IsValid(out _), Is.False);
        }

        private static void SetString(Object target, string field, string value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).stringValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void SelectionRequiresExactlyOneValidPatoAndUniqueIds()
        {
            Assert.That(CharacterDefinition.IsValidSelection(new[] { character }, out var reason), Is.True, reason);
            Assert.That(CharacterDefinition.IsValidSelection(null, out _), Is.False);
            Assert.That(CharacterDefinition.IsValidSelection(new CharacterDefinition[0], out _), Is.False);
            Assert.That(CharacterDefinition.IsValidSelection(new CharacterDefinition[] { null }, out _), Is.False);
            Assert.That(CharacterDefinition.IsValidSelection(new[] { character, character }, out _), Is.False);
            var duplicate = Object.Instantiate(character);
            try
            {
                Assert.That(CharacterDefinition.IsValidSelection(new[] { character, duplicate }, out reason), Is.False);
                Assert.That(reason, Does.Contain("Duplicate"));
                SetString(duplicate, "id", "outro");
                Assert.That(CharacterDefinition.IsValidSelection(new[] { character, duplicate }, out _), Is.False);
            }
            finally { Object.DestroyImmediate(duplicate); }
        }
    }
}
