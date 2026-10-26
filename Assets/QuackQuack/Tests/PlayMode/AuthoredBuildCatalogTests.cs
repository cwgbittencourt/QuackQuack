#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using QuackQuack.Content.Catalogs;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Player;
using QuackQuack.Gameplay.Weapons;
using QuackQuack.Systems.Combat;
using QuackQuack.Systems.Pooling;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace QuackQuack.Tests.PlayMode
{
    public class AuthoredBuildCatalogTests
    {
        private GameObject root;
        private PoolService pools;
        private WeaponRunner runner;

        [TestCase(false)]
        [TestCase(true)]
        public void AuthoredBasicAndEvolvedWeaponsRunThroughRealBoundedProjectilePool(bool evolved)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PrototypeBuildCatalog>("Assets/QuackQuack/Content/Catalogs/PrototypeBuildCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.IsValid(out var reason), Is.True, reason);
            var build = new BuildState();
            var expected = new HashSet<int>();
            if (!evolved)
            {
                foreach (var weapon in catalog.Weapons) { build.TryAddWeapon(weapon.Id); expected.Add(weapon.GetInstanceID()); }
            }
            else
            {
                using (var upgrades = catalog.CreateUpgradeSystem(42, build))
                {
                    foreach (var evolution in catalog.Evolutions)
                    {
                        build.TryAddWeapon(evolution.BaseWeapon.Id);
                        for (int i = 1; i < 5; i++) build.TryUpgradeWeapon(evolution.BaseWeapon.Id);
                        build.TryAddPassive(evolution.RequiredPassive.Id);
                        upgrades.GenerateOffer(evolution.Id, new[] { evolution.Id });
                        Assert.That(upgrades.TryConfirm(evolution.Id, evolution.Id), Is.True);
                        expected.Add(evolution.ResultWeapon.GetInstanceID());
                    }
                }
            }
            root = new GameObject("Authored build test");
            var enemy = new GameObject("Training enemy"); enemy.transform.SetParent(root.transform);
            enemy.transform.position = Vector3.right; enemy.layer = 9;
            var target = enemy.AddComponent<QuackTarget>(); target.Health = 1000;
            target.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            target.GetComponent<CircleCollider2D>().isTrigger = true;
            Physics2D.SyncTransforms();
            var run = new RunState(42); run.TryTransition(RunPhase.Playing);
            pools = new PoolService();
            WeaponRunner.RegisterProjectilePool(pools, "authored", 16, root.transform);
            var bus = new GameplayEventBus();
            var sources = new HashSet<int>();
            bus.Combat += value => { if (value.Type == CombatEventType.DamageApplied) sources.Add(value.Source); };
            runner = root.AddComponent<WeaponRunner>(); runner.AutomaticTick = false;
            runner.Initialize(run, build, pools, bus, root.transform, () => TerrainType.Land, catalog.GetAllWeapons(), "authored", 16);
            runner.Advance(0); runner.Advance(0.25f);
            CollectionAssert.AreEquivalent(expected, sources);
            Assert.That(target.Health, Is.LessThan(1000));
            Assert.That(pools.ActiveCount, Is.LessThanOrEqualTo(16));
            runner.enabled = false;
            Assert.That(pools.ActiveCount, Is.Zero);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (runner != null) runner.enabled = false;
            pools?.Dispose();
            if (root != null) Object.Destroy(root);
            yield return null;
        }
    }
}
#endif
