using System;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Core.Run;

namespace QuackQuack.Tests.EditMode
{
    public class RewardPolicyTests
    {
        [Test]
        public void HealingPreservesDamageInvulnerabilityAndCannotClaimRoundedAwayGain()
        {
            var player = new PlayerState(100, 100, 12, 24);
            player.TakeDamage(40, 1, 5);
            Assert.That(player.Heal(float.Epsilon), Is.Zero);
            Assert.That(player.Health, Is.EqualTo(60));
            Assert.That(player.Heal(0.15f), Is.EqualTo(15).Within(0.00001f));
            Assert.That(player.InvulnerableUntil, Is.EqualTo(6));
            Assert.That(player.TakeDamage(1, 2, 0), Is.False);
        }
        [TestCase(100f, 40f, 0.15f, 15f)]
        [TestCase(100f, 40f, 0.50f, 40f)]
        [TestCase(200f, 80f, 0.15f, 30f)]
        [TestCase(200f, 150f, 0.50f, 100f)]
        [TestCase(100f, 5f, 0.15f, 5f)]
        public void HealUsesCurrentMaximumAndClampsToMissingHealth(float maximum, float damage, float fraction, float applied)
        {
            var player = Player(maximum, damage);
            Assert.That(Heal(player, fraction), Is.EqualTo(applied).Within(0.00001f));
            Assert.That(player.Health, Is.EqualTo(maximum - damage + applied).Within(0.00001f));
            Assert.That(player.MaxHealth, Is.EqualTo(maximum));
        }

        [TestCase(0f)] [TestCase(100f)]
        public void HealDoesNotWasteFullHealthOrReviveDeadPlayer(float damage)
        {
            var player = Player(100, damage);
            float before = player.Health;
            Assert.That(Heal(player, 0.5f), Is.Zero);
            Assert.That(player.Health, Is.EqualTo(before));
        }

        [TestCase(0f)] [TestCase(-0.1f)] [TestCase(1.01f)]
        [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(float.NegativeInfinity)]
        public void InvalidFractionNeverChangesHealth(float fraction)
        {
            var player = Player(100, 40);
            Assert.That(Heal(player, fraction), Is.Zero);
            Assert.That(player.Health, Is.EqualTo(60));
        }

        [TestCase(0d, true)] [TestCase(0.099999999999d, true)]
        [TestCase(0.1d, false)] [TestCase(0.100000000001d, false)] [TestCase(0.999999999d, false)]
        public void CommonDropUsesStrictTenPercentBoundary(double roll, bool drops)
        {
            int calls = 0;
            var ledger = RewardContract.Create(1, () => { calls++; return roll; });
            Assert.That(RewardContract.Defeat(ledger, 7, 1, "Common"), Is.True);
            Assert.That(RewardContract.OfKind(ledger, "Experience").Length, Is.EqualTo(1));
            var healing = RewardContract.OfKind(ledger, "HealCommon");
            Assert.That(healing.Length, Is.EqualTo(drops ? 1 : 0));
            if (drops) Assert.That(RewardContract.Get<float>(healing[0], "Fraction"), Is.EqualTo(0.15f));
            Assert.That(calls, Is.EqualTo(1));
        }

        [TestCase(0.01d, 10)] [TestCase(0.9d, 0)]
        public void TenDeathsCanAllDropOrNoneDropInsteadOfEnforcingQuota(double roll, int expected)
        {
            int calls = 0;
            var ledger = RewardContract.Create(1, () => { calls++; return roll; });
            for (int i = 1; i <= 10; i++) RewardContract.Defeat(ledger, 7, i, "Common");
            Assert.That(RewardContract.OfKind(ledger, "HealCommon").Length, Is.EqualTo(expected));
            Assert.That(RewardContract.OfKind(ledger, "Experience").Length, Is.EqualTo(10));
            Assert.That(calls, Is.EqualTo(10));
        }

        [TestCase("MiniBoss", "HealMiniBoss", 0.5f)]
        [TestCase("FinalBoss", "HealMiniBoss", 0f)]
        public void SpecialOriginsDoNotUseCommonRollOrInheritCommonHealing(string origin, string kind, float fraction)
        {
            var ledger = RewardContract.Create(1, () => { Assert.Fail("Special origins must not roll common healing."); return 0; });
            RewardContract.Defeat(ledger, 8, 1, origin);
            Assert.That(RewardContract.OfKind(ledger, "Experience").Length, Is.EqualTo(origin == "FinalBoss" ? 0 : 1));
            Assert.That(RewardContract.OfKind(ledger, "HealCommon"), Is.Empty);
            var healing = RewardContract.OfKind(ledger, kind);
            Assert.That(healing.Length, Is.EqualTo(fraction > 0 ? 1 : 0));
            if (fraction > 0) Assert.That(RewardContract.Get<float>(healing[0], "Fraction"), Is.EqualTo(fraction));
        }

        internal static PlayerState Player(float maximum, float damage)
        {
            var player = new PlayerState(maximum, 100, 12, 24);
            if (damage > 0) Assert.That(player.TakeDamage(damage, 0, 0), Is.True);
            return player;
        }

        internal static float Heal(PlayerState player, float fraction)
        {
            return player.Heal(fraction);
        }
    }
}
