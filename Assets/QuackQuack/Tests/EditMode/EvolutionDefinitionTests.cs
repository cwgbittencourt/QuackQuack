using System;
using System.Collections.Generic;
using NUnit.Framework;
using QuackQuack.Content.Weapons;
using QuackQuack.Content.Passives;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.EditMode
{
    public class EvolutionDefinitionTests
    {
        private readonly List<Object> created = new List<Object>();
        private EvolutionDefinition evolution;
        private WeaponDefinition weapon, result;
        private PassiveDefinition passive;

        [SetUp]
        public void SetUp()
        {
            weapon = MakeWeapon("base");
            result = MakeWeapon("result");
            passive = Make<PassiveDefinition>("passive");
            Set(passive, "compatibleWeaponId", p => p.stringValue = "base");
            Set(weapon, "compatiblePassiveId", p => p.stringValue = "passive");
            Set(weapon, "evolutionWeaponId", p => p.stringValue = "result");
            evolution = Make<EvolutionDefinition>("evolution");
            Set(evolution, "baseWeapon", p => p.objectReferenceValue = weapon);
            Set(evolution, "requiredPassive", p => p.objectReferenceValue = passive);
            Set(evolution, "resultWeapon", p => p.objectReferenceValue = result);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in created) Object.DestroyImmediate(asset);
            created.Clear();
        }

        [Test]
        public void ValidRecipeExposesCanonicalReferencesAndRequiresLevelFive()
        {
            Assert.That(evolution.IsValid(out var reason), Is.True, reason);
            Assert.That(evolution.BaseWeapon, Is.SameAs(weapon));
            Assert.That(evolution.RequiredPassive, Is.SameAs(passive));
            Assert.That(evolution.ResultWeapon, Is.SameAs(result));
            Assert.That(evolution.RequiredWeaponLevel, Is.EqualTo(5));
            string before = EditorJsonUtility.ToJson(evolution) + EditorJsonUtility.ToJson(weapon) + EditorJsonUtility.ToJson(passive);
            Assert.That(evolution.CanEvolve("base", 5, new[] { "unrelated", "passive" }), Is.True);
            Assert.That(EditorJsonUtility.ToJson(evolution) + EditorJsonUtility.ToJson(weapon) + EditorJsonUtility.ToJson(passive), Is.EqualTo(before));
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(4)]
        [TestCase(6)]
        public void OnlyExactlyLevelFiveIsEligible(int level) =>
            Assert.That(evolution.CanEvolve("base", level, new[] { "passive" }), Is.False);

        [Test]
        public void MissingOrDifferentWeaponAndPassiveNeverEnableEvolution()
        {
            Assert.That(evolution.CanEvolve("other", 5, new[] { "passive" }), Is.False);
            Assert.That(evolution.CanEvolve(null, 5, new[] { "passive" }), Is.False);
            Assert.That(evolution.CanEvolve("base", 5, null), Is.False);
            Assert.That(evolution.CanEvolve("base", 5, Array.Empty<string>()), Is.False);
            Assert.That(evolution.CanEvolve("base", 5, new[] { "Passive" }), Is.False);
        }

        [TestCase("baseWeapon")]
        [TestCase("requiredPassive")]
        [TestCase("resultWeapon")]
        public void MissingReferenceIsInvalidAndNeverEligible(string field)
        {
            Set(evolution, field, p => p.objectReferenceValue = null);
            Assert.That(evolution.IsValid(out var reason), Is.False);
            Assert.That(reason, Is.Not.Empty);
            Assert.That(evolution.CanEvolve("base", 5, new[] { "passive" }), Is.False);
        }

        [Test]
        public void InvalidReferencedContentAndSelfEvolutionAreRejected()
        {
            Set(evolution, "resultWeapon", p => p.objectReferenceValue = weapon);
            Assert.That(evolution.IsValid(out _), Is.False);
            Set(evolution, "resultWeapon", p => p.objectReferenceValue = result);
            Set(result, "levels", p => p.arraySize = 4);
            Assert.That(evolution.IsValid(out _), Is.False);
            Set(result, "levels", p => p.arraySize = 5);
            Set(passive, "modifierLimit", p => p.floatValue = float.NaN);
            Assert.That(evolution.IsValid(out _), Is.False);
        }

        [TestCase("weaponPassive")]
        [TestCase("weaponResult")]
        [TestCase("passiveWeapon")]
        public void RecipeMustAgreeWithWeaponAndPassiveLinks(string mismatch)
        {
            if (mismatch == "weaponPassive") Set(weapon, "compatiblePassiveId", p => p.stringValue = "other");
            if (mismatch == "weaponResult") Set(weapon, "evolutionWeaponId", p => p.stringValue = "other");
            if (mismatch == "passiveWeapon") Set(passive, "compatibleWeaponId", p => p.stringValue = "other");
            Assert.That(evolution.IsValid(out _), Is.False);
        }

        [Test]
        public void CatalogRejectsDuplicateRecipeIdsAndDuplicateCombinationsEvenWithDifferentIds()
        {
            AssertCatalog(true, new[] { evolution });
            var second = Object.Instantiate(evolution);
            created.Add(second);
            AssertCatalog(false, new[] { evolution, second });
            Set(second, "id", p => p.stringValue = "different-recipe");
            AssertCatalog(false, new[] { evolution, second });
        }

        [Test]
        public void CatalogRequiresReferencesToCanonicalRegisteredAssets()
        {
            Assert.That(EvolutionDefinition.IsValidCatalog(new[] { evolution }, new[] { weapon }, new[] { passive }, out _), Is.False);
            Assert.That(EvolutionDefinition.IsValidCatalog(new[] { evolution }, new[] { weapon, result }, Array.Empty<PassiveDefinition>(), out _), Is.False);
            var unregistered = Object.Instantiate(result);
            created.Add(unregistered);
            Set(evolution, "resultWeapon", p => p.objectReferenceValue = unregistered);
            AssertCatalog(false, new[] { evolution });
        }

        [Test]
        public void EmptyMissingAndInvalidCatalogEntriesAreRejected()
        {
            AssertCatalog(false, null);
            AssertCatalog(false, Array.Empty<EvolutionDefinition>());
            AssertCatalog(false, new EvolutionDefinition[] { null });
            Assert.That(EvolutionDefinition.IsValidCatalog(new[] { evolution }, null, new[] { passive }, out _), Is.False);
            Assert.That(EvolutionDefinition.IsValidCatalog(new[] { evolution }, new[] { weapon, result }, null, out _), Is.False);
            Set(evolution, "id", p => p.stringValue = " ");
            AssertCatalog(false, new[] { evolution });
        }

        private void AssertCatalog(bool valid, EvolutionDefinition[] recipes)
        {
            Assert.That(EvolutionDefinition.IsValidCatalog(recipes, new[] { weapon, result }, new[] { passive }, out var reason), Is.EqualTo(valid), reason);
            if (!valid) Assert.That(reason, Is.Not.Empty);
        }

        private T Make<T>(string id) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            created.Add(asset);
            Set(asset, "id", p => p.stringValue = id);
            return asset;
        }

        private WeaponDefinition MakeWeapon(string id)
        {
            var asset = Make<WeaponDefinition>(id);
            Set(asset, "levels", p =>
            {
                p.arraySize = 5;
                for (int i = 0; i < 5; i++)
                {
                    var level = p.GetArrayElementAtIndex(i);
                    level.FindPropertyRelative("damage").floatValue = 1;
                    level.FindPropertyRelative("cooldown").floatValue = 1;
                    level.FindPropertyRelative("range").floatValue = 1;
                    level.FindPropertyRelative("projectileCount").intValue = 1;
                }
            });
            return asset;
        }

        private static void Set(Object asset, string field, Action<SerializedProperty> change)
        {
            var data = new SerializedObject(asset);
            change(data.FindProperty(field));
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
