using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;

namespace QuackQuack.Tests.EditMode
{
    public class RewardLedgerTests
    {
        [Test]
        public void ReplayOfFailedRollCannotRetryLotteryOrReplaceOriginalRewardData()
        {
            int rolls = 0;
            var ledger = RewardContract.Create(1, () => ++rolls == 1 ? 0.9 : 0);
            RewardContract.Defeat(ledger, 7, 11, "Common", 9, -4, 8);
            Assert.That(RewardContract.Defeat(ledger, 7, 11, "Common", 99, 100, 200), Is.False);
            Assert.That(RewardContract.OfKind(ledger, "HealCommon"), Is.Empty);
            var xp = RewardContract.OfKind(ledger, "Experience");
            Assert.That(xp.Length, Is.EqualTo(1));
            Assert.That(RewardContract.Get<int>(xp[0], "Experience"), Is.EqualTo(9));
            Assert.That(RewardContract.Get<float>(xp[0], "X"), Is.EqualTo(-4));
            Assert.That(RewardContract.Get<float>(xp[0], "Y"), Is.EqualTo(8));
            Assert.That(rolls, Is.EqualTo(1));
        }

        [TestCase("Common", 2)] [TestCase("MiniBoss", 2)] [TestCase("FinalBoss", 1)]
        public void DuplicateDefeatKeepsOneObligationPerKindAndDoesNotReroll(string origin, int count)
        {
            int rolls = 0;
            var ledger = RewardContract.Create(1, () => { rolls++; return 0; });
            Assert.That(RewardContract.Defeat(ledger, 7, 11, origin), Is.True);
            var ids = RewardContract.Entries(ledger).Select(RewardContract.Id).ToArray();
            for (int i = 0; i < 20; i++) Assert.That(RewardContract.Defeat(ledger, 7, 11, origin), Is.False);
            Assert.That(RewardContract.Entries(ledger).Length, Is.EqualTo(count));
            Assert.That(RewardContract.Entries(ledger).Select(RewardContract.Id), Is.EqualTo(ids));
            Assert.That(rolls, Is.EqualTo(origin == "Common" ? 1 : 0));
        }

        [Test]
        public void RewardIdentitySeparatesSourceLeaseRunAndKind()
        {
            var first = RewardContract.Create(1, () => 0);
            RewardContract.Defeat(first, 7, 11, "Common");
            RewardContract.Defeat(first, 7, 12, "Common");
            RewardContract.Defeat(first, 8, 11, "Common");
            var second = RewardContract.Create(2, () => 0);
            RewardContract.Defeat(second, 7, 11, "Common");
            var ids = RewardContract.Entries(first).Concat(RewardContract.Entries(second)).Select(RewardContract.Id).ToArray();
            Assert.That(ids.Length, Is.EqualTo(8));
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Length));
        }

        [TestCase("Common")] [TestCase("MiniBoss")] [TestCase("FinalBoss")]
        public void DespawnIsNotDefeatAndCannotConsumeRandomOrReward(string origin)
        {
            int rolls = 0;
            var ledger = RewardContract.Create(1, () => { rolls++; return 0; });
            Assert.That(RewardContract.Defeat(ledger, 7, 1, origin, defeated: false), Is.False);
            Assert.That(RewardContract.Entries(ledger), Is.Empty); Assert.That(rolls, Is.Zero);
            Assert.That(RewardContract.Defeat(ledger, 7, 1, origin), Is.True, "Despawn must not consume a future defeat identity.");
        }

        [TestCase(0)] [TestCase(-1)]
        public void NonPositiveExperienceRejectsWholeDefeatWithoutConsumingIdentity(int xp)
        {
            int rolls = 0;
            var ledger = RewardContract.Create(1, () => { rolls++; return 0; });
            Assert.That(RewardContract.Defeat(ledger, 7, 1, "Common", xp), Is.False);
            Assert.That(RewardContract.Entries(ledger), Is.Empty); Assert.That(rolls, Is.Zero);
            Assert.That(RewardContract.Defeat(ledger, 7, 1, "Common", 9), Is.True);
        }

        [Test]
        public void SaturationRetainsPositionValueIdentityAndRollUntilViewBecomesAvailable()
        {
            int rolls = 0;
            var ledger = RewardContract.Create(1, () => { rolls++; return 0.01; });
            RewardContract.Defeat(ledger, 7, 1, "Common", 9, -4.25f, 8.5f);
            var entries = RewardContract.Entries(ledger);
            foreach (var entry in entries)
            {
                object id = RewardContract.Id(entry);
                for (int attempt = 0; attempt < 20; attempt++)
                    Assert.That(RewardContract.Call<bool>(ledger, "TryMaterialize", id, false), Is.False);
                Assert.That(RewardContract.Get<object>(entry, "State").ToString(), Is.EqualTo("WaitingForView"));
                Assert.That(RewardContract.Get<float>(entry, "X"), Is.EqualTo(-4.25f));
                Assert.That(RewardContract.Get<float>(entry, "Y"), Is.EqualTo(8.5f));
                Assert.That(RewardContract.Id(entry), Is.EqualTo(id));
                Assert.That(RewardContract.Call<bool>(ledger, "TryMaterialize", id, true), Is.True);
                Assert.That(RewardContract.Call<bool>(ledger, "TryMaterialize", id, true), Is.False);
                Assert.That(RewardContract.Get<object>(entry, "State").ToString(), Is.EqualTo("Grounded"));
            }
            Assert.That(RewardContract.Get<int>(RewardContract.OfKind(ledger, "Experience")[0], "Experience"), Is.EqualTo(9));
            Assert.That(RewardContract.Get<float>(RewardContract.OfKind(ledger, "HealCommon")[0], "Fraction"), Is.EqualTo(0.15f));
            Assert.That(RewardContract.Entries(ledger).Length, Is.EqualTo(2)); Assert.That(rolls, Is.EqualTo(1));
        }

        [Test]
        public void PendingViewsDoNotReduceLaterCommonChanceOrMiniBossGuarantee()
        {
            int rolls = 0;
            var ledger = RewardContract.Create(1, () => { rolls++; return 0.01; });
            for (int i = 1; i <= 40; i++)
            {
                RewardContract.Defeat(ledger, 7, i, "Common");
                foreach (var entry in RewardContract.Entries(ledger))
                    RewardContract.Call<bool>(ledger, "TryMaterialize", RewardContract.Id(entry), false);
            }
            RewardContract.Defeat(ledger, 8, 1, "MiniBoss");
            Assert.That(RewardContract.OfKind(ledger, "Experience").Length, Is.EqualTo(41));
            Assert.That(RewardContract.OfKind(ledger, "HealCommon").Length, Is.EqualTo(40));
            Assert.That(RewardContract.OfKind(ledger, "HealMiniBoss").Length, Is.EqualTo(1));
            Assert.That(rolls, Is.EqualTo(40));
        }

        [TestCase(0.15f, "Common", "HealCommon")]
        [TestCase(0.50f, "MiniBoss", "HealMiniBoss")]
        public void HealingClaimsOnlyPositiveApplicationAndNeverTwice(float fraction, string origin, string kind)
        {
            var ledger = RewardContract.Create(1, () => 0);
            RewardContract.Defeat(ledger, 7, 1, origin);
            object id = RewardContract.Id(RewardContract.OfKind(ledger, kind)[0]);
            var player = RewardPolicyTests.Player(200, 0);
            Assert.That(RewardContract.Call<float>(ledger, "TryCollectHeal", id, player), Is.Zero, "Pending rewards cannot collect remotely.");
            RewardContract.Call<bool>(ledger, "TryMaterialize", id, true);
            Assert.That(RewardContract.Call<float>(ledger, "TryCollectHeal", id, player), Is.Zero);
            player.TakeDamage(10, 1, 0);
            Assert.That(RewardContract.Call<float>(ledger, "TryCollectHeal", id, player), Is.EqualTo(10));
            player.TakeDamage(100, 2, 0);
            Assert.That(RewardContract.Call<float>(ledger, "TryCollectHeal", id, player), Is.Zero);
            Assert.That(player.Health, Is.EqualTo(100));
            Assert.That(RewardContract.Get<object>(RewardContract.OfKind(ledger, kind)[0], "State").ToString(), Is.EqualTo("Claimed"));
        }

        [Test]
        public void DeadPlayerCannotConsumeOrBeRevivedByHealingReward()
        {
            var ledger = RewardContract.Create(1, () => 0);
            RewardContract.Defeat(ledger, 7, 1, "MiniBoss");
            var entry = RewardContract.OfKind(ledger, "HealMiniBoss")[0]; object id = RewardContract.Id(entry);
            RewardContract.Call<bool>(ledger, "TryMaterialize", id, true);
            var dead = RewardPolicyTests.Player(100, 100);
            Assert.That(RewardContract.Call<float>(ledger, "TryCollectHeal", id, dead), Is.Zero);
            Assert.That(dead.Health, Is.Zero);
            Assert.That(RewardContract.Get<object>(entry, "State").ToString(), Is.EqualTo("Grounded"));
        }

        [Test]
        public void ExperienceClaimsOnceThroughExistingProgressionAndNeverFromPendingView()
        {
            var ledger = RewardContract.Create(1, () => 0.9);
            RewardContract.Defeat(ledger, 7, 1, "Common", 25);
            object id = RewardContract.Id(RewardContract.OfKind(ledger, "Experience")[0]);
            var xp = new ExperienceState(new[] { 10, 20, 40 });
            Assert.That(RewardContract.Call<bool>(ledger, "TryCollectExperience", id, xp), Is.False);
            Assert.That(xp.Experience, Is.Zero);
            RewardContract.Call<bool>(ledger, "TryMaterialize", id, true);
            Assert.That(RewardContract.Call<bool>(ledger, "TryCollectExperience", id, xp), Is.True);
            Assert.That(RewardContract.Call<bool>(ledger, "TryCollectExperience", id, xp), Is.False);
            Assert.That(xp.Experience, Is.EqualTo(25)); Assert.That(xp.Level, Is.EqualTo(3));
        }

        [Test]
        public void ReproducibleDropSequenceIsUnaffectedByIndependentOfferRandom()
        {
            var rngA = new Random(71); var rngB = new Random(71); var offers = new Random(99);
            var a = RewardContract.Create(1, rngA.NextDouble); var b = RewardContract.Create(1, rngB.NextDouble);
            for (int i = 1; i <= 50; i++)
            {
                for (int n = 0; n < i; n++) offers.NextDouble();
                RewardContract.Defeat(a, 7, i, "Common"); RewardContract.Defeat(b, 7, i, "Common");
            }
            Assert.That(RewardContract.Entries(a).Select(e => RewardContract.Get<object>(e, "Kind").ToString()),
                Is.EqualTo(RewardContract.Entries(b).Select(e => RewardContract.Get<object>(e, "Kind").ToString())));
        }
    }

    // Proposed pure API seam: no simulated reward implementation or production stubs.
    internal static class RewardContract
    {
        internal static object Create(long run, Func<double> random)
        {
            var type = typeof(PlayerState).Assembly.GetType("QuackQuack.Core.Progression.RewardLedger");
            Assert.That(type, Is.Not.Null, "T025/T027 pending: pure RewardLedger is missing.");
            var constructor = type.GetConstructor(new[] { typeof(long), typeof(Func<double>), typeof(double), typeof(float), typeof(float) });
            Assert.That(constructor, Is.Not.Null, "Missing proposed RewardLedger constructor; adapt test seam if production API differs.");
            return constructor.Invoke(new object[] { run, random, 0.1d, 0.15f, 0.5f });
        }
        internal static bool Defeat(object ledger, int source, long lease, string origin, int xp = 9, float x = 0, float y = 0, bool defeated = true)
        {
            var method = ledger.GetType().GetMethod("ProcessDefeat");
            Assert.That(method, Is.Not.Null, "Missing ProcessDefeat contract.");
            var originType = method.GetParameters()[2].ParameterType;
            return (bool)method.Invoke(ledger, new object[] { source, lease, Enum.Parse(originType, origin), xp, x, y, defeated });
        }
        internal static object[] Entries(object ledger) => ((IEnumerable)Get<object>(ledger, "Entries")).Cast<object>().ToArray();
        internal static object[] OfKind(object ledger, string kind) => Entries(ledger).Where(e => Get<object>(e, "Kind").ToString() == kind).ToArray();
        internal static object Id(object entry) => Get<object>(entry, "Id");
        internal static T Get<T>(object value, string name)
        {
            var property = value.GetType().GetProperty(name); Assert.That(property, Is.Not.Null, "Missing reward property: " + name);
            return (T)property.GetValue(value);
        }
        internal static T Call<T>(object value, string name, params object[] arguments)
        {
            var method = value.GetType().GetMethod(name); Assert.That(method, Is.Not.Null, "Missing reward method: " + name);
            try { return (T)method.Invoke(value, arguments); }
            catch (TargetInvocationException error) { throw error.InnerException ?? error; }
        }
    }
}
