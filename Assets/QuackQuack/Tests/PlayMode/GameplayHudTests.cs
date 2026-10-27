using System.Collections;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Player;
using QuackQuack.Presentation.UI;
using QuackQuack.Systems.Run;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace QuackQuack.Tests.PlayMode
{
    public class GameplayHudTests
    {
        [UnityTest]
        public IEnumerator HudShowsHealthBreathTerrainAndReadyWithoutChangingRules()
        {
            yield return LoadArena();
            var hud = Object.FindAnyObjectByType<GameplayHud>();
            Assert.That(hud, Is.Not.Null, "Lake needs a bound uGUI HUD.");
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(Field<Text>(hud, "healthLabel").text, Does.Contain("100"));
            Assert.That(Field<Text>(hud, "breathLabel").text, Does.Contain("100"));
            Assert.That(Field<Text>(hud, "terrainLabel").text, Does.Contain("TERRA"));
            Assert.That(Field<Text>(hud, "quackLabel").text, Does.Contain("PRONTO"));
            Assert.That(Field<Image>(hud, "breathFill").fillAmount, Is.EqualTo(1));
            player.State.TakeDamage(25, GameBootstrap.Instance.Run.ElapsedSeconds, 0);
            yield return null; yield return null;
            Assert.That(Field<Image>(hud, "healthFill").fillAmount, Is.EqualTo(0.75f).Within(0.001f));
            Assert.That(player.State.Health, Is.EqualTo(75));
        }

        [UnityTest]
        public IEnumerator RejectionsPauseAndDeathReachVisibleStatus()
        {
            yield return LoadArena();
            var hud = Object.FindAnyObjectByType<GameplayHud>();
            Assert.That(hud, Is.Not.Null);
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(player.ActivateQuack(), Is.True);
            Assert.That(player.ActivateQuack(), Is.False);
            yield return null; yield return null;
            Assert.That(Field<Text>(hud, "statusLabel").text, Does.Contain("recarregando"));
            yield return new WaitForSeconds(0.4f);
            player.State.Breath.TryConsume(player.State.Breath.Capacity, out _);
            Assert.That(player.ActivateQuack(), Is.False);
            yield return null; yield return null;
            Assert.That(Field<Text>(hud, "statusLabel").text, Does.Contain("Sem Fôlego"));
            var run = GameBootstrap.Instance.Run;
            run.TryTransition(RunPhase.Paused);
            yield return null; yield return null;
            Assert.That(Field<Text>(hud, "statusLabel").text, Does.Contain("PAUSADO"));
            run.TryTransition(RunPhase.Playing);
            run.TryTransition(RunPhase.Lost);
            yield return null; yield return null;
            Assert.That(Field<Text>(hud, "statusLabel").text, Does.Contain("DERROTA"));
        }

        private static IEnumerator LoadArena()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            for (int i = 0; i < 120 && SceneManager.GetActiveScene().name != "Lake"; i++) yield return null;
            yield return null; yield return null;
        }

        private static T Field<T>(GameplayHud hud, string name) =>
            (T)typeof(GameplayHud).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hud);

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
            yield return null;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("After HUD test"));
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
