using System;
using NUnit.Framework;
using QuackQuack.Content.Enemies;
using QuackQuack.Systems.Pooling;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.EditMode
{
    public sealed class EnemyPoolDefinitionProbe : MonoBehaviour, IPoolLifecycle
    {
        public void OnAcquire() { }
        public void OnRelease() { }
        public void ResetState() { }
    }

    public class EnemyDefinitionTests
    {
        private EnemyDefinition enemy;
        private GameObject prefab, visual;
        private AudioClip audio;

        [SetUp]
        public void SetUp()
        {
            enemy = ScriptableObject.CreateInstance<EnemyDefinition>();
            prefab = new GameObject("Pooled enemy fixture");
            prefab.AddComponent<EnemyPoolDefinitionProbe>();
            visual = new GameObject("Danger visual fixture");
            audio = AudioClip.Create("Danger audio fixture", 100, 1, 1000, false);
            Set("id", p => p.stringValue = "enemy");
            Set("displayName", p => p.stringValue = "Enemy fixture");
            Set("prefab", p => p.objectReferenceValue = prefab);
            Set("telegraphVisual", p => p.objectReferenceValue = visual);
            Set("telegraphAudio", p => p.objectReferenceValue = audio);
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(enemy); Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(visual); Object.DestroyImmediate(audio);
        }

        [TestCase(EnemyRole.Swarm, false)]
        [TestCase(EnemyRole.WebCaster, true)]
        [TestCase(EnemyRole.Charger, true)]
        [TestCase(EnemyRole.FrontalArmored, false)]
        [TestCase(EnemyRole.SlowingTrail, true)]
        public void FiveTacticalRolesExposeAuthoredDataWithoutMutation(EnemyRole role, bool telegraphed)
        {
            Set("role", p => p.intValue = (int)role);
            string before = EditorJsonUtility.ToJson(enemy);
            Assert.That(enemy.IsValid(out string reason), Is.True, reason);
            Assert.That(enemy.Role, Is.EqualTo(role));
            Assert.That(enemy.HasTelegraphedAttack, Is.EqualTo(telegraphed));
            Assert.That(enemy.PoolFamily, Is.EqualTo(PoolFamily.Enemy));
            Assert.That(enemy.PoolKey, Is.EqualTo("enemy/enemy"));
            Assert.That(enemy.TelemetryId, Is.EqualTo(enemy.Id));
            Assert.That(enemy.Prefab, Is.SameAs(prefab));
            Assert.That(enemy.Health, Is.GreaterThan(0));
            Assert.That(enemy.MoveSpeed, Is.GreaterThan(0));
            Assert.That(EditorJsonUtility.ToJson(enemy), Is.EqualTo(before));
        }

        [TestCase("id")]
        [TestCase("displayName")]
        public void StableIdentityAndNameAreRequired(string field)
        {
            Set(field, p => p.stringValue = " ");
            Invalid();
        }

        [TestCase("health")]
        [TestCase("moveSpeed")]
        public void CorePositiveStatsRejectZeroNegativeAndNonfinite(string field)
        {
            foreach (float value in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            { Set(field, p => p.floatValue = value); Invalid(); }
        }

        [TestCase("knockbackResistance")]
        [TestCase("interruptionResistance")]
        public void ResistancesAreNormalizedIncludingEndpoints(string field)
        {
            foreach (float value in new[] { -0.1f, 1.1f, float.NaN, float.PositiveInfinity })
            { Set(field, p => p.floatValue = value); Invalid(); }
            foreach (float value in new[] { 0f, 1f })
            { Set(field, p => p.floatValue = value); Assert.That(enemy.IsValid(out _), Is.True); }
        }

        [Test]
        public void ContactDamageAllowsZeroButRejectsNegativeAndNonfinite()
        {
            foreach (float value in new[] { -1f, float.NaN, float.PositiveInfinity })
            { Set("contactDamage", p => p.floatValue = value); Invalid(); }
            Set("contactDamage", p => p.floatValue = 0);
            Assert.That(enemy.IsValid(out _), Is.True);
        }

        [TestCase("experienceReward")]
        [TestCase("coinReward")]
        public void RewardsRejectNegativesAndAllowZero(string field)
        {
            Set(field, p => p.intValue = -1); Invalid();
            Set(field, p => p.intValue = 0);
            Assert.That(enemy.IsValid(out _), Is.True);
        }

        [TestCase(0, 4)]
        [TestCase(-1, 4)]
        [TestCase(4, 3)]
        [TestCase(1, 0)]
        public void PoolBudgetRejectsInvalidBounds(int initial, int maximum)
        {
            Set("poolInitialSize", p => p.intValue = initial);
            Set("poolMaxSize", p => p.intValue = maximum); Invalid();
        }

        [Test]
        public void PrefabMustHaveLifecycleOnRoot()
        {
            Set("prefab", p => p.objectReferenceValue = null); Invalid();
            Set("prefab", p => p.objectReferenceValue = visual); Invalid();
            Object.DestroyImmediate(prefab.GetComponent<EnemyPoolDefinitionProbe>());
            var child = new GameObject("Child only"); child.transform.SetParent(prefab.transform);
            child.AddComponent<EnemyPoolDefinitionProbe>();
            Set("prefab", p => p.objectReferenceValue = prefab); Invalid();
        }

        [TestCase("telegraphVisual")]
        [TestCase("telegraphAudio")]
        public void SpecialAttackRequiresBothCueReferences(string field)
        {
            Set("role", p => p.intValue = (int)EnemyRole.Charger);
            Set(field, p => p.objectReferenceValue = null); Invalid();
        }

        [TestCase("attackInterval")]
        [TestCase("attackRange")]
        [TestCase("telegraphSeconds")]
        [TestCase("chargeSpeed")]
        [TestCase("chargeDuration")]
        public void ChargeAndTelegraphRejectInvalidPositiveValues(string field)
        {
            Set("role", p => p.intValue = (int)EnemyRole.Charger);
            foreach (float value in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            { Set(field, p => p.floatValue = value); Invalid(); }
        }

        [Test]
        public void ChargeMustBeFasterAndWarningCannotExceedInterval()
        {
            Set("role", p => p.intValue = (int)EnemyRole.Charger);
            Set("chargeSpeed", p => p.floatValue = enemy.MoveSpeed); Invalid();
            Set("chargeSpeed", p => p.floatValue = 6);
            Set("telegraphSeconds", p => p.floatValue = enemy.AttackInterval + 1); Invalid();
        }

        [TestCase(EnemyRole.WebCaster)]
        [TestCase(EnemyRole.SlowingTrail)]
        public void SlowEffectsRequireActualFiniteSlowAndDuration(EnemyRole role)
        {
            Set("role", p => p.intValue = (int)role);
            foreach (float value in new[] { 0f, -1f, 1f, 2f, float.NaN, float.PositiveInfinity })
            { Set("slowMultiplier", p => p.floatValue = value); Invalid(); }
            Set("slowMultiplier", p => p.floatValue = 0.5f);
            foreach (float value in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            { Set("effectDuration", p => p.floatValue = value); Invalid(); }
        }

        [Test]
        public void FrontalArmorRequiresReductionAndLeavesFlankingArc()
        {
            Set("role", p => p.intValue = (int)EnemyRole.FrontalArmored);
            foreach (float value in new[] { -1f, 1f, float.NaN, float.PositiveInfinity })
            { Set("frontalDamageMultiplier", p => p.floatValue = value); Invalid(); }
            Set("frontalDamageMultiplier", p => p.floatValue = 0);
            Assert.That(enemy.IsValid(out _), Is.True);
            foreach (float value in new[] { 0f, 181f, float.NaN, float.PositiveInfinity })
            { Set("frontalArcDegrees", p => p.floatValue = value); Invalid(); }
        }

        [Test]
        public void CatalogRejectsEmptyNullInvalidAndDuplicateIdentity()
        {
            Assert.That(EnemyDefinition.IsValidCatalog(null, out _), Is.False);
            Assert.That(EnemyDefinition.IsValidCatalog(Array.Empty<EnemyDefinition>(), out _), Is.False);
            Assert.That(EnemyDefinition.IsValidCatalog(new EnemyDefinition[] { null }, out _), Is.False);
            Assert.That(EnemyDefinition.IsValidCatalog(new[] { enemy }, out _), Is.True);
            var duplicate = Object.Instantiate(enemy);
            try { Assert.That(EnemyDefinition.IsValidCatalog(new[] { enemy, duplicate }, out _), Is.False); }
            finally { Object.DestroyImmediate(duplicate); }
            Set("role", p => p.intValue = 999); Invalid();
            Assert.That(EnemyDefinition.IsValidCatalog(new[] { enemy }, out _), Is.False);
        }

        private void Invalid()
        {
            Assert.That(enemy.IsValid(out string reason), Is.False);
            Assert.That(reason, Is.Not.Null.And.Not.Empty);
        }
        private void Set(string field, Action<SerializedProperty> change)
        {
            var serialized = new SerializedObject(enemy);
            change(serialized.FindProperty(field));
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
