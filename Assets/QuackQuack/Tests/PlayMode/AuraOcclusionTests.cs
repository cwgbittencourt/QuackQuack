using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Content.Shared;
using QuackQuack.Content.Weapons;
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
    public class AuraOcclusionTests
    {
        private GameObject root;
        private WeaponDefinition definition;
        private WeaponDefinition replacement;
        private Sprite sprite;
        private Texture2D texture;
        private RunState run;
        private BuildState build;
        private PoolService pools;
        private WeaponRunner runner;

        [SetUp]
        public void Setup()
        {
            root = new GameObject("Aura tests");
            definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            typeof(ContentDefinition).GetField("id", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(definition, "aura");
            texture = new Texture2D(16, 16); sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), Vector2.one * 0.5f, 16);
            Set("projectileSprite", sprite); Set("targetMode", WeaponTargetMode.Self);
            var levels = new WeaponLevel[5];
            for (int i = 0; i < 5; i++) levels[i] = new WeaponLevel(2, 1, 3, 1, 3 + i);
            Set("levels", levels); Set("projectileSpeed", 8f); Set("projectileLifetime", 1f);
            run = new RunState(19); run.TryTransition(RunPhase.Playing);
            build = new BuildState(); build.TryAddWeapon("aura"); pools = new PoolService();
        }
        private void StartRunner(WeaponBehavior behavior)
        {
            Set("behavior", behavior);
            WeaponRunner.RegisterProjectilePool(pools, "aura-test", 4, root.transform);
            runner = root.AddComponent<WeaponRunner>(); runner.AutomaticTick = false;
            runner.Initialize(run, build, pools, new GameplayEventBus(), root.transform,
                () => TerrainType.Land, replacement == null ? new[] { definition } : new[] { definition, replacement }, "aura-test", 4);
            Physics2D.SyncTransforms();
        }
        private QuackTarget Target(Vector2 position)
        {
            var instance = new GameObject("Enemy"); instance.transform.SetParent(root.transform);
            instance.transform.position = position; instance.layer = 9;
            var target = instance.AddComponent<QuackTarget>();
            target.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            target.GetComponent<CircleCollider2D>().isTrigger = true;
            target.GetComponent<CircleCollider2D>().radius = 0.1f;
            return target;
        }
        private BoxCollider2D Wall()
        {
            var instance = new GameObject("Solid wall"); instance.transform.SetParent(root.transform);
            instance.layer = 8; instance.transform.position = Vector2.right;
            var collider = instance.AddComponent<BoxCollider2D>(); collider.size = new Vector2(0.1f, 4);
            return collider;
        }

        [TestCase(WeaponBehavior.EnergyAura, 18f)]
        [TestCase(WeaponBehavior.Boomerang, 20f)]
        [TestCase(WeaponBehavior.Eggs, 20f)]
        [TestCase(WeaponBehavior.FrontalQuack, 20f)]
        [TestCase(WeaponBehavior.Rockets, 20f)]
        [TestCase(WeaponBehavior.Bubbles, 20f)]
        public void OnlyAuraIgnoresWallForDamageAndWallRemainsSolid(WeaponBehavior behavior, float health)
        {
            var target = Target(Vector2.right * 2); var wall = Wall();
            StartRunner(behavior); runner.Advance(0); runner.Advance(0.4f);
            Assert.That(target.Health, Is.EqualTo(health));
            Assert.That(wall.enabled, Is.True); Assert.That(wall.isTrigger, Is.False);
            Assert.That(Physics2D.Raycast(Vector2.zero, Vector2.right, 2, 1 << 8).collider, Is.SameAs(wall));
        }

        [TestCase(2.9f, 18f)] [TestCase(3f, 18f)] [TestCase(3.2f, 20f)]
        public void AuraIncludesRadiusBoundaryButNotTargetOutsideIt(float distance, float health)
        {
            var target = Target(Vector2.right * distance);
            StartRunner(WeaponBehavior.EnergyAura); runner.Advance(0);
            Assert.That(target.Health, Is.EqualTo(health));
        }

        [Test]
        public void MultipleCollidersReceiveOneHitPerPulseAndPauseDoesNotDealDamage()
        {
            var target = Target(Vector2.right * 2);
            var extra = target.gameObject.AddComponent<BoxCollider2D>(); extra.isTrigger = true;
            StartRunner(WeaponBehavior.EnergyAura); runner.Advance(0);
            Assert.That(target.Health, Is.EqualTo(18));
            run.TryTransition(RunPhase.Paused); runner.Advance(10);
            Assert.That(target.Health, Is.EqualTo(18));
            run.TryResume(); runner.Advance(1);
            Assert.That(target.Health, Is.EqualTo(16));
        }

        private SpriteRenderer Aura() => root.GetComponentsInChildren<SpriteRenderer>(true).SingleOrDefault(r => r.name == "Aura 0");

        [Test]
        public void VisualUsesActualRadiusFollowsOwnerAnimatesOpacityAndFreezesWithPause()
        {
            StartRunner(WeaponBehavior.EnergyAura); runner.Advance(0);
            var aura = Aura(); Assert.That(aura, Is.Not.Null, "T019: aura range view is missing.");
            Assert.That(aura.enabled, Is.True);
            Assert.That(root.GetComponentsInChildren<Collider2D>().Length, Is.Zero, "The range view must not add physical collision.");
            Assert.That(aura.bounds.size.x, Is.EqualTo(6).Within(0.001f));
            float alpha = aura.color.a; Assert.That(alpha, Is.GreaterThan(0).And.LessThan(1));
            int renderers = root.GetComponentsInChildren<SpriteRenderer>(true).Length;
            runner.Advance(0.2f);
            Assert.That(aura.color.a, Is.Not.EqualTo(alpha));
            Assert.That(aura.bounds.size.x, Is.EqualTo(6).Within(0.001f));
            root.transform.position = new Vector2(5, 4); runner.Advance(0);
            Assert.That((Vector2)aura.bounds.center, Is.EqualTo(new Vector2(5, 4)));
            run.TryTransition(RunPhase.Paused); alpha = aura.color.a; runner.Advance(10);
            Assert.That(aura.color.a, Is.EqualTo(alpha));
            Assert.That(root.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(renderers));
            run.TryResume(); run.TryTransition(RunPhase.Lost);
            Assert.That(aura.enabled, Is.False);
        }

        [Test]
        public void VisualAndDamageUpdateTogetherAfterLevelAndRelicChangeBetweenPulses()
        {
            var target = Target(Vector2.right * 4.2f);
            StartRunner(WeaponBehavior.EnergyAura); runner.Advance(0);
            var aura = Aura(); Assert.That(aura, Is.Not.Null, "T019: aura range view is missing.");
            Assert.That(target.Health, Is.EqualTo(20));
            Assert.That(build.TryUpgradeWeapon("aura"), Is.True);
            var modifiers = new RelicModifier[10];
            for (int i = 0; i < 10; i++) modifiers[i] = new RelicModifier("area" + i, RelicAttribute.Area, RelicTerrain.Any, 0.1f);
            var relics = new RelicLoadout(modifiers); relics.Unlock("area0"); relics.Equip("area0"); runner.Relics = relics;
            runner.Advance(0.1f);
            Assert.That(aura.bounds.size.x, Is.EqualTo(8.8f).Within(0.001f));
            Assert.That(target.Health, Is.EqualTo(20));
            runner.Advance(1); Assert.That(target.Health, Is.EqualTo(18));
            relics.Unequip("area0"); runner.Advance(0);
            Assert.That(aura.bounds.size.x, Is.EqualTo(8).Within(0.001f));
            runner.enabled = false; Assert.That(aura.enabled, Is.False);
        }

        [Test]
        public void ReplacingAuraWithAnotherWeaponHidesItsViewWithoutCreatingNewObjects()
        {
            replacement = Object.Instantiate(definition);
            typeof(ContentDefinition).GetField("id", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(replacement, "replacement");
            typeof(WeaponDefinition).GetField("behavior", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(replacement, WeaponBehavior.Bubbles);
            StartRunner(WeaponBehavior.EnergyAura); runner.Advance(0);
            var aura = Aura(); Assert.That(aura, Is.Not.Null);
            int renderers = root.GetComponentsInChildren<SpriteRenderer>(true).Length;
            for (int i = 1; i < 5; i++) Assert.That(build.TryUpgradeWeapon("aura"), Is.True);
            Assert.That(build.TryAddPassive("required"), Is.True);
            Assert.That(build.TryEvolveWeapon("aura", "required", "replacement"), Is.True);
            runner.Advance(0);
            Assert.That(aura.enabled, Is.False);
            Assert.That(root.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(renderers));
        }

        [Test]
        public void WorldRadiusIsPreservedUnderScaledHierarchyAndVisualCanBeDisabled()
        {
            StartRunner(WeaponBehavior.EnergyAura);
            root.transform.localScale = new Vector3(2, 3, 1);
            runner.Advance(0);
            var aura = Aura(); Assert.That(aura, Is.Not.Null);
            Assert.That(aura.bounds.size.x, Is.EqualTo(6).Within(0.001f));
            Assert.That(aura.bounds.size.y, Is.EqualTo(6).Within(0.001f));
            var view = root.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "WeaponVisuals");
            view.enabled = false; runner.Advance(0.1f);
            Assert.That(aura.enabled, Is.False);
        }

        private void Set(string field, object value) => typeof(WeaponDefinition).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(definition, value);
        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (runner != null) runner.enabled = false;
            pools.Dispose(); Object.Destroy(root); Object.Destroy(definition); Object.Destroy(sprite); Object.Destroy(texture);
            if (replacement != null) Object.Destroy(replacement);
            yield return null;
        }
    }
}
