using NUnit.Framework;
using QuackQuack.Core.Run;

namespace QuackQuack.Tests.EditMode
{
    public class RunResultTests
    {
        private RunState run;
        private RunResultSession session;

        [SetUp]
        public void CreateSession()
        {
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
            RunResult first = FinalizeResult();
            Assert.That(first, Is.Not.Null);
            Assert.That(first.Outcome, Is.EqualTo(outcome));
            Assert.That(first.IsVictory, Is.EqualTo(outcome == RunPhase.Won));
            Assert.That(first.DurationSeconds, Is.EqualTo(123.5));
            int coins = first.Coins;
            Assert.That(run.TryTransition(outcome), Is.False);
            for (int i = 0; i < 5; i++) Assert.That(FinalizeResult(), Is.SameAs(first));
            Assert.That(run.TryTransition(RunPhase.Results), Is.True);
            run.Advance(999);
            Assert.That(FinalizeResult(), Is.SameAs(first));
            Assert.That(first.Coins, Is.EqualTo(coins));
            Assert.That(first.DurationSeconds, Is.EqualTo(123.5));
        }

        [Test]
        public void FirstCollectionAfterResultsStillUsesOriginalTerminalOutcome()
        {
            Enter(RunPhase.Playing);
            End(RunPhase.Abandoned);
            Assert.That(run.TryTransition(RunPhase.Results), Is.True);
            RunResult result = FinalizeResult();
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Outcome, Is.EqualTo(RunPhase.Abandoned));
            Assert.That(result.IsVictory, Is.False);
            Assert.That(result.Coins, Is.EqualTo(50));
        }

        [TestCase(RunPhase.Playing)]
        [TestCase(RunPhase.Paused)]
        [TestCase(RunPhase.ChoosingUpgrade)]
        [TestCase(RunPhase.Boss)]
        public void AbandonmentFromEveryAllowedPhaseNeverBecomesVictory(RunPhase phase)
        {
            Enter(phase);
            Assert.That(run.TryTransition(RunPhase.Abandoned), Is.True);
            RunResult result = FinalizeResult();
            Assert.That(result.Outcome, Is.EqualTo(RunPhase.Abandoned));
            Assert.That(result.IsVictory, Is.False);
            Assert.That(result.Coins, Is.EqualTo(50),
                "Partial earned coins, rounded down; no victory bonus.");
            Assert.That(run.TryTransition(RunPhase.Won), Is.False);
            Assert.That(FinalizeResult(), Is.SameAs(result));
            Assert.That(result.IsVictory, Is.False);
        }

        [Test]
        public void VictoryReceivesFullEarnedCoinsAndBonusInsteadOfAbandonmentPenalty()
        {
            Enter(RunPhase.Playing);
            End(RunPhase.Won);
            RunResult result = FinalizeResult();
            Assert.That(result.IsVictory, Is.True);
            Assert.That(result.Coins, Is.EqualTo(151));
        }

        [Test]
        public void AbandoningWithoutEarningsCannotGrantVictoryBonus()
        {
            session.Dispose();
            session = Create(run, 0, 50, 0.5);
            Enter(RunPhase.Playing);
            End(RunPhase.Abandoned);
            Assert.That(FinalizeResult().Coins, Is.Zero);
        }

        [Test]
        public void PartialRewardUsesInjectedPolicy()
        {
            session.Dispose();
            session = Create(run, 100, 50, 0.25);
            Enter(RunPhase.Playing);
            End(RunPhase.Abandoned);
            Assert.That(FinalizeResult().Coins, Is.EqualTo(25));
        }

        [Test]
        public void FinalizationBeforeEndDoesNotConsumeFutureResult()
        {
            Enter(RunPhase.Playing);
            Assert.That(FinalizeResult(), Is.Null);
            End(RunPhase.Lost);
            RunResult result = FinalizeResult();
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Outcome, Is.EqualTo(RunPhase.Lost));
            Assert.That(result.IsVictory, Is.False);
        }

        [Test]
        public void SessionsWithSameSeedDoNotShareResultOrOutcome()
        {
            Enter(RunPhase.Playing);
            End(RunPhase.Abandoned);
            RunResult abandoned = FinalizeResult();
            var otherRun = new RunState(47);
            using var otherSession = Create(otherRun, 101, 50, 0.5);
            otherRun.TryTransition(RunPhase.Playing);
            otherRun.TryTransition(RunPhase.Boss);
            otherRun.TryTransition(RunPhase.Won);
            RunResult won = FinalizeResult(otherSession);
            Assert.That(won, Is.Not.SameAs(abandoned));
            Assert.That(won.IsVictory, Is.True);
            Assert.That(abandoned.IsVictory, Is.False);
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

        private RunResultSession Create(RunState state, int coins, int bonus, double factor) =>
            new RunResultSession(state, coins, bonus, factor);

        private RunResult FinalizeResult(RunResultSession source = null) => (source ?? session).FinalizeResult();

        [TearDown] public void Cleanup() => session.Dispose();
    }
}
