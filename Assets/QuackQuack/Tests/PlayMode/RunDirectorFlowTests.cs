#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
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
