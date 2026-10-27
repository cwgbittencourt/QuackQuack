#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using QuackQuack.Content.Characters;
using QuackQuack.Content.Enemies;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Run;
using QuackQuack.Core.Shared;
using QuackQuack.Gameplay.Bosses;
using QuackQuack.Gameplay.Player;
using QuackQuack.Platform.Input;
using QuackQuack.Systems.Combat;
using QuackQuack.Systems.Pooling;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
namespace QuackQuack.Tests.PlayMode
{
    public class FrogKingEncounterTests
    {
        private sealed class NoInput : IPlayerInput { public PlayerCommand Read() => default; }
        private GameObject root;
        private PlayerController player;
        private FrogKingController boss;
        private FrogKingSettings settings;
        private FrogKingSafeArea refuge;
        private RunState run;
        private PoolService pools;
        private GameplayEventBus events;
        private readonly List<CombatEvent> combat = new List<CombatEvent>();
        [SetUp] public void SetUp()
        {
            combat.Clear(); root = new GameObject("T057 fixture"); pools = new PoolService(); events = new GameplayEventBus();
            run = new RunState(57); run.TryTransition(RunPhase.Playing); run.TryTransition(RunPhase.Boss);
            var target = new GameObject("Player", typeof(PlayerController)); target.transform.SetParent(root.transform);
            player = target.GetComponent<PlayerController>();
            player.Initialize(AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/QuackQuack/Content/Characters/Pato.asset"), new NoInput(), run, events);
            player.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic; target.transform.position = new Vector2(20, 0);
            settings = Object.Instantiate(AssetDatabase.LoadAssetAtPath<FrogKingSettings>("Assets/QuackQuack/Content/Enemies/Prototype/FrogKingSettings.asset"));
            settings.TelegraphSeconds = 0.1f; settings.HazardCapacity = 3; settings.TadpoleCount = 2;
            var area = new GameObject("Refuge", typeof(CircleCollider2D), typeof(FrogKingSafeArea)); area.transform.SetParent(root.transform);
            area.GetComponent<CircleCollider2D>().isTrigger = true; area.GetComponent<CircleCollider2D>().radius = 2;
            refuge = area.GetComponent<FrogKingSafeArea>();
            var actor = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/QuackQuack/Prefabs/Prototype/Enemies/sapo-rei.prefab"), root.transform);
            boss = actor.GetComponent<FrogKingController>(); boss.AutomaticTick = false;
            boss.InitializeEncounter(run, events, pools, player, settings, actor.GetComponentInChildren<LineRenderer>(), actor.GetComponent<AudioSource>(), new[] { refuge });
            events.Combat += value => combat.Add(value); Physics2D.SyncTransforms();
        }
        [TearDown] public void Cleanup() { boss.enabled = false; pools.Dispose(); Object.DestroyImmediate(root); Object.DestroyImmediate(settings); events.Clear(); }
        private void Attack(string id, Vector2 target) { Assert.That(boss.TryBeginAttack(id, target), Is.True); run.Advance(0.11); boss.Advance(0.11f); }
        [Test] public void HealthUnlocksOnlyLaterPatterns()
        {
            Assert.That(boss.Phase, Is.EqualTo(FrogKingPhase.Opening));
            Assert.That(boss.TryBeginAttack("tadpoles", Vector2.zero), Is.False);
            Assert.That(boss.TryBeginAttack("destroy-safe-area", Vector2.zero), Is.False);
            boss.TakeDamage(310, 1, 0); Assert.That(boss.Phase, Is.EqualTo(FrogKingPhase.Summoning));
            Attack("tadpoles", Vector2.zero); Assert.That(boss.ActiveHazards, Is.EqualTo(2));
            Assert.That(boss.TryBeginAttack("destroy-safe-area", Vector2.zero), Is.False);
            boss.TakeDamage(350, 1, 1); Assert.That(boss.Phase, Is.EqualTo(FrogKingPhase.Destruction));
            Attack("destroy-safe-area", Vector2.zero); Assert.That(refuge.IsDestroyed, Is.True);
            Assert.That(boss.TryBeginAttack("destroy-safe-area", Vector2.zero), Is.False);
        }
        [Test] public void JumpLocksItsMarkAndCanBeDodged()
        {
            Vector2 mark = player.transform.position;
            Assert.That(boss.TryBeginAttack("jump", mark), Is.True);
            player.transform.position = Vector2.zero; boss.Advance(0.11f);
            Assert.That((Vector2)boss.transform.position, Is.EqualTo(mark)); Assert.That(player.State.Health, Is.EqualTo(player.State.MaxHealth));
        }
        [Test] public void JumpDamagesOnlyAfterWarningAndHonorsInvulnerability()
        {
            float before = player.State.Health;
            Assert.That(boss.TryBeginAttack("jump", player.transform.position), Is.True);
            boss.Advance(0.09f); Assert.That(player.State.Health, Is.EqualTo(before)); boss.Advance(0.02f);
            Assert.That(player.State.Health, Is.EqualTo(before - settings.JumpDamage));
            Attack("jump", player.transform.position); Assert.That(player.State.Health, Is.EqualTo(before - settings.JumpDamage));
            Assert.That(combat.Count, Is.EqualTo(1));
        }
        [Test] public void WaveExpandsSweepsPlayerAndExpires()
        {
            player.transform.position = new Vector2(3, 0); Attack("wave", Vector2.zero);
            Assert.That(boss.ActiveHazards, Is.EqualTo(1)); boss.Advance(1);
            Assert.That(player.State.Health, Is.EqualTo(player.State.MaxHealth - settings.WaveDamage));
            boss.Advance(4); Assert.That(boss.ActiveHazards, Is.Zero); Assert.That(pools.ActiveCount, Is.Zero);
        }
        [Test] public void RefugeProtectsFromWaveUntilTelegraphedDestruction()
        {
            player.transform.position = Vector2.zero; Physics2D.SyncTransforms();
            Attack("wave", Vector2.zero); boss.Advance(0.1f); Assert.That(player.State.Health, Is.EqualTo(player.State.MaxHealth));
            boss.TakeDamage(700, 1, 0); Attack("destroy-safe-area", Vector2.zero);
            Assert.That(refuge.IsDestroyed, Is.True); Assert.That(refuge.GetComponent<Collider2D>().enabled, Is.False);
            Attack("wave", Vector2.zero); boss.Advance(0.1f);
            Assert.That(player.State.Health, Is.LessThan(player.State.MaxHealth));
        }
        [Test] public void SummonsAreBoundedReusedAndResetWithoutInstantiation()
        {
            boss.TakeDamage(400, 1, 0);
            int before = Object.FindObjectsByType<FrogKingHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            Attack("tadpoles", Vector2.zero); Attack("tadpoles", Vector2.zero);
            Assert.That(boss.ActiveHazards, Is.EqualTo(3)); Assert.That(boss.SuppressedHazards, Is.EqualTo(1));
            boss.Advance(11); Assert.That(boss.ActiveHazards, Is.Zero);
            Attack("tadpoles", Vector2.zero); Assert.That(boss.ActiveHazards, Is.EqualTo(2));
            Assert.That(Object.FindObjectsByType<FrogKingHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(before));
        }
        [Test] public void TadpolesChaseAndDamageOnContact()
        {
            boss.TakeDamage(400, 1, 0); player.transform.position = new Vector2(3, 0);
            Attack("tadpoles", Vector2.zero); boss.Advance(0.6f);
            Assert.That(player.State.Health, Is.EqualTo(player.State.MaxHealth - settings.TadpoleDamage));
            Assert.That(boss.ActiveHazards, Is.EqualTo(1));
        }
        [TestCase(RunPhase.Lost)] [TestCase(RunPhase.Abandoned)]
        public void TerminalRunClearsHazardsAndPendingAttacks(RunPhase end)
        {
            Attack("wave", Vector2.zero); Assert.That(boss.TryBeginAttack("jump", Vector2.zero), Is.True);
            run.TryTransition(end); Assert.That(pools.ActiveCount, Is.Zero);
            boss.Advance(10); Assert.That(boss.ActiveHazards, Is.Zero); Assert.That(combat, Is.Empty);
        }
        [Test] public void KillingBossWinsOnceAndCancelsEverything()
        {
            Attack("wave", Vector2.zero); boss.TryBeginAttack("jump", Vector2.zero);
            Assert.That(boss.TakeDamage(9999, 5, 1), Is.True); Assert.That(run.Phase, Is.EqualTo(RunPhase.Won));
            Assert.That(boss.Health, Is.Zero); Assert.That(boss.Phase, Is.EqualTo(FrogKingPhase.Defeated));
            Assert.That(pools.ActiveCount, Is.Zero); Assert.That(boss.TakeDamage(1, 5, 1), Is.False);
            Assert.That(combat.FindAll(e => e.Type == CombatEventType.EntityDefeated).Count, Is.EqualTo(1));
        }
        [Test] public void ReusedBossResetsSubscribersAndReusesPrewarmedHazards()
        {
            int before = Object.FindObjectsByType<FrogKingHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            int staleCommits = 0; boss.AttackCommitted += (_, _) => staleCommits++;
            Attack("wave", Vector2.zero); Assert.That(staleCommits, Is.EqualTo(1));
            boss.ResetState(); Assert.That(pools.ActiveCount, Is.Zero);
            boss.InitializeEncounter(run, events, pools, player, settings, boss.GetComponentInChildren<LineRenderer>(), boss.GetComponent<AudioSource>(), new[] { refuge });
            boss.AutomaticTick = false; Attack("wave", Vector2.zero);
            Assert.That(staleCommits, Is.EqualTo(1)); Assert.That(boss.Health, Is.EqualTo(settings.Health));
            Assert.That(Object.FindObjectsByType<FrogKingHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(before));
        }
        [Test] public void DisposedPoolCancelsWithoutAcquiringOrCommitting()
        {
            Attack("wave", Vector2.zero); pools.Dispose();
            Assert.DoesNotThrow(() => boss.Advance(1)); Assert.That(boss.ActiveHazards, Is.Zero);
            Assert.That(boss.TryBeginAttack("jump", Vector2.zero), Is.False);
        }
        [Test] public void ZeroStepCannotApplyHazardDamage()
        {
            player.transform.position = new Vector2(2, 0); boss.TakeDamage(400, 1, 0);
            Attack("tadpoles", Vector2.zero); boss.Advance(0);
            Assert.That(player.State.Health, Is.EqualTo(player.State.MaxHealth));
        }        [Test] public void AutomaticSelectionProgressesThroughEligiblePatterns()
        {
            boss.AutomaticTick = true; boss.TakeDamage(700, 1, 0);
            var patterns = new List<string>(); boss.AttackCommitted += (id, _) => patterns.Add(id);
            for (int i = 0; i < 4; i++) { boss.Advance(2); boss.Advance(0.11f); }
            CollectionAssert.AreEqual(new[] { "jump", "wave", "tadpoles", "destroy-safe-area" }, patterns);
        }
    }
}
#endif
