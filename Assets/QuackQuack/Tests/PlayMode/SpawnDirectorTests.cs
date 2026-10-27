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
using QuackQuack.Systems.Spawning;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace QuackQuack.Tests.PlayMode
{
    public class SpawnDirectorTests
    {
        private PoolService pools;
        private RunState run;
        private GameplayEventBus events;
        private GameObject target;
        private EnemyDefinition[] definitions;
        private readonly List<GameObject> prefabs=new List<GameObject>();
        private readonly List<EnemyReward> rewards=new List<EnemyReward>();
        private readonly List<SpawnDirector> directors=new List<SpawnDirector>();
        private readonly Rect visible=new Rect(-2,-2,4,4), region=new Rect(10,10,10,10);
        [SetUp]
        public void SetUp()
        {
            pools=new PoolService(); run=new RunState(54); run.TryTransition(RunPhase.Playing);
            events=new GameplayEventBus(); target=new GameObject("Spawn target fixture"); rewards.Clear();
            definitions=new[] {Make("ant"),Make("wasp")};
            foreach(var definition in definitions) PooledEnemy.RegisterPool(pools,definition);
        }
        [TearDown]
        public void Cleanup()
        {
            foreach(var director in directors) director.Dispose(); directors.Clear(); pools.Dispose(); events.Clear();
            foreach(var prefab in prefabs) Object.DestroyImmediate(prefab); prefabs.Clear();
            foreach(var definition in definitions) Object.DestroyImmediate(definition); Object.DestroyImmediate(target);
        }
        private EnemyDefinition Make(string id)
        {
            var prefab=new GameObject("Pooled "+id);prefab.AddComponent<PooledEnemy>();prefab.SetActive(false);prefabs.Add(prefab);
            var definition=ScriptableObject.CreateInstance<EnemyDefinition>(); var s=new SerializedObject(definition);
            s.FindProperty("id").stringValue=id;s.FindProperty("displayName").stringValue=id;s.FindProperty("prefab").objectReferenceValue=prefab;
            s.FindProperty("poolInitialSize").intValue=3;s.FindProperty("poolMaxSize").intValue=3;s.ApplyModifiedPropertiesWithoutUndo();return definition;
        }
        private SpawnDirector Director(int limit=3,int budget=8,int queue=4,int seed=54)
        {
            var value=new SpawnDirector(run,pools,events,target.transform,definitions,limit,queue,budget,16,1,seed,rewards.Add);
            directors.Add(value);return value;
        }
        private SpawnRequest Request(long id,int quantity=1,string enemy="ant",long deadline=10,EventPriority priority=EventPriority.Feedback) =>
            new SpawnRequest(id,"wave",enemy,quantity,region,5,priority,deadline);

        [Test]
        public void ActiveLimitReducesBatchWithoutCreatingOutsidePool()
        {
            var director=Director();int before=Object.FindObjectsByType<PooledEnemy>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;
            Assert.That(director.TryEnqueue(Request(1,10)),Is.True);Assert.That(director.Process(0,visible),Is.EqualTo(3));
            Assert.That(director.ActiveCount,Is.EqualTo(3));Assert.That(director.PendingCount,Is.Zero);Assert.That(director.ReducedCount,Is.EqualTo(7));
            Assert.That(Object.FindObjectsByType<PooledEnemy>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length,Is.EqualTo(before));
            Assert.That(rewards,Is.Empty);
        }

        [Test]
        public void GlobalLimitCountsExternalEnemiesAndDisposePreservesTheirLeases()
        {
            var external=PooledEnemy.Spawn(pools,definitions[0],run,events,target.transform,Vector2.one,rewards.Add);
            var director=Director(limit:2);director.TryEnqueue(Request(1,4));Assert.That(director.Process(0,visible),Is.EqualTo(1));
            Assert.That(pools.GetActiveCount(PoolFamily.Enemy),Is.EqualTo(2));director.Dispose();
            Assert.That(pools.IsActive(external,PoolFamily.Enemy),Is.True);Assert.That(pools.ActiveCount,Is.EqualTo(1));
        }

        [Test]
        public void ExpirationPrecedesSpawnAndDeadlineItselfRemainsValid()
        {
            var director=Director();director.TryEnqueue(Request(1,3,deadline:4));Assert.That(director.Process(5,visible),Is.Zero);
            Assert.That(director.ExpiredCount,Is.EqualTo(3));Assert.That(director.ActiveCount,Is.Zero);
            director.TryEnqueue(Request(2,deadline:6));Assert.That(director.Process(6,visible),Is.EqualTo(1));
        }

        [Test]
        public void PriorityAndPerTickBudgetAreStableAndDoNotReplayRequest()
        {
            var director=Director(budget:1);director.TryEnqueue(Request(1));director.TryEnqueue(Request(2,enemy:"wasp",priority:EventPriority.Danger));
            Assert.That(director.Process(0,visible),Is.EqualTo(1));Assert.That(director.TryGetActive(0,out var first,out _),Is.True);
            Assert.That(first.Definition.Id,Is.EqualTo("wasp"));Assert.That(director.Process(0,visible),Is.Zero);
            Assert.That(director.TryEnqueue(Request(2)),Is.False);Assert.That(director.Process(1,visible),Is.EqualTo(1));
            Assert.Throws<ArgumentException>(()=>director.Process(0,visible));
        }

        [Test]
        public void FullQueueRejectsWithoutConsumingNewRequestIdAndUnknownEnemyIsRejected()
        {
            var director=Director(queue:1);Assert.That(director.TryEnqueue(Request(1)),Is.True);
            Assert.That(director.TryEnqueue(Request(2)),Is.False);director.Process(0,visible);
            Assert.That(director.TryEnqueue(Request(2)),Is.True);Assert.That(director.TryEnqueue(Request(3,enemy:"unknown")),Is.False);
        }

        [Test]
        public void PoolSaturationDropsDensityAndAllowsAnotherSpeciesToProgress()
        {
            var director=Director(limit:6);director.TryEnqueue(Request(1,5));director.TryEnqueue(Request(2,2,enemy:"wasp"));
            Assert.That(director.Process(0,visible),Is.EqualTo(5));Assert.That(director.ReducedCount,Is.EqualTo(2));
            Assert.That(director.ActiveCount,Is.EqualTo(5));Assert.That(director.PendingCount,Is.Zero);
        }

        [Test]
        public void PositionsAreInsideAllowedRegionOutsideExpandedViewAndRepeatForSameSeed()
        {
            var director=Director();director.TryEnqueue(Request(1,3));director.Process(0,visible);var positions=new Vector2[3];
            for(int i=0;i<3;i++)
            {
                Assert.That(director.TryGetActive(i,out var enemy,out long lease),Is.True);positions[i]=enemy.transform.position;
                Assert.That(region.Contains(positions[i]),Is.True);Assert.That(Vector2.Distance(positions[i],target.transform.position),Is.GreaterThanOrEqualTo(5));
                Assert.That(new Rect(-3,-3,6,6).Contains(positions[i]),Is.False);Assert.That(lease,Is.EqualTo(enemy.LeaseId));
            }
            director.Dispose();var repeat=Director();repeat.TryEnqueue(Request(1,3));repeat.Process(0,visible);
            for(int i=0;i<3;i++){repeat.TryGetActive(i,out var enemy,out _);Assert.That((Vector2)enemy.transform.position,Is.EqualTo(positions[i]));}
        }

        [Test]
        public void FullyExcludedRegionAndPlayerDistanceFailClosedWithoutAcquisition()
        {
            var director=Director();
            director.TryEnqueue(new SpawnRequest(1,"wave","ant",4,new Rect(2.1f,0,0.2f,0.2f),0,EventPriority.Danger,10));
            Assert.That(director.Process(0,visible),Is.Zero);Assert.That(director.ReducedCount,Is.EqualTo(4));
            director.TryEnqueue(new SpawnRequest(2,"wave","ant",2,region,100,EventPriority.Danger,10));
            target.transform.position=new Vector2(15,15);
            Assert.That(director.Process(1,visible),Is.Zero);Assert.That(pools.ActiveCount,Is.Zero);Assert.That(director.PendingCount,Is.Zero);
        }

        [TestCase(RunPhase.Paused)] [TestCase(RunPhase.ChoosingUpgrade)]
        public void SuspensionPreservesQueueAndDoesNotAdvanceExpiryOrTick(RunPhase phase)
        {
            var director=Director();director.TryEnqueue(Request(1,deadline:0));run.TryTransition(phase);
            Assert.That(director.Process(100,visible),Is.Zero);Assert.That(director.PendingCount,Is.EqualTo(1));
            run.TryTransition(RunPhase.Playing);Assert.That(director.Process(0,visible),Is.EqualTo(1));
        }

        [TestCase(RunPhase.Lost)] [TestCase(RunPhase.Abandoned)]
        public void TerminalProcessClearsOwnedLeasesAndPendingWithoutReward(RunPhase phase)
        {
            var director=Director(budget:1);director.TryEnqueue(Request(1,3));director.Process(0,visible);run.TryTransition(phase);
            Assert.That(director.Process(1,visible),Is.Zero);Assert.That(director.ActiveCount,Is.Zero);Assert.That(director.PendingCount,Is.Zero);
            Assert.That(pools.ActiveCount,Is.Zero);Assert.That(rewards,Is.Empty);Assert.That(director.TryEnqueue(Request(2)),Is.False);
        }

        [Test]
        public void ReacquiredExternalLeaseIsNeverReleasedByOldDirectorSlot()
        {
            var director=Director();director.TryEnqueue(Request(1));director.Process(0,visible);director.TryGetActive(0,out var old,out long lease);
            old.Despawn(lease);var external=PooledEnemy.Spawn(pools,definitions[0],run,events,target.transform,Vector2.one,rewards.Add);
            Assert.That(external,Is.SameAs(old));director.Dispose();Assert.That(pools.IsActive(external,PoolFamily.Enemy),Is.True);
        }

        [Test]
        public void InvalidConfigurationBoundsAndDisposedServicesRejectSafely()
        {
            Assert.Throws<ArgumentException>(()=>Director(limit:0));var director=Director();
            Assert.That(director.TryEnqueue(default),Is.False);Assert.Throws<ArgumentException>(()=>director.Process(0,default));
            pools.Dispose();Assert.That(director.TryEnqueue(Request(1)),Is.False);Assert.That(director.Process(0,visible),Is.Zero);
            director.Dispose();Assert.That(director.Process(1,visible),Is.Zero);
        }

        [Test]
        public void DeferredRemainderExpiresWithoutCatchUpBurst()
        {
            var director=Director(budget:1);director.TryEnqueue(Request(1,3,deadline:0));
            Assert.That(director.Process(0,visible),Is.EqualTo(1));Assert.That(director.PendingCount,Is.EqualTo(1));
            Assert.That(director.Process(1,visible),Is.Zero);Assert.That(director.ExpiredCount,Is.EqualTo(2));
            Assert.That(director.PendingCount,Is.Zero);Assert.That(director.ActiveCount,Is.EqualTo(1));
        }
    }
}
#endif
