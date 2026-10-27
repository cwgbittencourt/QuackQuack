#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Content.Characters;
using QuackQuack.Content.Enemies;
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
    public class EnemyUpdateSchedulerTests
    {
        private sealed class NoInput:IPlayerInput{public PlayerCommand Read()=>default;}
        private GameObject root;
        private PoolService pools;private RunState run;private GameplayEventBus events;
        private PlayerController player;private SpawnDirector director;private EnemyUpdateScheduler scheduler;
        private EnemyBehaviorSettings settings;private EnemyDefinition[] definitions;private AudioClip clip;private Material material;
        private readonly List<PresentationEvent> warnings=new List<PresentationEvent>();
        private long request;
        [SetUp]
        public void SetUp()
        {
            warnings.Clear();request=0;root=new GameObject("Scheduler fixture");pools=new PoolService();run=new RunState(55);run.TryTransition(RunPhase.Playing);
            events=new GameplayEventBus();events.Presentation+=e=>{if(e.Type==PresentationEventType.DangerTelegraphed)warnings.Add(e);};
            var playerRoot=new GameObject("Player");playerRoot.transform.SetParent(root.transform);player=playerRoot.AddComponent<PlayerController>();
            player.Initialize(AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/QuackQuack/Content/Characters/Pato.asset"),new NoInput(),run,events);
            player.GetComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Kinematic;
            clip=AudioClip.Create("Enemy warning fixture",44100,1,44100,false);var samples=new float[44100];
            for(int i=0;i<samples.Length;i++)samples[i]=Mathf.Sin(i*2*Mathf.PI*440/44100)*0.1f;clip.SetData(samples,0);
            material=new Material(Shader.Find("Sprites/Default"));
            definitions=new EnemyDefinition[5];
            for(int i=0;i<5;i++)
            {
                var prefab=new GameObject("Enemy "+i);prefab.transform.SetParent(root.transform);prefab.layer=9;prefab.AddComponent<PooledEnemy>();prefab.SetActive(false);
                var visual=new GameObject("Warning "+i);visual.transform.SetParent(root.transform);visual.AddComponent<LineRenderer>().sharedMaterial=material;visual.SetActive(false);
                var d=ScriptableObject.CreateInstance<EnemyDefinition>();definitions[i]=d;
                Set(d,"id",p=>p.stringValue=((EnemyRole)i).ToString());Set(d,"displayName",p=>p.stringValue=((EnemyRole)i).ToString());Set(d,"role",p=>p.intValue=i);
                Set(d,"prefab",p=>p.objectReferenceValue=prefab);Set(d,"telegraphVisual",p=>p.objectReferenceValue=visual);Set(d,"telegraphAudio",p=>p.objectReferenceValue=clip);
                Set(d,"poolInitialSize",p=>p.intValue=2);Set(d,"poolMaxSize",p=>p.intValue=2);
                Set(d,"telegraphSeconds",p=>p.floatValue=0.2f);Set(d,"effectDuration",p=>p.floatValue=1);
                PooledEnemy.RegisterPool(pools,d,root.transform);
            }
            settings=ScriptableObject.CreateInstance<EnemyBehaviorSettings>();Set(settings,"steeringBudget",p=>p.intValue=1);
            Set(settings,"effectCapacityPerType",p=>p.intValue=1);Set(settings,"fieldRadius",p=>p.floatValue=3);
            director=new SpawnDirector(run,pools,events,player.transform,definitions,8,8,8,8,0,55,r=>{});
            scheduler=root.AddComponent<EnemyUpdateScheduler>();scheduler.AutomaticTick=false;
            scheduler.Initialize(run,director,pools,events,player,settings,definitions);
        }
        [TearDown]
        public void Cleanup()
        {
            scheduler.enabled=false;director.Dispose();pools.Dispose();Object.DestroyImmediate(root);
            foreach(var d in definitions)Object.DestroyImmediate(d);Object.DestroyImmediate(settings);Object.DestroyImmediate(clip);Object.DestroyImmediate(material);events.Clear();
        }
        private PooledEnemy Spawn(EnemyRole role)
        {
            var d=definitions[(int)role];long id=++request;
            Assert.That(director.TryEnqueue(new SpawnRequest(id,"test",d.Id,1,new Rect(2,0,0.1f,0.1f),1,EventPriority.Danger,100)),Is.True);
            Assert.That(director.Process(id,new Rect(-0.5f,-0.5f,1,1)),Is.EqualTo(1));Physics2D.SyncTransforms();
            PooledEnemy result=null;for(int i=0;i<director.Capacity;i++)if(director.TryGetActive(i,out var e,out _)&&e.Definition==d)result=e;
            return result;
        }
        private void Advance(float seconds){run.Advance(seconds);scheduler.Advance(seconds);Physics2D.SyncTransforms();}

        [Test]
        public void SwarmPursuesAndSteeringIsBudgetedWithoutPerEnemyUpdate()
        {
            var first=Spawn(EnemyRole.Swarm);var second=Spawn(EnemyRole.Swarm);Vector2 a=first.Body.position,b=second.Body.position;
            Advance(0.1f);Assert.That(first.Body.position.x,Is.LessThan(a.x));Assert.That(second.Body.position.x,Is.LessThan(b.x));
            Assert.That(scheduler.LastSteeringCount,Is.LessThanOrEqualTo(1));
            Assert.That(typeof(PooledEnemy).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public),Is.Null);
            Assert.That(typeof(EnemyAreaEffect).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public),Is.Null);
        }
        [Test]
        public void ChargerWarnsWithRealVisualAndAudioBeforeLockedDirectionCharge()
        {
            var enemy=Spawn(EnemyRole.Charger);Vector2 start=enemy.Body.position;Advance(0);
            Assert.That(warnings.Count,Is.EqualTo(1));Assert.That(warnings[0].Priority,Is.EqualTo(EventPriority.Danger));
            var effect=Object.FindAnyObjectByType<EnemyAreaEffect>();Assert.That(effect.Warning,Is.True);
            Assert.That(effect.GetComponent<LineRenderer>().enabled,Is.True);Assert.That(effect.GetComponent<AudioSource>().isPlaying,Is.True);
            Advance(0.19f);Assert.That(enemy.Body.position,Is.EqualTo(start));
            player.transform.position=new Vector2(0,10);Advance(0.02f);
            Assert.That(enemy.Body.position.x,Is.LessThan(start.x));Assert.That(Mathf.Abs(enemy.Body.position.y-start.y),Is.LessThan(0.05f));
        }
        [TestCase(EnemyRole.WebCaster)] [TestCase(EnemyRole.SlowingTrail)]
        public void WebAndTrailCreatePooledSlowAreasThatExpireToBaseline(EnemyRole role)
        {
            Spawn(role);Advance(0);Assert.That(player.EnemySlowMultiplier,Is.EqualTo(1));
            Advance(0.21f);Advance(0);Assert.That(player.EnemySlowMultiplier,Is.EqualTo(0.5f));
            Advance(1.1f);Assert.That(player.EnemySlowMultiplier,Is.EqualTo(1));Assert.That(scheduler.ActiveEffectCount,Is.Zero);
        }
        [Test]
        public void OverlappingSlowFieldsTakeStrongestEffectWithoutMultiplying()
        {
            Spawn(EnemyRole.WebCaster);Spawn(EnemyRole.SlowingTrail);Advance(0);Advance(0.21f);Advance(0);
            Assert.That(player.EnemySlowMultiplier,Is.EqualTo(0.5f));scheduler.enabled=false;
            Assert.That(player.EnemySlowMultiplier,Is.EqualTo(1));Assert.That(scheduler.ActiveEffectCount,Is.Zero);
        }
        [Test]
        public void FrontalArmorReducesFrontDamageAndAllowsFlanking()
        {
            var enemy=Spawn(EnemyRole.FrontalArmored);Advance(0);float health=enemy.State.Health;long lease=enemy.LeaseId;
            Assert.That(scheduler.ApplyHit(enemy,lease,4,player.transform.position,1,1),Is.True);
            Assert.That(enemy.State.Health,Is.EqualTo(health-1).Within(0.001));
            Assert.That(scheduler.ApplyHit(enemy,lease,4,enemy.Body.position+Vector2.right*10,1,2),Is.True);
            Assert.That(enemy.State.Health,Is.EqualTo(health-5).Within(0.001));
        }
        [Test]
        public void InterruptionCancelsChargeWarningAndRespectsResistance()
        {
            var enemy=Spawn(EnemyRole.Charger);Advance(0);long lease=enemy.LeaseId;
            Assert.That(scheduler.ApplyControl(enemy,lease,Vector2.right,1),Is.True);Advance(0.1f);
            Assert.That(scheduler.ActiveEffectCount,Is.Zero);Assert.That(enemy.State.InterruptionRemaining,Is.GreaterThan(0));
            Assert.That(enemy.Body.position.x,Is.GreaterThan(2));Assert.That(warnings.Count,Is.EqualTo(1));
            Assert.That(scheduler.ApplyControl(enemy,lease-1,Vector2.one,1),Is.False);
        }
        [Test]
        public void WallsBlockSweptMovement()
        {
            var enemy=Spawn(EnemyRole.Swarm);var wall=new GameObject("Wall");wall.transform.SetParent(root.transform);wall.layer=8;
            wall.transform.position=new Vector2(1.2f,0);wall.AddComponent<BoxCollider2D>().size=new Vector2(0.2f,8);Physics2D.SyncTransforms();
            Advance(1);Assert.That(enemy.Body.position.x,Is.GreaterThan(1.79f));Assert.That(enemy.Body.position.x,Is.LessThan(2.1f));
        }
        [TestCase(RunPhase.Paused)] [TestCase(RunPhase.ChoosingUpgrade)]
        public void SuspensionFreezesWarningTimersMotionAndResumesWithoutDuplicate(RunPhase phase)
        {
            var enemy=Spawn(EnemyRole.Charger);Advance(0);Vector2 position=enemy.Body.position;run.TryTransition(phase);Advance(5);
            Assert.That(enemy.Body.position,Is.EqualTo(position));Assert.That(warnings.Count,Is.EqualTo(1));
            run.TryTransition(RunPhase.Playing);Advance(0.1f);Assert.That(enemy.Body.position,Is.EqualTo(position));Assert.That(warnings.Count,Is.EqualTo(1));
        }
        [Test]
        public void SaturatedWarningPoolSuppressesAttackInsteadOfExecutingUnannounced()
        {
            Spawn(EnemyRole.Charger);Spawn(EnemyRole.Charger);Advance(0);
            Assert.That(warnings.Count,Is.EqualTo(1));Assert.That(scheduler.SuppressedAttacks,Is.EqualTo(1));
            Assert.That(scheduler.ActiveEffectCount,Is.EqualTo(1));
        }
        [Test]
        public void DefeatDuringWarningAndReusedLeaseDoNotExecuteOldAttack()
        {
            var enemy=Spawn(EnemyRole.Charger);Advance(0);long oldLease=enemy.LeaseId;
            scheduler.ApplyHit(enemy,oldLease,100,Vector2.zero,1,1);Advance(0);
            Assert.That(scheduler.ActiveEffectCount,Is.Zero);
            var next=Spawn(EnemyRole.Charger);Assert.That(next,Is.SameAs(enemy));Advance(0);
            Assert.That(scheduler.ApplyHit(next,oldLease,100,Vector2.zero,1,2),Is.False);Assert.That(next.State.Health,Is.EqualTo(next.Definition.Health));
        }
        [Test]
        public void TerminalAndDisableClearFieldsAndSlowAndRejectInvalidAdvance()
        {
            Spawn(EnemyRole.WebCaster);Advance(0);Advance(0.21f);Advance(0);Assert.That(player.EnemySlowMultiplier,Is.EqualTo(0.5f));
            Assert.Throws<ArgumentOutOfRangeException>(()=>scheduler.Advance(float.NaN));run.TryTransition(RunPhase.Lost);
            Assert.That(scheduler.ActiveEffectCount,Is.Zero);Assert.That(player.EnemySlowMultiplier,Is.EqualTo(1));
        }
        [Test]
        public void SweptContactDamagesPlayerOnceWithInvulnerabilityAcrossEnemies()
        {
            var hits=new List<CombatEvent>();events.Combat+=e=>{if(e.Type==CombatEventType.DamageApplied)hits.Add(e);};
            Spawn(EnemyRole.Swarm);Spawn(EnemyRole.Swarm);float health=player.State.Health;
            Advance(1);Assert.That(player.State.Health,Is.EqualTo(health-1));
            Advance(0);Assert.That(player.State.Health,Is.EqualTo(health-1));
            Advance(2);Assert.That(hits.Count,Is.EqualTo(2));Assert.That(hits[0].CorrelationId,Is.Not.EqualTo(hits[1].CorrelationId));
        }
        [Test]
        public void FullControlResistancePreventsPushAndInterruption()
        {
            Set(definitions[0],"knockbackResistance",p=>p.floatValue=1);Set(definitions[0],"interruptionResistance",p=>p.floatValue=1);
            var enemy=Spawn(EnemyRole.Swarm);Advance(0);
            Assert.That(scheduler.ApplyControl(enemy,enemy.LeaseId,Vector2.right,1),Is.True);
            Assert.That(enemy.State.Knockback,Is.EqualTo(Vector2.zero));Assert.That(enemy.State.InterruptionRemaining,Is.Zero);
            Assert.That(scheduler.ApplyControl(enemy,enemy.LeaseId,Vector2.right,-1),Is.False);
        }
        [Test]
        public void ReentrantDefeatOnWarningCannotExecuteContactOrRetainWarning()
        {
            var enemy=Spawn(EnemyRole.Charger);float health=player.State.Health;
            events.Presentation+=value=>{if(value.Type==PresentationEventType.DangerTelegraphed)enemy.TakeDamage(enemy.LeaseId,100,1,1);};
            Advance(0);Advance(0.3f);
            Assert.That(player.State.Health,Is.EqualTo(health));Assert.That(scheduler.ActiveEffectCount,Is.Zero);
        }
        private static void Set(Object obj,string name,Action<SerializedProperty> action)
        {var s=new SerializedObject(obj);action(s.FindProperty(name));s.ApplyModifiedPropertiesWithoutUndo();}
    }
}
#endif
