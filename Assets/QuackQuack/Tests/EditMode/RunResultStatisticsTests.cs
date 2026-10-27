using System;
using System.Collections.Generic;
using NUnit.Framework;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;

namespace QuackQuack.Tests.EditMode
{
    public class RunResultStatisticsTests
    {
        private RunState run;
        private BuildState build;
        private RelicLoadout relics;
        private RunResultSession session;

        [SetUp] public void SetUp()
        {
            run = new RunState(58); build = new BuildState();
            var ids = new string[10]; var bonuses = new float[10];
            for (int i = 0; i < 10; i++) { ids[i] = "relic-" + i; bonuses[i] = 0.05f; }
            relics = new RelicLoadout(ids, bonuses);
            session = new RunResultSession(run, 0, 20, 0.5, build, relics);
            run.TryTransition(RunPhase.Playing);
        }
        [TearDown] public void Cleanup() => session.Dispose();
        private RunResult Lose() { run.TryTransition(RunPhase.Lost); return session.FinalizeResult(); }

        [Test] public void AllFieldsAreCapturedIncludingBuildEvolutionAndProgress()
        {
            run.Advance(125.25);
            Assert.That(session.RecordBossReached("libelula-alfa"), Is.True);
            session.RecordDefeat("formiga"); session.RecordDefeat("formiga"); session.BreakStreak(); session.RecordDefeat("aranha");
            session.RecordWeaponDamage("bumerangue", 12.5); session.RecordWeaponDamage("bumerangue", 7.5); session.RecordWeaponDamage("quack", 3);
            build.TryAddWeapon("bumerangue"); build.TryAddPassive("pena");
            for (int i = 1; i < 5; i++) build.TryUpgradeWeapon("bumerangue");
            build.TryEvolveWeapon("bumerangue", "pena", "bumerangue-real"); build.TryUseReroll(); build.TryUseBanish();
            relics.Unlock("relic-0"); relics.Equip("relic-0");
            session.RecordSynergy("pena-bumerangue"); session.AddCoins(25); session.RecordItem("relic-1", 1);
            session.SetRecord("kills", 2, 3); session.SetChallengeProgress("hunter", 7, 10, 10);
            var result = Lose();
            Assert.That(result.Seed, Is.EqualTo(58)); Assert.That(result.DurationSeconds, Is.EqualTo(125.25));
            Assert.That(result.BossReached, Is.True); Assert.That(result.BossId, Is.EqualTo("libelula-alfa"));
            Assert.That(result.EnemiesDefeated, Is.EqualTo(3)); Assert.That(result.DefeatsByEnemy["formiga"], Is.EqualTo(2));
            Assert.That(result.LongestStreak, Is.EqualTo(2)); Assert.That(result.DamageByWeapon["bumerangue"], Is.EqualTo(20));
            Assert.That(result.Build.Weapons["bumerangue-real"], Is.EqualTo(5)); Assert.That(result.Build.Passives["pena"], Is.EqualTo(1));
            Assert.That(result.Build.Evolutions["bumerangue"], Is.EqualTo("bumerangue-real")); Assert.That(result.Build.History.Count, Is.EqualTo(7));
            Assert.That(result.Build.RerollsRemaining, Is.EqualTo(1)); Assert.That(result.Build.BanishRemaining, Is.Zero);
            CollectionAssert.AreEqual(new[] { "relic-0" }, result.Build.EquippedRelicIds);
            CollectionAssert.AreEqual(new[] { "pena-bumerangue" }, result.Synergies);
            Assert.That(result.Coins, Is.EqualTo(25)); Assert.That(result.EarnedCoins, Is.EqualTo(25)); Assert.That(result.VictoryBonus, Is.Zero);
            Assert.That(result.Items["relic-1"], Is.EqualTo(1)); Assert.That(result.Records["kills"].IsNewRecord, Is.True);
            Assert.That(result.Challenges["hunter"].Gained, Is.EqualTo(3)); Assert.That(result.Challenges["hunter"].IsCompleted, Is.True);
        }

        [Test] public void FreezeHappensAtTerminalEvenBeforeFirstCollection()
        {
            build.TryAddWeapon("a"); session.RecordDefeat("ant"); session.RecordWeaponDamage("a", 4);
            run.TryTransition(RunPhase.Lost);
            build.TryUpgradeWeapon("a"); build.TryAddPassive("late"); relics.Unlock("relic-1"); relics.Equip("relic-1");
            Assert.That(session.RecordDefeat("late"), Is.False); Assert.That(session.RecordWeaponDamage("a", 900), Is.False);
            Assert.That(session.AddCoins(10), Is.False); Assert.That(session.RecordItem("late", 1), Is.False);
            Assert.That(session.RecordSynergy("late"), Is.False); Assert.That(session.RecordBossReached("late"), Is.False);
            Assert.That(session.SetRecord("late", 0, 1), Is.False); Assert.That(session.SetChallengeProgress("late", 0, 1, 1), Is.False);
            var result = session.FinalizeResult();
            Assert.That(result.Build.Weapons["a"], Is.EqualTo(1)); Assert.That(result.Build.Passives, Is.Empty);
            Assert.That(result.Build.EquippedRelicIds, Is.Empty); Assert.That(result.EnemiesDefeated, Is.EqualTo(1));
            Assert.That(result.DamageByWeapon["a"], Is.EqualTo(4));
            Assert.That(session.FinalizeResult(), Is.SameAs(result));
        }

        [Test] public void ReadOnlyCollectionsRejectMutation()
        {
            build.TryAddWeapon("a"); session.RecordDefeat("ant"); session.RecordWeaponDamage("a", 1); session.RecordItem("item", 1);
            session.RecordSynergy("s"); session.SetRecord("r", 0, 1); session.SetChallengeProgress("c", 0, 1, 2);
            var result = Lose();
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, int>)result.Build.Weapons).Add("b", 1));
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, int>)result.DefeatsByEnemy).Clear());
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, double>)result.DamageByWeapon)["a"] = 99);
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, int>)result.Items).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<string>)result.Synergies).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<BuildChange>)result.Build.History).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<string>)result.Build.EquippedRelicIds).Add("late"));
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, RunRecordProgress>)result.Records).Clear());
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, RunChallengeProgress>)result.Challenges).Clear());
        }

        [TestCase(RunPhase.Paused)] [TestCase(RunPhase.ChoosingUpgrade)]
        public void FrozenCombatPhasesRejectFactsWithoutResettingStreak(RunPhase phase)
        {
            session.RecordDefeat("ant"); run.TryTransition(phase);
            Assert.That(session.RecordDefeat("ant"), Is.False); Assert.That(session.BreakStreak(), Is.False);
            Assert.That(session.RecordWeaponDamage("a", 1), Is.False); Assert.That(session.AddCoins(1), Is.False);
            run.Advance(50); run.TryTransition(RunPhase.Playing); session.RecordDefeat("ant");
            var result = Lose(); Assert.That(result.EnemiesDefeated, Is.EqualTo(2));
            Assert.That(result.LongestStreak, Is.EqualTo(2)); Assert.That(result.DurationSeconds, Is.Zero);
        }

        [Test] public void BossPhaseAutomaticallyMarksReachedWithoutInferringFromClock()
        {
            run.Advance(901); Assert.That(session.FinalizeResult(), Is.Null);
            run.TryTransition(RunPhase.Boss); var result = Lose(); Assert.That(result.BossReached, Is.True);
        }
        [Test] public void LongElapsedTimeAloneDoesNotMarkBossReached()
        { run.Advance(901); Assert.That(Lose().BossReached, Is.False); }

        [Test] public void DefaultsContainAllEmptyCollectionsAndZeroStatistics()
        {
            var result = Lose();
            Assert.That(result.BossReached, Is.False); Assert.That(result.BossId, Is.Empty);
            Assert.That(result.EnemiesDefeated, Is.Zero); Assert.That(result.LongestStreak, Is.Zero); Assert.That(result.Coins, Is.Zero);
            Assert.That(result.Build.Weapons, Is.Empty); Assert.That(result.Build.Passives, Is.Empty); Assert.That(result.Build.Evolutions, Is.Empty);
            Assert.That(result.DamageByWeapon, Is.Empty); Assert.That(result.DefeatsByEnemy, Is.Empty); Assert.That(result.Synergies, Is.Empty);
            Assert.That(result.Items, Is.Empty); Assert.That(result.Records, Is.Empty); Assert.That(result.Challenges, Is.Empty);
        }

        [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(-1)] [TestCase(0)]
        public void InvalidDamageIsRejectedWithoutMutation(double amount)
        { Assert.That(session.RecordWeaponDamage("a", amount), Is.False); Assert.That(Lose().DamageByWeapon, Is.Empty); }

        [TestCase(null)] [TestCase("")] [TestCase(" ")]
        public void InvalidIdsAreRejected(string id)
        {
            Assert.That(session.RecordDefeat(id), Is.False); Assert.That(session.RecordBossReached(id), Is.False);
            Assert.That(session.RecordWeaponDamage(id, 1), Is.False); Assert.That(session.RecordItem(id, 1), Is.False);
            Assert.That(session.RecordSynergy(id), Is.False); Assert.That(session.SetRecord(id, 0, 1), Is.False);
            Assert.That(session.SetChallengeProgress(id, 0, 1, 2), Is.False); Assert.That(Lose().EnemiesDefeated, Is.Zero);
        }

        [Test] public void OverflowingCountersAndDamageAreRejectedAtomically()
        {
            Assert.That(session.AddCoins(int.MaxValue), Is.True); Assert.That(session.AddCoins(1), Is.False);
            session.RecordItem("a", int.MaxValue); Assert.That(session.RecordItem("a", 1), Is.False);
            session.RecordWeaponDamage("a", double.MaxValue); Assert.That(session.RecordWeaponDamage("a", double.MaxValue), Is.False);
            run.TryTransition(RunPhase.Boss); run.TryTransition(RunPhase.Won); var result = session.FinalizeResult();
            Assert.That(result.Coins, Is.EqualTo(int.MaxValue)); Assert.That(result.Items["a"], Is.EqualTo(int.MaxValue));
            Assert.That(result.DamageByWeapon["a"], Is.EqualTo(double.MaxValue));
        }

        [Test] public void InvalidRewardQuantitiesDoNotMutateSession()
        {
            Assert.That(session.AddCoins(0), Is.False); Assert.That(session.AddCoins(-1), Is.False);
            Assert.That(session.RecordItem("a", 0), Is.False); Assert.That(session.RecordItem("a", -1), Is.False);
            Assert.That(Lose().Items, Is.Empty);
        }

        [Test] public void ProgressIsMonotonicWithStableBaselineAndTarget()
        {
            session.SetRecord("r", 4, 2); Assert.That(session.SetRecord("r", 4, 1), Is.False);
            Assert.That(session.SetRecord("r", 3, 5), Is.False); session.SetRecord("r", 4, 5);
            session.SetChallengeProgress("c", 2, 3, 5);
            Assert.That(session.SetChallengeProgress("c", 2, 2, 5), Is.False);
            Assert.That(session.SetChallengeProgress("c", 0, 4, 5), Is.False);
            Assert.That(session.SetChallengeProgress("c", 2, 4, 6), Is.False);
            Assert.That(session.SetChallengeProgress("c", 2, 6, 5), Is.False);
            session.SetChallengeProgress("c", 2, 5, 5);
            var result = Lose(); Assert.That(result.Records["r"].Current, Is.EqualTo(5));
            Assert.That(result.Challenges["c"].Current, Is.EqualTo(5));
        }

        [Test] public void InvalidProgressIsRejected()
        {
            Assert.That(session.SetRecord("r", double.NaN, 1), Is.False);
            Assert.That(session.SetRecord("r", 0, double.PositiveInfinity), Is.False);
            Assert.That(session.SetRecord("r", -1, 0), Is.False);
            Assert.That(session.SetChallengeProgress("c", -1, 1, 2), Is.False);
            Assert.That(session.SetChallengeProgress("c", 1, 0, 2), Is.False);
            Assert.That(session.SetChallengeProgress("c", 0, 0, 0), Is.False);
            var result = Lose(); Assert.That(result.Challenges, Is.Empty); Assert.That(result.Records, Is.Empty);
        }

        [Test] public void DuplicateSynergiesAndProgressAreIdempotent()
        {
            session.RecordSynergy("z"); session.RecordSynergy("a"); Assert.That(session.RecordSynergy("a"), Is.False);
            session.SetRecord("r", 2, 2); session.SetRecord("r", 2, 2);
            session.SetChallengeProgress("c", 1, 2, 3); session.SetChallengeProgress("c", 1, 2, 3);
            var result = Lose(); CollectionAssert.AreEqual(new[] { "a", "z" }, result.Synergies);
            Assert.That(result.Records["r"].IsNewRecord, Is.False); Assert.That(result.Challenges["c"].Gained, Is.EqualTo(1));
        }

        [TestCase(0, 0)] [TestCase(1, 101)] [TestCase(0.25, 25)]
        public void AbandonmentFactorIsAppliedOnceOnlyToCoins(double factor, int expected)
        {
            session.Dispose(); session = new RunResultSession(run, 101, 500, factor);
            session.RecordItem("item", 2); run.TryTransition(RunPhase.Abandoned);
            var result = session.FinalizeResult(); Assert.That(result.Coins, Is.EqualTo(expected));
            Assert.That(result.VictoryBonus, Is.Zero); Assert.That(result.Items["item"], Is.EqualTo(2));
            Assert.That(session.FinalizeResult(), Is.SameAs(result));
        }

        [TestCase(-0.1)] [TestCase(1.1)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)]
        public void InvalidRewardPolicyIsRejected(double factor)
        { Assert.Throws<ArgumentOutOfRangeException>(() => new RunResultSession(run, 0, 0, factor)); }

        [Test] public void InvalidConstructorArgumentsAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new RunResultSession(null, 0, 0, 0.5));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunResultSession(run, -1, 0, 0.5));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunResultSession(run, 0, -1, 0.5));
        }

        [Test] public void DisposingDetachesAndPreventsFutureRecordingOrFinalization()
        {
            session.Dispose(); Assert.That(session.RecordDefeat("a"), Is.False);
            run.TryTransition(RunPhase.Lost); Assert.That(session.FinalizeResult(), Is.Null);
        }
        [Test] public void DisposingCompletedSessionPreservesItsResult()
        { var result = Lose(); session.Dispose(); Assert.That(session.FinalizeResult(), Is.SameAs(result)); }
    }
}
