#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Content.Catalogs;
using QuackQuack.Content.Characters;
using QuackQuack.Content.Encounters;
using QuackQuack.Content.Relics;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Core.Shared;
using QuackQuack.Gameplay.Bosses;
using QuackQuack.Gameplay.Enemies;
using QuackQuack.Gameplay.Pickups;
using QuackQuack.Gameplay.Player;
using QuackQuack.Gameplay.Weapons;
using QuackQuack.Platform.Input;
using QuackQuack.Systems.Combat;
using QuackQuack.Systems.Pooling;
using QuackQuack.Systems.Run;
using QuackQuack.Systems.Save;
using QuackQuack.Systems.Upgrades;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.PlayMode
{
    // Authored-content integration in the Editor. Physical/player acceptance belongs to T032.
    public class PickupRewardFlowTests
    {
        private sealed class NoInput : IPlayerInput { public PlayerCommand Read() => default; }
        private GameObject root;
        private RunState run;
        private PoolService pools;
        private GameplayEventBus events;
        private RunDirector director;
        private PlayerController player;
        private UpgradeSystem upgrades;
        private LakeRunSettings settings;
        private CharacterDefinition character;
        private string directory;
        private long requestId;
        private Action<EnemyReward> replayNotification;
        private readonly Rect camera = new Rect(-2, -2, 4, 4);
        private readonly Vector2 floor = new Vector2(8, 5);
        private RewardLedger Ledger => director.Rewards;
        private ExperiencePickup[] XpViews => root.GetComponentsInChildren<ExperiencePickup>();
        private HealingPickup[] HealViews => root.GetComponentsInChildren<HealingPickup>();

        private void CreateRun(float maxHealth = 100, bool smallPools = false, bool multipleLevels = false)
        {
            requestId = 100000; // Explicit test requests supersede automatic wave requests.
            directory = Path.Combine(Path.GetTempPath(), "QuackQuack-T031-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            root = new GameObject("T031 reward flow"); run = new RunState(31);
            pools = new PoolService(); events = new GameplayEventBus();
            character = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/QuackQuack/Content/Characters/Pato.asset"));
            var serialized = new SerializedObject(character);
            serialized.FindProperty("health").floatValue = maxHealth; serialized.ApplyModifiedPropertiesWithoutUndo();
            var duck = new GameObject("Pato", typeof(PlayerController)); duck.transform.SetParent(root.transform);
            player = duck.GetComponent<PlayerController>(); player.Initialize(character, new NoInput(), run, events);
            duck.GetComponent<CircleCollider2D>().radius = 0.42f;
            duck.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            MovePlayer(new Vector2(-6, -2));
            var catalog = AssetDatabase.LoadAssetAtPath<PrototypeBuildCatalog>("Assets/QuackQuack/Content/Catalogs/PrototypeBuildCatalog.asset");
            var build = new BuildState(); build.TryAddWeapon(catalog.Weapons[0].Id);
            upgrades = catalog.CreateUpgradeSystem(run.Seed, build);
            var choices = catalog.Weapons.Select(d => d.Id).Concat(catalog.Passives.Select(d => d.Id)).ToArray();
            upgrades.InitializeProgression(run, multipleLevels ? new[] { 1, 2, 3, 4 } : new[] { 100000 }, choices);
            WeaponRunner.RegisterProjectilePool(pools, "T031/projectiles", 8, root.transform);
            var weapons = root.AddComponent<WeaponRunner>();
            weapons.Initialize(run, build, pools, events, player.transform, () => player.State.Terrain, catalog.GetAllWeapons(), "T031/projectiles", 8);
            var relicCatalog = AssetDatabase.LoadAssetAtPath<PrototypeRelicCatalog>("Assets/QuackQuack/Content/Relics/PrototypeRelicCatalog.asset");
            settings = Object.Instantiate(AssetDatabase.LoadAssetAtPath<LakeRunSettings>("Assets/QuackQuack/Content/Encounters/LakeRunSettings.asset"));
            if (smallPools) { settings.PickupCapacity = 2; settings.HealingCapacity = 3; settings.MiniBossReservedHealingSlots = 1; }
            var refuge = new GameObject("Refuge", typeof(CircleCollider2D), typeof(FrogKingSafeArea)); refuge.transform.SetParent(root.transform);
            refuge.transform.position = new Vector2(4, 0); refuge.GetComponent<CircleCollider2D>().isTrigger = true;
            director = root.AddComponent<RunDirector>(); director.AutomaticTick = false;
            director.Initialize(run, pools, events, player, upgrades, weapons, catalog, relicCatalog.CreateLoadout(), settings,
                new[] { refuge.GetComponent<FrogKingSafeArea>() }, new JsonSaveService(Path.Combine(directory, "save.json"), relicCatalog.Relics.Select(d => d.Id).ToArray()));
            director.Scheduler.enabled = false; weapons.enabled = false;
            // Only protocol replay uses this adapter; first emission always comes from a real spawned enemy death.
            replayNotification = (Action<EnemyReward>)Delegate.CreateDelegate(typeof(Action<EnemyReward>), director,
                typeof(RunDirector).GetMethod("Reward", BindingFlags.NonPublic | BindingFlags.Instance));
        }

        [TearDown] public void Cleanup()
        {
            if (root != null) Object.DestroyImmediate(root);
            upgrades?.Dispose(); pools?.Dispose(); events?.Clear();
            if (character != null) Object.DestroyImmediate(character);
            if (settings != null) Object.DestroyImmediate(settings);
            if (directory == null) return;
            string resolved = Path.GetFullPath(directory);
            string temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Assert.That(resolved.StartsWith(temp, StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(Path.GetFileName(resolved), Does.StartWith("QuackQuack-T031-"));
            if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
            root = null; directory = null;
        }

        private void MovePlayer(Vector2 position)
        { player.transform.position = position; player.GetComponent<Rigidbody2D>().position = position; Physics2D.SyncTransforms(); }

        private PooledEnemy Spawn(string id, Vector2? position = null)
        {
            Assert.That(director.Spawns.TryEnqueue(new SpawnRequest(++requestId, "T031", id, 1,
                new Rect(8, 5, 1, 1), 0, EventPriority.Essential, long.MaxValue)), Is.True);
            director.Advance(0, camera);
            var enemy = root.GetComponentsInChildren<PooledEnemy>().Single(e => e.IsReady && e.Definition.Id == id);
            enemy.transform.position = position ?? floor; enemy.Body.position = position ?? floor;
            Physics2D.SyncTransforms(); return enemy;
        }

        private EnemyReward Kill(string id = "formiga", Vector2? position = null)
        {
            var enemy = Spawn(id, position); var definition = enemy.Definition;
            var receipt = new EnemyReward(enemy.LeaseId, id, definition.ExperienceReward, definition.CoinReward, enemy.Body.position);
            Assert.That(enemy.TakeDamage(receipt.LeaseId, enemy.State.Health, player.GetInstanceID(), requestId), Is.True);
            Assert.That(enemy.TakeDamage(receipt.LeaseId, 99999, player.GetInstanceID(), requestId), Is.False);
            return receipt;
        }

        private HealingPickup Healing(bool mini)
        {
            if (mini) Kill("libelula-alfa");
            else
                for (int i = 0; i < 64 && !HealViews.Any(p => p.BoundRewardId.Kind == RewardKind.HealCommon); i++) Kill();
            var kind = mini ? RewardKind.HealMiniBoss : RewardKind.HealCommon;
            return HealViews.Single(p => p.BoundRewardId.Kind == kind);
        }

        private static RewardId Id(IPickupAttractionView view) => view is HealingPickup heal ? heal.BoundRewardId : ((ExperiencePickup)view).BoundRewardId;
        private static Transform Target(IPickupAttractionView view) => view is HealingPickup heal ? heal.AttractionTarget : ((ExperiencePickup)view).AttractionTarget;

        private void Collect(IPickupAttractionView view)
        {
            MovePlayer(view.PickupTransform.position);
            view.CollectContact(player.GetComponent<Collider2D>());
        }

        private void AssertConserved()
        {
            Assert.That(Ledger.Entries.Select(e => e.Id).Distinct().Count(), Is.EqualTo(Ledger.Entries.Count));
            int pending = Ledger.Entries.Count(e => e.State == RewardState.Due || e.State == RewardState.WaitingForView);
            Assert.That(Ledger.PendingCount, Is.EqualTo(pending));
            var bound = XpViews.Select(p => p.BoundRewardId).Concat(HealViews.Select(p => p.BoundRewardId)).ToArray();
            Assert.That(bound.Distinct().Count(), Is.EqualTo(bound.Length));
            foreach (var entry in Ledger.Entries)
            {
                bool presented = entry.State == RewardState.Grounded || entry.State == RewardState.Attracted;
                Assert.That(bound.Count(id => id.Equals(entry.Id)), Is.EqualTo(presented ? 1 : 0), Receipt(entry));
            }
        }

        private static string Receipt(RewardEntry entry) =>
            $"run={entry.Id.RunId} source={entry.Id.SourceId} lease={entry.Id.LeaseId} kind={entry.Kind} ordinal={entry.Id.Ordinal} origin={entry.Origin} pos=({entry.X:R},{entry.Y:R}) xp={entry.Experience} state={entry.State}";

        private void Record(string stage)
        {
            Debug.Log($"T031_REWARD_FLOW stage={stage} entries={Ledger.Entries.Count} pending={Ledger.PendingCount} xp={upgrades.Experience} health={player.State.Health:R}/{player.State.MaxHealth:R}");
            foreach (var entry in Ledger.Entries.Take(2).Concat(Ledger.Entries.Where(e => e.State == RewardState.WaitingForView).Take(1)).Distinct())
                Debug.Log("T031_RECEIPT " + Receipt(entry));
        }

        [TestCase("formiga")] [TestCase("aranha")] [TestCase("vespa")] [TestCase("besouro")]
        [TestCase("caramujo-aquatico")] [TestCase("libelula-alfa")] [TestCase("sapo-cururu")]
        public void RealDeathAndReplayedNotificationKeepOneRewardAndOneCoinReceipt(string id)
        {
            CreateRun(); var death = Kill(id, new Vector2(7, 4));
            var before = Ledger.Entries.Select(Receipt).ToArray();
            replayNotification(death); replayNotification(death);
            Assert.That(Ledger.Entries.Select(Receipt), Is.EqualTo(before)); Assert.That(Ledger.DefeatCount, Is.EqualTo(1));
            var xp = Ledger.Entries.Single(e => e.Kind == RewardKind.Experience);
            Assert.That(xp.Experience, Is.EqualTo(death.Experience)); Assert.That(new Vector2(xp.X, xp.Y), Is.EqualTo(death.Position));
            bool mini = id == "libelula-alfa" || id == "sapo-cururu";
            Assert.That(Ledger.Entries.Count(e => e.Kind == RewardKind.HealMiniBoss), Is.EqualTo(mini ? 1 : 0));
            Assert.That(Ledger.Entries.Count(e => e.Kind == RewardKind.HealCommon), Is.Zero); // First seeded common roll is 0.348.
            var view = XpViews.Single(); Collect(view); Collect(view);
            Assert.That(upgrades.Experience, Is.EqualTo(death.Experience)); Assert.That(xp.State, Is.EqualTo(RewardState.Claimed));
            AssertConserved(); Record(id); director.TryAbandon();
            Assert.That(director.Result.EnemiesDefeated, Is.EqualTo(1)); Assert.That(director.Result.EarnedCoins, Is.EqualTo(death.Coins));
        }

        [Test]
        public void NonlethalDespawnAndStaleEnemyLeaseDoNotProduceDrops()
        {
            CreateRun(); var enemy = Spawn("formiga"); long oldLease = enemy.LeaseId;
            Assert.That(enemy.TakeDamage(oldLease, 1, 1, 1), Is.True);
            Assert.That(Ledger.Entries, Is.Empty); Assert.That(enemy.Despawn(oldLease), Is.True);
            var reused = Spawn("formiga"); Assert.That(reused, Is.SameAs(enemy)); Assert.That(reused.LeaseId, Is.Not.EqualTo(oldLease));
            Assert.That(reused.TakeDamage(oldLease, 99999, 1, 2), Is.False); Assert.That(Ledger.DefeatCount, Is.Zero);
            Assert.That(reused.TakeDamage(reused.LeaseId, reused.State.Health, 1, 3), Is.True);
            Assert.That(Ledger.DefeatCount, Is.EqualTo(1)); AssertConserved(); Record("enemy-reuse");
        }

        [Test]
        public void CommonDropSequenceSurvivesReplayDespawnMiniDeathsAndOfferRandom()
        {
            CreateRun(); var expectedRandom = new System.Random(run.Seed ^ 0x52dce729);
            int expectedHeals = 0;
            for (int i = 0; i < 24; i++)
            {
                bool expected = expectedRandom.NextDouble() < 0.1;
                var death = Kill();
                int after = Ledger.Entries.Count; replayNotification(death);
                Assert.That(Ledger.Entries.Count, Is.EqualTo(after));
                Assert.That(Ledger.Entries.Any(e => e.Id.LeaseId == death.LeaseId && e.Kind == RewardKind.HealCommon), Is.EqualTo(expected));
                if (expected) expectedHeals++;
                if (i % 4 != 0) continue;
                var despawn = Spawn("formiga"); Assert.That(despawn.Despawn(despawn.LeaseId), Is.True);
                Kill("libelula-alfa");
                var offer = upgrades.GenerateOffer("T031-rng-" + i);
                Assert.That(upgrades.TryReroll(offer.OfferId) || upgrades.RerollsRemaining == 0, Is.True);
                Assert.That(upgrades.TryConfirm(upgrades.CurrentOffer.OfferId, upgrades.CurrentOffer.Choices[0]), Is.True);
            }
            Assert.That(Ledger.Entries.Count(e => e.Kind == RewardKind.HealCommon), Is.EqualTo(expectedHeals));
            Assert.That(Ledger.Entries.Count(e => e.Kind == RewardKind.HealMiniBoss), Is.EqualTo(6));
            Assert.That(upgrades.Experience, Is.Zero); AssertConserved(); Record("seeded-drop-sequence");
        }

        [TestCase(false, 100f, 0.6f)] [TestCase(true, 100f, 0.6f)]
        [TestCase(false, 200f, 0.6f)] [TestCase(true, 200f, 0.6f)]
        [TestCase(false, 200f, 0.05f)] [TestCase(true, 200f, 0.05f)]
        public void HealingUsesActualMaximumClampsToMissingHealthAndClaimsOnce(bool mini, float maximum, float missing)
        {
            CreateRun(maximum); var view = Healing(mini); var id = view.BoundRewardId;
            Assert.That(player.State.MaxHealth, Is.EqualTo(maximum)); player.State.TakeDamage(maximum * missing, 1, 0);
            float before = player.State.Health;
            float fraction = mini ? settings.PickupPolicy.MiniBossHealFraction : settings.PickupPolicy.CommonHealFraction;
            Collect(view); Collect(view);
            Assert.That(player.State.Health, Is.EqualTo(before + Mathf.Min(maximum * missing, maximum * fraction)).Within(0.0001f));
            Assert.That(Ledger.TryGetEntry(id, out var entry), Is.True); Assert.That(entry.State, Is.EqualTo(RewardState.Claimed));
            Assert.That(view.ViewLeaseId, Is.Zero); AssertConserved(); Record("heal-max-" + maximum);
        }

        [TestCase(false)] [TestCase(true)]
        public void FullHealthLeavesUncapturedHealingOnFloorUntilDamage(bool mini)
        {
            CreateRun(); var view = Healing(mini); var id = view.BoundRewardId; Vector3 position = view.transform.position;
            MovePlayer(position); director.Advance(0, camera);
            Assert.That(view.ViewState, Is.EqualTo(RewardState.Grounded)); Assert.That(Target(view), Is.Null);
            Assert.That(view.transform.position, Is.EqualTo(position)); Assert.That(player.State.Health, Is.EqualTo(100));
            player.State.TakeDamage(10, 1, 0); director.Advance(0, camera);
            Assert.That(player.State.Health, Is.EqualTo(100)); Assert.That(Ledger.TryGetEntry(id, out var entry), Is.True);
            Assert.That(entry.State, Is.EqualTo(RewardState.Claimed)); AssertConserved(); Record("full-then-damage");
        }

        [TestCase(false)] [TestCase(true)]
        public void CapturedHealingIsDiscardedWhenHealthFillsWithoutGrantOrRequeue(bool mini)
        {
            CreateRun(); var view = Healing(mini); var id = view.BoundRewardId;
            player.State.TakeDamage(80, 1, 0); MovePlayer(floor + Vector2.left); director.Advance(0, camera);
            Assert.That(view.ViewState, Is.EqualTo(RewardState.Attracted)); Assert.That(Target(view), Is.EqualTo(player.transform));
            player.State.Heal(1); director.Advance(0, camera);
            Assert.That(Ledger.TryGetEntry(id, out var entry), Is.True); Assert.That(entry.State, Is.EqualTo(RewardState.Discarded));
            Assert.That(view.ViewLeaseId, Is.Zero); Assert.That(Target(view), Is.Null);
            player.State.TakeDamage(30, 2, 0); director.Advance(1, camera);
            Assert.That(player.State.Health, Is.EqualTo(70)); Assert.That(entry.State, Is.EqualTo(RewardState.Discarded));
            AssertConserved(); Record("discard-full-captured");
        }

        [TestCase(false)] [TestCase(true)]
        public void DeadPlayerCannotCollectXpOrHealingAndLossCancelsBoth(bool mini)
        {
            CreateRun(); var heal = Healing(mini); var xp = XpViews.First(); int count = Ledger.Entries.Count;
            player.State.TakeDamage(100, 1, 0); Collect(heal); Collect(xp);
            Assert.That(player.State.Health, Is.Zero); Assert.That(upgrades.Experience, Is.Zero);
            Assert.That(Ledger.Entries.Any(e => e.State == RewardState.Claimed), Is.False);
            director.Advance(0, camera);
            Assert.That(run.Outcome, Is.EqualTo(RunPhase.Lost)); Assert.That(Ledger.Closure.Cancelled, Is.EqualTo(count));
            Assert.That(pools.ActiveCount, Is.Zero); AssertConserved(); Record("dead-no-revive");
        }

        [Test]
        public void SaturatedAuthoredPoolsDrainEveryObligationWithOriginalIdentityAndPosition()
        {
            CreateRun(); int deaths = settings.PickupCapacity + 7;
            for (int i = 0; i < deaths; i++) Kill();
            Kill("libelula-alfa"); Kill("sapo-cururu");
            var original = Ledger.Entries.ToDictionary(e => e.Id, e => new Vector2(e.X, e.Y));
            int xpTotal = Ledger.Entries.Sum(e => e.Experience);
            Assert.That(Ledger.Entries.Count(e => e.Kind == RewardKind.Experience && e.State == RewardState.WaitingForView), Is.GreaterThan(0));
            Assert.That(Ledger.TryGetPending(RewardKind.HealCommon, out _), Is.True);
            Assert.That(HealViews.Count(p => p.BoundRewardId.Kind == RewardKind.HealMiniBoss), Is.EqualTo(2));
            Assert.That(XpViews.Length, Is.EqualTo(settings.PickupCapacity)); Assert.That(HealViews.Length, Is.EqualTo(settings.HealingCapacity));
            Assert.That(upgrades.Experience, Is.Zero); AssertConserved(); Record("saturated");
            for (int pass = 0; pass < 32 && Ledger.Entries.Any(e => e.State != RewardState.Claimed); pass++)
            {
                // Use the production attraction/contact driver, not direct grants into the ledger.
                MovePlayer(floor); if (player.State.Health > 0) player.State.TakeDamage(player.State.Health * 0.8f, pass + 1, 0);
                director.Advance(0, camera); AssertConserved();
            }
            Assert.That(Ledger.Entries.All(e => e.State == RewardState.Claimed), Is.True);
            Assert.That(Ledger.PendingCount, Is.Zero); Assert.That(pools.GetActiveCount(PoolFamily.Pickup), Is.Zero);
            Assert.That(upgrades.Experience, Is.EqualTo(xpTotal)); Assert.That(Ledger.Entries.Count, Is.EqualTo(original.Count));
            foreach (var entry in Ledger.Entries) Assert.That(new Vector2(entry.X, entry.Y), Is.EqualTo(original[entry.Id]), Receipt(entry));
            director.TryAbandon(); Assert.That(director.Result.EnemiesDefeated, Is.EqualTo(deaths + 2));
            Record("drained");
        }

        [Test]
        public void OnePickupCrossesSeveralLevelsAndEachOfferResolvesOnceBeforeOtherPickupsResume()
        {
            CreateRun(multipleLevels: true); Kill("libelula-alfa"); Kill("formiga", floor + Vector2.right * 3);
            var first = XpViews.Single(p => Ledger.Entries.Single(e => e.Id.Equals(p.BoundRewardId)).Experience == 50);
            var second = XpViews.Single(p => p != first); var secondId = second.BoundRewardId;
            Collect(first); Assert.That(upgrades.Experience, Is.EqualTo(50)); Assert.That(upgrades.Level, Is.EqualTo(5));
            Assert.That(upgrades.PendingLevels, Is.EqualTo(3)); Assert.That(run.Phase, Is.EqualTo(RunPhase.ChoosingUpgrade));
            double clock = run.ElapsedSeconds; Vector3 position = second.transform.position;
            Collect(second); director.Advance(10, camera);
            Assert.That(upgrades.Experience, Is.EqualTo(50)); Assert.That(second.transform.position, Is.EqualTo(position));
            Assert.That(run.ElapsedSeconds, Is.EqualTo(clock));
            var offers = new HashSet<string>();
            for (int i = 0; i < 4; i++)
            {
                var offer = upgrades.CurrentOffer; Assert.That(offers.Add(offer.OfferId), Is.True);
                Assert.That(upgrades.TryConfirm(offer.OfferId, offer.Choices[0]), Is.True);
                Assert.That(upgrades.TryConfirm(offer.OfferId, offer.Choices[0]), Is.False);
                Assert.That(run.Phase, Is.EqualTo(i == 3 ? RunPhase.Playing : RunPhase.ChoosingUpgrade));
            }
            Assert.That(upgrades.ConfirmedChoices.Count, Is.EqualTo(4)); Assert.That(upgrades.PendingLevels, Is.Zero);
            Collect(second); Assert.That(upgrades.Experience, Is.EqualTo(52));
            Assert.That(Ledger.TryGetEntry(secondId, out var entry), Is.True); Assert.That(entry.State, Is.EqualTo(RewardState.Claimed));
            AssertConserved(); Record("multiple-levels");
        }

        [Test]
        public void DeathIssuedBeforePauseRemainsDueAndMaterializesOnlyAfterResume()
        {
            CreateRun(); var enemy = Spawn("libelula-alfa");
            events.Combat += value => { if (value.Type == CombatEventType.EntityDefeated) director.TogglePause(); };
            Assert.That(enemy.TakeDamage(enemy.LeaseId, enemy.State.Health, 1, 1), Is.True);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Paused)); Assert.That(Ledger.PendingCount, Is.EqualTo(2));
            Assert.That(XpViews, Is.Empty); Assert.That(HealViews, Is.Empty); var original = Ledger.Entries.Select(Receipt).ToArray();
            director.Advance(10, camera); Assert.That(Ledger.Entries.Select(Receipt), Is.EqualTo(original));
            director.TogglePause(); director.Advance(0, camera);
            Assert.That(Ledger.PendingCount, Is.Zero); Assert.That(XpViews.Length, Is.EqualTo(1)); Assert.That(HealViews.Length, Is.EqualTo(1));
            AssertConserved(); Record("issued-during-pause");
        }

        [TestCase(false)] [TestCase(true)]
        public void AttractionAndContactFreezeAcrossPauseAndReleasedViewRebindsWithoutOldTarget(bool healing)
        {
            CreateRun(); Kill("libelula-alfa"); player.State.TakeDamage(80, 1, 0);
            IPickupAttractionView view = healing ? (IPickupAttractionView)HealViews.Single() : XpViews.Single();
            var id = Id(view); long lease = view.ViewLeaseId;
            MovePlayer(floor + Vector2.left); director.Advance(0, camera);
            Assert.That(Target(view), Is.EqualTo(player.transform)); Vector3 original = view.PickupTransform.position;
            director.TogglePause(); MovePlayer(floor); view.CollectContact(player.GetComponent<Collider2D>()); director.Advance(10, camera);
            Assert.That(view.PickupTransform.position, Is.EqualTo(original)); Assert.That(upgrades.Experience, Is.Zero);
            Assert.That(player.State.Health, Is.EqualTo(20));
            Assert.That(pools.Release((Component)view), Is.True);
            Assert.That(view.ViewLeaseId, Is.Zero); Assert.That(Target(view), Is.Null); Assert.That(view.Attraction.Captured, Is.False);
            director.Advance(0, camera); Assert.That(Ledger.PendingCount, Is.EqualTo(1));
            MovePlayer(new Vector2(-6, -2)); director.TogglePause(); director.Advance(0, camera);
            var rebound = healing ? (IPickupAttractionView)HealViews.Single() : XpViews.Single();
            Assert.That(rebound, Is.SameAs(view)); Assert.That(rebound.ViewLeaseId, Is.GreaterThan(lease));
            Assert.That(Id(rebound), Is.EqualTo(id)); Assert.That(Target(rebound), Is.Null);
            Assert.That(rebound.Attraction.Captured, Is.False); Assert.That((Vector2)rebound.PickupTransform.position, Is.EqualTo(floor));
            Collect(rebound); AssertConserved(); Record("pause-rebind");
        }

        [TestCase(false)] [TestCase(true)]
        public void ClaimedViewReusedByAnotherDeathDoesNotKeepPreviousRewardOrAttraction(bool healing)
        {
            CreateRun(); Kill("libelula-alfa"); player.State.TakeDamage(80, 1, 0);
            IPickupAttractionView first = healing ? (IPickupAttractionView)HealViews.Single() : XpViews.Single();
            var previousId = Id(first); long previousLease = first.ViewLeaseId;
            MovePlayer(floor + Vector2.left); director.Advance(0, camera);
            Assert.That(first.Attraction.Captured, Is.True); Collect(first);
            Assert.That(first.ViewLeaseId, Is.Zero); Assert.That(Target(first), Is.Null);
            MovePlayer(new Vector2(-6, -2)); Kill("libelula-alfa", new Vector2(9, 3));
            var next = healing ? (IPickupAttractionView)HealViews.Single() : XpViews.Single(p => p.ViewLeaseId != 0);
            Assert.That(next, Is.SameAs(first)); Assert.That(next.ViewLeaseId, Is.GreaterThan(previousLease));
            Assert.That(Id(next), Is.Not.EqualTo(previousId)); Assert.That(Target(next), Is.Null);
            Assert.That(next.Attraction.Captured, Is.False); Assert.That(next.ViewState, Is.EqualTo(RewardState.Grounded));
            Assert.That((Vector2)next.PickupTransform.position, Is.EqualTo(new Vector2(9, 3)));
            Ledger.TryGetEntry(previousId, out var previous); Assert.That(previous.State, Is.EqualTo(RewardState.Claimed));
            var nextId = Id(next); Collect(next); Collect(next);
            Ledger.TryGetEntry(nextId, out var claimed); Assert.That(claimed.State, Is.EqualTo(RewardState.Claimed));
            if (healing) Assert.That(player.State.Health, Is.EqualTo(100));
            else Assert.That(upgrades.Experience, Is.EqualTo(100));
            AssertConserved(); Record("new-reward-reuses-view");
        }

        [Test]
        public void AbandonDuringMandatoryOfferRejectsStaleConfirmationAndCancelsOtherPickups()
        {
            CreateRun(multipleLevels: true); Kill("libelula-alfa");
            Collect(XpViews.Single()); var offer = upgrades.CurrentOffer;
            Assert.That(run.Phase, Is.EqualTo(RunPhase.ChoosingUpgrade));
            Assert.That(director.TryAbandon(), Is.True);
            Assert.That(upgrades.TryConfirm(offer.OfferId, offer.Choices[0]), Is.False);
            Assert.That(upgrades.TrySkip(offer.OfferId), Is.False);
            Assert.That(upgrades.ConfirmedChoices, Is.Empty); Assert.That(upgrades.Experience, Is.EqualTo(50));
            Assert.That(Ledger.Closure.Claimed, Is.EqualTo(1)); Assert.That(Ledger.Closure.Cancelled, Is.EqualTo(1));
            Assert.That(pools.ActiveCount, Is.Zero); AssertConserved(); Record("abandon-offer");
        }

        [Test]
        public void AbandonFromOfferCallbackCompletesInFlightXpWithoutRequeueOrStalePoolAccess()
        {
            CreateRun(multipleLevels: true); Kill("libelula-alfa");
            int callbacks = 0;
            upgrades.OfferChanged += _ => { callbacks++; Assert.That(director.TryAbandon(), Is.True); };
            Collect(XpViews.Single());
            Assert.That(callbacks, Is.EqualTo(1)); Assert.That(run.Outcome, Is.EqualTo(RunPhase.Abandoned));
            Assert.That(upgrades.Experience, Is.EqualTo(50)); Assert.That(Ledger.Closure.InFlight, Is.Zero);
            Assert.That(Ledger.Closure.Claimed, Is.EqualTo(1)); Assert.That(Ledger.Closure.Cancelled, Is.EqualTo(1));
            Assert.That(pools.ActiveCount, Is.Zero); Assert.That(Ledger.PendingCount, Is.Zero);
            AssertConserved(); Record("close-during-xp-consumer");
        }

        [TestCase(RunPhase.Lost)] [TestCase(RunPhase.Abandoned)] [TestCase(RunPhase.Won)]
        public void TerminalFlowCancelsGroundAttractedAndPendingRewardsWithoutSweeping(RunPhase outcome)
        {
            CreateRun(smallPools: true); Kill(); Kill(); Kill(); Kill("libelula-alfa");
            var claimed = XpViews.First(); Collect(claimed); MovePlayer(new Vector2(-6, -2));
            director.Advance(0, camera); player.State.TakeDamage(80, 1, 0);
            Assert.That(XpViews.First().TryMarkAttracted(player.transform), Is.True);
            foreach (var view in HealViews) Assert.That(view.TryMarkAttracted(player.transform), Is.True);
            int xpBefore = upgrades.Experience, pendingBefore = Ledger.PendingCount;
            Assert.That(pendingBefore, Is.GreaterThan(0)); AssertConserved(); Record("before-" + outcome);
            Assert.That(Ledger.Entries.Any(e => e.State == RewardState.Grounded), Is.True);
            Assert.That(Ledger.Entries.Any(e => e.State == RewardState.Attracted), Is.True);
            if (outcome == RunPhase.Lost) { player.State.TakeDamage(player.State.Health, 2, 0); director.Advance(0, camera); }
            else if (outcome == RunPhase.Abandoned) { director.TogglePause(); director.TryAbandon(); }
            else
            {
                run.Advance(900); director.Advance(0, camera);
                Assert.That(director.Boss, Is.Not.Null);
                director.Boss.TakeDamage(director.Boss.MaxHealth, player.GetInstanceID(), 1); director.Advance(0, camera);
            }
            Assert.That(run.Outcome, Is.EqualTo(outcome)); Assert.That(upgrades.Experience, Is.EqualTo(xpBefore));
            Assert.That(Ledger.Closure.Claimed, Is.EqualTo(1)); Assert.That(Ledger.Closure.InFlight, Is.Zero);
            Assert.That(Ledger.Closure.Cancelled, Is.EqualTo(Ledger.Entries.Count - 1)); Assert.That(Ledger.PendingCount, Is.Zero);
            Assert.That(Ledger.Closure.CancelledExperience, Is.EqualTo(Ledger.Entries.Sum(e => (long)e.Experience) - xpBefore));
            Assert.That(pools.ActiveCount, Is.Zero); Assert.That(Ledger.Entries.Any(e => e.Origin == RewardOrigin.FinalBoss), Is.False);
            AssertConserved(); Record("after-" + outcome);
        }

        [UnityTest]
        public IEnumerator RealPhysicsContactsCollectMiniXpAndHealingOnce()
        {
            CreateRun(); Kill("libelula-alfa"); var entries = Ledger.Entries.ToArray();
            player.State.TakeDamage(80, 1, 0); MovePlayer(floor);
            // No manual collection/attraction call: Unity must deliver trigger contacts to both prefabs.
            for (int i = 0; i < 4; i++) yield return new WaitForFixedUpdate();
            Assert.That(upgrades.Experience, Is.EqualTo(50)); Assert.That(player.State.Health, Is.EqualTo(70));
            Assert.That(entries.All(e => e.State == RewardState.Claimed), Is.True);
            Assert.That(pools.GetActiveCount(PoolFamily.Pickup), Is.Zero);
            yield return new WaitForFixedUpdate();
            Assert.That(upgrades.Experience, Is.EqualTo(50)); Assert.That(player.State.Health, Is.EqualTo(70));
            AssertConserved(); Record("physics-contact");
        }
    }
}
#endif
