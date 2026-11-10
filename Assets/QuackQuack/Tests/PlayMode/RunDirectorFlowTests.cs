#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Collections;
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
using QuackQuack.Gameplay.Player;
using QuackQuack.Gameplay.Pickups;
using QuackQuack.Gameplay.Weapons;
using QuackQuack.Platform.Input;
using QuackQuack.Systems.Combat;
using QuackQuack.Systems.Pooling;
using QuackQuack.Systems.Pickups;
using QuackQuack.Systems.Run;
using QuackQuack.Systems.Save;
using QuackQuack.Systems.Upgrades;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
namespace QuackQuack.Tests.PlayMode
{
    public sealed class RewardViewPoolProbe : MonoBehaviour, IPoolLifecycle
    {
        public void OnAcquire() { }
        public void OnRelease() { }
        public void ResetState() { }
    }
    public class RunDirectorFlowTests
    {
        private sealed class NoInput : IPlayerInput { public PlayerCommand Read() => default; }
        private GameObject root;
        private RunState run;
        private PoolService pools;
        private GameplayEventBus events;
        private RunDirector director;
        private PlayerController player;
        private WeaponRunner weapons;
        private UpgradeSystem upgrades;
        private PrototypeBuildCatalog catalog;
        private RelicLoadout relics;
        private LakeRunSettings settings;
        private JsonSaveService save;
        private string directory, path;
        private readonly Rect bounds = new Rect(-2, -2, 4, 4);
        [SetUp] public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "QuackQuack-T060-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); path = Path.Combine(directory, "save.json");
            root = new GameObject("T060 fixture"); run = new RunState(60); pools = new PoolService(); events = new GameplayEventBus();
            var target = new GameObject("Pato", typeof(PlayerController)); target.transform.SetParent(root.transform);
            player = target.GetComponent<PlayerController>(); player.Initialize(AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/QuackQuack/Content/Characters/Pato.asset"), new NoInput(), run, events);
            target.transform.position = new Vector2(-6, -2); player.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            catalog = AssetDatabase.LoadAssetAtPath<PrototypeBuildCatalog>("Assets/QuackQuack/Content/Catalogs/PrototypeBuildCatalog.asset");
            var build = new BuildState(); build.TryAddWeapon(catalog.Weapons[0].Id); upgrades = catalog.CreateUpgradeSystem(60, build);
            var choices = new List<string>(); foreach (var d in catalog.Weapons) choices.Add(d.Id); foreach (var d in catalog.Passives) choices.Add(d.Id); foreach (var d in catalog.Evolutions) choices.Add(d.Id);
            upgrades.InitializeProgression(run, new[] { 10, 30, 60, 100 }, choices.ToArray());
            WeaponRunner.RegisterProjectilePool(pools, "fixture/projectiles", 32, root.transform); weapons = root.AddComponent<WeaponRunner>();
            weapons.Initialize(run, build, pools, events, player.transform, () => player.State.Terrain, catalog.GetAllWeapons(), "fixture/projectiles", 32);
            var relicCatalog = AssetDatabase.LoadAssetAtPath<PrototypeRelicCatalog>("Assets/QuackQuack/Content/Relics/PrototypeRelicCatalog.asset"); relics = relicCatalog.CreateLoadout();
            var ids = new List<string>(); foreach (var d in relicCatalog.Relics) ids.Add(d.Id); save = new JsonSaveService(path, ids.ToArray());
            settings = AssetDatabase.LoadAssetAtPath<LakeRunSettings>("Assets/QuackQuack/Content/Encounters/LakeRunSettings.asset");
            var refuge = new GameObject("Refuge", typeof(CircleCollider2D), typeof(FrogKingSafeArea)); refuge.transform.SetParent(root.transform); refuge.transform.position = new Vector2(4, 0);
            refuge.GetComponent<CircleCollider2D>().isTrigger = true;
            director = root.AddComponent<RunDirector>(); director.AutomaticTick = false;
            director.Initialize(run, pools, events, player, upgrades, weapons, catalog, relics, settings, new[] { refuge.GetComponent<FrogKingSafeArea>() }, save);
        }
        [TearDown] public void Cleanup()
        {
            Object.DestroyImmediate(root); upgrades.Dispose(); pools.Dispose(); events.Clear();
            string resolved = Path.GetFullPath(directory); string temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Assert.That(resolved.StartsWith(temp, StringComparison.OrdinalIgnoreCase), Is.True); Assert.That(Path.GetFileName(resolved), Does.StartWith("QuackQuack-T060-"));
            if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
        }
        private void StaticTimeline() { director.Scheduler.enabled = false; weapons.enabled = false; }
        private void BossPhase() { StaticTimeline(); director.Advance(300, bounds); director.Advance(300, bounds); director.Advance(300, bounds); }

        private void DeliverReward(long lease, string id = "formiga", int xp = 2, int coins = 1, Vector2? position = null)
        {
            typeof(RunDirector).GetMethod("Reward", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(director,
                new object[] { new EnemyReward(lease, id, xp, coins, position ?? new Vector2(8, 5)) });
        }

        [Test] public void SaturatedExperiencePoolPresentsEveryObligationWithoutGrantingRemotely()
        {
            StaticTimeline();
            for (int i = 1; i <= settings.PickupCapacity + 3; i++) DeliverReward(100000 + i);
            var xp = director.Rewards.Entries.Where(e => e.Kind == RewardKind.Experience).ToArray();
            Assert.That(xp.Length, Is.EqualTo(settings.PickupCapacity + 3));
            Assert.That(xp.All(e => e.State == RewardState.Grounded), Is.True, "A full collider pool must not hide later rewards.");
            Assert.That(director.PendingRewards, Is.Zero);
            Assert.That(upgrades.Experience, Is.Zero);
            Assert.That(root.GetComponentsInChildren<ExperiencePickup>(true).Length, Is.EqualTo(settings.PickupCapacity), "No per-drop GameObjects may be grown during play.");
            director.Advance(0, bounds);
            var renderers = root.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled).ToArray();
            Assert.That(renderers, Is.Not.Empty);
            foreach (var renderer in renderers)
            {
                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(mesh.vertices.Any(v => v != Vector3.zero), Is.True);
                Assert.That(mesh.uv.All(uv => !float.IsNaN(uv.x) && !float.IsNaN(uv.y)), Is.True);
                Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("QuackQuack/RewardBatch"));
            }
            Assert.That(director.Rewards.DefeatCount, Is.EqualTo(settings.PickupCapacity + 3));
        }

        [Test] public void OverflowExperienceStillRequiresLocalContactAndClaimsOnce()
        {
            StaticTimeline();
            for (int i = 0; i < settings.PickupCapacity; i++) DeliverReward(1200000 + i);
            DeliverReward(1300000, position: (Vector2)player.transform.position + Vector2.right);
            var entry = director.Rewards.Entries.Last(e => e.Kind == RewardKind.Experience);
            Assert.That(entry.State, Is.EqualTo(RewardState.Grounded)); Assert.That(upgrades.Experience, Is.Zero);
            director.Advance(.02f, bounds); Assert.That(entry.State, Is.EqualTo(RewardState.Attracted));
            director.TogglePause(); director.Advance(5, bounds); Assert.That(entry.State, Is.EqualTo(RewardState.Attracted));
            player.transform.position += Vector3.left * 6;
            director.TogglePause(); director.Advance(2, bounds);
            Assert.That(entry.State, Is.EqualTo(RewardState.Claimed)); Assert.That(upgrades.Experience, Is.EqualTo(2));
            director.Advance(0, bounds); Assert.That(upgrades.Experience, Is.EqualTo(2));
        }

        [TestCase(false)] [TestCase(true)]
        public void BatchedHealingPreservesFullHealthCaptureAndSingleClaimRules(bool discardAfterCapture)
        {
            StaticTimeline(); var ledger = new RewardLedger(777, () => 0, 1, .15f, .5f, 1);
            Vector2 point = (Vector2)player.transform.position + Vector2.right;
            ledger.ProcessDefeat(7, 1, RewardOrigin.MiniBoss, 50, point.x, point.y, true);
            var entry = ledger.Entries.Single(e => e.Kind == RewardKind.HealMiniBoss);
            using var batch = new BatchedRewardPresentation(ledger, run, player, _ => false,
                settings.ExperiencePrefab.gameObject, settings.HealingPrefab.gameObject, root.transform);
            Assert.That(batch.TryAdd(entry), Is.True); ledger.TryMaterialize(entry.Id, true);
            batch.Advance(.02f, 1.26f, 7.5f); Assert.That(entry.State, Is.EqualTo(RewardState.Grounded));
            player.State.TakeDamage(player.State.MaxHealth * .8f, 1, 0);
            batch.Advance(.02f, 1.26f, 7.5f); Assert.That(entry.State, Is.EqualTo(RewardState.Attracted));
            if (discardAfterCapture) player.State.Heal(1);
            batch.Advance(1, 1.26f, 7.5f);
            Assert.That(entry.State, Is.EqualTo(discardAfterCapture ? RewardState.Discarded : RewardState.Claimed));
            Assert.That(player.State.Health, Is.EqualTo(player.State.MaxHealth * (discardAfterCapture ? 1 : .7f)).Within(.001));
            Assert.That(batch.ActiveCount, Is.Zero); batch.Advance(1, 1.26f, 7.5f);
            Assert.That(player.State.Health, Is.EqualTo(player.State.MaxHealth * (discardAfterCapture ? 1 : .7f)).Within(.001));
        }

        [Test] public void BatchedPresentationCoversWorstAuthoredRewardCountWithoutGrowingObjectsOrAllocatingSteadyFrames()
        {
            StaticTimeline(); int capacity = settings.CalculateRewardDefeatCapacity();
            var ledger = new RewardLedger(778, () => 0, 1, .15f, .5f, capacity);
            using var batch = new BatchedRewardPresentation(ledger, run, player, _ => false,
                settings.ExperiencePrefab.gameObject, settings.HealingPrefab.gameObject, root.transform);
            int objects = root.GetComponentsInChildren<Transform>(true).Length;
            for (int i = 1; i <= capacity; i++)
            {
                ledger.ProcessDefeat(7, i, RewardOrigin.Common, 2, 8, 5, true);
                while (ledger.TryGetPending(RewardKind.Experience, out var xp)) { Assert.That(batch.TryAdd(xp), Is.True); ledger.TryMaterialize(xp.Id, true); }
                while (ledger.TryGetPending(RewardKind.HealCommon, out var heal)) { Assert.That(batch.TryAdd(heal), Is.True); ledger.TryMaterialize(heal.Id, true); }
            }
            batch.Flush(); Assert.That(batch.ActiveCount, Is.EqualTo(capacity * 2)); Assert.That(ledger.PendingCount, Is.Zero);
            Assert.That(root.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(objects));
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
                Assert.That(renderer.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.LessThanOrEqualTo(60000));
            for (int i = 0; i < 10; i++) batch.Advance(.016f, 1.26f, 7.5f);
            GC.GetAllocatedBytesForCurrentThread(); long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 20; i++) batch.Advance(.016f, 1.26f, 7.5f);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
            ledger.Close(); batch.Clear(); Assert.That(batch.ActiveCount, Is.Zero);
            Assert.That(ledger.Closure.CancelledExperience, Is.EqualTo(capacity * 2));
        }

        [Test] public void RealExperienceViewClaimsLedgerAndProgressionOnce()
        {
            StaticTimeline(); DeliverReward(200001);
            var entry = director.Rewards.Entries.Single(e => e.Kind == RewardKind.Experience);
            var pickup = root.GetComponentsInChildren<ExperiencePickup>().Single(p => p.BoundRewardId.Equals(entry.Id));
            pickup.SendMessage("OnTriggerEnter2D", player.GetComponent<Collider2D>(), SendMessageOptions.RequireReceiver);
            Assert.That(entry.State, Is.EqualTo(RewardState.Claimed)); Assert.That(upgrades.Experience, Is.EqualTo(2));
            // Unity does not dispatch SendMessage to an inactive pooled object; invoke the stale callback directly.
            typeof(ExperiencePickup).GetMethod("TryCollect", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pickup, new object[] { player.GetComponent<Collider2D>() });
            Assert.That(upgrades.Experience, Is.EqualTo(2));
            director.Advance(0, bounds); Assert.That(entry.State, Is.EqualTo(RewardState.Claimed));
        }

        [Test] public void MiniHealingHasFirstAccessToSharedPoolAndUnavailableCommonHealingRemainsPending()
        {
            StaticTimeline(); director.HealingMaterializer = null; // exercise the adapter independently of the production pool
            for (int i = 1; i <= 100 && !director.Rewards.Entries.Any(e => e.Kind == RewardKind.HealCommon); i++) DeliverReward(300000 + i);
            Assert.That(director.Rewards.Entries.Any(e => e.Kind == RewardKind.HealCommon), Is.True);
            DeliverReward(400001, "libelula-alfa", 50, 15);
            pools.Register("healing-probe", PoolFamily.Pickup, () =>
            {
                var obj = new GameObject("Healing pool probe"); obj.transform.SetParent(root.transform);
                return obj.AddComponent<RewardViewPoolProbe>();
            }, 1);
            var attempted = new List<RewardKind>();
            director.HealingMaterializer = entry => { attempted.Add(entry.Kind); return pools.Acquire<RewardViewPoolProbe>("healing-probe") != null; };
            director.Advance(0, bounds);
            Assert.That(attempted[0], Is.EqualTo(RewardKind.HealMiniBoss));
            Assert.That(director.Rewards.Entries.Single(e => e.Kind == RewardKind.HealMiniBoss).State, Is.EqualTo(RewardState.Grounded));
            Assert.That(director.Rewards.TryGetPending(RewardKind.HealCommon, out _), Is.True);
            Assert.That(player.State.Health, Is.EqualTo(player.State.MaxHealth));
            // The fixture view intentionally has no healing behavior: production view belongs to T028.
            foreach (var probe in root.GetComponentsInChildren<RewardViewPoolProbe>()) pools.Release(probe);
        }

        [Test] public void AlreadyIssuedDefeatDeliveredDuringPauseWaitsForResume()
        {
            StaticTimeline(); director.TogglePause(); DeliverReward(450001);
            var entry = director.Rewards.Entries.Single(e => e.Kind == RewardKind.Experience);
            Assert.That(entry.State, Is.EqualTo(RewardState.Due));
            Assert.That(root.GetComponentsInChildren<ExperiencePickup>(), Is.Empty);
            director.TogglePause(); director.Advance(0, bounds);
            Assert.That(entry.State, Is.EqualTo(RewardState.Grounded)); Assert.That(upgrades.Experience, Is.Zero);
            director.TryAbandon(); Assert.That(director.Result.EnemiesDefeated, Is.EqualTo(1));
            Assert.That(director.Result.EarnedCoins, Is.EqualTo(1));
        }

        [Test] public void ReplayedRewardDoesNotDuplicateExperienceCoinsOrDefeatStatistics()
        {
            StaticTimeline(); DeliverReward(500001); DeliverReward(500001);
            Assert.That(director.Rewards.DefeatCount, Is.EqualTo(1));
            Assert.That(director.Rewards.Entries.Count(e => e.Kind == RewardKind.Experience), Is.EqualTo(1));
            director.TryAbandon(); Assert.That(director.Result.EnemiesDefeated, Is.EqualTo(1));
            Assert.That(director.Result.EarnedCoins, Is.EqualTo(1));
        }

        private HealingPickup DeliverHealing(long lease, bool mini)
        {
            if (mini) DeliverReward(lease, "libelula-alfa", 50, 15);
            else
                for (int i = 0; i < 100 && !director.Rewards.Entries.Any(e => e.Kind == RewardKind.HealCommon); i++) DeliverReward(lease + i);
            var kind = mini ? RewardKind.HealMiniBoss : RewardKind.HealCommon;
            return root.GetComponentsInChildren<HealingPickup>().First(p => p.BoundRewardId.Kind == kind);
        }

        [TestCase(false, 0.15f)] [TestCase(true, 0.5f)]
        public void HealingViewAppliesAuthoredFractionOnceAndReleases(bool mini, float fraction)
        {
            StaticTimeline(); float max = player.State.MaxHealth;
            player.State.TakeDamage(max * 0.6f, 1, 0);
            var pickup = DeliverHealing(600001, mini); var id = pickup.BoundRewardId;
            pickup.SendMessage("OnTriggerEnter2D", player.GetComponent<Collider2D>(), SendMessageOptions.RequireReceiver);
            Assert.That(player.State.Health, Is.EqualTo(max * (0.4f + fraction)).Within(0.0001f));
            Assert.That(pickup.ViewState, Is.EqualTo(RewardState.Released)); Assert.That(pickup.BoundRewardId, Is.EqualTo(default(RewardId)));
            Assert.That(director.Rewards.TryGetEntry(id, out var entry), Is.True); Assert.That(entry.State, Is.EqualTo(RewardState.Claimed));
            typeof(HealingPickup).GetMethod("TryCollect", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pickup, new object[] { player.GetComponent<Collider2D>() });
            Assert.That(player.State.Health, Is.EqualTo(max * (0.4f + fraction)).Within(0.0001f));
        }

        [TestCase("full")] [TestCase("paused")] [TestCase("dead")]
        public void HealingIsPreservedForFullPausedOrDeadPlayer(string condition)
        {
            StaticTimeline(); var pickup = DeliverHealing(700001, true); var id = pickup.BoundRewardId;
            if (condition == "dead") player.State.TakeDamage(player.State.MaxHealth, 1, 0);
            if (condition == "paused") { player.State.TakeDamage(60, 1, 0); director.TogglePause(); }
            float before = player.State.Health;
            pickup.SendMessage("OnTriggerStay2D", player.GetComponent<Collider2D>(), SendMessageOptions.RequireReceiver);
            Assert.That(player.State.Health, Is.EqualTo(before)); Assert.That(pools.IsActive(pickup, PoolFamily.Pickup), Is.True);
            Assert.That(director.Rewards.TryGetEntry(id, out var entry), Is.True); Assert.That(entry.State, Is.EqualTo(RewardState.Grounded));
            if (condition == "full") { Assert.That(pickup.TryMarkAttracted(player.transform), Is.False); player.State.TakeDamage(20, 2, 0); }
            if (condition == "paused") director.TogglePause();
            if (condition != "dead")
            {
                pickup.SendMessage("OnTriggerStay2D", player.GetComponent<Collider2D>(), SendMessageOptions.RequireReceiver);
                Assert.That(entry.State, Is.EqualTo(RewardState.Claimed));
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void ReleasedAttractedViewClearsTargetAndReturnsObligationForReuse(bool heal)
        {
            StaticTimeline(); player.State.TakeDamage(60, 1, 0);
            Component view; RewardId id;
            if (heal)
            {
                var pickup = DeliverHealing(800001, true); view = pickup; id = pickup.BoundRewardId;
                Assert.That(pickup.TryMarkAttracted(player.transform), Is.True);
            }
            else
            {
                DeliverReward(800001);
                var pickup = root.GetComponentsInChildren<ExperiencePickup>().Single(); view = pickup; id = pickup.BoundRewardId;
                Assert.That(pickup.TryMarkAttracted(player.transform), Is.True);
            }
            director.Rewards.TryGetEntry(id, out var entry); Assert.That(entry.State, Is.EqualTo(RewardState.Attracted));
            pools.Release(view); Assert.That(entry.State, Is.EqualTo(RewardState.WaitingForView));
            if (heal)
            {
                Assert.That(((HealingPickup)view).AttractionTarget, Is.Null); Assert.That(((HealingPickup)view).ViewState, Is.EqualTo(RewardState.Released));
            }
            else
            {
                Assert.That(((ExperiencePickup)view).AttractionTarget, Is.Null); Assert.That(((ExperiencePickup)view).ViewState, Is.EqualTo(RewardState.Released));
            }
            director.Advance(0, bounds); Assert.That(entry.State, Is.EqualTo(RewardState.Grounded));
            if (heal) Assert.That(((HealingPickup)view).AttractionTarget, Is.Null);
            else Assert.That(((ExperiencePickup)view).AttractionTarget, Is.Null);
        }

        [TestCase(false)] [TestCase(true)]
        public void ExperienceViewReleasedInsideConsumerCompletesWithoutStalePoolAccess(bool accepted)
        {
            StaticTimeline();
            var ledger = new RewardLedger(1, () => 0.9, 0.1, 0.15f, 0.5f, 1);
            ledger.ProcessDefeat(7, 1, RewardOrigin.Common, 2, 0, 0, true);
            var entry = ledger.Entries[0];
            ExperiencePickup.RegisterPool(pools, "xp-release-callback", 1, root.transform);
            var view = ExperiencePickup.Spawn(pools, "xp-release-callback", upgrades, 2, player.transform.position);
            view.BindReward(ledger, entry.Id, _ => { pools.Release(view); return accepted; }); ledger.TryMaterialize(entry.Id, true);
            view.SendMessage("OnTriggerEnter2D", player.GetComponent<Collider2D>(), SendMessageOptions.RequireReceiver);
            Assert.That(entry.State, Is.EqualTo(accepted ? RewardState.Claimed : RewardState.WaitingForView));
            Assert.That(view.ViewState, Is.EqualTo(RewardState.Released)); Assert.That(view.ViewLeaseId, Is.Zero);
            Assert.That(view.BoundRewardId, Is.EqualTo(default(RewardId)));
        }

        [Test] public void CommonHealingCannotOccupyReservedMiniBossSlots()
        {
            StaticTimeline(); int target = settings.HealingCapacity - settings.MiniBossReservedHealingSlots;
            for (int i = 1; i <= 500 && director.Rewards.Entries.Count(e => e.Kind == RewardKind.HealCommon) <= target; i++) DeliverReward(900000 + i);
            Assert.That(root.GetComponentsInChildren<HealingPickup>().Length, Is.EqualTo(target));
            Assert.That(director.Rewards.TryGetPending(RewardKind.HealCommon, out _), Is.False);
            Assert.That(director.Rewards.Entries.Where(e => e.Kind == RewardKind.HealCommon).All(e => e.State == RewardState.Grounded), Is.True);
            DeliverReward(950001, "libelula-alfa", 50, 15); DeliverReward(950002, "sapo-cururu", 50, 15);
            Assert.That(root.GetComponentsInChildren<HealingPickup>().Length, Is.EqualTo(settings.HealingCapacity));
            Assert.That(director.Rewards.Entries.Count(e => e.Kind == RewardKind.HealMiniBoss && e.State == RewardState.Grounded), Is.EqualTo(2));
            Assert.That(root.GetComponentsInChildren<HealingPickup>(true).Length, Is.EqualTo(settings.HealingCapacity));
        }

        [Test]
        public void BossDefeatEndsWithoutXpOrUpgradeAndCountsCoinsOnce()
        {
            BossPhase(); bool observed = false;
            run.Ended += _ =>
            {
                observed = true; Assert.That(upgrades.Experience, Is.Zero);
                Assert.That(upgrades.CurrentOffer, Is.Null); Assert.That(upgrades.PendingLevels, Is.Zero);
            };
            var boss = director.Boss; boss.TakeDamage(boss.MaxHealth, player.GetInstanceID(), 1);
            Assert.That(observed, Is.True); Assert.That(run.Outcome, Is.EqualTo(RunPhase.Won));
            Assert.That(director.Rewards.IsClosed, Is.True);
            Assert.That(director.Rewards.Entries.Any(e => e.Origin == RewardOrigin.FinalBoss), Is.False);
            Assert.That(boss.TryFinalizeDefeat(), Is.False); Assert.That(boss.TakeDamage(1, 1, 2), Is.False);
            Assert.That(upgrades.Experience, Is.Zero); Assert.That(director.Result.EnemiesDefeated, Is.EqualTo(1));
            Assert.That(director.Result.EarnedCoins, Is.EqualTo(settings.BossCoins));
            Assert.That(director.Result.Coins, Is.EqualTo(settings.BossCoins + settings.VictoryBonus));
        }

        [Test] public void PausedBossDefeatRetriesVictoryWithoutRepublishingDefeat()
        {
            BossPhase(); int defeats = 0;
            events.Combat += value => { if (value.Type == CombatEventType.EntityDefeated) { defeats++; director.TogglePause(); } };
            director.Boss.TakeDamage(director.Boss.MaxHealth, player.GetInstanceID(), 1);
            Assert.That(run.Outcome.HasValue, Is.False); Assert.That(upgrades.Experience, Is.Zero);
            director.TogglePause(); director.Advance(0, bounds);
            Assert.That(run.Outcome, Is.EqualTo(RunPhase.Won)); Assert.That(defeats, Is.EqualTo(1));
            Assert.That(upgrades.Experience, Is.Zero);
        }

        [Test] public void FakeBossDefeatEventCannotGrantRewardWhileBossLives()
        {
            BossPhase();
            events.Publish(new CombatEvent(1, 1, player.GetInstanceID(), director.Boss.GetInstanceID(), Vector2.zero, 0, CombatEventType.EntityDefeated));
            Assert.That(director.Boss.TryFinalizeDefeat(), Is.False); Assert.That(upgrades.Experience, Is.Zero);
            Assert.That(director.Rewards.Entries.Any(e => e.Origin == RewardOrigin.FinalBoss), Is.False);
        }

        [TestCase(false)] [TestCase(true)]
        public void LossAndAbandonmentCancelFloorRewardsWithoutGrant(bool lost)
        {
            StaticTimeline(); DeliverReward(1000001); int entries = director.Rewards.Entries.Count;
            if (lost) { player.State.TakeDamage(player.State.MaxHealth, 1, 0); director.Advance(0, bounds); }
            else director.TryAbandon();
            Assert.That(upgrades.Experience, Is.Zero); Assert.That(director.Rewards.IsClosed, Is.True);
            Assert.That(director.Rewards.Closure.Cancelled, Is.EqualTo(entries)); Assert.That(director.Rewards.Closure.InFlight, Is.Zero);
            Assert.That(director.Rewards.PendingCount, Is.Zero); Assert.That(pools.ActiveCount, Is.Zero);
            DeliverReward(1000002); Assert.That(director.Rewards.Entries.Count, Is.EqualTo(entries));
        }

        [Test] public void VictoryDoesNotSweepSaturatedFloorRewardsOrGrantBossXp()
        {
            StaticTimeline(); int deaths = settings.PickupCapacity + 3;
            for (int i = 0; i < deaths; i++) DeliverReward(1100000 + i);
            BossPhase(); Assert.That(upgrades.Experience, Is.Zero);
            director.Boss.TakeDamage(director.Boss.MaxHealth, player.GetInstanceID(), 1);
            Assert.That(upgrades.Experience, Is.Zero);
            director.Advance(0, bounds);
            Assert.That(director.Rewards.Closure.Claimed, Is.Zero);
            Assert.That(director.Rewards.Closure.CancelledExperience, Is.EqualTo(deaths * 2));
            Assert.That(director.Rewards.PendingCount, Is.Zero); Assert.That(pools.ActiveCount, Is.Zero);
        }

        [Test] public void RewardAmountsAreResolvedFromCatalogInsteadOfNotificationPayload()
        {
            StaticTimeline(); DeliverReward(1200001, "formiga", 999, 999);
            Assert.That(director.Rewards.Entries.Single(e => e.Kind == RewardKind.Experience).Experience, Is.EqualTo(2));
            director.TryAbandon(); Assert.That(director.Result.EarnedCoins, Is.EqualTo(1));
        }

        [Test] public void TimelineRequestsBothMinibossesBeforeSingleBossAndStopsCommonWaves()
        {
            StaticTimeline(); var seen = new List<string>(); director.EncounterStarted += seen.Add;
            director.Advance(299, bounds); Assert.That(seen, Does.Not.Contain("encounter-libelula-alfa"));
            director.Advance(1, bounds); Assert.That(seen, Does.Contain("encounter-libelula-alfa")); Assert.That(run.Phase, Is.EqualTo(RunPhase.Playing));
            Assert.That(Find("libelula-alfa"), Is.Not.Null);
            director.Advance(300, bounds); Assert.That(Find("sapo-cururu"), Is.Not.Null);
            director.Advance(300, bounds); Assert.That(run.Phase, Is.EqualTo(RunPhase.Boss)); Assert.That(director.BossRequests, Is.EqualTo(1));
            Assert.That(director.Spawns.ActiveCount, Is.Zero); director.Advance(0, bounds); Assert.That(director.BossRequests, Is.EqualTo(1));
            Assert.That(seen.Count, Is.EqualTo(9));
        }
        private QuackQuack.Gameplay.Enemies.PooledEnemy Find(string id)
        { for (int i = 0; i < director.Spawns.Capacity; i++) if (director.Spawns.TryGetActive(i, out var enemy, out _) && enemy.Definition.Id == id) return enemy; return null; }
        [Test] public void WaveCadenceAndCapAreBoundedWithoutCatchUpAfterPause()
        {
            StaticTimeline(); director.Advance(0, bounds); long before = director.Spawns.SpawnedCount;
            director.TogglePause(); director.Advance(100, bounds); Assert.That(run.ElapsedSeconds, Is.Zero); Assert.That(director.Spawns.SpawnedCount, Is.EqualTo(before));
            director.TogglePause(); director.Advance(0, bounds); Assert.That(director.Spawns.SpawnedCount, Is.EqualTo(before));
            for (int i = 0; i < 30; i++) director.Advance(3, bounds);
            Assert.That(director.Spawns.ActiveCount, Is.EqualTo(settings.Catalog.Waves[0].MaxActive)); Assert.That(director.Spawns.ReducedCount, Is.GreaterThan(0));
        }
        [Test] public void QuackKillsBoundEnemyOnceWithDamageRewardAndCleanup()
        {
            director.Advance(0, bounds); var enemy = Find("formiga"); Assert.That(enemy, Is.Not.Null);
            enemy.transform.position = player.transform.position + Vector3.right; enemy.Body.position = enemy.transform.position; Physics2D.SyncTransforms();
            float health = player.Definition.Quack.Damage * player.Definition.Quack.FullMultiplier;
            enemy.TakeDamage(enemy.LeaseId, enemy.State.Health - health, 0, 0);
            Assert.That(player.ActivateQuack(), Is.True);
            Assert.That(director.TryAbandon(), Is.True); var result = director.Result;
            Assert.That(result.EnemiesDefeated, Is.EqualTo(1)); Assert.That(result.DamageByWeapon["quack"], Is.EqualTo(health));
            Assert.That(result.EarnedCoins, Is.EqualTo(1)); Assert.That(pools.GetActiveCount(PoolFamily.Enemy), Is.Zero); Assert.That(pools.GetActiveCount(PoolFamily.Pickup), Is.Zero);
        }
        [Test] public void RealWeaponProjectileDamagesBoundEnemyAndAttributesWeaponId()
        {
            director.Advance(0, bounds); var enemy = Find("formiga"); enemy.transform.position = player.transform.position + Vector3.right; enemy.Body.position = enemy.transform.position; Physics2D.SyncTransforms();
            for (int i = 0; i < 10 && enemy.IsReady; i++) director.Advance(0.1f, bounds);
            director.TryAbandon(); Assert.That(director.Result.DamageByWeapon.ContainsKey(catalog.Weapons[0].Id), Is.True);
            Assert.That(director.Result.DamageByWeapon[catalog.Weapons[0].Id], Is.GreaterThan(0));
        }
        [Test] public void CentralStepActuallyMovesEnemyBodiesAndPresentationTogether()
        {
            director.Advance(0, bounds); var enemy = Find("formiga");
            float before = Vector2.Distance(enemy.Body.position, player.transform.position);
            director.Advance(0.25f, bounds);
            Assert.That(Vector2.Distance(enemy.Body.position, player.transform.position), Is.LessThan(before));
            Assert.That(Vector2.Distance(enemy.transform.position, enemy.Body.position), Is.LessThan(0.01f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(enemy.transform.eulerAngles.z, enemy.Body.rotation)), Is.LessThan(0.01f));
        }
        [UnityTest] public IEnumerator DeathPickupIsCollectedThroughRealPhysicsAndGrantsXpOnce()
        {
            director.Advance(0, bounds); var enemy = Find("formiga");
            Vector2 position = (Vector2)player.transform.position + Vector2.right;
            enemy.transform.position = position; enemy.Body.position = position; Physics2D.SyncTransforms();
            enemy.TakeDamage(enemy.LeaseId, enemy.State.Health - 4, 0, 0); player.ActivateQuack();
            player.transform.position = position; player.GetComponent<Rigidbody2D>().position = position; Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate(); yield return null;
            Assert.That(upgrades.Experience, Is.EqualTo(2));
            yield return new WaitForFixedUpdate(); Assert.That(upgrades.Experience, Is.EqualTo(2));
        }
        [Test] public void BossFinalHitAndDefeatAreRecordedBeforeStableVictory()
        {
            BossPhase(); int notifications = 0; director.ResultAvailable += _ => notifications++;
            var boss = director.Boss; float health = boss.Health; int source = catalog.Weapons[0].GetInstanceID();
            Assert.That(boss.GetComponent<QuackTarget>().ApplyBoundDamage(99999, source, 1, player.transform.position), Is.True);
            director.Advance(0, bounds); var result = director.Result;
            Assert.That(result.Outcome, Is.EqualTo(RunPhase.Won)); Assert.That(result.BossId, Is.EqualTo("sapo-rei"));
            Assert.That(result.DamageByWeapon[catalog.Weapons[0].Id], Is.EqualTo(health)); Assert.That(result.DefeatsByEnemy["sapo-rei"], Is.EqualTo(1));
            Assert.That(result.Coins, Is.EqualTo(settings.BossCoins + settings.VictoryBonus));
            director.Advance(1, bounds); Assert.That(director.Result, Is.SameAs(result)); Assert.That(notifications, Is.EqualTo(1)); Assert.That(pools.ActiveCount, Is.Zero);
        }
        [Test] public void BossWarningFreezesAcrossPauseAndResumesInBoss()
        {
            BossPhase(); var boss = director.Boss; int commits = 0; boss.AttackCommitted += (_, _) => commits++;
            director.TogglePause(); double before = run.ElapsedSeconds; director.Advance(10, bounds); Assert.That(commits, Is.Zero);
            Assert.That(run.ElapsedSeconds, Is.EqualTo(before)); director.TogglePause(); Assert.That(run.Phase, Is.EqualTo(RunPhase.Boss));
            director.Advance(0.5f, bounds); Assert.That(commits, Is.Zero); director.Advance(0.6f, bounds); Assert.That(commits, Is.EqualTo(1));
        }
        [Test] public void UpgradeModalDuringBossResumesBossWithoutAnotherBossRequest()
        {
            BossPhase(); Assert.That(upgrades.TryCollectExperience(10), Is.True); Assert.That(run.Phase, Is.EqualTo(RunPhase.ChoosingUpgrade));
            director.Advance(20, bounds); Assert.That(run.ElapsedSeconds, Is.EqualTo(900));
            upgrades.TrySkip(upgrades.CurrentOffer.OfferId); Assert.That(run.Phase, Is.EqualTo(RunPhase.Boss)); Assert.That(director.BossRequests, Is.EqualTo(1));
        }
        [Test] public void DeathEndsLostAndReturnsAllRunOwnedEntities()
        {
            director.Advance(0, bounds); player.State.TakeDamage(999, 0, 0); director.Advance(0, bounds);
            Assert.That(director.Result.Outcome, Is.EqualTo(RunPhase.Lost)); Assert.That(director.Result.IsVictory, Is.False);
            Assert.That(pools.ActiveCount, Is.Zero); Assert.That(director.TryAbandon(), Is.False);
        }
        [TestCase(false)] [TestCase(true)] public void AbandonmentIsUniqueAndNeverVictory(bool paused)
        {
            director.Advance(0, bounds); if (paused) director.TogglePause();
            Assert.That(director.TryAbandon(), Is.True); var first = director.Result;
            Assert.That(first.Outcome, Is.EqualTo(RunPhase.Abandoned)); Assert.That(first.IsVictory, Is.False);
            Assert.That(director.TryAbandon(), Is.False); director.Advance(1000, bounds); Assert.That(director.Result, Is.SameAs(first));
            Assert.That(director.BossRequests, Is.Zero); Assert.That(pools.ActiveCount, Is.Zero);
        }
        [TestCase(false)] [TestCase(true)]
        public void EndingAfterACombatComponentWasDestroyedStillClosesRewardsAndFreezesResult(bool destroyScheduler)
        {
            Object.DestroyImmediate(destroyScheduler ? (Object)director.Scheduler : weapons);
            Assert.DoesNotThrow(() => director.TryAbandon());
            Assert.That(director.Result.Outcome, Is.EqualTo(RunPhase.Abandoned));
            Assert.That(director.Rewards.IsClosed, Is.True);
            Assert.That(director.TryAbandon(), Is.False);
        }
        [Test]
        public void DiagnosticSpawnProcessingSharesTheDirectorClockAndPauseDoesNotAdvanceIt()
        {
            director.Advance(0.02f, bounds);
            long tick = director.SimulationTick;
            Assert.That(tick, Is.Positive);
            Assert.DoesNotThrow(() => director.Spawns.Process(tick, bounds));
            Assert.DoesNotThrow(() => director.Advance(0.02f, bounds));
            Assert.That(director.SimulationTick, Is.EqualTo(tick + 1));
            director.TogglePause(); director.Advance(1, bounds);
            Assert.That(director.SimulationTick, Is.EqualTo(tick + 1));
        }
        [Test] public void SaveFailureDoesNotBlockEndingOrChangeFrozenResult()
        {
            string id = AssetDatabase.LoadAssetAtPath<PrototypeRelicCatalog>("Assets/QuackQuack/Content/Relics/PrototypeRelicCatalog.asset").Relics[0].Id;
            relics.Unlock(id); relics.Equip(id); Assert.That(director.Persist().Status, Is.EqualTo(SaveStatus.Success)); string before = File.ReadAllText(path);
            using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                director.TryAbandon(); Assert.That(director.Result.Outcome, Is.EqualTo(RunPhase.Abandoned));
                Assert.That(director.Persist().Status, Is.EqualTo(SaveStatus.Failed)); Assert.That(File.ReadAllText(path), Is.EqualTo(before));
            }
            CollectionAssert.AreEqual(new[] { id }, save.Load().Data.EquippedRelicIds);
        }
    }
}
#endif
