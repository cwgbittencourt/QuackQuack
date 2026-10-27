using System;
using System.Collections.Generic;
using NUnit.Framework;
using QuackQuack.Content.Relics;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.EditMode
{
    public class RelicDefinitionTests
    {
        [Test]
        public void AuthoredCatalogHasTenValidRelicsAndIndependentLockedLoadouts()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PrototypeRelicCatalog>("Assets/QuackQuack/Content/Relics/PrototypeRelicCatalog.asset");
            Assert.That(catalog, Is.Not.Null); Assert.That(catalog.IsValid(out string reason), Is.True, reason);
            Assert.That(catalog.Relics.Count, Is.EqualTo(10));
            var first = catalog.CreateLoadout(); var second = catalog.CreateLoadout();
            Assert.That(first.Unlock(catalog.Relics[0].Id), Is.True);
            Assert.That(second.UnlockedIds, Is.Empty);
            Assert.That(first.EquippedIds, Is.Empty);
        }

        [TestCase(RelicAttribute.Damage)]
        [TestCase(RelicAttribute.MovementSpeed)]
        [TestCase(RelicAttribute.AttackSpeed)]
        [TestCase(RelicAttribute.Range)]
        [TestCase(RelicAttribute.Area)]
        [TestCase(RelicAttribute.BreathRecovery)]
        public void EveryAttributeComposesFromBaselineAndRemovesItsOwnEffect(RelicAttribute attribute)
        {
            var data = new RelicModifier[10];
            for (int i = 0; i < 10; i++) data[i] = new RelicModifier("r" + i, attribute, i == 0 ? RelicTerrain.Water : RelicTerrain.Any, 0.05f);
            var loadout = new RelicLoadout(data); loadout.Unlock("r0"); loadout.Unlock("r1"); loadout.Equip("r0"); loadout.Equip("r1");
            Assert.That(loadout.ApplyEffects(100, attribute, TerrainType.Water), Is.EqualTo(110).Within(0.001));
            Assert.That(loadout.ApplyEffects(100, attribute, TerrainType.Land), Is.EqualTo(105).Within(0.001));
            for (int i = 0; i < 5; i++) Assert.That(loadout.ApplyEffects(100, attribute, TerrainType.Water), Is.EqualTo(110).Within(0.001));
            loadout.Unequip("r0"); Assert.That(loadout.ApplyEffects(100, attribute, TerrainType.Water), Is.EqualTo(105).Within(0.001));
            loadout.Unequip("r1"); Assert.That(loadout.ApplyEffects(100, attribute, TerrainType.Water), Is.EqualTo(100));
            Assert.Throws<NotSupportedException>(() => ((ICollection<string>)loadout.UnlockedIds).Add("injected"));
        }

        [Test]
        public void UnlockThresholdsAreInclusiveWithoutMutatingDefinitions()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PrototypeRelicCatalog>("Assets/QuackQuack/Content/Relics/PrototypeRelicCatalog.asset");
            foreach (var d in catalog.Relics)
            {
                string before = EditorJsonUtility.ToJson(d);
                if (d.Unlock == RelicUnlock.Available) Assert.That(d.CanUnlock(0, 0), Is.True);
                else if (d.Unlock == RelicUnlock.SurviveSeconds) { Assert.That(d.CanUnlock(d.Threshold - 1, 0), Is.False); Assert.That(d.CanUnlock(d.Threshold, 0), Is.True); }
                else { Assert.That(d.CanUnlock(0, d.Threshold - 1), Is.False); Assert.That(d.CanUnlock(0, d.Threshold), Is.True); }
                Assert.That(EditorJsonUtility.ToJson(d), Is.EqualTo(before));
            }
        }

        [TestCase(0f)] [TestCase(-1f)] [TestCase(0.11f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void InvalidOrExcessiveBonusIsRejected(float bonus)
        {
            var definition = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PrototypeRelicCatalog>("Assets/QuackQuack/Content/Relics/PrototypeRelicCatalog.asset").Relics[0]);
            try { var s = new SerializedObject(definition); s.FindProperty("bonus").floatValue = bonus; s.ApplyModifiedPropertiesWithoutUndo(); Assert.That(definition.IsValid(out _), Is.False); }
            finally { Object.DestroyImmediate(definition); }
        }
    }
}
