using System;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Core.Run;

namespace QuackQuack.Tests.EditMode
{
    public class RunResultTests
    {
        private RunState run;
        private Type sessionType;
        private object session;

        [SetUp]
        public void CreateSession()
        {
            // Temporary adapter for the red phase; no replacement production implementation.
            sessionType = typeof(RunState).Assembly.GetType("QuackQuack.Core.Run.RunResultSession");
            Assert.That(sessionType, Is.Not.Null,
                "T058/T060 pending: production RunResultSession is missing (T047 red phase).");
            run = new RunState(47);
            session = Create(run, 101, 50, 0.5);
        }

        [TestCase(RunPhase.Preparing)]
        [TestCase(RunPhase.Playing)]
        [TestCase(RunPhase.Paused)]
        [TestCase(RunPhase.ChoosingUpgrade)]
        [TestCase(RunPhase.Boss)]
        public void NonterminalRunCannotProduceResult(RunPhase phase)
        {
            Enter(phase);
            Assert.That(FinalizeResult(), Is.Null);
            Assert.That(FinalizeResult(), Is.Null);
        }

        [TestCase(RunPhase.Won)]
        [TestCase(RunPhase.Lost)]
        [TestCase(RunPhase.Abandoned)]
        public void TerminalRunProducesOneStableResultIncludingAfterResultsScreen(RunPhase outcome)
        {
            Enter(RunPhase.Playing);
            run.Advance(123.5);
            End(outcome);
            object first = FinalizeResult();
            Assert.That(first, Is.Not.Null);
            Assert.That(Read<RunPhase>(first, "Outcome"), Is.EqualTo(outcome));
            Assert.That(Read<bool>(first, "IsVictory"), Is.EqualTo(outcome == RunPhase.Won));
            Assert.That(Read<double>(first, "DurationSeconds"), Is.EqualTo(123.5));
            int coins = Read<int>(first, "Coins");
            Assert.That(run.TryTransition(outcome), Is.False);
            for (int i = 0; i < 5; i++) Assert.That(FinalizeResult(), Is.SameAs(first));
            Assert.That(run.TryTransition(RunPhase.Results), Is.True);
            run.Advance(999);
            Assert.That(FinalizeResult(), Is.SameAs(first));
            Assert.That(Read<int>(first, "Coins"), Is.EqualTo(coins));
            Assert.That(Read<double>(first, "DurationSeconds"), Is.EqualTo(123.5));
        }

        [Test]
        public void FirstCollectionAfterResultsStillUsesOriginalTerminalOutcome()
        {
            Enter(RunPhase.Playing);
            End(RunPhase.Abandoned);
            Assert.That(run.TryTransition(RunPhase.Results), Is.True);
            object result = FinalizeResult();
            Assert.That(result, Is.Not.Null);
            Assert.That(Read<RunPhase>(result, "Outcome"), Is.EqualTo(RunPhase.Abandoned));
            Assert.That(Read<bool>(result, "IsVictory"), Is.False);
            Assert.That(Read<int>(result, "Coins"), Is.EqualTo(50));
        }

        [TestCase(RunPhase.Playing)]
        [TestCase(RunPhase.Paused)]
        [TestCase(RunPhase.ChoosingUpgrade)]
        [TestCase(RunPhase.Boss)]
        public void AbandonmentFromEveryAllowedPhaseNeverBecomesVictory(RunPhase phase)
        {
            Enter(phase);
            Assert.That(run.TryTransition(RunPhase.Abandoned), Is.True);
            object result = FinalizeResult();
            Assert.That(Read<RunPhase>(result, "Outcome"), Is.EqualTo(RunPhase.Abandoned));
            Assert.That(Read<bool>(result, "IsVictory"), Is.False);
            Assert.That(Read<int>(result, "Coins"), Is.EqualTo(50),
                "Partial earned coins, rounded down; no victory bonus.");
            Assert.That(run.TryTransition(RunPhase.Won), Is.False);
            Assert.That(FinalizeResult(), Is.SameAs(result));
            Assert.That(Read<bool>(result, "IsVictory"), Is.False);
        }

        [Test]
        public void VictoryReceivesFullEarnedCoinsAndBonusInsteadOfAbandonmentPenalty()
        {
            Enter(RunPhase.Playing);
            End(RunPhase.Won);
            object result = FinalizeResult();
            Assert.That(Read<bool>(result, "IsVictory"), Is.True);
            Assert.That(Read<int>(result, "Coins"), Is.EqualTo(151));
        }

        [Test]
        public void AbandoningWithoutEarningsCannotGrantVictoryBonus()
        {
            session = Create(run, 0, 50, 0.5);
            Enter(RunPhase.Playing);
            End(RunPhase.Abandoned);
            Assert.That(Read<int>(FinalizeResult(), "Coins"), Is.Zero);
        }

        [Test]
        public void PartialRewardUsesInjectedPolicy()
        {
            session = Create(run, 100, 50, 0.25);
            Enter(RunPhase.Playing);
            End(RunPhase.Abandoned);
            Assert.That(Read<int>(FinalizeResult(), "Coins"), Is.EqualTo(25));
        }

        [Test]
        public void FinalizationBeforeEndDoesNotConsumeFutureResult()
        {
            Enter(RunPhase.Playing);
            Assert.That(FinalizeResult(), Is.Null);
            End(RunPhase.Lost);
            object result = FinalizeResult();
            Assert.That(result, Is.Not.Null);
            Assert.That(Read<RunPhase>(result, "Outcome"), Is.EqualTo(RunPhase.Lost));
            Assert.That(Read<bool>(result, "IsVictory"), Is.False);
        }

        [Test]
        public void SessionsWithSameSeedDoNotShareResultOrOutcome()
        {
            Enter(RunPhase.Playing);
            End(RunPhase.Abandoned);
            object abandoned = FinalizeResult();
            var otherRun = new RunState(47);
            object otherSession = Create(otherRun, 101, 50, 0.5);
            otherRun.TryTransition(RunPhase.Playing);
            otherRun.TryTransition(RunPhase.Boss);
            otherRun.TryTransition(RunPhase.Won);
            object won = FinalizeResult(otherSession);
            Assert.That(won, Is.Not.SameAs(abandoned));
            Assert.That(Read<bool>(won, "IsVictory"), Is.True);
            Assert.That(Read<bool>(abandoned, "IsVictory"), Is.False);
        }

        private void Enter(RunPhase phase)
        {
            if (phase == RunPhase.Preparing) return;
            Assert.That(run.TryTransition(RunPhase.Playing), Is.True);
            if (phase != RunPhase.Playing) Assert.That(run.TryTransition(phase), Is.True);
        }

        private void End(RunPhase outcome)
        {
            if (outcome == RunPhase.Won) Assert.That(run.TryTransition(RunPhase.Boss), Is.True);
            Assert.That(run.TryTransition(outcome), Is.True);
        }

        private object Create(RunState state, int coins, int bonus, double factor)
        {
            var constructor = sessionType.GetConstructor(new[] { typeof(RunState), typeof(int), typeof(int), typeof(double) });
            Assert.That(constructor, Is.Not.Null,
                "Expected RunResultSession(RunState, int earnedCoins, int victoryBonus, double abandonmentFactor).");
            return constructor.Invoke(new object[] { state, coins, bonus, factor });
        }

        private object FinalizeResult(object source = null)
        {
            var method = sessionType.GetMethod("FinalizeResult", Type.EmptyTypes);
            Assert.That(method, Is.Not.Null, "Expected FinalizeResult() returning RunResult or null.");
            object result = method.Invoke(source ?? session, null);
            if (result != null) Assert.That(result.GetType().FullName, Is.EqualTo("QuackQuack.Core.Run.RunResult"));
            return result;
        }

        private static T Read<T>(object result, string name)
        {
            Assert.That(result, Is.Not.Null);
            PropertyInfo property = result.GetType().GetProperty(name);
            Assert.That(property, Is.Not.Null, "Expected result property " + name);
            return (T)property.GetValue(result);
        }
    }
}
