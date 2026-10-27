#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using NUnit.Framework;
using QuackQuack.Content.Enemies;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Enemies;
using QuackQuack.Systems.Combat;
using QuackQuack.Systems.Pooling;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.PlayMode
{
    public class PooledEnemyTests
    {
        private PoolService pools;
        private EnemyDefinition definition;
        private GameObject prefab, target;
        private RunState run;
        private GameplayEventBus events;
        private readonly List<EnemyReward> rewards = new List<EnemyReward>();
        private readonly List<CombatEvent> combat = new List<CombatEvent>();

        [SetUp]
        public void SetUp()
        {
            rewards.Clear(); combat.Clear(); pools=new PoolService(); events=new GameplayEventBus(); events.Combat+=combat.Add;
            run=new RunState(53); run.TryTransition(RunPhase.Playing);
            prefab=new GameObject("Enemy prefab fixture"); prefab.AddComponent<PooledEnemy>(); prefab.SetActive(false);
            target=new GameObject("Enemy target fixture"); definition=ScriptableObject.CreateInstance<EnemyDefinition>();
            var s=new SerializedObject(definition); s.FindProperty("id").stringValue="ant";
            s.FindProperty("displayName").stringValue="Ant fixture"; s.FindProperty("prefab").objectReferenceValue=prefab;
            s.FindProperty("experienceReward").intValue=3; s.FindProperty("coinReward").intValue=2;
            s.FindProperty("poolInitialSize").intValue=1; s.FindProperty("poolMaxSize").intValue=1; s.ApplyModifiedPropertiesWithoutUndo();
            PooledEnemy.RegisterPool(pools,definition);
        }
        [TearDown]
        public void Cleanup()
        {
            pools.Dispose(); events.Clear(); Object.DestroyImmediate(prefab); Object.DestroyImmediate(target); Object.DestroyImmediate(definition);
        }
        private PooledEnemy Spawn(Action<EnemyReward> sink=null) => PooledEnemy.Spawn(pools,definition,run,events,target.transform,new Vector2(2,3),sink ?? rewards.Add);

        [Test]
        public void LethalHitReturnsToPoolBeforeOneRewardAndDefeatNotification()
        {
            var enemy=Spawn(); long lease=enemy.LeaseId; int defeated=0;
            events.Combat+=value=> { if(value.Type==CombatEventType.EntityDefeated) {defeated++; Assert.That(pools.IsActive(enemy,PoolFamily.Enemy),Is.False);} };
            Assert.That(enemy.TakeDamage(lease,100,7,1),Is.True);
            Assert.That(rewards.Count,Is.EqualTo(1)); Assert.That(defeated,Is.EqualTo(1));
            Assert.That(rewards[0].Experience,Is.EqualTo(3)); Assert.That(rewards[0].Coins,Is.EqualTo(2));
            Assert.That(rewards[0].Position,Is.EqualTo(new Vector2(2,3)));
            Assert.That(enemy.TakeDamage(lease,100,7,1),Is.False); Assert.That(pools.Release(enemy),Is.False);
            Assert.That(rewards.Count,Is.EqualTo(1)); Assert.That(combat.Count,Is.EqualTo(2));
        }

        [Test]
        public void ReentrantRewardCanReuseInstanceWithoutOldCallbackDamagingNewLease()
        {
            PooledEnemy next=null; long oldLease=0;
            var enemy=Spawn(reward=> { rewards.Add(reward); next=Spawn(); Assert.That(next.TakeDamage(oldLease,100,1,2),Is.False); });
            oldLease=enemy.LeaseId; enemy.TakeDamage(oldLease,100,1,1);
            Assert.That(next,Is.SameAs(enemy)); Assert.That(next.LeaseId,Is.GreaterThan(oldLease));
            Assert.That(next.State.Health,Is.EqualTo(definition.Health)); Assert.That(rewards.Count,Is.EqualTo(1));
            next.TakeDamage(next.LeaseId,100,1,3);
            Assert.That(rewards.Count,Is.EqualTo(2),"Reuse must use the new reward consumer, without retaining the previous callback.");
        }

        [Test]
        public void SaturationDoesNotCreateExtraInstanceAndReuseResetsAllLeaseData()
        {
            var first=Spawn(); long lease=first.LeaseId;
            Assert.That(Spawn(),Is.Null); Assert.That(pools.GetActiveCount(PoolFamily.Enemy),Is.EqualTo(1));
            first.State.TrySetControl(lease,Vector2.one,4); first.GetComponent<Rigidbody2D>().linearVelocity=Vector2.one;
            Assert.That(first.Despawn(lease),Is.True);
            Assert.That(first.Definition,Is.Null); Assert.That(first.Target,Is.Null); Assert.That(first.State.DefinitionId,Is.Null);
            Assert.That(first.State.InterruptionRemaining,Is.Zero); Assert.That(first.State.Knockback,Is.EqualTo(Vector2.zero));
            Assert.That(first.GetComponent<Rigidbody2D>().linearVelocity,Is.EqualTo(Vector2.zero));
            Assert.That(first.gameObject.activeSelf,Is.False); Assert.That(rewards,Is.Empty);
            var second=Spawn(); Assert.That(second,Is.SameAs(first)); Assert.That(second.LeaseId,Is.GreaterThan(lease));
            Assert.That(second.Despawn(lease),Is.False); second.TakeDamage(second.LeaseId,100,1,1);
            Assert.That(rewards.Count,Is.EqualTo(1));
        }

        [TestCase(RunPhase.Paused)] [TestCase(RunPhase.ChoosingUpgrade)] [TestCase(RunPhase.Lost)] [TestCase(RunPhase.Abandoned)]
        public void SuspendedOrTerminalRunRejectsDamageAndNewSpawns(RunPhase phase)
        {
            var enemy=Spawn(); long lease=enemy.LeaseId; run.TryTransition(phase);
            Assert.That(enemy.TakeDamage(lease,100,1,1),Is.False); Assert.That(Spawn(),Is.Null);
            Assert.That(enemy.State.Health,Is.EqualTo(definition.Health)); Assert.That(rewards,Is.Empty);
        }

        [Test]
        public void ForeignPoolAndUnacquiredObjectsCannotInitialize()
        {
            var raw=prefab.GetComponent<PooledEnemy>();
            Assert.Throws<InvalidOperationException>(()=>raw.Initialize(pools,definition,run,events,null,Vector2.zero,rewards.Add));
            var enemy=pools.Acquire<PooledEnemy>(definition.PoolKey);
            using(var foreign=new PoolService()) Assert.Throws<InvalidOperationException>(()=>enemy.Initialize(foreign,definition,run,events,null,Vector2.zero,rewards.Add));
            pools.Release(enemy); Assert.That(rewards,Is.Empty);
        }

        [Test]
        public void FailedSpawnConfigurationReleasesAcquiredSlot()
        {
            Assert.Throws<ArgumentException>(()=>PooledEnemy.Spawn(pools,definition,run,null,target.transform,Vector2.zero,rewards.Add));
            Assert.That(pools.ActiveCount,Is.Zero); Assert.That(Spawn(),Is.Not.Null);
        }

        [Test]
        public void DisposingPoolCleansActiveEnemyWithoutRewardOrLateDamage()
        {
            var enemy=Spawn(); long lease=enemy.LeaseId; pools.Dispose();
            Assert.That(enemy.IsReady,Is.False); Assert.That(enemy.State.IsActive,Is.False);
            Assert.That(enemy.Target,Is.Null); Assert.That(enemy.Definition,Is.Null);
            Assert.That(enemy.TakeDamage(lease,100,1,1),Is.False); Assert.That(rewards,Is.Empty);
        }

        [Test]
        public void ObserverExceptionCannotLeaveDeadEnemyActiveOrLoseRewardCallback()
        {
            var enemy=Spawn(); long lease=enemy.LeaseId;
            events.Combat+=value=>throw new InvalidOperationException("Observer fixture failure");
            Assert.Throws<InvalidOperationException>(()=>enemy.TakeDamage(lease,100,1,1));
            Assert.That(pools.ActiveCount,Is.Zero); Assert.That(enemy.State.IsActive,Is.False);
            Assert.That(rewards.Count,Is.EqualTo(1)); Assert.That(enemy.TakeDamage(lease,100,1,1),Is.False);
        }

        [Test]
        public void NonlethalHitsUseDistinctCorrelationsAndDoNotGrantRewards()
        {
            var enemy=Spawn(); long lease=enemy.LeaseId;
            Assert.That(enemy.TakeDamage(lease,1,1,1),Is.True);
            Assert.That(enemy.TakeDamage(lease,1,1,1),Is.True);
            Assert.That(combat.Count,Is.EqualTo(2)); Assert.That(combat[0].CorrelationId,Is.Not.EqualTo(combat[1].CorrelationId));
            Assert.That(enemy.State.Health,Is.EqualTo(definition.Health-2)); Assert.That(rewards,Is.Empty);
        }
    }
}
#endif
