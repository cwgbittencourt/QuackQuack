using System;
using System.Collections.Generic;

using NUnit.Framework;
using QuackQuack.Core.Progression;

namespace QuackQuack.Tests.EditMode
{
    public class BuildStateTests
    {
        private BuildState build;

        [SetUp]
        public void CreateBuild() => build = new BuildState();

        [Test]
        public void NewRunStartsEmptyWithTwoRerollsAndOneBanish()
        {
            Assert.That(build.Weapons, Is.Empty);
            Assert.That(build.Passives, Is.Empty);
            Assert.That(build.RerollsRemaining, Is.EqualTo(2));
            Assert.That(build.BanishRemaining, Is.EqualTo(1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SixSlotsAcceptSixDistinctItemsAndRejectSeventhWithoutMutation(bool weapons)
        {
            for (int i = 0; i < 6; i++)
                Assert.That(Add(weapons, "item-" + i), Is.True);
            var before = Snapshot(weapons);
            Assert.That(Add(weapons, "seventh"), Is.False);
            CollectionAssert.AreEquivalent(before, Snapshot(weapons));
            Assert.That((weapons ? build.Weapons : build.Passives).Count, Is.EqualTo(6));
        }

        [Test]
        public void WeaponAndPassiveCapacitiesAreIndependent()
        {
            for (int i = 0; i < 6; i++)
            {
                Assert.That(Add(true, "weapon-" + i), Is.True);
                Assert.That(Add(false, "passive-" + i), Is.True);
            }
            Assert.That(build.Weapons.Count, Is.EqualTo(6));
            Assert.That(build.Passives.Count, Is.EqualTo(6));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DuplicateAcquisitionDoesNotConsumeSlotOrUpgradeItem(bool weapons)
        {
            Assert.That(Add(weapons, "owned"), Is.True);
            var before = Snapshot(weapons);
            Assert.That(Add(weapons, "owned"), Is.False);
            CollectionAssert.AreEquivalent(before, Snapshot(weapons));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void InvalidIdsCannotOccupyEitherFamily(string id)
        {
            Assert.That(Add(true, id), Is.False);
            Assert.That(Add(false, id), Is.False);
            Assert.That(build.Weapons, Is.Empty);
            Assert.That(build.Passives, Is.Empty);
        }

        [Test]
        public void OwnedWeaponUpgradesToFiveAtCapacityButNeverBeyond()
        {
            for (int i = 0; i < 6; i++) Assert.That(Add(true, "weapon-" + i), Is.True);
            for (int level = 2; level <= 5; level++)
            {
                Assert.That(build.TryUpgradeWeapon("weapon-0"), Is.True);
                Assert.That(build.Weapons["weapon-0"], Is.EqualTo(level));
                Assert.That(build.Weapons.Count, Is.EqualTo(6));
            }
            var before = Snapshot(true);
            Assert.That(build.TryUpgradeWeapon("weapon-0"), Is.False);
            Assert.That(build.TryUpgradeWeapon("missing"), Is.False);
            CollectionAssert.AreEquivalent(before, Snapshot(true));
        }

        [TestCase("TryUseReroll", "RerollsRemaining", 2)]
        [TestCase("TryUseBanish", "BanishRemaining", 1)]
        public void ResourcesAllowExactlyTheirBudgetAndNeverBecomeNegative(string operation, string resource, int budget)
        {
            for (int used = 1; used <= budget; used++)
            {
                Assert.That((operation == "TryUseReroll" ? build.TryUseReroll() : build.TryUseBanish()), Is.True);
                Assert.That((resource == "RerollsRemaining" ? build.RerollsRemaining : build.BanishRemaining), Is.EqualTo(budget - used));
            }
            for (int attempt = 0; attempt < 3; attempt++)
            {
                Assert.That((operation == "TryUseReroll" ? build.TryUseReroll() : build.TryUseBanish()), Is.False);
                Assert.That((resource == "RerollsRemaining" ? build.RerollsRemaining : build.BanishRemaining), Is.Zero);
            }
            Assert.That((resource == "RerollsRemaining" ? build.BanishRemaining : build.RerollsRemaining),
                Is.EqualTo(resource == "RerollsRemaining" ? 1 : 2));
        }

        [Test]
        public void ResourcesAndInventoryAreIsolatedBetweenRuns()
        {
            Add(true, "weapon");
            Add(false, "passive");
            build.TryUseReroll();
            build.TryUseBanish();
            var other = new BuildState();
            Assert.That(other.Weapons, Is.Empty);
            Assert.That(other.Passives, Is.Empty);
            Assert.That(other.RerollsRemaining, Is.EqualTo(2));
            Assert.That(other.BanishRemaining, Is.EqualTo(1));
        }

        [Test]
        public void EvolutionReplacesMaxLevelBaseAtFullCapacityAndKeepsPassive()
        {
            PrepareEvolution(5, true);
            var passives = Snapshot(false);
            Assert.That(build.TryEvolveWeapon("base", "required", "evolved"), Is.True);
            Assert.That(build.Weapons.Count, Is.EqualTo(6));
            Assert.That(build.Weapons.ContainsKey("base"), Is.False);
            Assert.That(build.Weapons.ContainsKey("evolved"), Is.True);
            for (int i = 1; i < 6; i++) Assert.That(build.Weapons["weapon-" + i], Is.EqualTo(1));
            CollectionAssert.AreEquivalent(passives, Snapshot(false));
            var after = Snapshot(true);
            Assert.That(build.TryEvolveWeapon("base", "required", "evolved"), Is.False);
            CollectionAssert.AreEquivalent(after, Snapshot(true));
        }

        [TestCase(1, true)]
        [TestCase(4, true)]
        [TestCase(5, false)]
        public void EvolutionRequiresLevelFiveAndRequiredPassiveWithoutPartialMutation(int level, bool passive)
        {
            PrepareEvolution(level, passive);
            AssertEvolutionRejected("base", "required", "evolved");
        }

        [TestCase("missing", "required", "evolved")]
        [TestCase("base", "other", "evolved")]
        [TestCase("base", "required", "weapon-1")]
        [TestCase("base", "required", "base")]
        [TestCase("base", "required", "")]
        [TestCase("base", "required", null)]
        public void InvalidEvolutionCannotRemoveBaseOrDuplicateAnOwnedResult(string source, string passive, string result)
        {
            PrepareEvolution(5, true);
            AssertEvolutionRejected(source, passive, result);
        }

        private void PrepareEvolution(int level, bool passive)
        {
            Assert.That(Add(true, "base"), Is.True);
            for (int i = 1; i < level; i++) Assert.That(build.TryUpgradeWeapon("base"), Is.True);
            for (int i = 1; i < 6; i++) Assert.That(Add(true, "weapon-" + i), Is.True);
            if (passive) Assert.That(Add(false, "required"), Is.True);
        }

        private void AssertEvolutionRejected(string source, string passive, string result)
        {
            var weapons = Snapshot(true);
            var passives = Snapshot(false);
            Assert.That(build.TryEvolveWeapon(source, passive, result), Is.False);
            CollectionAssert.AreEquivalent(weapons, Snapshot(true));
            CollectionAssert.AreEquivalent(passives, Snapshot(false));
            Assert.That(build.RerollsRemaining, Is.EqualTo(2));
            Assert.That(build.BanishRemaining, Is.EqualTo(1));
        }

        private Dictionary<string, int> Snapshot(bool weapons)
        {
            var copy = new Dictionary<string, int>();
            foreach (var item in weapons ? build.Weapons : build.Passives) copy.Add(item.Key, item.Value);
            return copy;
        }

        [Test]
        public void ReadOnlyViewsCannotBypassInventoryHistoryOrEvolutionInvariants()
        {
            Add(true, "weapon");
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, int>)build.Weapons).Add("injected", 99));
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, int>)build.Passives).Add("injected", 99));
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, string>)build.Evolutions).Add("base", "result"));
            Assert.Throws<NotSupportedException>(() => ((IList<BuildChange>)build.History).Clear());
            Assert.That(build.Weapons.Count, Is.EqualTo(1));
        }

        [Test]
        public void HistoryRecordsAcceptedChangesInOrderAndNeverRecordsRejections()
        {
            Assert.That(build.History, Is.Empty);
            Add(true, "weapon");
            Add(false, "passive");
            build.TryUpgradeWeapon("weapon");
            Assert.That(build.History.Count, Is.EqualTo(3));
            Assert.That(build.History[0].Kind, Is.EqualTo(BuildChangeKind.WeaponAcquired));
            Assert.That(build.History[1].Kind, Is.EqualTo(BuildChangeKind.PassiveAcquired));
            Assert.That(build.History[2].Kind, Is.EqualTo(BuildChangeKind.WeaponUpgraded));
            Assert.That(build.History[2].ItemId, Is.EqualTo("weapon"));
            Assert.That(build.History[2].Level, Is.EqualTo(2));
            Assert.That(Add(true, "weapon"), Is.False);
            Assert.That(build.TryEvolveWeapon("weapon", "passive", "result"), Is.False);
            Assert.That(build.History.Count, Is.EqualTo(3));
        }

        [Test]
        public void EvolvedFormKeepsMaxLevelAndCannotReacquireOrEvolveItsBaseAgain()
        {
            Add(true, "base");
            Add(false, "required");
            for (int i = 1; i < 5; i++) build.TryUpgradeWeapon("base");
            Assert.That(build.TryEvolveWeapon("base", "required", "evolved"), Is.True);
            Assert.That(build.Weapons["evolved"], Is.EqualTo(5));
            Assert.That(build.Evolutions["base"], Is.EqualTo("evolved"));
            Assert.That(build.History[build.History.Count - 1].PreviousId, Is.EqualTo("base"));
            Assert.That(build.History[build.History.Count - 1].Kind, Is.EqualTo(BuildChangeKind.WeaponEvolved));
            int count = build.History.Count;
            Assert.That(Add(true, "base"), Is.False);
            Assert.That(build.TryUpgradeWeapon("evolved"), Is.False);
            Assert.That(build.TryEvolveWeapon("evolved", "required", "another"), Is.False);
            Assert.That(build.History.Count, Is.EqualTo(count));
        }

        [Test]
        public void PassiveUpgradesUseAuthoredMaximumEvenWithSixOccupiedSlots()
        {
            for (int i = 0; i < 6; i++) Add(false, "passive-" + i);
            Assert.That(build.TryUpgradePassive("passive-0", 2), Is.True);
            Assert.That(build.Passives["passive-0"], Is.EqualTo(2));
            Assert.That(build.TryUpgradePassive("passive-0", 2), Is.False);
            Assert.That(build.TryUpgradePassive("passive-1", 0), Is.False);
            Assert.That(build.TryUpgradePassive(null, 2), Is.False);
            Assert.That(build.TryUpgradePassive("unknown", 2), Is.False);
            Assert.That(build.Passives.Count, Is.EqualTo(6));
        }

        private bool Add(bool weapon, string id) => weapon ? build.TryAddWeapon(id) : build.TryAddPassive(id);
    }
}
