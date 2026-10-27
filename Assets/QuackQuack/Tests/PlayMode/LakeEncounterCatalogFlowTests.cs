#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using QuackQuack.Content.Catalogs;
using QuackQuack.Content.Characters;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Run;
using QuackQuack.Core.Shared;
using QuackQuack.Gameplay.Enemies;
using QuackQuack.Gameplay.Player;
using QuackQuack.Platform.Input;
using QuackQuack.Systems.Combat;
using QuackQuack.Systems.Pooling;
using QuackQuack.Systems.Spawning;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuackQuack.Tests.PlayMode
{
    public class LakeEncounterCatalogFlowTests
    {
        private sealed class NoInput:IPlayerInput{public PlayerCommand Read()=>default;}
        private GameObject root,target;
        private LakeEncounterCatalog catalog;
        private PoolService pools;
        private RunState run;
        private GameplayEventBus events;
        private SpawnDirector director;
        [SetUp]public void SetUp()
        {
            root=new GameObject("Catalog flow fixture");target=new GameObject("Target");target.transform.SetParent(root.transform);
            catalog=AssetDatabase.LoadAssetAtPath<LakeEncounterCatalog>("Assets/QuackQuack/Content/Catalogs/LakeEncounterCatalog.asset");
            pools=new PoolService();run=new RunState(56);run.TryTransition(RunPhase.Playing);events=new GameplayEventBus();
            catalog.RegisterPools(pools,root.transform);director=catalog.CreateDirector(run,pools,events,target.transform,56,r=>{});
        }
        [TearDown]public void Cleanup()
        {
            var scheduler=root.GetComponent<EnemyUpdateScheduler>();if(scheduler!=null)scheduler.enabled=false;
            director.Dispose();pools.Dispose();events.Clear();Object.DestroyImmediate(root);
        }
        [Test]
        public void AuthoredPoolsSpawnAllSevenCanonicalDefinitionsWithoutExtraInstances()
        {
            int before=Object.FindObjectsByType<PooledEnemy>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;
            var all=catalog.GetAllEnemies();for(int i=0;i<all.Length;i++)Assert.That(director.TryEnqueue(new SpawnRequest(i+1,"catalog-fixture",all[i].Id,1,new Rect(10,10,5,5),4,EventPriority.Essential,100)),Is.True);
            Assert.That(director.Process(0,new Rect(-1,-1,2,2)),Is.EqualTo(7));Assert.That(director.ActiveCount,Is.EqualTo(7));
            Assert.That(Object.FindObjectsByType<PooledEnemy>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length,Is.EqualTo(before));
            var seen=new HashSet<string>();for(int i=0;i<director.Capacity;i++)if(director.TryGetActive(i,out var enemy,out _))seen.Add(enemy.Definition.Id);
            Assert.That(seen.Count,Is.EqualTo(7));
        }
        [Test]
        public void BothAuthoredMinibossesUseRealWarningAssetsAndSharedTacticalComposition()
        {
            var player=target.AddComponent<PlayerController>();player.Initialize(AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/QuackQuack/Content/Characters/Pato.asset"),new NoInput(),run,events);
            player.GetComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Kinematic;
            var warnings=new List<PresentationEvent>();events.Presentation+=value=>{if(value.Type==PresentationEventType.DangerTelegraphed)warnings.Add(value);};
            for(int i=0;i<2;i++)director.TryEnqueue(new SpawnRequest(i+1,"miniboss-fixture",catalog.MiniBosses[i].Id,1,new Rect(3,0,0.1f,0.1f),2,EventPriority.Essential,100));
            Assert.That(director.Process(0,new Rect(-0.5f,-0.5f,1,1)),Is.EqualTo(2));Physics2D.SyncTransforms();
            var scheduler=root.AddComponent<EnemyUpdateScheduler>();scheduler.AutomaticTick=false;
            scheduler.Initialize(run,director,pools,events,player,catalog.BehaviorSettings,catalog.GetAllEnemies());scheduler.Advance(0);
            Assert.That(warnings.Count,Is.EqualTo(2));Assert.That(scheduler.ActiveEffectCount,Is.EqualTo(2));
            foreach(var effect in Object.FindObjectsByType<EnemyAreaEffect>(FindObjectsSortMode.None))
            {if(!effect.Warning)continue;Assert.That(effect.GetComponent<LineRenderer>().sharedMaterial,Is.Not.Null);Assert.That(effect.GetComponent<AudioSource>().clip,Is.Not.Null);}
            run.Advance(1.01f);scheduler.Advance(1.01f);scheduler.Advance(0);
            Assert.That(player.EnemySlowMultiplier,Is.EqualTo(0.65f));
        }
    }
}
#endif
