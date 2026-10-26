using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Core.Run;

namespace QuackQuack.Tests.EditMode
{
    // T033 precedes T039. Reflection keeps the suite compilable while reporting the
    // missing production contract as a test failure, without a fake implementation.
    public class BuildStateTests
    {
        private BuildContract build;

        [SetUp]
        public void CreateBuild() => build = new BuildContract();

        [Test]
        public void NewRunStartsEmptyWithTwoRerollsAndOneBanish()
        {
            Assert.That(build.Weapons, Is.Empty);
            Assert.That(build.Passives, Is.Empty);
            Assert.That(build.Resource("RerollsRemaining"), Is.EqualTo(2));
            Assert.That(build.Resource("BanishRemaining"), Is.EqualTo(1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SixSlotsAcceptSixDistinctItemsAndRejectSeventhWithoutMutation(bool weapons)
        {
            for (int i = 0; i < 6; i++)
                Assert.That(build.Add(weapons, "item-" + i), Is.True);
            var before = Snapshot(weapons);
            Assert.That(build.Add(weapons, "seventh"), Is.False);
            CollectionAssert.AreEquivalent(before, Snapshot(weapons));
            Assert.That((weapons ? build.Weapons : build.Passives).Count, Is.EqualTo(6));
        }

        [Test]
        public void WeaponAndPassiveCapacitiesAreIndependent()
        {
            for (int i = 0; i < 6; i++)
            {
                Assert.That(build.Add(true, "weapon-" + i), Is.True);
                Assert.That(build.Add(false, "passive-" + i), Is.True);
            }
            Assert.That(build.Weapons.Count, Is.EqualTo(6));
            Assert.That(build.Passives.Count, Is.EqualTo(6));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DuplicateAcquisitionDoesNotConsumeSlotOrUpgradeItem(bool weapons)
        {
            Assert.That(build.Add(weapons, "owned"), Is.True);
            var before = Snapshot(weapons);
            Assert.That(build.Add(weapons, "owned"), Is.False);
            CollectionAssert.AreEquivalent(before, Snapshot(weapons));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void InvalidIdsCannotOccupyEitherFamily(string id)
        {
            Assert.That(build.Add(true, id), Is.False);
            Assert.That(build.Add(false, id), Is.False);
            Assert.That(build.Weapons, Is.Empty);
            Assert.That(build.Passives, Is.Empty);
        }

        [Test]
        public void OwnedWeaponUpgradesToFiveAtCapacityButNeverBeyond()
        {
            for (int i = 0; i < 6; i++) Assert.That(build.Add(true, "weapon-" + i), Is.True);
            for (int level = 2; level <= 5; level++)
            {
                Assert.That(build.Call("TryUpgradeWeapon", "weapon-0"), Is.True);
                Assert.That(build.Weapons["weapon-0"], Is.EqualTo(level));
                Assert.That(build.Weapons.Count, Is.EqualTo(6));
            }
            var before = Snapshot(true);
            Assert.That(build.Call("TryUpgradeWeapon", "weapon-0"), Is.False);
            Assert.That(build.Call("TryUpgradeWeapon", "missing"), Is.False);
            CollectionAssert.AreEquivalent(before, Snapshot(true));
        }

        [TestCase("TryUseReroll", "RerollsRemaining", 2)]
        [TestCase("TryUseBanish", "BanishRemaining", 1)]
        public void ResourcesAllowExactlyTheirBudgetAndNeverBecomeNegative(string operation, string resource, int budget)
        {
            for (int used = 1; used <= budget; used++)
            {
                Assert.That(build.Call(operation), Is.True);
                Assert.That(build.Resource(resource), Is.EqualTo(budget - used));
            }
            for (int attempt = 0; attempt < 3; attempt++)
            {
                Assert.That(build.Call(operation), Is.False);
                Assert.That(build.Resource(resource), Is.Zero);
            }
            Assert.That(build.Resource(resource == "RerollsRemaining" ? "BanishRemaining" : "RerollsRemaining"),
                Is.EqualTo(resource == "RerollsRemaining" ? 1 : 2));
        }

        [Test]
        public void ResourcesAndInventoryAreIsolatedBetweenRuns()
        {
            build.Add(true, "weapon");
            build.Add(false, "passive");
            build.Call("TryUseReroll");
            build.Call("TryUseBanish");
            var other = new BuildContract();
            Assert.That(other.Weapons, Is.Empty);
            Assert.That(other.Passives, Is.Empty);
            Assert.That(other.Resource("RerollsRemaining"), Is.EqualTo(2));
            Assert.That(other.Resource("BanishRemaining"), Is.EqualTo(1));
        }

        [Test]
        public void EvolutionReplacesMaxLevelBaseAtFullCapacityAndKeepsPassive()
        {
            PrepareEvolution(5, true);
            var passives = Snapshot(false);
            Assert.That(build.Call("TryEvolveWeapon", "base", "required", "evolved"), Is.True);
            Assert.That(build.Weapons.Count, Is.EqualTo(6));
            Assert.That(build.Weapons.ContainsKey("base"), Is.False);
            Assert.That(build.Weapons.ContainsKey("evolved"), Is.True);
            for (int i = 1; i < 6; i++) Assert.That(build.Weapons["weapon-" + i], Is.EqualTo(1));
            CollectionAssert.AreEquivalent(passives, Snapshot(false));
            var after = Snapshot(true);
            Assert.That(build.Call("TryEvolveWeapon", "base", "required", "evolved"), Is.False);
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
            Assert.That(build.Add(true, "base"), Is.True);
            for (int i = 1; i < level; i++) Assert.That(build.Call("TryUpgradeWeapon", "base"), Is.True);
            for (int i = 1; i < 6; i++) Assert.That(build.Add(true, "weapon-" + i), Is.True);
            if (passive) Assert.That(build.Add(false, "required"), Is.True);
        }

        private void AssertEvolutionRejected(string source, string passive, string result)
        {
            var weapons = Snapshot(true);
            var passives = Snapshot(false);
            Assert.That(build.Call("TryEvolveWeapon", source, passive, result), Is.False);
            CollectionAssert.AreEquivalent(weapons, Snapshot(true));
            CollectionAssert.AreEquivalent(passives, Snapshot(false));
            Assert.That(build.Resource("RerollsRemaining"), Is.EqualTo(2));
            Assert.That(build.Resource("BanishRemaining"), Is.EqualTo(1));
        }

        private Dictionary<string, int> Snapshot(bool weapons)
        {
            var copy = new Dictionary<string, int>();
            foreach (var item in weapons ? build.Weapons : build.Passives) copy.Add(item.Key, item.Value);
            return copy;
        }

        private sealed class BuildContract
        {
            private readonly Type type;
            private readonly object instance;

            public BuildContract()
            {
                type = typeof(PlayerState).Assembly.GetType("QuackQuack.Core.Progression.BuildState");
                Assert.That(type, Is.Not.Null, "T039 pending: production BuildState is missing (T033 red phase).");
                instance = Activator.CreateInstance(type);
            }

            public IReadOnlyDictionary<string, int> Weapons => Read<IReadOnlyDictionary<string, int>>("Weapons");
            public IReadOnlyDictionary<string, int> Passives => Read<IReadOnlyDictionary<string, int>>("Passives");
            public int Resource(string name) => Read<int>(name);
            public bool Add(bool weapon, string id) => Call(weapon ? "TryAddWeapon" : "TryAddPassive", id);

            private T Read<T>(string name)
            {
                var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                Assert.That(property, Is.Not.Null, "Missing BuildState property: " + name);
                return (T)property.GetValue(instance);
            }

            public bool Call(string name, params string[] args)
            {
                var parameterTypes = new Type[args.Length];
                var values = new object[args.Length];
                for (int i = 0; i < args.Length; i++) { parameterTypes[i] = typeof(string); values[i] = args[i]; }
                var method = type.GetMethod(name, parameterTypes);
                Assert.That(method, Is.Not.Null, "Missing BuildState operation: " + name);
                return (bool)method.Invoke(instance, values);
            }
        }
    }
}
