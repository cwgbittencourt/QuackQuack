#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Player;
using QuackQuack.Systems.Run;
using QuackQuack.Systems.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace QuackQuack.Tests.PlayMode
{
    public class ResultsSceneFlowTests
    {
        private string directory, path;
        private Func<string> previousPath;
        private RunDirector director;
        private ResultsBridge bridge;
        [UnitySetUp] public IEnumerator SetUp()
        {
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject); yield return null;
            directory = Path.Combine(Path.GetTempPath(), "QuackQuack-T061Scene-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); path = Path.Combine(directory, "save.json");
            previousPath = RunDirector.SavePathProvider; RunDirector.SavePathProvider = () => path;
            yield return SceneManager.LoadSceneAsync("RunBootstrap");
            for (int i = 0; i < 240; i++) { director = Object.FindAnyObjectByType<RunDirector>(); if (director != null && director.Ready) break; yield return null; }
            Assert.That(director, Is.Not.Null); Assert.That(director.Ready, Is.True); director.AutomaticTick = false;
            yield return null; yield return null; bridge = director.GetComponent<ResultsBridge>(); Assert.That(bridge, Is.Not.Null);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var live = Object.FindAnyObjectByType<RunDirector>(); if (live != null) live.TryAbandon();
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
            yield return SceneManager.LoadSceneAsync("Bootstrap"); yield return null;
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject); yield return null;
            RunDirector.SavePathProvider = previousPath;
            string resolved = Path.GetFullPath(directory), temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Assert.That(resolved.StartsWith(temp, StringComparison.OrdinalIgnoreCase), Is.True); Assert.That(Path.GetFileName(resolved), Does.StartWith("QuackQuack-T061Scene-"));
            if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
        }
        private JsonSaveService Service()
        { var ids = new List<string>(); foreach (var d in director.RelicCatalog.Relics) ids.Add(d.Id); return new JsonSaveService(path, ids.ToArray()); }
        private void EarnTwoCoins()
        {
            director.Advance(0, new Rect(-2, -2, 4, 4));
            int count = 0; for (int i = 0; i < director.Spawns.Capacity && count < 2; i++) if (director.Spawns.TryGetActive(i, out var enemy, out long lease))
            { enemy.TakeDamage(lease, 9999, director.BuildCatalog.Weapons[0].GetInstanceID(), 0); count++; }
            Assert.That(count, Is.EqualTo(2));
        }
        [UnityTest] public IEnumerator VictoryShowsResultsAndPersistsOneVictory()
        {
            director.Scheduler.enabled = false;
            foreach (var weapon in director.GetComponents<QuackQuack.Gameplay.Weapons.WeaponRunner>()) weapon.enabled = false;
            // WeaponRunner lives on the progression object in the scene.
            foreach (var weapon in Object.FindObjectsByType<QuackQuack.Gameplay.Weapons.WeaponRunner>(FindObjectsSortMode.None)) weapon.enabled = false;
            director.Advance(300, new Rect(-2, -2, 4, 4)); director.Advance(300, new Rect(-2, -2, 4, 4)); director.Advance(300, new Rect(-2, -2, 4, 4));
            director.Boss.TakeDamage(99999, director.BuildCatalog.Weapons[0].GetInstanceID(), 0); director.Advance(0, new Rect(-2, -2, 4, 4));
            yield return null;
            Assert.That(bridge.View.TitleText, Is.EqualTo("VITÓRIA")); Assert.That(GameBootstrap.Instance.Run.Phase, Is.EqualTo(RunPhase.Results));
            var profile = Service().Load().Data.Progress; Assert.That(profile.Victories, Is.EqualTo(1)); Assert.That(profile.Coins, Is.EqualTo(100));
            Assert.That(profile.Challenges["frog-king-victory"], Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator DeathShowsDefeatWithoutVictoryCredit()
        {
            var player = Object.FindAnyObjectByType<PlayerController>(); player.State.TakeDamage(9999, GameBootstrap.Instance.Run.ElapsedSeconds, 0);
            director.Advance(0, new Rect(-2, -2, 4, 4)); yield return null;
            Assert.That(bridge.View.TitleText, Is.EqualTo("DERROTA")); Assert.That(Service().Load().Data.Progress.Victories, Is.Zero);
        }
        [UnityTest] public IEnumerator AbandonmentShowsPartialCoinsAndStableReopenedProgress()
        {
            EarnTwoCoins(); director.TryAbandon(); yield return null;
            Assert.That(bridge.View.TitleText, Is.EqualTo("PARTIDA ABANDONADA")); Assert.That(bridge.View.RewardsText, Does.Contain("Recebidas nesta partida  1"));
            string before = File.ReadAllText(path); bridge.Retry(); bridge.Retry();
            Assert.That(File.ReadAllText(path), Is.EqualTo(before)); Assert.That(Service().Load().Data.Progress.Coins, Is.EqualTo(1));
            Assert.That(Service().Load().Data.Progress.Victories, Is.Zero);
        }
        [UnityTest] public IEnumerator FailedSaveRemainsReviewableAndRetryCommitsOnce()
        {
            EarnTwoCoins(); director.Persist();
            using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                director.TryAbandon(); yield return null;
                Assert.That(bridge.View.SaveText, Does.Contain("não salvo")); Assert.That(bridge.View.CanRetry, Is.True); Assert.That(bridge.View.CanRestart, Is.True);
                Assert.That(Service().Load().Data.Progress.Runs, Is.Zero);
            }
            bridge.Retry(); bridge.Retry(); Assert.That(bridge.View.SaveText, Is.EqualTo("Progresso salvo"));
            Assert.That(Service().Load().Data.Progress.Coins, Is.EqualTo(1)); Assert.That(Service().Load().Data.Progress.Runs, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator PlayAgainButtonStartsFreshRunAndKeepsCommittedWallet()
        {
            EarnTwoCoins(); director.TryAbandon(); yield return null; string originalId = director.RunId;
            foreach (var button in bridge.View.GetComponentsInChildren<Button>()) if (button.name == "JOGAR NOVAMENTE") button.onClick.Invoke();
            for (int i = 0; i < 240; i++)
            { var candidate = Object.FindAnyObjectByType<RunDirector>(); if (candidate != null && candidate.Ready && candidate.RunId != originalId) { director = candidate; break; } yield return null; }
            Assert.That(director.RunId, Is.Not.EqualTo(originalId)); Assert.That(GameBootstrap.Instance.Run.Phase, Is.EqualTo(RunPhase.Playing));
            Assert.That(Service().Load().Data.Progress.Coins, Is.EqualTo(1)); Assert.That(Service().Load().Data.Progress.Runs, Is.EqualTo(1));
        }
    }
}
#endif
