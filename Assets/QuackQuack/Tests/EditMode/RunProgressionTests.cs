using System;
using System.Collections.Generic;
using NUnit.Framework;
using QuackQuack.Core.Run;
namespace QuackQuack.Tests.EditMode
{
    public class RunProgressionTests
    {
        public static RunResult Make(RunPhase outcome, int coins = 101, int kills = 3, double seconds = 120)
        {
            var run = new RunState(61); using var session = new RunResultSession(run, coins, 50, 0.5);
            run.TryTransition(RunPhase.Playing); run.Advance(seconds);
            for (int i = 0; i < kills; i++) session.RecordDefeat("ant");
            session.RecordWeaponDamage("weapon", 12.5);
            if (outcome == RunPhase.Won) run.TryTransition(RunPhase.Boss); run.TryTransition(outcome);
            return session.FinalizeResult();
        }
        private static ResultChallengeRule[] Rules => new[] { new ResultChallengeRule("kills", ResultMetric.EnemiesDefeated, 5, true), new ResultChallengeRule("survive", ResultMetric.SurvivalSeconds, 900, false), new ResultChallengeRule("win", ResultMetric.Victories, 1, true) };
        [TestCase(RunPhase.Won, 151, 1)] [TestCase(RunPhase.Lost, 101, 0)] [TestCase(RunPhase.Abandoned, 50, 0)]
        public void OutcomeUsesAlreadyCalculatedRewardAndNeverGrantsAbandonmentVictory(RunPhase outcome, int expected, int wins)
        {
            var receipt = RunProgression.Apply(new RunProgressProfile(), "run", Make(outcome), Rules);
            Assert.That(receipt.Profile.Coins, Is.EqualTo(expected)); Assert.That(receipt.Profile.Victories, Is.EqualTo(wins));
            Assert.That(receipt.Challenges["win"].Current, Is.EqualTo(wins)); Assert.That(receipt.Profile.Runs, Is.EqualTo(1));
        }
        [Test] public void ReapplyingRunIdCannotCreditCurrencyChallengesOrWinsTwice()
        {
            var result = Make(RunPhase.Won); var first = RunProgression.Apply(new RunProgressProfile(), "run", result, Rules);
            var second = RunProgression.Apply(first.Profile, "run", result, Rules);
            Assert.That(second.AlreadyApplied, Is.True); Assert.That(second.CoinsAdded, Is.Zero); Assert.That(second.Profile, Is.SameAs(first.Profile));
        }
        [Test] public void DifferentRunsWithSameSeedAreIndependentCredits()
        {
            var result = Make(RunPhase.Lost); var a = RunProgression.Apply(new RunProgressProfile(), "a", result, Rules);
            var b = RunProgression.Apply(a.Profile, "b", result, Rules);
            Assert.That(b.Profile.Coins, Is.EqualTo(202)); Assert.That(b.Profile.Runs, Is.EqualTo(2)); Assert.That(b.Profile.Challenges["kills"], Is.EqualTo(5));
        }
        [Test] public void BestRecordsAndNonCumulativeChallengesNeverRegress()
        {
            var a = RunProgression.Apply(new RunProgressProfile(), "a", Make(RunPhase.Lost, 0, 10, 920), Rules);
            var b = RunProgression.Apply(a.Profile, "b", Make(RunPhase.Abandoned, 0, 1, 30), Rules);
            Assert.That(b.Profile.Records["survival-seconds"], Is.EqualTo(920)); Assert.That(b.Profile.Records["enemies-defeated"], Is.EqualTo(10));
            Assert.That(b.Records["survival-seconds"].IsNewRecord, Is.False); Assert.That(b.Profile.Challenges["survive"], Is.EqualTo(900));
        }
        [Test] public void CurrencySaturatesWithoutWrapping()
        {
            var a = RunProgression.Apply(new RunProgressProfile(int.MaxValue - 1), "a", Make(RunPhase.Won), Rules);
            Assert.That(a.Profile.Coins, Is.EqualTo(int.MaxValue)); Assert.That(a.CoinsAdded, Is.EqualTo(1));
        }
        [Test] public void TimeRecordUsesDisplayedWholeSeconds()
        {
            var receipt = RunProgression.Apply(new RunProgressProfile(), "short", Make(RunPhase.Abandoned, 0, 0, 0.9), Rules);
            Assert.That(receipt.Profile.Records["survival-seconds"], Is.Zero);
            Assert.That(receipt.Records["survival-seconds"].IsNewRecord, Is.False);
        }
        [Test] public void ProfileCollectionsAreCopiesAndRejectMutation()
        {
            var ids = new[] { "a" }; var records = new Dictionary<string, double> { { "record", 3 } };
            var profile = new RunProgressProfile(1, 1, 0, records, null, ids); ids[0] = "changed"; records["record"] = 100;
            Assert.That(profile.AppliedRunIds[0], Is.EqualTo("a")); Assert.That(profile.Records["record"], Is.EqualTo(3));
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, double>)profile.Records).Clear());
        }
        [Test] public void InvalidProfileOrChallengeDoesNotBecomeProgress()
        {
            Assert.Throws<ArgumentException>(() => new RunProgressProfile(-1));
            Assert.Throws<ArgumentException>(() => new RunProgressProfile(0, 1));
            Assert.Throws<ArgumentException>(() => new RunProgressProfile(0, 1, 2, null, null, new[] { "a" }));
            Assert.Throws<ArgumentException>(() => RunProgression.Apply(new RunProgressProfile(), "run", Make(RunPhase.Lost), new[] { default(ResultChallengeRule) }));
            Assert.Throws<ArgumentException>(() => RunProgression.Apply(new RunProgressProfile(), "run", Make(RunPhase.Lost), new[] { Rules[0], Rules[0] }));
        }
    }
}
