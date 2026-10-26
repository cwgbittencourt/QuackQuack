using System;
using System.Collections.Generic;
using NUnit.Framework;
using QuackQuack.Content.Weapons;
using QuackQuack.Content.Passives;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Systems.Upgrades;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.EditMode
{
    public class UpgradeCatalogTests
    {
        private readonly List<Object> assets = new List<Object>();
        private BuildState build;
        private UpgradeSystem system;

        [SetUp]
        public void SetUp()
        {
            var weapons = new List<WeaponDefinition>();
            for (int i = 0; i < 7; i++) weapons.Add(Weapon("weapon-" + i));
            var result = Weapon("result");
            weapons.Add(result);
            var passive = Make<PassiveDefinition>("passive");
            Set(passive, "compatibleWeaponId", p => p.stringValue = "weapon-0");
            Set(passive, "maxLevel", p => p.intValue = 2);
            Set(weapons[0], "compatiblePassiveId", p => p.stringValue = "passive");
            Set(weapons[0], "evolutionWeaponId", p => p.stringValue = "result");
            var evolution = Make<EvolutionDefinition>("evolution");
            Set(evolution, "baseWeapon", p => p.objectReferenceValue = weapons[0]);
            Set(evolution, "requiredPassive", p => p.objectReferenceValue = passive);
            Set(evolution, "resultWeapon", p => p.objectReferenceValue = result);
            build = new BuildState();
            system = new UpgradeSystem(42, build, weapons, new[] { passive }, new[] { evolution });
        }

        [TearDown]
        public void TearDown()
        {
            system.Dispose();
            foreach (var asset in assets) Object.DestroyImmediate(asset);
            assets.Clear();
        }

        [Test]
        public void CatalogRejectsUnknownAndPrematureEvolutionChoicesAndAppliesAcquisitionOnce()
        {
            Assert.That(system.GenerateOffer("none", new[] { "unknown", "result", "evolution" }), Is.Null);
            var offer = system.GenerateOffer("one", new[] { "weapon-0", "passive" });
            CollectionAssert.AreEquivalent(new[] { "weapon-0", "passive" }, offer.Choices);
            Assert.That(system.TryConfirm("one", "weapon-0"), Is.True);
            Assert.That(system.TryConfirm("one", "weapon-0"), Is.False);
            Assert.That(build.Weapons["weapon-0"], Is.EqualTo(1));
            Assert.That(build.History.Count, Is.EqualTo(1));
        }

        [Test]
        public void FullSlotsAndMaxLevelsFilterAcquisitionsButStillAllowOwnedUpgrades()
        {
            for (int i = 0; i < 6; i++) build.TryAddWeapon("weapon-" + i);
            for (int i = 1; i < 5; i++) build.TryUpgradeWeapon("weapon-0");
            build.TryAddPassive("passive");
            build.TryUpgradePassive("passive", 2);
            var offer = system.GenerateOffer("one", new[] { "weapon-0", "weapon-6", "passive", "weapon-1" });
            CollectionAssert.AreEqual(new[] { "weapon-1" }, offer.Choices);
            Assert.That(system.TryConfirm("one", "weapon-1"), Is.True);
            Assert.That(build.Weapons.Count, Is.EqualTo(6));
            Assert.That(build.Weapons["weapon-1"], Is.EqualTo(2));
        }

        [Test]
        public void ValidatedEvolutionReplacesBaseAtFullCapacityAndDoesNotReturnAsAnAcquisition()
        {
            for (int i = 0; i < 6; i++) build.TryAddWeapon("weapon-" + i);
            for (int i = 1; i < 5; i++) build.TryUpgradeWeapon("weapon-0");
            build.TryAddPassive("passive");
            Assert.That(system.GenerateOffer("evolve", new[] { "evolution" }), Is.Not.Null);
            Assert.That(system.TryConfirm("evolve", "evolution"), Is.True);
            Assert.That(build.Evolutions["weapon-0"], Is.EqualTo("result"));
            Assert.That(build.Weapons.Count, Is.EqualTo(6));
            Assert.That(system.GenerateOffer("later", new[] { "weapon-0", "result", "evolution" }), Is.Null);
        }

        [Test]
        public void ConfirmationRechecksEligibilityAfterExternalBuildChange()
        {
            build.TryAddWeapon("weapon-0");
            for (int i = 1; i < 4; i++) build.TryUpgradeWeapon("weapon-0");
            system.GenerateOffer("one", new[] { "weapon-0" });
            build.TryUpgradeWeapon("weapon-0");
            int history = build.History.Count;
            Assert.That(system.TryConfirm("one", "weapon-0"), Is.False);
            Assert.That(build.History.Count, Is.EqualTo(history));
            Assert.That(system.ConfirmedChoices, Is.Empty);
            Assert.That(system.TrySkip("one"), Is.True);
        }

        [Test]
        public void SharedResourcesReadOnlyOffersAndRunTransitionsRemainSingleApplication()
        {
            var run = new RunState(42);
            run.TryTransition(RunPhase.Playing);
            system.BindRun(run);
            int resolved = 0, phases = 0;
            system.OfferResolved += _ => resolved++;
            run.Changed += _ => phases++;
            build.TryUseReroll();
            var first = system.GenerateOffer("one", new[] { "weapon-0" });
            Assert.That(run.Phase, Is.EqualTo(RunPhase.ChoosingUpgrade));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)first.Choices).Clear());
            Assert.That(system.GenerateOffer("unrelated", new[] { "weapon-1" }), Is.SameAs(first));
            Assert.That(system.TryReroll("one"), Is.True);
            Assert.That(build.RerollsRemaining, Is.Zero);
            string id = system.CurrentOffer.OfferId;
            Assert.That(system.TrySkip(id), Is.True);
            Assert.That(system.TrySkip(id), Is.False);
            Assert.That(resolved, Is.EqualTo(1));
            Assert.That(phases, Is.EqualTo(2));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Playing));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)system.ConfirmedChoices).Add("injected"));
        }

        [Test]
        public void TerminalRunRejectsEveryLateCallbackWithoutSpendingOrApplying()
        {
            var run = new RunState(42);
            run.TryTransition(RunPhase.Playing);
            system.BindRun(run);
            system.GenerateOffer("one", new[] { "weapon-0" });
            run.TryTransition(RunPhase.Abandoned);
            Assert.That(system.TryConfirm("one", "weapon-0"), Is.False);
            Assert.That(system.TryReroll("one"), Is.False);
            Assert.That(system.TryBanish("one", "weapon-0"), Is.False);
            Assert.That(system.TrySkip("one"), Is.False);
            Assert.That(system.GenerateOffer("later"), Is.Null);
            Assert.That(build.History, Is.Empty);
            Assert.That(build.RerollsRemaining, Is.EqualTo(2));
            Assert.That(build.BanishRemaining, Is.EqualTo(1));
        }

        private T Make<T>(string id) where T : ScriptableObject
        {
            var value = ScriptableObject.CreateInstance<T>();
            assets.Add(value);
            Set(value, "id", p => p.stringValue = id);
            return value;
        }
        private WeaponDefinition Weapon(string id)
        {
            var weapon = Make<WeaponDefinition>(id);
            Set(weapon, "levels", p =>
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
            return weapon;
        }
        private static void Set(Object target, string field, Action<SerializedProperty> change)
        {
            var data = new SerializedObject(target);
            change(data.FindProperty(field));
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
