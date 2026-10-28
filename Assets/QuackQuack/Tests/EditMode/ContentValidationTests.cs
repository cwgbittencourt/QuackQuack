using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using QuackQuack.Content.Catalogs;
using QuackQuack.Content.Characters;
using QuackQuack.Content.Encounters;
using QuackQuack.Content.Passives;
using QuackQuack.Content.Shared;
using QuackQuack.Content.Weapons;
using QuackQuack.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.EditMode
{
    public class ContentValidationTests
    {
        private const string QuackPath = "Assets/QuackQuack/Content/Characters/PatoQuack.asset";
        private string folder;
        private readonly List<Object> transient = new List<Object>();
        private PrototypeBuildCatalog catalog;
        [SetUp]
        public void SetUp()
        {
            folder = "Assets/QuackQuack/Tests/T073Fixtures-" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/QuackQuack/Tests", Path.GetFileName(folder));
            catalog = AssetDatabase.LoadAssetAtPath<PrototypeBuildCatalog>("Assets/QuackQuack/Content/Catalogs/PrototypeBuildCatalog.asset");
        }
        [TearDown]
        public void Cleanup()
        {
            foreach (var item in transient) if (item != null && !AssetDatabase.Contains(item)) Object.DestroyImmediate(item);
            transient.Clear();
            Assert.That(folder.StartsWith("Assets/QuackQuack/Tests/T073Fixtures-", StringComparison.Ordinal), Is.True);
            string resolved = Path.GetFullPath(folder);
            string expectedRoot = Path.GetFullPath("Assets/QuackQuack/Tests").TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Assert.That(resolved.StartsWith(expectedRoot, StringComparison.OrdinalIgnoreCase), Is.True);
            AssetDatabase.DeleteAsset(folder);
        }
        private T Clone<T>(T source, string id) where T : ContentDefinition
        {
            var item = Object.Instantiate(source); item.name = id; transient.Add(item); Set(item, "id", p => p.stringValue = id); return item;
        }
        private static void Set(Object asset, string field, Action<SerializedProperty> edit)
        { var data = new SerializedObject(asset); edit(data.FindProperty(field)); data.ApplyModifiedPropertiesWithoutUndo(); }
        private static bool Has(ContentValidationReport r, string code) => r.Issues.Any(i => i.Code == code);

        [Test]
        public void RealProjectPassesAndValidationDoesNotModifyAssets()
        {
            var source = AssetDatabase.LoadAssetAtPath<QuackDefinition>(QuackPath);
            byte[] before = File.ReadAllBytes(QuackPath); bool dirty = EditorUtility.IsDirty(source);
            var first = ContentValidationWindow.ValidateProject();
            var second = ContentValidationWindow.ValidateProject();
            Assert.That(first.AssetCount, Is.GreaterThan(50));
            Assert.That(first.Errors, Is.Zero, string.Join("\n", first.Issues.Select(i => i.ToString())));
            CollectionAssert.AreEqual(first.Issues.Select(i => i.ToString()), second.Issues.Select(i => i.ToString()));
            CollectionAssert.AreEqual(before, File.ReadAllBytes(QuackPath));
            Assert.That(EditorUtility.IsDirty(source), Is.EqualTo(dirty));
        }

        [Test]
        public void DuplicateIdsAcrossTypesAreReportedForBothAssetsButRepeatedObjectsAreDeduplicated()
        {
            var quack = Clone(AssetDatabase.LoadAssetAtPath<QuackDefinition>(QuackPath), "same");
            var passive = Clone(catalog.Passives[0], "same");
            var result = ContentValidationWindow.Validate(new ScriptableObject[] { quack, passive, quack });
            Assert.That(result.AssetCount, Is.EqualTo(2));
            Assert.That(result.Issues.Count(i => i.Code == "DuplicateId"), Is.EqualTo(2));
        }

        [Test]
        public void NumericLimitsNonFiniteFieldsAndMissingRequiredReferencesAreDetected()
        {
            var quack = Clone(AssetDatabase.LoadAssetAtPath<QuackDefinition>(QuackPath), "invalid-quack");
            Set(quack, "cost", p => p.floatValue = 101); Set(quack, "damage", p => p.floatValue = float.NaN);
            var character = Clone(AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/QuackQuack/Content/Characters/Pato.asset"), "pato");
            Set(character, "quack", p => p.objectReferenceValue = null);
            var result = ContentValidationWindow.Validate(new ScriptableObject[] { quack, character });
            Assert.That(result.Issues.Count(i => i.Code == "InvalidAsset"), Is.EqualTo(2));
            Assert.That(result.Issues.Any(i => i.Code == "NonFinite" && i.Property == "damage"), Is.True);
        }

        [Test]
        public void TypedIdsAndReciprocalLinksAreValidated()
        {
            var weapon = Clone(catalog.Weapons[0], "t073-weapon");
            var passive = Clone(catalog.Passives[0], "t073-passive");
            var evolved = Clone(catalog.Weapons[0], "t073-result");
            Set(evolved, "compatiblePassiveId", p => p.stringValue = ""); Set(evolved, "evolutionWeaponId", p => p.stringValue = "");
            Set(weapon, "compatiblePassiveId", p => p.stringValue = passive.Id); Set(weapon, "evolutionWeaponId", p => p.stringValue = evolved.Id);
            Set(passive, "compatibleWeaponId", p => p.stringValue = weapon.Id);
            ScriptableObject[] set = { weapon, passive, evolved };
            Assert.That(ContentValidationWindow.Validate(set).Errors, Is.Zero);
            Set(weapon, "compatiblePassiveId", p => p.stringValue = "absent");
            Assert.That(Has(ContentValidationWindow.Validate(set), "UnknownId"), Is.True);
            Set(weapon, "compatiblePassiveId", p => p.stringValue = weapon.Id);
            Assert.That(Has(ContentValidationWindow.Validate(set), "ReferenceType"), Is.True);
            Set(weapon, "compatiblePassiveId", p => p.stringValue = passive.Id);
            Set(passive, "compatibleWeaponId", p => p.stringValue = "another-weapon");
            Assert.That(Has(ContentValidationWindow.Validate(set), "ReciprocalReference"), Is.True);
        }

        [Test]
        public void BrokenOptionalObjectReferenceIsDetectedAfterDeletingItsNativeAsset()
        {
            var texture = new Texture2D(2, 2); AssetDatabase.CreateAsset(texture, folder + "/texture.asset");
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero);
            AssetDatabase.CreateAsset(sprite, folder + "/sprite.asset");
            var weapon = Clone(catalog.Weapons[0], "broken-optional");
            Set(weapon, "projectileSprite", p => p.objectReferenceValue = sprite);
            Set(weapon, "compatiblePassiveId", p => p.stringValue = ""); Set(weapon, "evolutionWeaponId", p => p.stringValue = "");
            AssetDatabase.CreateAsset(weapon, folder + "/weapon.asset"); AssetDatabase.SaveAssets();
            AssetDatabase.DeleteAsset(folder + "/sprite.asset");
            var result = ContentValidationWindow.Validate(new ScriptableObject[] { weapon });
            Assert.That(result.Issues.Any(i => i.Code == "MissingReference" && i.Property == "projectileSprite"), Is.True);
        }

        [Test]
        public void CatalogLimitsAndNestedChallengeIdsUseDomainRules()
        {
            var build = Object.Instantiate(catalog); transient.Add(build);
            Set(build, "weapons", p => p.arraySize = 5);
            var challenges = ScriptableObject.CreateInstance<ResultsProgressionDefinition>(); transient.Add(challenges);
            challenges.Challenges = new[] {
                new ResultsChallengeDefinition { Id = "repeat", Title = "A", Target = 1 },
                new ResultsChallengeDefinition { Id = "repeat", Title = "B", Target = 1 }
            };
            var report = ContentValidationWindow.Validate(new ScriptableObject[] { build, challenges });
            Assert.That(report.Issues.Count(i => i.Code == "InvalidAsset"), Is.EqualTo(2));
        }

        [Test]
        public void DuplicateEvolutionRecipesAreDetectedIndependentlyOfIds()
        {
            var recipe = Clone(catalog.Evolutions[0], "another-recipe");
            var assets = new List<ScriptableObject>(catalog.GetAllWeapons());
            assets.AddRange(catalog.Passives); assets.Add(catalog.Evolutions[0]); assets.Add(recipe);
            var report = ContentValidationWindow.Validate(assets);
            Assert.That(report.Issues.Count(i => i.Code == "DuplicateRecipe"), Is.EqualTo(2));
        }

        [Test]
        public void EmptyOrMissingScopesAndNullEntriesCannotSilentlyPass()
        {
            Assert.That(ContentValidationWindow.ValidateProject(folder).Errors, Is.GreaterThan(0));
            Assert.That(Has(ContentValidationWindow.ValidateProject(folder + "/missing"), "InvalidRoot"), Is.True);
            Assert.That(Has(ContentValidationWindow.Validate(new ScriptableObject[] { null }), "MissingAsset"), Is.True);
        }

        [Test]
        public void BatchValidationFailsForInvalidPersistedContent()
        {
            var broken = Clone(AssetDatabase.LoadAssetAtPath<QuackDefinition>(QuackPath), "t073-batch-invalid");
            Set(broken, "cost", p => p.floatValue = 101);
            AssetDatabase.CreateAsset(broken, folder + "/bad.asset"); AssetDatabase.SaveAssets();
            LogAssert.Expect(LogType.Error, new Regex("T073 .*InvalidAsset"));
            Assert.Throws<BuildFailedException>(() => ContentValidationWindow.ValidateForCI());
        }
    }
}
