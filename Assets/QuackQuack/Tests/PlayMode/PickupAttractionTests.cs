using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Content.Characters;
using QuackQuack.Content.Pickups;
using QuackQuack.Content.Shared;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Core.Shared;
using QuackQuack.Gameplay.Pickups;
using QuackQuack.Gameplay.Player;
using QuackQuack.Platform.Input;
using QuackQuack.Systems.Combat;
using QuackQuack.Systems.Pickups;
using QuackQuack.Systems.Pooling;
using QuackQuack.Systems.Upgrades;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.PlayMode
{
    // No UnityEditor/assets/scenes: the same fixture runs in native test players.
    public class PickupAttractionTests
    {
        private sealed class NoInput : IPlayerInput { public PlayerCommand Read() => default; }
        private GameObject root;
        private PlayerController player;
        private RunState run;
        private PoolService pools;
        private GameplayEventBus events;
        private UpgradeSystem upgrades;
        private PickupPolicyDefinition policy;
        private readonly List<Object> owned = new List<Object>();
        private RewardLedger ledger;
        private PickupAttractionSystem system;
        private readonly ExperiencePickup[] xp = new ExperiencePickup[1];
        private readonly HealingPickup[] heal = new HealingPickup[1];
        private static void Set(object target, Type type, string field, object value) => type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private T Definition<T>(string id) where T : ContentDefinition
        { var value = ScriptableObject.CreateInstance<T>(); Set(value, typeof(ContentDefinition), "id", id); owned.Add(value); return value; }

        [SetUp] public void Setup()
        {
            xp[0] = null; heal[0] = null;
            root = new GameObject("Attraction fixture"); run = new RunState(29); run.TryTransition(RunPhase.Playing);
            pools = new PoolService(); events = new GameplayEventBus();
            var quack = Definition<QuackDefinition>("quack"); var character = Definition<CharacterDefinition>("pato");
            Set(character, typeof(CharacterDefinition), "quack", quack);
            var duck = new GameObject("Duck"); duck.transform.SetParent(root.transform);
            player = duck.AddComponent<PlayerController>(); player.Initialize(character, new NoInput(), run, events);
            duck.GetComponent<CircleCollider2D>().radius = 0.42f; duck.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            upgrades = new UpgradeSystem(29); upgrades.InitializeProgression(run, new[] { 1000 }, Array.Empty<string>());
            policy = Definition<PickupPolicyDefinition>("policy"); ledger = policy.CreateLedger(1, () => 0, 1);
            ExperiencePickup.RegisterPool(pools, "xp", 1, root.transform);
            pools.Register("heal", PoolFamily.Pickup, () =>
            { var item = new GameObject("Healing"); item.transform.SetParent(root.transform); item.SetActive(false); return item.AddComponent<HealingPickup>(); }, 1);
            system = new PickupAttractionSystem(run, player, policy, upgrades);
        }
        [TearDown] public void Cleanup()
        {
            Object.DestroyImmediate(root); upgrades.Dispose(); pools.Dispose(); events.Clear();
            foreach (var item in owned) Object.DestroyImmediate(item); owned.Clear();
        }
        private void Spawn(bool healing, float x, bool mini = false)
        {
            ledger.ProcessDefeat(7, 1, mini ? RewardOrigin.MiniBoss : RewardOrigin.Common, 1, x, 0, true);
            if (healing)
            {
                heal[0] = pools.Acquire<HealingPickup>("heal");
                heal[0].Initialize(pools, ledger, ledger.Entries[1].Id, player, run); heal[0].transform.position = new Vector2(x, 0);
                heal[0].GetComponent<CircleCollider2D>().radius = 0.1f; ledger.TryMaterialize(ledger.Entries[1].Id, true);
            }
            else
            {
                xp[0] = ExperiencePickup.Spawn(pools, "xp", upgrades, 1, new Vector2(x, 0));
                xp[0].BindReward(ledger, ledger.Entries[0].Id, upgrades.TryCollectExperience);
                xp[0].GetComponent<CircleCollider2D>().radius = 0.1f; ledger.TryMaterialize(ledger.Entries[0].Id, true);
            }
        }
        private void Advance(float seconds) => system.Advance(seconds, xp, heal);

        [TestCase(-0.0001f, true)] [TestCase(0f, true)] [TestCase(0.0001f, false)]
        public void CaptureIncludesExactPhysicalBoundaryWithoutRemoteCollection(float offset, bool captured)
        {
            float boundary = player.GetComponent<CircleCollider2D>().radius * player.transform.lossyScale.x * policy.CaptureBodyRadiusMultiplier;
            Spawn(false, boundary + offset); Advance(0);
            Assert.That(xp[0].Attraction.Captured, Is.EqualTo(captured));
            Assert.That(system.CaptureRadius, Is.EqualTo(1.26f).Within(0.00001f));
            Assert.That(upgrades.Experience, Is.Zero);
        }
        [Test]
        public void ChangingPhysicalScaleAndRadiusUpdatesCaptureWhileDecorationDoesNot()
        {
            Spawn(false, 2.4f); Advance(0); Assert.That(xp[0].Attraction.Captured, Is.False);
            var decoration = new GameObject("Large decoration"); decoration.transform.SetParent(player.transform); decoration.transform.localScale = Vector3.one * 100;
            Advance(0); Assert.That(xp[0].Attraction.Captured, Is.False);
            player.transform.localScale = Vector3.one * 2; Advance(0);
            Assert.That(xp[0].Attraction.Captured, Is.True); Assert.That(system.CaptureRadius, Is.EqualTo(2.52f).Within(0.00001f));
            player.GetComponent<CircleCollider2D>().radius = 0.25f; Advance(0);
            Assert.That(system.CaptureRadius, Is.EqualTo(1.5f));
        }
        [Test]
        public void CapturedExperienceFollowsBeyondRadiusAndCollectsOnceOnlyAtContact()
        {
            Spawn(false, 1.2f); Advance(0); player.transform.position = new Vector2(-10, 0); Advance(0.1f);
            Assert.That(xp[0].transform.position.x, Is.EqualTo(0.45f).Within(0.0001f));
            Assert.That(xp[0].Attraction.Captured, Is.True); Assert.That(upgrades.Experience, Is.Zero);
            Advance(10); Assert.That(upgrades.Experience, Is.EqualTo(1));
            Assert.That(xp[0].ViewState, Is.EqualTo(RewardState.Released)); Advance(10); Assert.That(upgrades.Experience, Is.EqualTo(1));
        }
        [Test]
        public void SpeedUsesWaterModifiersOnLandAndRefreshesWithLoadoutAndSlow()
        {
            var modifiers = new List<RelicModifier> { new RelicModifier("water", RelicAttribute.MovementSpeed, RelicTerrain.Water, 0.1f),
                new RelicModifier("land", RelicAttribute.MovementSpeed, RelicTerrain.Land, 0.5f) };
            for (int i = 0; i < 8; i++) modifiers.Add(new RelicModifier("unused" + i, RelicAttribute.Damage, RelicTerrain.Any, 0.01f));
            var relics = new RelicLoadout(modifiers);
            relics.Unlock("land"); relics.Equip("land");
            system = new PickupAttractionSystem(run, player, policy, upgrades, relics);
            Advance(0); Assert.That(player.State.Terrain, Is.EqualTo(TerrainType.Land)); Assert.That(system.Speed, Is.EqualTo(7.5f));
            relics.Unlock("water"); relics.Equip("water"); Advance(0); Assert.That(system.Speed, Is.EqualTo(8.25f).Within(0.0001f));
            player.SetEnemySlow(0.5f); Advance(0); Assert.That(system.Speed, Is.EqualTo(4.125f).Within(0.0001f));
            relics.Unequip("water"); Advance(0); Assert.That(system.Speed, Is.EqualTo(3.75f));
        }
        [TestCase(false)] [TestCase(true)]
        public void FullHealingStaysGroundedButCapturedHealingIsDiscardedWhenHealthBecomesFull(bool mini)
        {
            Spawn(true, 1.2f, mini); Advance(0.1f); Assert.That(heal[0].Attraction.Captured, Is.False);
            player.State.TakeDamage(50, 1, 0); Advance(0.01f); Assert.That(heal[0].Attraction.Captured, Is.True);
            var id = heal[0].BoundRewardId;
            player.State.Heal(1); player.transform.position = new Vector2(-3, 0);
            Advance(0.1f); Assert.That(pools.IsActive(heal[0], PoolFamily.Pickup), Is.False);
            Assert.That(player.State.Health, Is.EqualTo(100)); Assert.That(heal[0].AttractionTarget, Is.Null);
            ledger.TryGetEntry(id, out var entry); Assert.That(entry.State, Is.EqualTo(RewardState.Discarded));
            Assert.That(ledger.ReturnToPending(id), Is.False); Assert.That(ledger.TryDiscardCapturedHealing(id), Is.False);
            player.State.TakeDamage(20, 2, 0); Advance(1); Assert.That(player.State.Health, Is.EqualTo(80));
        }
        [TestCase(RunPhase.Paused)] [TestCase(RunPhase.ChoosingUpgrade)]
        public void PauseAndSelectionFreezeCaptureMotionAndClaim(RunPhase phase)
        {
            Spawn(false, 1.2f); Advance(0); var position = xp[0].transform.position;
            Assert.That(run.TryTransition(phase), Is.True); Advance(10);
            Assert.That(xp[0].transform.position, Is.EqualTo(position)); Assert.That(upgrades.Experience, Is.Zero);
        }
        [TestCase(false)] [TestCase(true)]
        public void DeathAndTerminalOutcomeCancelFollowingWithoutGrant(bool terminal)
        {
            Spawn(false, 1.2f); Advance(0);
            if (terminal) run.TryTransition(RunPhase.Abandoned); else player.State.TakeDamage(100, 1, 0);
            Advance(1); Assert.That(xp[0].Attraction.Captured, Is.False); Assert.That(xp[0].AttractionTarget, Is.Null);
            Assert.That(upgrades.Experience, Is.Zero);
        }
        [UnityTest]
        public IEnumerator PickupCrossesSolidWallWhilePlayerPhysicsStillBlocksMovement()
        {
            Spawn(false, 1.2f);
            var wall = new GameObject("Wall"); wall.transform.SetParent(root.transform); wall.layer = 8; wall.transform.position = new Vector2(0.7f, 0);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.1f, 4); Physics2D.SyncTransforms();
            Advance(0.2f); Assert.That(upgrades.Experience, Is.EqualTo(1));
            player.enabled = false; var body = player.GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Dynamic;
            for (int i = 0; i < 10; i++) { body.linearVelocity = Vector2.right * 10; yield return new WaitForFixedUpdate(); }
            Assert.That(body.position.x, Is.LessThan(0.3f));
        }
        [Test]
        public void ReleasedViewClearsAttractionAndReusedLeaseDoesNotInheritCapture()
        {
            Spawn(false, 1.2f); Advance(0); long lease = xp[0].ViewLeaseId; pools.Release(xp[0]);
            Assert.That(xp[0].Attraction.Captured, Is.False); Assert.That(xp[0].AttractionTarget, Is.Null);
            xp[0] = ExperiencePickup.Spawn(pools, "xp", upgrades, 1, new Vector2(9, 0));
            xp[0].BindReward(ledger, ledger.Entries[0].Id, upgrades.TryCollectExperience); ledger.TryMaterialize(ledger.Entries[0].Id, true); Advance(0.1f);
            Assert.That(xp[0].ViewLeaseId, Is.GreaterThan(lease)); Assert.That(xp[0].Attraction.Captured, Is.False);
            Assert.That(xp[0].transform.position.x, Is.EqualTo(9));
        }
        [Test]
        public void WarmCentralAdvanceDoesNotAllocatePerPickup()
        {
            Spawn(false, 1.2f); Advance(0);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) Advance(0);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
    }
}
