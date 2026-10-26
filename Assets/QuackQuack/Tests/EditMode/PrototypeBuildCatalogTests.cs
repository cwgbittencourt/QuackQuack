using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuackQuack.Content.Catalogs;
using QuackQuack.Content.Shared;
using QuackQuack.Content.Weapons;
using QuackQuack.Core.Progression;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.EditMode
{
    public class PrototypeBuildCatalogTests
    {
        private PrototypeBuildCatalog catalog;
        private readonly List<Object> temporary = new List<Object>();
        private const string Path = "Assets/QuackQuack/Content/Catalogs/PrototypeBuildCatalog.asset";

        [SetUp]
        public void SetUp()
        {
            catalog = AssetDatabase.LoadAssetAtPath<PrototypeBuildCatalog>(Path);
            Assert.That(catalog, Is.Not.Null, "Generate and version the T043 catalog with Unity.");
        }
        [TearDown]
        public void TearDown()
        {
            foreach (var asset in temporary) Object.DestroyImmediate(asset);
            temporary.Clear();
        }

        [Test]
        public void AuthoredCatalogContainsSixBehaviorsSixPassivesAndOnlyThreeInitialRecipes()
        {
            Assert.That(catalog.IsValid(out var reason), Is.True, reason);
            Assert.That(catalog.Weapons.Count, Is.EqualTo(6));
            Assert.That(catalog.Passives.Count, Is.EqualTo(6));
            Assert.That(catalog.Evolutions.Count, Is.EqualTo(3));
            Assert.That(catalog.GetAllWeapons().Count, Is.EqualTo(9));
            CollectionAssert.AreEquivalent(Enum.GetValues(typeof(WeaponBehavior)), catalog.Weapons.Select(w => w.Behavior));
            CollectionAssert.AreEquivalent(new[] { "bumerangue", "ovos", "quack-frontal" }, catalog.Evolutions.Select(e => e.BaseWeapon.Id));
            CollectionAssert.AreEquivalent(new[] { "tornado-penas", "incubadora-combate", "quack-supersonico" }, catalog.Evolutions.Select(e => e.ResultWeapon.Id));
        }

        [Test]
        public void EveryDefinitionIsPersistedNamedAndUsesStableUniqueIds()
        {
            var definitions = catalog.GetAllWeapons().Cast<ContentDefinition>().Concat(catalog.Passives).Concat(catalog.Evolutions).Append(catalog).ToArray();
            Assert.That(definitions.Length, Is.EqualTo(19));
            Assert.That(definitions.Select(d => d.Id).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(19));
            foreach (var definition in definitions)
            {
                Assert.That(EditorUtility.IsPersistent(definition), Is.True);
                Assert.That(definition.DisplayName, Is.Not.Null.And.Not.Empty);
                Assert.That(definition.IsValid(out var reason), Is.True, reason);
            }
            foreach (var weapon in catalog.GetAllWeapons())
            {
                Assert.That(weapon.LevelCount, Is.EqualTo(5));
                Assert.That(weapon.ProjectileSprite, Is.Not.Null);
                for (int i = 2; i <= 5; i++) Assert.That(weapon.GetLevel(i).Damage, Is.GreaterThan(weapon.GetLevel(i - 1).Damage));
            }
        }

        [TestCase("weapons", 5)]
        [TestCase("weapons", 7)]
        [TestCase("passives", 5)]
        [TestCase("passives", 7)]
        [TestCase("evolutions", 2)]
        [TestCase("evolutions", 4)]
        public void PrototypeCardinalitiesCannotExpandOrShrink(string field, int count)
        {
            var copy = Clone();
            Set(copy, field, p => p.arraySize = count);
            Assert.That(copy.IsValid(out var reason), Is.False);
            Assert.That(reason, Is.Not.Empty);
        }

        [Test]
        public void MissingDuplicateAndNonCanonicalReferencesAreRejected()
        {
            var copy = Clone();
            Set(copy, "weapons", p => p.GetArrayElementAtIndex(1).objectReferenceValue = catalog.Weapons[0]);
            Assert.That(copy.IsValid(out _), Is.False);
            Set(copy, "weapons", p => p.GetArrayElementAtIndex(1).objectReferenceValue = catalog.Weapons[1]);
            Set(copy, "passives", p => p.GetArrayElementAtIndex(0).objectReferenceValue = null);
            Assert.That(copy.IsValid(out _), Is.False);
            Set(copy, "passives", p => p.GetArrayElementAtIndex(0).objectReferenceValue = catalog.Passives[0]);
            var unregistered = Object.Instantiate(catalog.Weapons[0]); temporary.Add(unregistered);
            Set(copy, "weapons", p => p.GetArrayElementAtIndex(0).objectReferenceValue = unregistered);
            Assert.That(copy.IsValid(out _), Is.False, "Recipes must reference the registered basic weapon asset.");
        }

        [Test]
        public void OffersExcludeResultsAndLockedRecipesButEveryAuthoredRecipeCanEvolveOnce()
        {
            var lockedIds = catalog.Evolutions.Select(e => e.Id).Concat(catalog.Evolutions.Select(e => e.ResultWeapon.Id)).ToArray();
            using (var system = catalog.CreateUpgradeSystem(42, new BuildState()))
            {
                Assert.That(system.GenerateOffer("locked", lockedIds), Is.Null);
                var offer = system.GenerateOffer("level");
                Assert.That(offer.Choices.Count, Is.EqualTo(3));
                Assert.That(offer.Choices.Intersect(lockedIds), Is.Empty);
            }
            string before = EditorJsonUtility.ToJson(catalog);
            foreach (var evolution in catalog.Evolutions)
            {
                var build = new BuildState();
                build.TryAddWeapon(evolution.BaseWeapon.Id);
                for (int i = 1; i < 5; i++) build.TryUpgradeWeapon(evolution.BaseWeapon.Id);
                build.TryAddPassive(evolution.RequiredPassive.Id);
                using (var system = catalog.CreateUpgradeSystem(42, build))
                {
                    Assert.That(system.GenerateOffer("reward", new[] { evolution.Id }), Is.Not.Null);
                    Assert.That(system.TryConfirm("reward", evolution.Id), Is.True);
                    Assert.That(system.TryConfirm("reward", evolution.Id), Is.False);
                    Assert.That(build.Evolutions[evolution.BaseWeapon.Id], Is.EqualTo(evolution.ResultWeapon.Id));
                }
            }
            Assert.That(EditorJsonUtility.ToJson(catalog), Is.EqualTo(before));
            Assert.Throws<NotSupportedException>(() => ((IList<WeaponDefinition>)catalog.Weapons).Clear());
        }

        [Test]
        public void AuthoredTerrainAffinityFavorsEggsOnLandAndBubblesOnWater()
        {
            var eggs = catalog.Weapons.Single(w => w.Id == "ovos");
            var bubbles = catalog.Weapons.Single(w => w.Id == "bolhas");
            Assert.That(eggs.GetLevel(1, QuackQuack.Core.Run.TerrainType.Land).Damage,
                Is.GreaterThan(eggs.GetLevel(1, QuackQuack.Core.Run.TerrainType.Water).Damage));
            Assert.That(bubbles.GetLevel(1, QuackQuack.Core.Run.TerrainType.Water).Damage,
                Is.GreaterThan(bubbles.GetLevel(1, QuackQuack.Core.Run.TerrainType.Land).Damage));
        }

        private PrototypeBuildCatalog Clone()
        {
            var clone = Object.Instantiate(catalog); temporary.Add(clone); return clone;
        }
        private static void Set(Object value, string field, Action<SerializedProperty> change)
        {
            var data = new SerializedObject(value); change(data.FindProperty(field)); data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
