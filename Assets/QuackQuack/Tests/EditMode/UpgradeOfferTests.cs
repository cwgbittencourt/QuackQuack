using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Systems.Run;

namespace QuackQuack.Tests.EditMode
{
    // T034 precedes T040: resolve the real production type without a stub or Ignore.
    // Replace this boundary adapter with typed calls when implementing T040.
    public class UpgradeOfferTests
    {
        private OfferContract system;

        [SetUp]
        public void CreateSession() => system = new OfferContract(12345);

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(8)]
        public void OffersContainUpToThreeDistinctEligibleChoices(int eligibleCount)
        {
            var eligible = Enumerable.Range(0, eligibleCount).Select(i => "eligible-" + i).ToArray();
            var offer = system.Generate("level-1", eligible);
            Assert.That(offer, Is.Not.Null);
            var choices = Choices(offer);
            Assert.That(choices.Count, Is.EqualTo(Math.Min(3, eligibleCount)));
            Assert.That(choices.Distinct().Count(), Is.EqualTo(choices.Count));
            Assert.That(choices.All(eligible.Contains), Is.True);
            Assert.That(choices, Does.Not.Contain("ineligible"));
            Assert.That(Read<string>(offer, "OfferId"), Is.EqualTo("level-1"));
            Assert.That(Read<bool>(offer, "IsClosed"), Is.False);
        }

        [Test]
        public void DuplicateAndInvalidCandidateIdsDoNotFillMissingChoices()
        {
            var offer = system.Generate("level-1", new[] { "a", "a", null, "", "  ", "b", "b" });
            CollectionAssert.AreEquivalent(new[] { "a", "b" }, Choices(offer));
        }

        [Test]
        public void NoEligibleCandidatesProducesNoOfferOrResourceConsumption()
        {
            Assert.That(system.Generate("empty", Array.Empty<string>()), Is.Null);
            Assert.That(system.Current, Is.Null);
            Assert.That(system.Call("TrySkip", "empty"), Is.False);
            Assert.That(system.Call("TryReroll", "empty"), Is.False);
            Assert.That(system.Confirmed, Is.Empty);
            AssertResources(2, 1);
            Assert.That(system.Generate("later", new[] { "a" }), Is.Not.Null);
        }

        [Test]
        public void SameSeedAndRequestsProduceSameOffersWithoutModifyingCandidates()
        {
            var other = new OfferContract(12345);
            var candidates = new[] { "a", "b", "c", "d", "e", "f" };
            var original = (string[])candidates.Clone();
            CollectionAssert.AreEqual(Choices(system.Generate("level-1", candidates)),
                Choices(other.Generate("level-1", candidates)));
            Assert.That(system.Call("TryReroll", "level-1"), Is.True);
            Assert.That(other.Call("TryReroll", "level-1"), Is.True);
            CollectionAssert.AreEqual(Choices(system.Current), Choices(other.Current));
            CollectionAssert.AreEqual(original, candidates);
        }

        [Test]
        public void RepeatingGenerationDoesNotChangeOrReplaceAnOpenOffer()
        {
            system.Generate("level-1", new[] { "a", "b", "c", "d" });
            string before = Snapshot();
            system.Generate("level-1", new[] { "unexpected" });
            Assert.That(Snapshot(), Is.EqualTo(before));
        }

        [Test]
        public void ConfirmationAppliesOnceAndClosesOfferAgainstEveryOtherOperation()
        {
            system.Generate("level-1", new[] { "a", "b" });
            Assert.That(system.Call("TryConfirm", "level-1", "a"), Is.True);
            CollectionAssert.AreEqual(new[] { "a" }, system.Confirmed);
            Assert.That(Read<bool>(system.Current, "IsClosed"), Is.True);
            string before = Snapshot();
            Assert.That(system.Call("TryConfirm", "level-1", "a"), Is.False);
            Assert.That(system.Call("TryConfirm", "level-1", "b"), Is.False);
            Assert.That(system.Call("TryReroll", "level-1"), Is.False);
            Assert.That(system.Call("TryBanish", "level-1", "b"), Is.False);
            Assert.That(system.Call("TrySkip", "level-1"), Is.False);
            system.Generate("level-1", new[] { "a", "b" });
            Assert.That(Snapshot(), Is.EqualTo(before));
            AssertResources(2, 1);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("not-offered")]
        public void InvalidConfirmationLeavesOfferAndResourcesAvailable(string choice)
        {
            system.Generate("level-1", new[] { "a" });
            string before = Snapshot();
            Assert.That(system.Call("TryConfirm", "level-1", choice), Is.False);
            Assert.That(Snapshot(), Is.EqualTo(before));
            Assert.That(system.Call("TryConfirm", "level-1", "a"), Is.True);
        }

        [Test]
        public void RerollConsumesOncePerOfferAndInvalidatesPreviousId()
        {
            var oldOffer = system.Generate("level-1", new[] { "a", "b", "c", "d" });
            var oldChoices = Choices(oldOffer).ToArray();
            Assert.That(system.Call("TryReroll", "level-1"), Is.True);
            string replacement = Read<string>(system.Current, "OfferId");
            Assert.That(replacement, Is.Not.Null.And.Not.Empty.And.Not.EqualTo("level-1"));
            Assert.That(Choices(system.Current).Count, Is.EqualTo(3));
            Assert.That(Choices(system.Current).Distinct().Count(), Is.EqualTo(3));
            Assert.That(Choices(system.Current).All(new[] { "a", "b", "c", "d" }.Contains), Is.True);
            AssertResources(1, 1);
            string before = Snapshot();
            Assert.That(system.Call("TryReroll", "level-1"), Is.False);
            Assert.That(system.Call("TryConfirm", "level-1", oldChoices[0]), Is.False);
            Assert.That(system.Call("TryBanish", "level-1", oldChoices[0]), Is.False);
            Assert.That(system.Call("TrySkip", "level-1"), Is.False);
            Assert.That(Snapshot(), Is.EqualTo(before));
            CollectionAssert.AreEqual(oldChoices, Choices(oldOffer));
            Assert.That(system.Call("TryReroll", replacement), Is.True);
            AssertResources(0, 1);
            before = Snapshot();
            Assert.That(system.Call("TryReroll", Read<string>(system.Current, "OfferId")), Is.False);
            Assert.That(Snapshot(), Is.EqualTo(before));
        }

        [Test]
        public void BanishConsumesOnceAndExcludedChoiceNeverReturnsInLaterOffers()
        {
            system.Generate("level-1", new[] { "a", "b", "c" });
            Assert.That(system.Call("TryBanish", "level-1", "a"), Is.True);
            string replacement = Read<string>(system.Current, "OfferId");
            Assert.That(replacement, Is.Not.Null.And.Not.Empty.And.Not.EqualTo("level-1"));
            CollectionAssert.AreEquivalent(new[] { "b", "c" }, Choices(system.Current));
            AssertResources(2, 0);
            string before = Snapshot();
            Assert.That(system.Call("TryBanish", "level-1", "a"), Is.False);
            Assert.That(system.Call("TryBanish", replacement, "b"), Is.False);
            Assert.That(Snapshot(), Is.EqualTo(before));
            Assert.That(system.Call("TryReroll", replacement), Is.True);
            Assert.That(Choices(system.Current), Does.Not.Contain("a"));
            Assert.That(system.Call("TrySkip", Read<string>(system.Current, "OfferId")), Is.True);
            CollectionAssert.AreEquivalent(new[] { "b", "c" },
                Choices(system.Generate("level-2", new[] { "a", "b", "c" })));
        }

        [Test]
        public void BanishLastCandidateEndsSelectionWithoutCreatingAnEmptyOffer()
        {
            system.Generate("level-1", new[] { "a" });
            Assert.That(system.Call("TryBanish", "level-1", "a"), Is.True);
            Assert.That(system.Current == null || Read<bool>(system.Current, "IsClosed"), Is.True);
            Assert.That(system.Generate("level-2", new[] { "a" }), Is.Null);
            Assert.That(system.Confirmed, Is.Empty);
            AssertResources(2, 0);
        }

        [Test]
        public void InvalidBanishDoesNotConsumeResourceOrCloseOffer()
        {
            system.Generate("level-1", new[] { "a" });
            string before = Snapshot();
            Assert.That(system.Call("TryBanish", "level-1", "not-offered"), Is.False);
            Assert.That(Snapshot(), Is.EqualTo(before));
            Assert.That(system.Call("TryBanish", "level-1", "a"), Is.True);
        }

        [Test]
        public void SkipClosesOnceWithoutApplyingChoiceOrSpendingResources()
        {
            system.Generate("level-1", new[] { "a" });
            Assert.That(system.Call("TrySkip", "level-1"), Is.True);
            Assert.That(Read<bool>(system.Current, "IsClosed"), Is.True);
            string before = Snapshot();
            Assert.That(system.Call("TrySkip", "level-1"), Is.False);
            Assert.That(system.Call("TryConfirm", "level-1", "a"), Is.False);
            Assert.That(system.Call("TryReroll", "level-1"), Is.False);
            Assert.That(system.Call("TryBanish", "level-1", "a"), Is.False);
            Assert.That(Snapshot(), Is.EqualTo(before));
            Assert.That(system.Confirmed, Is.Empty);
            AssertResources(2, 1);
            Assert.That(system.Generate("level-2", new[] { "b" }), Is.Not.Null);
            Assert.That(system.Call("TryConfirm", "level-2", "b"), Is.True);
            CollectionAssert.AreEqual(new[] { "b" }, system.Confirmed);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("unknown")]
        public void UnknownOfferIdCannotMutateTheCurrentOffer(string offerId)
        {
            system.Generate("level-1", new[] { "a" });
            string before = Snapshot();
            Assert.That(system.Call("TryConfirm", offerId, "a"), Is.False);
            Assert.That(system.Call("TryReroll", offerId), Is.False);
            Assert.That(system.Call("TryBanish", offerId, "a"), Is.False);
            Assert.That(system.Call("TrySkip", offerId), Is.False);
            Assert.That(Snapshot(), Is.EqualTo(before));
        }

        private void AssertResources(int rerolls, int banish)
        {
            Assert.That(system.Read<int>("RerollsRemaining"), Is.EqualTo(rerolls));
            Assert.That(system.Read<int>("BanishRemaining"), Is.EqualTo(banish));
        }

        private string Snapshot()
        {
            object offer = system.Current;
            return (offer == null ? "none" : Read<string>(offer, "OfferId") + ":" +
                Read<bool>(offer, "IsClosed") + ":" + string.Join(",", Choices(offer))) +
                "|" + system.Read<int>("RerollsRemaining") + "|" + system.Read<int>("BanishRemaining") +
                "|" + string.Join(",", system.Confirmed);
        }

        private static IReadOnlyList<string> Choices(object offer) => Read<IReadOnlyList<string>>(offer, "Choices");

        private static T Read<T>(object owner, string name)
        {
            Assert.That(owner, Is.Not.Null, "Expected an offer/session before reading " + name);
            var property = owner.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, "Missing upgrade contract property: " + name);
            return (T)property.GetValue(owner);
        }

        private sealed class OfferContract
        {
            private readonly object instance;
            private readonly Type type;

            public OfferContract(int seed)
            {
                type = typeof(GameBootstrap).Assembly.GetType("QuackQuack.Systems.Upgrades.UpgradeSystem");
                Assert.That(type, Is.Not.Null, "T040 pending: production UpgradeSystem is missing (T034 red phase).");
                instance = Activator.CreateInstance(type, seed);
            }

            public object Current => Read<object>("CurrentOffer");
            public IReadOnlyList<string> Confirmed => Read<IReadOnlyList<string>>("ConfirmedChoices");
            public T Read<T>(string name) => UpgradeOfferTests.Read<T>(instance, name);
            public object Generate(string offerId, string[] eligibleIds) =>
                Invoke("GenerateOffer", new[] { typeof(string), typeof(string[]) }, new object[] { offerId, eligibleIds });

            public bool Call(string name, params string[] args) =>
                (bool)Invoke(name, args.Select(_ => typeof(string)).ToArray(), args.Cast<object>().ToArray());

            private object Invoke(string name, Type[] types, object[] values)
            {
                var method = type.GetMethod(name, types);
                Assert.That(method, Is.Not.Null, "Missing UpgradeSystem operation: " + name);
                return method.Invoke(instance, values);
            }
        }
    }
}
