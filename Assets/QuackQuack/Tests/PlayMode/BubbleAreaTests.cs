using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Content.Passives;
using QuackQuack.Content.Shared;
using QuackQuack.Content.Weapons;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Player;
using QuackQuack.Gameplay.Weapons;
using QuackQuack.Systems.Combat;
using QuackQuack.Systems.Pooling;
using QuackQuack.Systems.Upgrades;
using UnityEngine;
using UnityEngine.TestTools;

namespace QuackQuack.Tests.PlayMode
{
    public class BubbleAreaTests
    {
        private GameObject root;
        private WeaponDefinition definition;
        private PassiveDefinition soap;
        private Sprite sprite;
        private Texture2D texture;
        private BuildState build;
        private RunState run;
        private PoolService pools;
        private UpgradeSystem upgrades;
        private WeaponRunner runner;
        private const float BaseRadius = 0.2f;

        [SetUp]
        public void Setup()
        {
            root = new GameObject("Bubble radius tests");
            definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            typeof(ContentDefinition).GetField("id", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(definition, "bolhas");
            Set(definition, "behavior", WeaponBehavior.Bubbles);
            Set(definition, "targetMode", WeaponTargetMode.Forward);
            Set(definition, "hitRadius", BaseRadius);
            Set(definition, "projectileSpeed", 3f); Set(definition, "projectileLifetime", 1f);
            var levels = new WeaponLevel[5];
            for (int i = 0; i < 5; i++) levels[i] = new WeaponLevel(2, 2, 6, 1, 0);
            Set(definition, "levels", levels);
            // Non-unit, rectangular sprite bounds ensure sizing uses world diameter,
            // not an assumption about pixels-per-unit or an existing transform scale.
            texture = new Texture2D(16, 8);
            sprite = Sprite.Create(texture, new Rect(0, 0, 16, 8), Vector2.one * 0.5f, 16);
            Set(definition, "projectileSprite", sprite);
            soap = ScriptableObject.CreateInstance<PassiveDefinition>();
            typeof(ContentDefinition).GetField("id", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(soap, "sabao-magico");
            Set(soap, "compatibleWeaponId", "bolhas"); Set(soap, "attribute", PassiveAttribute.AreaRadius);
            Set(soap, "modifierPerLevel", 0.16f); Set(soap, "modifierLimit", 0.8f);
            build = new BuildState(); build.TryAddWeapon("bolhas");
            run = new RunState(18); run.TryTransition(RunPhase.Playing);
            upgrades = new UpgradeSystem(18, build, new[] { definition }, new[] { soap }, new EvolutionDefinition[0]);
            pools = new PoolService();
        }

        private void StartRunner(int level, WeaponBehavior behavior = WeaponBehavior.Bubbles)
        {
            Set(definition, "behavior", behavior);
            if (level > 0)
            {
                Assert.That(build.TryAddPassive(soap.Id), Is.True);
                for (int i = 1; i < level; i++) Assert.That(build.TryUpgradePassive(soap.Id, soap.MaxLevel), Is.True);
            }
            WeaponRunner.RegisterProjectilePool(pools, "bubbles", 1, root.transform);
            runner = root.AddComponent<WeaponRunner>(); runner.AutomaticTick = false;
            runner.Initialize(run, build, pools, new GameplayEventBus(), root.transform,
                () => TerrainType.Land, new[] { definition }, "bubbles", 1);
            runner.Upgrades = upgrades;
        }
        private WeaponProjectile Shot() => root.GetComponentInChildren<WeaponProjectile>();
        private QuackTarget Target(Vector2 position, float radius = 0.01f)
        {
            var instance = new GameObject("Target"); instance.transform.SetParent(root.transform);
            instance.transform.position = position; instance.layer = 9;
            var target = instance.AddComponent<QuackTarget>();
            target.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            target.GetComponent<CircleCollider2D>().isTrigger = true;
            target.GetComponent<CircleCollider2D>().radius = radius;
            target.Health = 20; Physics2D.SyncTransforms(); return target;
        }

        [TestCase(0, 1f)] [TestCase(1, 1.16f)] [TestCase(5, 1.8f)]
        public void RendererDiameterMatchesEffectiveHitRadius(int level, float multiplier)
        {
            StartRunner(level); runner.Advance(0);
            var shot = Shot(); Assert.That(shot, Is.Not.Null);
            var bounds = shot.GetComponent<SpriteRenderer>().bounds;
            Assert.That(bounds.size.x, Is.EqualTo(2 * BaseRadius * multiplier).Within(0.0001f));
            Assert.That(bounds.size.y, Is.EqualTo(2 * BaseRadius * multiplier).Within(0.0001f));
            Assert.That(shot.Stats.AreaRadius, Is.Zero);
        }

        [TestCase(0, 0.27f, true)] [TestCase(0, 0.33f, false)]
        [TestCase(1, 0.302f, true)] [TestCase(1, 0.362f, false)]
        [TestCase(5, 0.43f, true)] [TestCase(5, 0.49f, false)]
        [TestCase(0, 0.32f, false)] [TestCase(5, 0.32f, true)]
        public void SweptCollisionUsesEffectiveRadiusAtInsideAndOutsideBoundary(int level, float lateral, bool hit)
        {
            // Keep a 0.03-unit margin from tangency: tiny 0.01-radius targets
            // gave no hit even in an independent CircleCast with the expected radius.
            var target = Target(new Vector2(0.5f, lateral), 0.1f);
            StartRunner(level); runner.Advance(0);
            runner.Advance(0.3f);
            Assert.That(target.Health, Is.EqualTo(hit ? 18 : 20));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(5)]
        public void SoapPreservesDamageSpeedLifetimeAndAcquisitionRange(int level)
        {
            Set(definition, "targetMode", WeaponTargetMode.NearestEnemy);
            var outside = Target(new Vector2(6.1f, 0));
            StartRunner(level); runner.Advance(0);
            Assert.That(runner.ActiveProjectileCount, Is.Zero, "Soap must not expand target acquisition.");
            outside.transform.position = new Vector2(0.5f, 0); Physics2D.SyncTransforms();
            runner.Advance(0); var shot = Shot();
            Assert.That(shot.Stats.Damage, Is.EqualTo(2)); Assert.That(shot.Stats.Range, Is.EqualTo(6));
            runner.Advance(0.1f);
            Assert.That(shot.transform.position.x, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(shot.Definition.ProjectileLifetime, Is.EqualTo(1));
            runner.Advance(0.2f); Assert.That(outside.Health, Is.EqualTo(18));
            runner.Advance(0.7f); Assert.That(runner.ActiveProjectileCount, Is.Zero);
        }

        [Test]
        public void SoapDoesNotIncreaseOtherProjectileRadiusOrVisualScale()
        {
            var target = Target(new Vector2(0.5f, 0.25f));
            StartRunner(5, WeaponBehavior.Boomerang); runner.Advance(0);
            Assert.That(Shot().transform.localScale, Is.EqualTo(Vector3.one));
            runner.Advance(0.3f); Assert.That(target.Health, Is.EqualTo(20));
        }

        [Test]
        public void PauseAndPooledReusePreserveThenResetBubbleRadiusAndScale()
        {
            StartRunner(5); runner.Advance(0);
            var first = Shot();
            float diameter = first.GetComponent<SpriteRenderer>().bounds.size.x;
            run.TryTransition(RunPhase.Paused); runner.Advance(2);
            Assert.That(first.Age, Is.Zero);
            Assert.That(first.GetComponent<SpriteRenderer>().bounds.size.x, Is.EqualTo(diameter));
            run.TryResume(); runner.Advance(1);
            Assert.That(first.Definition, Is.Null);
            Assert.That(first.transform.localScale, Is.EqualTo(Vector3.one));
            runner.Upgrades = null;
            runner.Advance(1);
            Assert.That(Shot(), Is.SameAs(first));
            Assert.That(Shot().GetComponent<SpriteRenderer>().bounds.size.x, Is.EqualTo(0.4f).Within(0.0001f));
        }

        [Test]
        public void AreaRelicComposesOnceWithSoapAndVisualDiameterUnderScaledPoolParent()
        {
            StartRunner(5);
            root.transform.localScale = new Vector3(2, 3, 1);
            var modifiers = new RelicModifier[10];
            for (int i = 0; i < 10; i++) modifiers[i] = new RelicModifier("area" + i, RelicAttribute.Area, RelicTerrain.Any, 0.1f);
            var relics = new RelicLoadout(modifiers); relics.Unlock("area0"); relics.Equip("area0");
            runner.Relics = relics; runner.Advance(0);
            var bounds = Shot().GetComponent<SpriteRenderer>().bounds;
            Assert.That(bounds.size.x, Is.EqualTo(0.2f * 1.8f * 1.1f * 2).Within(0.0001f));
            Assert.That(bounds.size.y, Is.EqualTo(bounds.size.x).Within(0.0001f));
        }

        [Test]
        public void LargerBubbleStillStopsAtWallBeforeDamagingEnemyBehindIt()
        {
            var target = Target(new Vector2(1, 0));
            var wall = new GameObject("Wall"); wall.transform.SetParent(root.transform);
            wall.transform.position = new Vector2(0.5f, 0); wall.layer = 8;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.1f, 2);
            Physics2D.SyncTransforms();
            StartRunner(5); runner.Advance(0); runner.Advance(0.4f);
            Assert.That(target.Health, Is.EqualTo(20));
            Assert.That(runner.ActiveProjectileCount, Is.Zero);
        }

        [Test]
        public void UpgradingSoapChangesNewEmissionWithoutResizingInFlightProjectileOrMutatingAsset()
        {
            StartRunner(1); string before = JsonUtility.ToJson(definition);
            runner.Advance(0); var first = Shot();
            for (int i = 1; i < 5; i++) Assert.That(build.TryUpgradePassive(soap.Id, soap.MaxLevel), Is.True);
            runner.Advance(0.1f);
            Assert.That(first.GetComponent<SpriteRenderer>().bounds.size.x, Is.EqualTo(0.464f).Within(0.0001f));
            runner.Advance(0.91f); runner.Advance(1.1f);
            Assert.That(Shot().GetComponent<SpriteRenderer>().bounds.size.x, Is.EqualTo(0.72f).Within(0.0001f));
            Assert.That(JsonUtility.ToJson(definition), Is.EqualTo(before));
        }

        private static void Set(object instance, string name, object value) =>
            instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(instance, value);
        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (runner != null) runner.enabled = false;
            pools.Dispose(); upgrades.Dispose(); Object.Destroy(root);
            Object.Destroy(definition); Object.Destroy(soap); Object.Destroy(sprite); Object.Destroy(texture);
            yield return null;
        }
    }
}
