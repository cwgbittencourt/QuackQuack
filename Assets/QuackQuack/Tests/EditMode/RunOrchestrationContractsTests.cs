using NUnit.Framework;
using QuackQuack.Core.Run;
using QuackQuack.Systems.Run;
namespace QuackQuack.Tests.EditMode
{
    public class RunOrchestrationContractsTests
    {
        [Test] public void ProductionDirectorExists() => Assert.That(typeof(GameBootstrap).Assembly.GetType("QuackQuack.Systems.Run.RunDirector"), Is.Not.Null);
        [TestCase(RunPhase.Paused)] [TestCase(RunPhase.ChoosingUpgrade)]
        public void BossCanSuspendAndResumeWithoutReturningToCommonWaves(RunPhase modal)
        {
            var run = new RunState(60); run.TryTransition(RunPhase.Playing); run.TryTransition(RunPhase.Boss);
            Assert.That(run.TryTransition(modal), Is.True); run.Advance(20);
            Assert.That(run.TryTransition(RunPhase.Playing), Is.False);
            Assert.That(run.TryTransition(RunPhase.Boss), Is.True); Assert.That(run.ElapsedSeconds, Is.Zero);
        }
        [Test] public void EncounterHealthScalingIsOncePerLeaseAndResetsOnReuse()
        {
            var enemy = new QuackQuack.Core.Combat.EnemyState();
            enemy.TryActivate(1, "ant", 8, 1, 1, UnityEngine.Vector2.zero, 1);
            Assert.That(enemy.TryScaleEncounter(1, 2), Is.True); Assert.That(enemy.MaxHealth, Is.EqualTo(16));
            Assert.That(enemy.TryScaleEncounter(1, 2), Is.False); enemy.Reset(1);
            enemy.TryActivate(2, "ant", 8, 1, 1, UnityEngine.Vector2.zero, 1);
            Assert.That(enemy.TryScaleEncounter(2, 3), Is.True); Assert.That(enemy.MaxHealth, Is.EqualTo(24));
        }
        [Test] public void SelectionMetadataIsCapturedBeforeAbandonmentWithoutAcceptingCombatFacts()
        {
            var run = new RunState(60); using var session = new RunResultSession(run, 0, 0, 0.5);
            run.TryTransition(RunPhase.Playing); run.TryTransition(RunPhase.ChoosingUpgrade);
            Assert.That(session.RecordSynergy("recipe"), Is.True); Assert.That(session.RecordItem("relic", 1), Is.True);
            Assert.That(session.RecordWeaponDamage("weapon", 3), Is.False); Assert.That(session.RecordDefeat("enemy"), Is.False);
            run.TryTransition(RunPhase.Abandoned); var result = session.FinalizeResult();
            Assert.That(result.Items["relic"], Is.EqualTo(1)); Assert.That(result.Synergies, Does.Contain("recipe"));
        }
    }
}
