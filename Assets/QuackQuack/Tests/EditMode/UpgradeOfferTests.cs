using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;
using QuackQuack.Systems.Upgrades;

namespace QuackQuack.Tests.EditMode
{


    public class UpgradeOfferTests
    {
        private UpgradeSystem system;

        [SetUp]
        public void CreateSession() => system = new UpgradeSystem(12345);

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(8)]
        public void OffersContainUpToThreeDistinctEligibleChoices(int eligibleCount)
        {
            var eligible = Enumerable.Range(0, eligibleCount).Select(i => "eligible-" + i).ToArray();
            var offer = system.GenerateOffer("level-1", eligible);
            Assert.That(offer, Is.Not.Null);
            var choices = Choices(offer);
            Assert.That(choices.Count, Is.EqualTo(Math.Min(3, eligibleCount)));
            Assert.That(choices.Distinct().Count(), Is.EqualTo(choices.Count));
            Assert.That(choices.All(eligible.Contains), Is.True);
            Assert.That(choices, Does.Not.Contain("ineligible"));
            Assert.That(offer.OfferId, Is.EqualTo("level-1"));
            Assert.That(offer.IsClosed, Is.False);
        }

        [Test]
        public void DuplicateAndInvalidCandidateIdsDoNotFillMissingChoices()
        {
            var offer = system.GenerateOffer("level-1", new[] { "a", "a", null, "", "  ", "b", "b" });
            CollectionAssert.AreEquivalent(new[] { "a", "b" }, Choices(offer));
        }

        [Test]
        public void NoEligibleCandidatesProducesNoOfferOrResourceConsumption()
        {
            Assert.That(system.GenerateOffer("empty", Array.Empty<string>()), Is.Null);
            Assert.That(system.CurrentOffer, Is.Null);
            Assert.That(system.TrySkip("empty"), Is.False);
            Assert.That(system.TryReroll("empty"), Is.False);
            Assert.That(system.ConfirmedChoices, Is.Empty);
            AssertResources(2, 1);
            Assert.That(system.GenerateOffer("later", new[] { "a" }), Is.Not.Null);
        }

        [Test]
        public void SameSeedAndRequestsProduceSameOffersWithoutModifyingCandidates()
        {
            var other = new UpgradeSystem(12345);
            var candidates = new[] { "a", "b", "c", "d", "e", "f" };
            var original = (string[])candidates.Clone();
            CollectionAssert.AreEqual(Choices(system.GenerateOffer("level-1", candidates)),
                Choices(other.GenerateOffer("level-1", candidates)));
            Assert.That(system.TryReroll("level-1"), Is.True);
            Assert.That(other.TryReroll("level-1"), Is.True);
            CollectionAssert.AreEqual(Choices(system.CurrentOffer), Choices(other.CurrentOffer));
            CollectionAssert.AreEqual(original, candidates);
        }

        [Test]
        public void RepeatingGenerationDoesNotChangeOrReplaceAnOpenOffer()
        {
            system.GenerateOffer("level-1", new[] { "a", "b", "c", "d" });
            string before = Snapshot();
            system.GenerateOffer("level-1", new[] { "unexpected" });
            Assert.That(Snapshot(), Is.EqualTo(before));
        }

        [Test]
        public void ConfirmationAppliesOnceAndClosesOfferAgainstEveryOtherOperation()
        {
            system.GenerateOffer("level-1", new[] { "a", "b" });
            Assert.That(system.TryConfirm("level-1", "a"), Is.True);
            CollectionAssert.AreEqual(new[] { "a" }, system.ConfirmedChoices);
            Assert.That(system.CurrentOffer.IsClosed, Is.True);
            string before = Snapshot();
            Assert.That(system.TryConfirm("level-1", "a"), Is.False);
            Assert.That(system.TryConfirm("level-1", "b"), Is.False);
            Assert.That(system.TryReroll("level-1"), Is.False);
            Assert.That(system.TryBanish("level-1", "b"), Is.False);
            Assert.That(system.TrySkip("level-1"), Is.False);
            system.GenerateOffer("level-1", new[] { "a", "b" });
            Assert.That(Snapshot(), Is.EqualTo(before));
            AssertResources(2, 1);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("not-offered")]
        public void InvalidConfirmationLeavesOfferAndResourcesAvailable(string choice)
        {
            system.GenerateOffer("level-1", new[] { "a" });
            string before = Snapshot();
            Assert.That(system.TryConfirm("level-1", choice), Is.False);
            Assert.That(Snapshot(), Is.EqualTo(before));
            Assert.That(system.TryConfirm("level-1", "a"), Is.True);
        }

        [Test]
        public void RerollConsumesOncePerOfferAndInvalidatesPreviousId()
        {
            var oldOffer = system.GenerateOffer("level-1", new[] { "a", "b", "c", "d" });
            var oldChoices = Choices(oldOffer).ToArray();
            Assert.That(system.TryReroll("level-1"), Is.True);
            string replacement = system.CurrentOffer.OfferId;
            Assert.That(replacement, Is.Not.Null.And.Not.Empty.And.Not.EqualTo("level-1"));
            Assert.That(Choices(system.CurrentOffer).Count, Is.EqualTo(3));
            Assert.That(Choices(system.CurrentOffer).Distinct().Count(), Is.EqualTo(3));
            Assert.That(Choices(system.CurrentOffer).All(new[] { "a", "b", "c", "d" }.Contains), Is.True);
            AssertResources(1, 1);
            string before = Snapshot();
            Assert.That(system.TryReroll("level-1"), Is.False);
            Assert.That(system.TryConfirm("level-1", oldChoices[0]), Is.False);
            Assert.That(system.TryBanish("level-1", oldChoices[0]), Is.False);
            Assert.That(system.TrySkip("level-1"), Is.False);
            Assert.That(Snapshot(), Is.EqualTo(before));
            CollectionAssert.AreEqual(oldChoices, Choices(oldOffer));
            Assert.That(system.TryReroll(replacement), Is.True);
            AssertResources(0, 1);
            before = Snapshot();
            Assert.That(system.TryReroll(system.CurrentOffer.OfferId), Is.False);
            Assert.That(Snapshot(), Is.EqualTo(before));
        }

        [Test]
        public void BanishConsumesOnceAndExcludedChoiceNeverReturnsInLaterOffers()
        {
            system.GenerateOffer("level-1", new[] { "a", "b", "c" });
            Assert.That(system.TryBanish("level-1", "a"), Is.True);
            string replacement = system.CurrentOffer.OfferId;
            Assert.That(replacement, Is.Not.Null.And.Not.Empty.And.Not.EqualTo("level-1"));
            CollectionAssert.AreEquivalent(new[] { "b", "c" }, Choices(system.CurrentOffer));
            AssertResources(2, 0);
            string before = Snapshot();
            Assert.That(system.TryBanish("level-1", "a"), Is.False);
            Assert.That(system.TryBanish(replacement, "b"), Is.False);
            Assert.That(Snapshot(), Is.EqualTo(before));
            Assert.That(system.TryReroll(replacement), Is.True);
            Assert.That(Choices(system.CurrentOffer), Does.Not.Contain("a"));
            Assert.That(system.TrySkip(system.CurrentOffer.OfferId), Is.True);
            CollectionAssert.AreEquivalent(new[] { "b", "c" },
                Choices(system.GenerateOffer("level-2", new[] { "a", "b", "c" })));
        }

        [Test]
        public void BanishLastCandidateEndsSelectionWithoutCreatingAnEmptyOffer()
        {
            system.GenerateOffer("level-1", new[] { "a" });
            Assert.That(system.TryBanish("level-1", "a"), Is.True);
            Assert.That(system.CurrentOffer == null || system.CurrentOffer.IsClosed, Is.True);
            Assert.That(system.GenerateOffer("level-2", new[] { "a" }), Is.Null);
            Assert.That(system.ConfirmedChoices, Is.Empty);
            AssertResources(2, 0);
        }

        [Test]
        public void InvalidBanishDoesNotConsumeResourceOrCloseOffer()
        {
            system.GenerateOffer("level-1", new[] { "a" });
            string before = Snapshot();
            Assert.That(system.TryBanish("level-1", "not-offered"), Is.False);
            Assert.That(Snapshot(), Is.EqualTo(before));
            Assert.That(system.TryBanish("level-1", "a"), Is.True);
        }

        [Test]
        public void SkipClosesOnceWithoutApplyingChoiceOrSpendingResources()
        {
            system.GenerateOffer("level-1", new[] { "a" });
            Assert.That(system.TrySkip("level-1"), Is.True);
            Assert.That(system.CurrentOffer.IsClosed, Is.True);
            string before = Snapshot();
            Assert.That(system.TrySkip("level-1"), Is.False);
            Assert.That(system.TryConfirm("level-1", "a"), Is.False);
            Assert.That(system.TryReroll("level-1"), Is.False);
            Assert.That(system.TryBanish("level-1", "a"), Is.False);
            Assert.That(Snapshot(), Is.EqualTo(before));
            Assert.That(system.ConfirmedChoices, Is.Empty);
            AssertResources(2, 1);
            Assert.That(system.GenerateOffer("level-2", new[] { "b" }), Is.Not.Null);
            Assert.That(system.TryConfirm("level-2", "b"), Is.True);
            CollectionAssert.AreEqual(new[] { "b" }, system.ConfirmedChoices);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("unknown")]
        public void UnknownOfferIdCannotMutateTheCurrentOffer(string offerId)
        {
            system.GenerateOffer("level-1", new[] { "a" });
            string before = Snapshot();
            Assert.That(system.TryConfirm(offerId, "a"), Is.False);
            Assert.That(system.TryReroll(offerId), Is.False);
            Assert.That(system.TryBanish(offerId, "a"), Is.False);
            Assert.That(system.TrySkip(offerId), Is.False);
            Assert.That(Snapshot(), Is.EqualTo(before));
        }

        [Test]
        public void ConfirmationNotificationIsPublishedAfterClosingAndCannotBeReentered()
        {
            system.GenerateOffer("once", new[] { "a" });
            int notifications = 0;
            system.ChoiceConfirmed += value =>
            {
                notifications++;
                Assert.That(system.CurrentOffer.IsClosed, Is.True);
                Assert.That(value.OfferId, Is.EqualTo("once"));
                Assert.That(value.ChoiceId, Is.EqualTo("a"));
                Assert.That(value.IsEvolution, Is.False);
                Assert.That(system.TryConfirm(value.OfferId, value.ChoiceId), Is.False);
            };
            Assert.That(system.TryConfirm("once", "a"), Is.True);
            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(system.ConfirmedChoices.Count, Is.EqualTo(1));
        }

        private void AssertResources(int rerolls, int banish)
        {
            Assert.That(system.RerollsRemaining, Is.EqualTo(rerolls));
            Assert.That(system.BanishRemaining, Is.EqualTo(banish));
        }

        private string Snapshot()
        {
            UpgradeOffer offer = system.CurrentOffer;
            return (offer == null ? "none" : offer.OfferId + ":" +
                offer.IsClosed + ":" + string.Join(",", Choices(offer))) +
                "|" + system.RerollsRemaining + "|" + system.BanishRemaining +
                "|" + string.Join(",", system.ConfirmedChoices);
        }

        private static IReadOnlyList<string> Choices(UpgradeOffer offer) => offer.Choices;

    }
}
