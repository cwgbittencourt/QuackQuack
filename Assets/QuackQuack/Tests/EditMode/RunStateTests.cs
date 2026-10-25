using NUnit.Framework;
using QuackQuack.Core.Run;

namespace QuackQuack.Tests.EditMode
{
    public class RunStateTests
    {
        [Test]
        public void CannotWinBeforeBoss()
        {
            var run = new RunState(42);
            Assert.That(run.TryTransition(RunPhase.Won), Is.False);
            Assert.That(run.TryTransition(RunPhase.Playing), Is.True);
            Assert.That(run.TryTransition(RunPhase.Won), Is.False);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Playing));
        }

        [TestCase(RunPhase.Won)]
        [TestCase(RunPhase.Lost)]
        [TestCase(RunPhase.Abandoned)]
        public void TerminalAndResultsEventsAreEmittedOnce(RunPhase ending)
        {
            var run = new RunState(42);
            int ended = 0, results = 0;
            run.Ended += _ => ended++;
            run.ResultsReady += () => results++;
            run.TryTransition(RunPhase.Playing);
            if (ending == RunPhase.Won) run.TryTransition(RunPhase.Boss);
            Assert.That(run.TryTransition(ending), Is.True);
            Assert.That(run.TryTransition(ending), Is.False);
            Assert.That(run.TryTransition(RunPhase.Playing), Is.False);
            Assert.That(run.TryTransition(RunPhase.Results), Is.True);
            Assert.That(run.TryTransition(RunPhase.Results), Is.False);
            Assert.That(ended, Is.EqualTo(1));
            Assert.That(results, Is.EqualTo(1));
            Assert.That(run.Outcome, Is.EqualTo(ending));
        }

        [Test]
        public void PauseAndUpgradeFreezeClock()
        {
            var run = new RunState(1);
            run.TryTransition(RunPhase.Playing);
            run.Advance(2);
            run.TryTransition(RunPhase.Paused);
            run.Advance(10);
            run.TryTransition(RunPhase.Playing);
            run.TryTransition(RunPhase.ChoosingUpgrade);
            run.Advance(10);
            Assert.That(run.ElapsedSeconds, Is.EqualTo(2));
        }
    }
}
