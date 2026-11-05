using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Content.Shared;
using QuackQuack.Content.Weapons;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Player;
using QuackQuack.Gameplay.Weapons;
using QuackQuack.Systems.Combat;
using QuackQuack.Systems.Pooling;
using UnityEngine;
using UnityEngine.TestTools;

namespace QuackQuack.Tests.PlayMode
{
    public class WeaponRunnerTests
    {
        private GameObject root;
        private WeaponDefinition definition;
        private WeaponRunner runner;
        private QuackTarget target;
        private RunState run;
        private BuildState build;
        private PoolService pools;
        private GameplayEventBus bus;
        private readonly List<CombatEvent> events = new List<CombatEvent>();
        private TerrainType terrain;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Weapon test");
            var enemy = new GameObject("Enemy");
            enemy.transform.SetParent(root.transform); enemy.transform.position = new Vector3(2, 0, 0); enemy.layer = 9;
            target = enemy.AddComponent<QuackTarget>();
            target.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            target.GetComponent<CircleCollider2D>().isTrigger = true;
            definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            typeof(ContentDefinition).GetField("id", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(definition, "weapon");
            Set("targetMode", WeaponTargetMode.Forward);
            Set("projectileSpeed", 8f); Set("projectileLifetime", 1f);
            var levels = new WeaponLevel[5];
            for (int i = 0; i < 5; i++) levels[i] = new WeaponLevel(3 + i, 10, 6, 1, 3);
            Set("levels", levels);
            run = new RunState(42); run.TryTransition(RunPhase.Playing);
            build = new BuildState(); build.TryAddWeapon("weapon");
            pools = new PoolService();
            events.Clear(); terrain = TerrainType.Land;
            Physics2D.SyncTransforms();
        }

        private void StartRunner(WeaponBehavior behavior, int capacity = 2)
        {
            Set("behavior", behavior);
            bus = new GameplayEventBus(); bus.Combat += value => events.Add(value);
            WeaponRunner.RegisterProjectilePool(pools, "weapons", capacity, root.transform);
            runner = root.AddComponent<WeaponRunner>(); runner.AutomaticTick = false;
            runner.Initialize(run, build, pools, bus, root.transform, () => terrain, new[] { definition }, "weapons", capacity);
        }

        [TestCase(WeaponBehavior.Boomerang)]
        [TestCase(WeaponBehavior.Eggs)]
        [TestCase(WeaponBehavior.FrontalQuack)]
        [TestCase(WeaponBehavior.EnergyAura)]
        [TestCase(WeaponBehavior.Rockets)]
        [TestCase(WeaponBehavior.Bubbles)]
        public void EachPatternDamagesOncePerAttackAndAttributesDamageToItsDefinition(WeaponBehavior behavior)
        {
            StartRunner(behavior);
            runner.Advance(0); runner.Advance(0.25f); runner.Advance(0.25f);
            Assert.That(target.Health, Is.EqualTo(17));
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Type, Is.EqualTo(CombatEventType.DamageApplied));
            Assert.That(events[0].Source, Is.EqualTo(definition.GetInstanceID()));
        }

        [TestCase(WeaponBehavior.Boomerang)]
        [TestCase(WeaponBehavior.Eggs)]
        [TestCase(WeaponBehavior.FrontalQuack)]
        [TestCase(WeaponBehavior.Rockets)]
        [TestCase(WeaponBehavior.Bubbles)]
        public void ObstaclesBlockDamageForOtherPatterns(WeaponBehavior behavior)
        {
            var wall = new GameObject("Wall"); wall.transform.SetParent(root.transform);
            wall.transform.position = Vector3.right; wall.layer = 8;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.2f, 4);
            Physics2D.SyncTransforms();
            StartRunner(behavior);
            runner.Advance(0); runner.Advance(0.5f);
            Assert.That(target.Health, Is.EqualTo(20));
            Assert.That(events, Is.Empty);
        }

        [Test]
        public void BoomerangTravelsOutAndBackWithoutHittingSameTargetTwice()
        {
            StartRunner(WeaponBehavior.Boomerang);
            runner.Advance(0); runner.Advance(0.5f);
            var projectile = root.GetComponentInChildren<WeaponProjectile>();
            Assert.That(projectile.transform.position.x, Is.GreaterThan(3));
            runner.Advance(0.25f);
            Assert.That(projectile.transform.position.x, Is.LessThan(3));
            runner.Advance(0.25f);
            Assert.That(runner.ActiveProjectileCount, Is.Zero);
            Assert.That(target.Health, Is.EqualTo(17));
        }

        [Test]
        public void SaturationExpiryAndResetNeverCreateOverflowOrRetainTargets()
        {
            Set("levels", new[] { new WeaponLevel(3, 10, 6, 4, 3), new WeaponLevel(3, 10, 6, 4, 3),
                new WeaponLevel(3, 10, 6, 4, 3), new WeaponLevel(3, 10, 6, 4, 3), new WeaponLevel(3, 10, 6, 4, 3) });
            StartRunner(WeaponBehavior.Bubbles, 1);
            runner.Advance(0);
            Assert.That(pools.GetActiveCount(PoolFamily.Projectile), Is.EqualTo(1));
            var projectile = root.GetComponentInChildren<WeaponProjectile>();
            runner.Advance(2);
            Assert.That(pools.GetActiveCount(PoolFamily.Projectile), Is.Zero);
            Assert.That(projectile.Definition, Is.Null);
            Assert.That(projectile.Target, Is.Null);
            runner.Advance(8);
            Assert.That(root.GetComponentsInChildren<WeaponProjectile>(true).Length, Is.EqualTo(1));
            Assert.That(pools.ActiveCount, Is.EqualTo(1));
        }

        [Test]
        public void SelectionAndPauseFreezeProjectilesAndCooldownWhileTerminalStateReleasesThem()
        {
            StartRunner(WeaponBehavior.Bubbles);
            runner.Advance(0);
            var projectile = root.GetComponentInChildren<WeaponProjectile>();
            run.TryTransition(RunPhase.ChoosingUpgrade);
            runner.Advance(20);
            Assert.That(projectile.Age, Is.Zero);
            Assert.That(target.Health, Is.EqualTo(20));
            run.TryTransition(RunPhase.Playing); run.TryTransition(RunPhase.Paused);
            runner.Advance(20);
            Assert.That(projectile.Age, Is.Zero);
            run.TryTransition(RunPhase.Playing); runner.Advance(0.25f);
            Assert.That(target.Health, Is.EqualTo(17));
            run.TryTransition(RunPhase.Lost); runner.Advance(0);
            Assert.That(pools.ActiveCount, Is.Zero);
        }

        [Test]
        public void FrontalConeDoesNotHitBehindAndAuraHitsAroundOwner()
        {
            target.transform.position = Vector3.left * 2; Physics2D.SyncTransforms();
            StartRunner(WeaponBehavior.FrontalQuack);
            runner.Advance(0);
            Assert.That(target.Health, Is.EqualTo(20));
            Set("behavior", WeaponBehavior.EnergyAura);
            runner.Advance(10);
            Assert.That(target.Health, Is.EqualTo(17));
        }

        [Test]
        public void ProjectileKeepsAuthoredTerrainSnapshotAndDefeatEventOccursOnce()
        {
            Set("hasTerrainAffinity", true); Set("waterDamageMultiplier", 2f);
            terrain = TerrainType.Water; target.Health = 5;
            StartRunner(WeaponBehavior.Bubbles);
            string before = JsonUtility.ToJson(definition);
            runner.Advance(0); terrain = TerrainType.Land;
            runner.Advance(0.25f); runner.Advance(0.25f);
            Assert.That(target.Health, Is.Zero);
            Assert.That(events.Count, Is.EqualTo(2));
            Assert.That(events[0].Amount, Is.EqualTo(5));
            Assert.That(events[1].Type, Is.EqualTo(CombatEventType.EntityDefeated));
            Assert.That(events[0].CorrelationId, Is.EqualTo(events[1].CorrelationId));
            Assert.That(JsonUtility.ToJson(definition), Is.EqualTo(before));
        }

        [Test]
        public void HostileProjectilesAreNotWeaponDamageTargetsAndDisableCleansPool()
        {
            target.IsProjectile = true;
            StartRunner(WeaponBehavior.Bubbles);
            runner.Advance(0); runner.Advance(0.25f);
            Assert.That(target.Health, Is.EqualTo(20));
            Assert.That(events, Is.Empty);
            runner.enabled = false;
            Assert.That(pools.ActiveCount, Is.Zero);
        }

        [Test]
        public void CombatCallbackEndingRunStopsRemainingAreaHitsImmediately()
        {
            var second = new GameObject("Second enemy"); second.transform.SetParent(root.transform);
            second.transform.position = new Vector3(2, 1, 0); second.layer = 9;
            var other = second.AddComponent<QuackTarget>();
            other.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            other.GetComponent<CircleCollider2D>().isTrigger = true;
            Physics2D.SyncTransforms();
            StartRunner(WeaponBehavior.FrontalQuack);
            bus.Combat += _ => run.TryTransition(RunPhase.Lost);
            runner.Advance(0);
            Assert.That(target.Health + other.Health, Is.EqualTo(37));
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Lost));
        }

        private void Set(string name, object value) => typeof(WeaponDefinition).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(definition, value);
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (runner != null) runner.enabled = false;
            pools.Dispose();
            Object.Destroy(root); Object.Destroy(definition);
            yield return null;
        }
    }
}
