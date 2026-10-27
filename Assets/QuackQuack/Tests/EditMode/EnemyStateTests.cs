using NUnit.Framework;
using QuackQuack.Core.Combat;
using UnityEngine;

namespace QuackQuack.Tests.EditMode
{
    public class EnemyStateTests
    {
        private EnemyState state;
        [SetUp] public void SetUp() => state = new EnemyState();
        private void Activate() => Assert.That(state.TryActivate(1, "ant", 10, 3, 2, new Vector2(2, 3), 17), Is.True);

        [Test]
        public void ActivationCapturesValuesAndCannotOverwriteActiveLease()
        {
            Activate(); Assert.That(state.Health, Is.EqualTo(10)); Assert.That(state.MaxHealth, Is.EqualTo(10));
            Assert.That(state.TargetId, Is.EqualTo(17)); Assert.That(state.DefinitionId, Is.EqualTo("ant"));
            Assert.That(state.TryActivate(2,"other",20,9,9,Vector2.zero,0), Is.False);
            Assert.That(state.Health, Is.EqualTo(10)); Assert.That(state.LeaseId, Is.EqualTo(1));
        }

        [Test]
        public void DamageClampsToRemainingHealthAndReservesOneReward()
        {
            Activate(); Assert.That(state.TryDamage(1, 4, out float applied, out var reward), Is.True);
            Assert.That(applied, Is.EqualTo(4)); Assert.That(reward.HasValue, Is.False);
            Assert.That(state.Health, Is.EqualTo(6));
            Assert.That(state.TryDamage(1, 100, out applied, out reward), Is.True);
            Assert.That(applied, Is.EqualTo(6)); Assert.That(state.Health, Is.Zero);
            Assert.That(state.IsDefeated && state.RewardClaimed, Is.True);
            Assert.That(reward.Value.LeaseId, Is.EqualTo(1)); Assert.That(reward.Value.DefinitionId, Is.EqualTo("ant"));
            Assert.That(reward.Value.Experience, Is.EqualTo(3)); Assert.That(reward.Value.Coins, Is.EqualTo(2));
            Assert.That(reward.Value.Position, Is.EqualTo(new Vector2(2,3)));
            for(int i=0;i<3;i++) { Assert.That(state.TryDamage(1,1,out applied,out reward),Is.False); Assert.That(reward.HasValue,Is.False); }
        }

        [TestCase(0f)] [TestCase(-1f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(float.NegativeInfinity)]
        public void InvalidDamageDoesNotChangeHealthOrClaimReward(float amount)
        {
            Activate(); Assert.That(state.TryDamage(1, amount, out float applied, out var reward), Is.False);
            Assert.That(applied,Is.Zero); Assert.That(reward.HasValue,Is.False);
            Assert.That(state.Health,Is.EqualTo(10)); Assert.That(state.RewardClaimed,Is.False);
        }

        [Test]
        public void ResetClearsEffectsReferencesCountersAndRejectsOldLeaseAfterReuse()
        {
            Activate(); state.TrySetControl(1,new Vector2(5,2),3);
            Assert.That(state.Reset(2),Is.False); Assert.That(state.Reset(1),Is.True);
            Assert.That(state.IsActive,Is.False); Assert.That(state.DefinitionId,Is.Null); Assert.That(state.Health,Is.Zero);
            Assert.That(state.MaxHealth,Is.Zero); Assert.That(state.TargetId,Is.Zero); Assert.That(state.LeaseId,Is.Zero);
            Assert.That(state.Position,Is.EqualTo(Vector2.zero)); Assert.That(state.Knockback,Is.EqualTo(Vector2.zero));
            Assert.That(state.InterruptionRemaining,Is.Zero); Assert.That(state.RewardClaimed,Is.False);
            Assert.That(state.TryActivate(1,"ant",10,3,2,Vector2.zero,0),Is.False);
            Assert.That(state.TryActivate(2,"wasp",20,7,4,Vector2.one,0),Is.True);
            Assert.That(state.TryDamage(1,100,out _,out _),Is.False); Assert.That(state.Reset(1),Is.False);
            Assert.That(state.TrySetControl(1,Vector2.one,2),Is.False); Assert.That(state.TrySetPosition(1,Vector2.one),Is.False);
            Assert.That(state.Health,Is.EqualTo(20));
        }

        [Test]
        public void InvalidActivationAndControlAreAtomic()
        {
            Assert.That(state.TryActivate(0,"ant",10,1,0,Vector2.zero,0),Is.False);
            Assert.That(state.TryActivate(1," ",10,1,0,Vector2.zero,0),Is.False);
            Assert.That(state.TryActivate(1,"ant",float.NaN,1,0,Vector2.zero,0),Is.False);
            Assert.That(state.TryActivate(1,"ant",10,-1,0,Vector2.zero,0),Is.False);
            Assert.That(state.TryActivate(1,"ant",10,1,-1,Vector2.zero,0),Is.False);
            Assert.That(state.TryActivate(1,"ant",10,1,0,new Vector2(float.PositiveInfinity,0),0),Is.False);
            Activate(); Assert.That(state.TrySetControl(1,Vector2.one,float.NaN),Is.False);
            Assert.That(state.TrySetControl(1,new Vector2(float.NaN,0),1),Is.False);
            Assert.That(state.Knockback,Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void DespawnWithoutDefeatCannotProduceRewardAndNextLeaseHasOwnReward()
        {
            Activate(); state.Reset(1); Assert.That(state.TryDamage(1,100,out _,out _),Is.False);
            Assert.That(state.TryActivate(2,"ant",1,0,0,Vector2.zero,0),Is.True);
            Assert.That(state.TryDamage(2,1,out _,out var reward),Is.True);
            Assert.That(reward.HasValue,Is.True); Assert.That(reward.Value.Experience,Is.Zero);
        }
    }
}
