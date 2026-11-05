using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuackQuack.Content.Encounters;
using QuackQuack.Content.Enemies;
using QuackQuack.Content.Pickups;
using QuackQuack.Core.Progression;
using QuackQuack.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.EditMode
{
    public class PickupPolicyDefinitionTests
    {
        private readonly List<Object> owned = new List<Object>();
        private PickupPolicyDefinition Policy() => Clone(AssetDatabase.LoadAssetAtPath<PickupPolicyDefinition>(PickupPolicySetup.PolicyPath));
        private T Clone<T>(T source) where T : Object
        { Assert.That(source, Is.Not.Null); var copy = Object.Instantiate(source); owned.Add(copy); return copy; }
        [TearDown] public void Cleanup() { foreach (var item in owned) Object.DestroyImmediate(item); owned.Clear(); }
        private static void Set(Object target, string field, float value)
        {
            var data = new SerializedObject(target); data.FindProperty(field).floatValue = value; data.ApplyModifiedPropertiesWithoutUndo();
        }

        public static IEnumerable InvalidFields
        {
            get
            {
                foreach (string field in new[] { "commonHealChance", "commonHealFraction", "miniBossHealFraction", "captureBodyRadiusMultiplier", "attractionWaterSpeedMultiplier" })
                    foreach (float value in new[] { -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                        yield return new TestCaseData(field, value);
                foreach (string field in new[] { "commonHealFraction", "miniBossHealFraction", "captureBodyRadiusMultiplier", "attractionWaterSpeedMultiplier" })
                    yield return new TestCaseData(field, 0f);
                foreach (string field in new[] { "commonHealChance", "commonHealFraction", "miniBossHealFraction" })
                    yield return new TestCaseData(field, 1.01f);
                yield return new TestCaseData("attractionWaterSpeedMultiplier", 1f);
            }
        }

        [TestCaseSource(nameof(InvalidFields))]
        public void InvalidPolicyIsRejectedByDomainFactoryAndContentValidation(string field, float value)
        {
            var policy = Policy(); Set(policy, field, value);
            Assert.That(policy.IsValid(out var reason), Is.False); Assert.That(reason, Does.Contain(field));
            Assert.Throws<InvalidOperationException>(() => policy.CreateLedger(1, () => 0, 1));
            var issues = ContentValidationWindow.Validate(new[] { policy }).Issues;
            Assert.That(issues.Any(i => i.Code == "InvalidAsset" && i.Asset == policy), Is.True);
            Assert.That(issues.Any(i => i.Code == "UnsupportedType"), Is.False);
        }

        [TestCase(0f, 1)] [TestCase(1f, 2)] [TestCase(float.Epsilon, 2)]
        public void ChanceEndpointsAndTinyPositiveValuesRemainValid(float chance, int count)
        {
            var policy = Policy(); Set(policy, "commonHealChance", chance);
            Assert.That(policy.IsValid(out _), Is.True);
            var ledger = policy.CreateLedger(1, () => 0, 1);
            ledger.ProcessDefeat(7, 1, RewardOrigin.Common, 1, 0, 0, true);
            Assert.That(ledger.Entries.Count, Is.EqualTo(count));
        }

        [TestCase(0.099999999999d, 2)] [TestCase(0.10d, 1)] [TestCase(0.100000000001d, 1)]
        public void AuthoredFloatTenPercentPreservesStrictDoubleBoundary(double roll, int count)
        {
            var policy = Policy(); Assert.That(policy.CommonHealChance, Is.EqualTo(0.1f));
            var ledger = policy.CreateLedger(1, () => roll, 1);
            ledger.ProcessDefeat(7, 1, RewardOrigin.Common, 1, 0, 0, true);
            Assert.That(ledger.Entries.Count, Is.EqualTo(count));
        }

        [Test]
        public void GeneratedPolicyAndLakeReferencesUseApprovedAuthoredValues()
        {
            var policy = AssetDatabase.LoadAssetAtPath<PickupPolicyDefinition>(PickupPolicySetup.PolicyPath);
            Assert.That(policy, Is.Not.Null); Assert.That(policy.IsValid(out _), Is.True);
            Assert.That(policy.CommonHealChance, Is.EqualTo(0.1f)); Assert.That(policy.CommonHealFraction, Is.EqualTo(0.15f));
            Assert.That(policy.MiniBossHealFraction, Is.EqualTo(0.5f)); Assert.That(policy.CaptureBodyRadiusMultiplier, Is.EqualTo(3));
            Assert.That(policy.AttractionWaterSpeedMultiplier, Is.EqualTo(1.25f));
            var settings = AssetDatabase.LoadAssetAtPath<LakeRunSettings>(LakeRunSetup.SettingsPath);
            Assert.That(settings.IsValid, Is.True); Assert.That(settings.PickupPolicy, Is.SameAs(policy));
            Assert.That(settings.BossExperience, Is.EqualTo(50));
            Assert.That(settings.Catalog.GetAllEnemies().All(e => e.ExperienceReward > 0), Is.True);
        }

        [TestCase(0)] [TestCase(-1)]
        public void BossExperienceMustBePositive(int xp)
        {
            var settings = Clone(AssetDatabase.LoadAssetAtPath<LakeRunSettings>(LakeRunSetup.SettingsPath));
            settings.BossExperience = xp;
            Assert.That(settings.IsValid, Is.False);
            Assert.That(ContentValidationWindow.Validate(new[] { settings }).Issues.Any(i => i.Code == "NonPositiveBossExperience"), Is.True);
        }

        [TestCase(0)] [TestCase(-1)]
        public void EveryEnemyRequiresPositiveExperience(int xp)
        {
            var enemy = Clone(AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/QuackQuack/Content/Enemies/Prototype/formiga.asset"));
            var data = new SerializedObject(enemy); data.FindProperty("experienceReward").intValue = xp; data.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(enemy.IsValid(out _), Is.False);
            Assert.That(ContentValidationWindow.Validate(new[] { enemy }).Issues.Any(i => i.Code == "NonPositiveExperience"), Is.True);
        }

        [Test]
        public void LakeRejectsMissingAndInvalidReferencedPolicy()
        {
            var settings = Clone(AssetDatabase.LoadAssetAtPath<LakeRunSettings>(LakeRunSetup.SettingsPath));
            settings.PickupPolicy = null; Assert.That(settings.IsValid, Is.False);
            Assert.That(ContentValidationWindow.Validate(new[] { settings }).Issues.Any(i => i.Code == "MissingPickupPolicy"), Is.True);
            settings.PickupPolicy = Policy(); Set(settings.PickupPolicy, "commonHealFraction", 0);
            Assert.That(settings.IsValid, Is.False);
            Assert.That(ContentValidationWindow.Validate(new[] { settings }).Issues.Any(i => i.Code == "InvalidPickupPolicy"), Is.True);
        }

        [Test]
        public void EditableFractionsReachCoreWithoutMutatingPolicy()
        {
            var policy = Policy(); Set(policy, "commonHealChance", 1); Set(policy, "commonHealFraction", 0.2f); Set(policy, "miniBossHealFraction", 0.75f);
            Set(policy, "captureBodyRadiusMultiplier", 0.5f); Set(policy, "attractionWaterSpeedMultiplier", 1.01f);
            string before = EditorJsonUtility.ToJson(policy);
            var ledger = policy.CreateLedger(1, () => 0, 2);
            ledger.ProcessDefeat(7, 1, RewardOrigin.Common, 3, 0, 0, true);
            ledger.ProcessDefeat(8, 1, RewardOrigin.MiniBoss, 5, 0, 0, true);
            Assert.That(ledger.Entries.Single(e => e.Kind == RewardKind.HealCommon).Fraction, Is.EqualTo(0.2f));
            Assert.That(ledger.Entries.Single(e => e.Kind == RewardKind.HealMiniBoss).Fraction, Is.EqualTo(0.75f));
            Assert.That(EditorJsonUtility.ToJson(policy), Is.EqualTo(before));
        }
    }
}
