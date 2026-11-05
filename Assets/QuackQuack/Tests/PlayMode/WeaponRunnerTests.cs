using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Content.Enemies;
using QuackQuack.Content.Shared;
using QuackQuack.Content.Weapons;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Player;
using QuackQuack.Gameplay.Enemies;
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
        private EnemyDefinition pooledDefinition;
        private GameObject pooledPrefab;

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
        public void BoomerangHitsOncePerPassageAcrossSmallStepsWithoutResettingEveryFrame()
        {
            StartRunner(WeaponBehavior.Boomerang);
            runner.Advance(0);
            for (int i = 0; i < 4; i++) runner.Advance(0.125f);
            var projectile = root.GetComponentInChildren<WeaponProjectile>();
            Assert.That(projectile.transform.position.x, Is.EqualTo(4).Within(0.001f));
            Assert.That(target.Health, Is.EqualTo(17), "Only one outbound hit, even while overlapping across steps.");
            for (int i = 0; i < 4; i++) runner.Advance(0.125f);
            Assert.That(runner.ActiveProjectileCount, Is.Zero);
            Assert.That(target.Health, Is.EqualTo(14), "The same target must receive exactly one inbound hit.");
            Assert.That(events.Count, Is.EqualTo(2));
        }

        [Test]
        public void BoomerangStepCrossingTurnPreservesOutboundAndInboundHits()
        {
            StartRunner(WeaponBehavior.Boomerang);
            runner.Advance(0); runner.Advance(0.75f);
            var projectile = root.GetComponentInChildren<WeaponProjectile>();
            Assert.That(projectile.transform.position.x, Is.EqualTo(2).Within(0.001f));
            Assert.That(target.Health, Is.EqualTo(14));
            Assert.That(events.Count, Is.EqualTo(2));
            runner.Advance(0.25f);
            Assert.That(target.Health, Is.EqualTo(14));
            Assert.That(runner.ActiveProjectileCount, Is.Zero);
        }

        [Test]
        public void BoomerangStepBeyondLifetimePreservesBothPassagesAndExpiresOnce()
        {
            StartRunner(WeaponBehavior.Boomerang, 1);
            runner.Advance(0); runner.Advance(2);
            Assert.That(target.Health, Is.EqualTo(14));
            Assert.That(events.Count, Is.EqualTo(2));
            Assert.That(runner.ActiveProjectileCount, Is.Zero);
            Assert.That(pools.GetActiveCount(PoolFamily.Projectile), Is.Zero);
        }

        [Test]
        public void ZeroTimeAtTurnDoesNotAdvancePassageOrClearInboundHistoryAgain()
        {
            StartRunner(WeaponBehavior.Boomerang);
            runner.Advance(0); runner.Advance(0.5f);
            var projectile = root.GetComponentInChildren<WeaponProjectile>();
            Assert.That(projectile.Passage, Is.EqualTo(ProjectilePassage.Outbound));
            runner.Advance(0);
            Assert.That(projectile.Passage, Is.EqualTo(ProjectilePassage.Outbound));
            Assert.That(target.Health, Is.EqualTo(17));
            runner.Advance(0.25f); // Return from x=4 to x=2 before asserting the hit.
            Assert.That(projectile.Passage, Is.EqualTo(ProjectilePassage.Inbound));
            Assert.That(target.Health, Is.EqualTo(14));
            runner.Advance(0); runner.Advance(0.125f);
            Assert.That(target.Health, Is.EqualTo(14));
            Assert.That(events.Count, Is.EqualTo(2));
        }

        [Test]
        public void ProjectileLeaseAndPassageResetBeforeReusingCompletedInboundEmission()
        {
            StartRunner(WeaponBehavior.Boomerang, 1); runner.Advance(0);
            var projectile = root.GetComponentInChildren<WeaponProjectile>();
            long firstLease = projectile.LeaseId; Assert.That(firstLease, Is.GreaterThan(0));
            runner.Advance(0.75f);
            Assert.That(projectile.Passage, Is.EqualTo(ProjectilePassage.Inbound));
            runner.Advance(0.25f);
            Assert.That(projectile.LeaseId, Is.Zero);
            Assert.That(projectile.Passage, Is.EqualTo(ProjectilePassage.Outbound));
            runner.Advance(9);
            Assert.That(root.GetComponentInChildren<WeaponProjectile>(), Is.SameAs(projectile));
            Assert.That(projectile.LeaseId, Is.GreaterThan(firstLease));
            Assert.That(projectile.Passage, Is.EqualTo(ProjectilePassage.Outbound));
            Assert.That(projectile.Age, Is.Zero);
            runner.Advance(0.25f);
            Assert.That(target.Health, Is.EqualTo(11));
        }

        [Test]
        public void BoomerangTargetAtTurnCanBeHitOnceOnEachSideOfTheTurn()
        {
            target.transform.position = Vector2.right * 4; Physics2D.SyncTransforms();
            StartRunner(WeaponBehavior.Boomerang);
            runner.Advance(0); runner.Advance(0.5f);
            Assert.That(target.Health, Is.EqualTo(17));
            runner.Advance(0.125f);
            Assert.That(target.Health, Is.EqualTo(14), "Initial overlap at the start of inbound is a new passage.");
            runner.Advance(0.375f);
            Assert.That(target.Health, Is.EqualTo(14));
            Assert.That(events.Count, Is.EqualTo(2));
        }

        [Test]
        public void BoomerangCanHitTargetThatEntersOnlyDuringReturn()
        {
            target.transform.position = new Vector2(2, 3); Physics2D.SyncTransforms();
            StartRunner(WeaponBehavior.Boomerang);
            runner.Advance(0); runner.Advance(0.5f);
            Assert.That(target.Health, Is.EqualTo(20));
            target.transform.position = new Vector2(2, 0); Physics2D.SyncTransforms();
            runner.Advance(0.25f); runner.Advance(0.25f);
            Assert.That(target.Health, Is.EqualTo(17));
            Assert.That(events.Count, Is.EqualTo(1));
        }

        [Test]
        public void BoomerangDeduplicatesRootAndChildCollidersSeparatelyInEachPassage()
        {
            var extra = new GameObject("Second collider"); extra.transform.SetParent(target.transform, false); extra.layer = 9;
            extra.AddComponent<BoxCollider2D>().isTrigger = true;
            Physics2D.SyncTransforms(); StartRunner(WeaponBehavior.Boomerang);
            runner.Advance(0); runner.Advance(0.5f);
            Assert.That(target.Health, Is.EqualTo(17));
            runner.Advance(0.5f);
            Assert.That(target.Health, Is.EqualTo(14));
            Assert.That(events.Count, Is.EqualTo(2));
        }

        [Test]
        public void ReusedBoomerangProjectileDoesNotRetainPreviousEmissionHitHistory()
        {
            StartRunner(WeaponBehavior.Boomerang, 1);
            runner.Advance(0); var first = root.GetComponentInChildren<WeaponProjectile>();
            runner.Advance(0.25f); Assert.That(target.Health, Is.EqualTo(17));
            target.transform.position = new Vector2(2, 3); Physics2D.SyncTransforms();
            runner.Advance(0.75f);
            Assert.That(first.Definition, Is.Null); Assert.That(first.Age, Is.Zero);
            target.transform.position = new Vector2(2, 0); Physics2D.SyncTransforms();
            runner.Advance(9);
            Assert.That(root.GetComponentInChildren<WeaponProjectile>(), Is.SameAs(first));
            runner.Advance(0.25f);
            Assert.That(target.Health, Is.EqualTo(14));
            Assert.That(events.Count, Is.EqualTo(2));
        }

        [TestCase(false)] [TestCase(true)]
        public void ReusedTargetLeaseCanBeHitAgainWithinTheSamePassage(bool inbound)
        {
            target.gameObject.SetActive(false);
            RegisterPooledTarget(); StartRunner(WeaponBehavior.Boomerang);
            runner.Advance(0);
            if (inbound) runner.Advance(0.5f);
            var first = SpawnPooledTarget(new Vector2(inbound ? 3 : 1, 0));
            int instance = first.gameObject.GetInstanceID(); long firstLease = first.LeaseId;
            runner.Advance(0.125f);
            Assert.That(first.State.Health, Is.EqualTo(17));
            Assert.That(first.Despawn(firstLease), Is.True);
            var second = SpawnPooledTarget(new Vector2(2, 0));
            Assert.That(second, Is.SameAs(first));
            Assert.That(second.gameObject.GetInstanceID(), Is.EqualTo(instance));
            Assert.That(second.LeaseId, Is.GreaterThan(firstLease));
            runner.Advance(0.125f);
            Assert.That(second.State.Health, Is.EqualTo(17), "A new lease must not inherit the former life immunity.");
            Assert.That(events.FindAll(e => e.Type == CombatEventType.DamageApplied).Count, Is.EqualTo(2));
        }

        [Test]
        public void ReentrantTargetReuseDoesNotApplyOldCastContactsToNewLife()
        {
            target.gameObject.SetActive(false);
            RegisterPooledTarget(); SetEnemy("health", 3f);
            StartRunner(WeaponBehavior.Boomerang); runner.Advance(0);
            PooledEnemy next = null;
            var first = SpawnPooledTarget(Vector2.right, _ =>
            {
                next = SpawnPooledTarget(new Vector2(2.5f, 0));
                Assert.That(next.ConfigureEncounter(10, 1), Is.True);
            });
            first.gameObject.AddComponent<BoxCollider2D>().isTrigger = true;
            Physics2D.SyncTransforms(); long oldLease = first.LeaseId;
            runner.Advance(0.25f);
            Assert.That(next, Is.SameAs(first));
            Assert.That(next.LeaseId, Is.GreaterThan(oldLease));
            Assert.That(next.State.Health, Is.EqualTo(30), "Contacts captured from the old lease must not damage its replacement.");
            runner.Advance(0.125f);
            Assert.That(next.State.Health, Is.EqualTo(27), "The new lease can be hit by a fresh sweep in the same passage.");
            Assert.That(events.FindAll(e => e.Type == CombatEventType.DamageApplied).Count, Is.EqualTo(2));
        }

        [Test]
        public void PausingAtTurnPreservesMovementAndDoesNotConsumeInboundHit()
        {
            StartRunner(WeaponBehavior.Boomerang); runner.Advance(0); runner.Advance(0.5f);
            var projectile = root.GetComponentInChildren<WeaponProjectile>();
            run.TryTransition(RunPhase.Paused); runner.Advance(10);
            Assert.That(projectile.Age, Is.EqualTo(0.5f));
            Assert.That(projectile.transform.position.x, Is.EqualTo(4).Within(0.001f));
            Assert.That(target.Health, Is.EqualTo(17));
            run.TryResume(); runner.Advance(0.5f);
            Assert.That(target.Health, Is.EqualTo(14));
            Assert.That(events.Count, Is.EqualTo(2));
        }

        [Test]
        public void ObstacleInsertedOnReturnStopsBoomerangBeforeSecondHit()
        {
            StartRunner(WeaponBehavior.Boomerang); runner.Advance(0); runner.Advance(0.5f);
            Assert.That(target.Health, Is.EqualTo(17));
            var wall = new GameObject("Return wall"); wall.transform.SetParent(root.transform);
            wall.layer = 8; wall.transform.position = Vector2.right * 3;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.2f, 4); Physics2D.SyncTransforms();
            runner.Advance(0.5f);
            Assert.That(target.Health, Is.EqualTo(17));
            Assert.That(runner.ActiveProjectileCount, Is.Zero);
            Assert.That(events.Count, Is.EqualTo(1));
        }

        private void RegisterPooledTarget()
        {
            pooledPrefab = new GameObject("Pooled target prefab"); pooledPrefab.transform.SetParent(root.transform); pooledPrefab.layer = 9;
            pooledPrefab.AddComponent<PooledEnemy>();
            pooledPrefab.AddComponent<QuackTarget>();
            pooledPrefab.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            pooledPrefab.GetComponent<CircleCollider2D>().isTrigger = true;
            pooledPrefab.GetComponent<CircleCollider2D>().radius = 0.1f;
            pooledPrefab.SetActive(false);
            pooledDefinition = ScriptableObject.CreateInstance<EnemyDefinition>();
            typeof(ContentDefinition).GetField("id", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(pooledDefinition, "lease-target");
            typeof(ContentDefinition).GetField("displayName", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(pooledDefinition, "Lease target");
            SetEnemy("prefab", pooledPrefab); SetEnemy("health", 20f);
            SetEnemy("poolInitialSize", 1); SetEnemy("poolMaxSize", 1);
            Assert.That(pooledDefinition.IsValid(out var reason), Is.True, reason);
            PooledEnemy.RegisterPool(pools, pooledDefinition, root.transform);
        }
        private void SetEnemy(string field, object value) => typeof(EnemyDefinition).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(pooledDefinition, value);
        private PooledEnemy SpawnPooledTarget(Vector2 position, System.Action<EnemyReward> sink = null)
        {
            var enemy = PooledEnemy.Spawn(pools, pooledDefinition, run, bus, root.transform, position, sink ?? (_ => { }));
            Assert.That(enemy, Is.Not.Null);
            long lease = enemy.LeaseId;
            enemy.GetComponent<QuackTarget>().BindCombat(() => enemy.State.Health,
                (amount, source, step, attacker) => enemy.TakeDamage(lease, amount, source, step), (push, interruption) => { });
            Physics2D.SyncTransforms(); return enemy;
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

        [TestCase(60f)] [TestCase(120f)]
        public void FrontalAttackHasVisibleCueAndFreezesWithPause(float angle)
        {
            Set("coneAngle", angle);
            StartRunner(WeaponBehavior.FrontalQuack); runner.Advance(0);
            var line = System.Array.Find(root.GetComponentsInChildren<LineRenderer>(true), l => l.name == "Frontal 0");
            Assert.That(line, Is.Not.Null, "Frontal attack needs a visible emission cue.");
            Assert.That(line.enabled, Is.True);
            float farthest = 0;
            for (int i = 0; i < line.positionCount; i++) farthest = Mathf.Max(farthest, ((Vector2)line.GetPosition(i)).magnitude);
            Assert.That(farthest, Is.EqualTo(definition.GetLevel(1).Range).Within(0.001f));
            Assert.That(target.Health, Is.EqualTo(17));
            float alpha = line.startColor.a;
            run.TryTransition(RunPhase.Paused); runner.Advance(1);
            Assert.That(line.enabled, Is.True); Assert.That(line.startColor.a, Is.EqualTo(alpha));
            run.TryResume(); runner.Advance(0.3f);
            Assert.That(line.enabled, Is.False);
            runner.Advance(10); Assert.That(line.enabled, Is.True);
            runner.enabled = false; Assert.That(line.enabled, Is.False);
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (runner != null) runner.enabled = false;
            pools.Dispose();
            Object.Destroy(root); Object.Destroy(definition);
            if (pooledDefinition != null) Object.Destroy(pooledDefinition);
            yield return null;
        }
    }
}
