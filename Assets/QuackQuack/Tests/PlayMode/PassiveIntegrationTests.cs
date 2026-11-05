using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Content.Characters;
using QuackQuack.Content.Passives;
using QuackQuack.Content.Shared;
using QuackQuack.Content.Weapons;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Core.Shared;
using QuackQuack.Gameplay.Player;
using QuackQuack.Gameplay.Weapons;
using QuackQuack.Gameplay.Terrain;
using QuackQuack.Platform.Input;
using QuackQuack.Systems.Combat;
using QuackQuack.Systems.Pooling;
using QuackQuack.Systems.Upgrades;
using UnityEngine;
using UnityEngine.TestTools;

namespace QuackQuack.Tests.PlayMode
{
    public class PassiveIntegrationTests
    {
        private GameObject root;
        private readonly List<ScriptableObject> assets = new List<ScriptableObject>();
        private WeaponDefinition weapon;
        private CharacterDefinition character;
        private PassiveDefinition damage, attack, capacity, recovery, movement;
        private UpgradeSystem upgrades;
        private BuildState build;
        private RunState run;
        private PoolService pools;
        private WeaponRunner runner;
        private PlayerController player;
        private readonly Commands commands = new Commands();
        private sealed class Commands : IPlayerInput
        { public PlayerCommand Read() => new PlayerCommand(Vector2.right); }

        [SetUp]
        public void Setup()
        {
            root = new GameObject("Passive integration");
            weapon = Create<WeaponDefinition>("weapon");
            Set(weapon, "behavior", WeaponBehavior.FrontalQuack);
            Set(weapon, "targetMode", WeaponTargetMode.Self);
            var levels = new WeaponLevel[5];
            for (int i = 0; i < 5; i++) levels[i] = new WeaponLevel(2 + i, 1, 6, 0, 3);
            Set(weapon, "levels", levels);
            Set(weapon, "hasTerrainAffinity", true);
            Set(weapon, "waterDamageMultiplier", 2f);
            damage = Passive("damage", PassiveAttribute.Damage);
            attack = Passive("attack", PassiveAttribute.AttackSpeed);
            capacity = Passive("capacity", PassiveAttribute.BreathCapacity);
            recovery = Passive("recovery", PassiveAttribute.BreathRecovery);
            movement = Passive("movement", PassiveAttribute.MovementSpeed);
            character = Create<CharacterDefinition>("pato");
            Set(character, "quack", Create<QuackDefinition>("quack"));
            build = new BuildState(); Assert.That(build.TryAddWeapon(weapon.Id), Is.True);
            run = new RunState(17); run.TryTransition(RunPhase.Playing);
            pools = new PoolService();
            upgrades = new UpgradeSystem(17, build, new[] { weapon },
                new[] { damage, attack, capacity, recovery, movement }, new EvolutionDefinition[0]);
            upgrades.BindRun(run);
        }

        private void Attach(object target)
        {
            var property = target.GetType().GetProperty("Upgrades");
            Assert.That(property, Is.Not.Null, "T017: gameplay consumer must bind the same UpgradeSystem.");
            property.SetValue(target, upgrades);
        }

        private void StartPlayer()
        {
            var instance = new GameObject("Pato"); instance.transform.SetParent(root.transform);
            player = instance.AddComponent<PlayerController>();
            player.Initialize(character, commands, run, new GameplayEventBus());
            Attach(player);
        }

        private QuackTarget StartWeapon()
        {
            var enemy = new GameObject("Target"); enemy.transform.SetParent(root.transform);
            enemy.transform.position = Vector2.right * 2; enemy.layer = 9;
            var target = enemy.AddComponent<QuackTarget>(); target.Health = 1000;
            target.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            target.GetComponent<CircleCollider2D>().isTrigger = true;
            WeaponRunner.RegisterProjectilePool(pools, "passive-projectiles", 4, root.transform);
            runner = root.AddComponent<WeaponRunner>(); runner.AutomaticTick = false;
            runner.Initialize(run, build, pools, new GameplayEventBus(), root.transform,
                () => player == null ? TerrainType.Land : player.State.Terrain, new[] { weapon }, "passive-projectiles", 4);
            Attach(runner);
            Physics2D.SyncTransforms();
            return target;
        }

        [Test]
        public void WeaponDamageAndIntervalChangeThroughConfirmedOffersWithoutAccumulation()
        {
            var target = StartWeapon();
            runner.Advance(0); Assert.That(target.Health, Is.EqualTo(998));
            Choose(damage.Id, "damage-1"); Choose(attack.Id, "attack-1");
            runner.Advance(1); Assert.That(target.Health, Is.EqualTo(995.8f).Within(0.0001f));
            runner.Advance(0.89f); Assert.That(target.Health, Is.EqualTo(995.8f).Within(0.0001f));
            runner.Advance(0.02f); Assert.That(target.Health, Is.EqualTo(993.6f).Within(0.0001f));
            Choose(damage.Id, "damage-2");
            runner.Advance(1); Assert.That(target.Health, Is.EqualTo(991.2f).Within(0.0001f));
        }

        [UnityTest]
        public IEnumerator CapacityAndRecoveryAffectLiveReserveWithoutFreeRefillOrFrameStacking()
        {
            StartPlayer();
            Assert.That(player.State.Breath.TryConsume(35, out _), Is.True);
            float before = player.State.Breath.Current;
            Choose(capacity.Id, "capacity-1"); Choose(recovery.Id, "recovery-1");
            run.TryTransition(RunPhase.Paused);
            yield return new WaitForFixedUpdate();
            Assert.That(player.State.Breath.Current, Is.EqualTo(before));
            run.TryResume();
            yield return new WaitForFixedUpdate();
            Assert.That(player.State.Breath.Capacity, Is.EqualTo(110).Within(0.0001f));
            Assert.That(player.State.Breath.Current, Is.EqualTo(before + 12 * 1.1f * Time.fixedDeltaTime).Within(0.001f));
            for (int i = 0; i < 8; i++) yield return new WaitForFixedUpdate();
            Assert.That(player.State.Breath.Capacity, Is.EqualTo(110).Within(0.0001f));
            Choose(capacity.Id, "capacity-2");
            yield return new WaitForFixedUpdate();
            Assert.That(player.State.Breath.Capacity, Is.EqualTo(120).Within(0.0001f));
        }

        [UnityTest]
        public IEnumerator MovementRecomputesAfterBuildAndRelicReplacementAndDetach()
        {
            StartPlayer();
            yield return null;
            yield return new WaitForFixedUpdate();
            Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.x, Is.EqualTo(4).Within(0.001f));
            Choose(movement.Id, "move-1");
            yield return new WaitForFixedUpdate();
            Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.x, Is.EqualTo(4.4f).Within(0.001f));
            var modifiers = new RelicModifier[10];
            for (int i = 0; i < 10; i++) modifiers[i] = new RelicModifier("relic" + i, RelicAttribute.MovementSpeed, RelicTerrain.Land, 0.1f);
            var relics = new RelicLoadout(modifiers); relics.Unlock("relic0"); relics.Equip("relic0");
            player.Relics = relics;
            yield return new WaitForFixedUpdate();
            Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.x, Is.EqualTo(4.84f).Within(0.001f));
            relics.Unequip("relic0");
            yield return new WaitForFixedUpdate();
            Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.x, Is.EqualTo(4.4f).Within(0.001f));
            typeof(PlayerController).GetProperty("Upgrades").SetValue(player, null);
            yield return new WaitForFixedUpdate();
            Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.x, Is.EqualTo(4).Within(0.001f));
        }

        [Test]
        public void TerrainAndRelicChangesReachPlayerRecoveryAndActualWeaponDamage()
        {
            StartPlayer();
            var target = StartWeapon();
            Choose(damage.Id, "damage"); Choose(recovery.Id, "recovery");
            player.State.Breath.TryConsume(100, out _);
            player.ApplyCommand(new PlayerCommand(Vector2.right));
            var zone = root.AddComponent<TerrainZone>(); zone.Configure(new Vector2(10, 10), 0);
            Set(player, "water", new[] { zone });
            player.SendMessage("FixedUpdate");
            Assert.That(player.State.Terrain, Is.EqualTo(TerrainType.Water));
            Assert.That(player.State.Breath.Current, Is.EqualTo(24 * 1.1f * Time.fixedDeltaTime).Within(0.001f));
            runner.Advance(0);
            Assert.That(target.Health, Is.EqualTo(995.6f).Within(0.0001f));
            var modifiers = new RelicModifier[10];
            for (int i = 0; i < 10; i++) modifiers[i] = new RelicModifier("r" + i, RelicAttribute.Damage, RelicTerrain.Land, 0.1f);
            var relics = new RelicLoadout(modifiers); relics.Unlock("r0"); relics.Equip("r0");
            runner.Relics = relics;
            zone.enabled = false; player.SendMessage("FixedUpdate");
            Assert.That(player.State.Terrain, Is.EqualTo(TerrainType.Land));
            runner.Advance(1);
            Assert.That(target.Health, Is.EqualTo(993.18f).Within(0.0001f));
            relics.Unequip("r0"); runner.Advance(1);
            Assert.That(target.Health, Is.EqualTo(990.98f).Within(0.0001f));
        }

        private void Choose(string id, string offerId)
        {
            var offer = upgrades.GenerateOffer(offerId, new[] { id });
            Assert.That(offer, Is.Not.Null);
            Assert.That(upgrades.TryConfirm(offer.OfferId, id), Is.True);
        }
        private T Create<T>(string id) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>(); assets.Add(asset);
            typeof(ContentDefinition).GetField("id", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asset, id);
            return asset;
        }
        private PassiveDefinition Passive(string id, PassiveAttribute attribute)
        {
            var asset = Create<PassiveDefinition>(id); Set(asset, "attribute", attribute);
            Set(asset, "compatibleWeaponId", "weapon"); return asset;
        }
        private static void Set(object instance, string field, object value) =>
            instance.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(instance, value);

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (runner != null) runner.enabled = false;
            Object.Destroy(root); yield return null;
            upgrades.Dispose(); pools.Dispose();
            foreach (var asset in assets) Object.Destroy(asset);
            assets.Clear(); yield return null;
        }
    }
}
