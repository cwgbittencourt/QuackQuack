using System.Collections;
using NUnit.Framework;
using QuackQuack.Core.Run;
using QuackQuack.Systems.Pooling;
using QuackQuack.Gameplay.Player;
using QuackQuack.Systems.Run;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace QuackQuack.Tests.PlayMode
{
    public class LakeArenaTests
    {
        [UnityTest]
        public IEnumerator BootstrapOpensPopulatedPlayableArena()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            for (int i = 0; i < 120 && SceneManager.GetActiveScene().name != "Lake"; i++) yield return null;
            yield return null;
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(player, Is.Not.Null, "Lake must contain the configured Pato.");
            Assert.That(player.State, Is.Not.Null);
            Assert.That(player.Definition.IsValid(out var reason), Is.True, reason);
            Assert.That(GameBootstrap.Instance.Pools.ActiveCount, Is.GreaterThanOrEqualTo(3));
            Assert.That(Object.FindObjectsByType<QuackTarget>(FindObjectsSortMode.None).Length, Is.GreaterThanOrEqualTo(3));
        }

        [UnityTest]
        public IEnumerator SceneSupportsQuackTerrainPauseAndOneShotRiskReward()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            for (int i = 0; i < 120 && SceneManager.GetActiveScene().name != "Lake"; i++) yield return null;
            yield return null;
            var arena = Object.FindAnyObjectByType<LakeArenaController>();
            Assert.That(arena.Ready, Is.True);
            var player = arena.Player;
            var body = player.GetComponent<Rigidbody2D>();
            var bootstrap = GameBootstrap.Instance;
            Assert.That(bootstrap.Pools.GetActiveCount(PoolFamily.Enemy), Is.EqualTo(2));
            Assert.That(bootstrap.Pools.GetActiveCount(PoolFamily.Pickup), Is.EqualTo(5));
            var targets = Object.FindObjectsByType<QuackTarget>(FindObjectsSortMode.None);
            QuackTarget enemy = null;
            foreach (var target in targets)
                if (target.gameObject.layer == 9 && !target.IsProjectile) { enemy = target; break; }
            Assert.That(enemy, Is.Not.Null);
            body.position = (Vector2)enemy.transform.position + Vector2.down * 1.5f;
            Physics2D.SyncTransforms();
            Assert.That(player.ActivateQuack(), Is.True);
            Assert.That(enemy.Health, Is.LessThan(20));
            body.position = new Vector2(0, -3.5f); // Keep this US1 test away from the new XP pickup.
            yield return new WaitForFixedUpdate();
            Assert.That(player.State.Terrain, Is.EqualTo(TerrainType.Water));
            bootstrap.Run.TryTransition(RunPhase.Paused);
            double paused = bootstrap.Run.ElapsedSeconds;
            yield return new WaitForSeconds(0.2f);
            Assert.That(bootstrap.Run.ElapsedSeconds, Is.EqualTo(paused));
            bootstrap.Run.TryTransition(RunPhase.Playing);
            player.State.Breath.TryConsume(player.State.Breath.Capacity, out _);
            body.position = new Vector2(8.5f, 2.5f);
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(arena.RewardClaimed, Is.True);
            Assert.That(bootstrap.Pools.GetActiveCount(PoolFamily.Pickup), Is.EqualTo(4));
            Assert.That(player.State.Breath.Current, Is.GreaterThan(40));
            player.State.Breath.TryConsume(player.State.Breath.Capacity, out _);
            yield return null;
            Assert.That(player.State.Breath.Current, Is.LessThan(20), "Remaining on the reward must not grant it again.");
        }

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
            yield return null;
            var loaded = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("After Lake test");
            SceneManager.SetActiveScene(empty);
            if (loaded.IsValid() && loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
        }
    }
}
