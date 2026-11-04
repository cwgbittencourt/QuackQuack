using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.LowLevel;
using QuackQuack.Presentation.UI;
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
    [Category("ResultsRuntime")]
    public class ResultsSceneFlowTests
    {
        private static readonly bool[] SaveStates = { false, true };
        private static readonly string[] ScrollInputs = { "Dpad", "Stick" };
        private MenuRegressionInput driver;
        private int controllerRetries, controllerRestarts;
        private string directory, path;
        private Func<string> previousPath;
        private RunDirector director;
        private ResultsBridge bridge;
        [UnitySetUp] public IEnumerator SetUp()
        {
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject); yield return null;
            directory = Path.Combine(Application.temporaryCachePath, "QuackQuack-T061Scene-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); path = Path.Combine(directory, "save.json");
            previousPath = RunDirector.SavePathProvider; RunDirector.SavePathProvider = () => path;
            yield return SceneManager.LoadSceneAsync("RunBootstrap");
            for (int i = 0; i < 240; i++) { director = Object.FindAnyObjectByType<RunDirector>(); if (director != null && director.Ready) break; yield return null; }
            Assert.That(director, Is.Not.Null); Assert.That(director.Ready, Is.True); director.AutomaticTick = false;
            yield return null; yield return null; bridge = director.GetComponent<ResultsBridge>(); Assert.That(bridge, Is.Not.Null);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            driver?.Dispose(); driver = null;
            var live = Object.FindAnyObjectByType<RunDirector>(); if (live != null) live.TryAbandon();
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
            yield return null;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("After results validation"));
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            yield return null;
            RunDirector.SavePathProvider = previousPath;
            string resolved = Path.GetFullPath(directory), temp = Path.GetFullPath(Application.temporaryCachePath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Assert.That(resolved.StartsWith(temp, StringComparison.OrdinalIgnoreCase), Is.True); Assert.That(Path.GetFileName(resolved), Does.StartWith("QuackQuack-T061Scene-"));
            if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
        }
        private IEnumerator ControllerResults(bool saved)
        {
            driver=new MenuRegressionInput(); director.TryAbandon(); yield return null;
            controllerRetries=controllerRestarts=0;
            bridge.View.Bind(()=>controllerRetries++,()=>controllerRestarts++);
            bridge.View.Show(director.Result,bridge.Committed.Receipt,id=>id,saved);
            foreach(var scroll in bridge.View.GetComponentsInChildren<ScrollRect>()) scroll.content.GetComponent<Text>().text += string.Concat(Enumerable.Repeat("\nDetalhe da partida",80));
            Canvas.ForceUpdateCanvases(); yield return null; yield return null; Canvas.ForceUpdateCanvases();
            Assert.That(bridge.View.GetComponentsInChildren<Scrollbar>().Length,Is.EqualTo(3));
        }
        private Button ResultButton(string name) => bridge.View.GetComponentsInChildren<Button>(true).Single(b=>b.name==name);
        [UnityTest] public IEnumerator ResultsControllerCanReachActionsFromEveryColumn([ValueSource(nameof(SaveStates))] bool saved)
        {
            yield return ControllerResults(saved);
            var restart=ResultButton("JOGAR NOVAMENTE"); var retry=ResultButton("TENTAR SALVAR");
            foreach(var bar in bridge.View.GetComponentsInChildren<Scrollbar>()) {
                bar.Select(); bool reachedRestart=false, reachedRetry=false;
                for(int i=0;i<5;i++) {
                    yield return driver.Press(GamepadButton.DpadRight);
                    var selected=EventSystem.current.currentSelectedGameObject;
                    reachedRestart |= selected==restart.gameObject; reachedRetry |= selected==retry.gameObject;
                    MenuRegressionInput.AssertUsableFocus(bridge.View.transform);
                }
                Assert.That(reachedRestart,Is.True,"Right navigation trapped in columns");
                Assert.That(reachedRetry,Is.EqualTo(!saved),"Save action must be reachable only when available");
            }
            MenuRegressionInput.AssertReachableActions(bridge.View.transform,restart);
            if(!saved) {retry.Select(); yield return driver.Press(GamepadButton.South); Assert.That(controllerRetries,Is.EqualTo(1));}
            restart.Select(); yield return driver.Press(GamepadButton.South);
            Assert.That(controllerRestarts,Is.EqualTo(1)); Assert.That(GameBootstrap.Instance.Run.Phase,Is.EqualTo(RunPhase.Results));
        }
        [UnityTest] public IEnumerator ResultsScrollPreservesVerticalAxisAndCancelReachesActions([ValueSource(nameof(ScrollInputs))] string source)
        {
            yield return ControllerResults(true);
            foreach(var bar in bridge.View.GetComponentsInChildren<Scrollbar>()) {
                bar.value=1; bar.Select();
                if(source=="Dpad")yield return driver.Press(GamepadButton.DpadDown); else yield return driver.Tilt(Vector2.down);
                Assert.That(EventSystem.current.currentSelectedGameObject,Is.EqualTo(bar.gameObject)); Assert.That(bar.value,Is.LessThan(1));
                yield return driver.Press(GamepadButton.East);
                Assert.That(EventSystem.current.currentSelectedGameObject,Is.EqualTo(ResultButton("JOGAR NOVAMENTE").gameObject));
            }
            EventSystem.current.SetSelectedGameObject(null); driver.Dispose(); driver=new MenuRegressionInput(); yield return null;
            MenuRegressionInput.AssertUsableFocus(bridge.View.transform);
            Assert.That(controllerRetries+controllerRestarts,Is.Zero,"Cancel/hotplug must not activate an action");
        }
        [UnityTest] public IEnumerator ResultsHiddenBarsAndSavedRetryCannotTrapFocus()
        {
            yield return ControllerResults(false);
            var bar=bridge.View.GetComponentsInChildren<Scrollbar>()[0]; bar.Select();
            foreach(var scroll in bridge.View.GetComponentsInChildren<ScrollRect>()) scroll.content.GetComponent<Text>().text="";
            Canvas.ForceUpdateCanvases(); yield return null; yield return null; Canvas.ForceUpdateCanvases();
            MenuRegressionInput.AssertUsableFocus(bridge.View.transform);
            bridge.View.Show(director.Result,bridge.Committed.Receipt,id=>id,true); yield return null;
            Assert.That(ResultButton("TENTAR SALVAR").IsInteractable(),Is.False);
            MenuRegressionInput.AssertReachableActions(bridge.View.transform,ResultButton("JOGAR NOVAMENTE"));
            Assert.That(controllerRetries+controllerRestarts,Is.Zero);
        }
        [UnityTest] public IEnumerator ResultsControllerRetrySavesWithoutImplicitRestart()
        {
            driver=new MenuRegressionInput(); director.Persist();
            string held=path+".controller-held";
            Assert.That(Path.GetDirectoryName(Path.GetFullPath(held)),Is.EqualTo(Path.GetFullPath(directory)));
            File.Move(path,held);
            try {
                Directory.CreateDirectory(path); director.TryAbandon(); yield return null;
                Assert.That(bridge.View.CanRetry,Is.True);
            } finally {
                if(Directory.Exists(path))Directory.Delete(path,false);
                File.Move(held,path);
            }
            string original=director.RunId; ResultButton("TENTAR SALVAR").Select(); yield return driver.Press(GamepadButton.South);
            Assert.That(bridge.View.CanRetry,Is.False); Assert.That(bridge.View.CanRestart,Is.True);
            Assert.That(director.RunId,Is.EqualTo(original)); Assert.That(GameBootstrap.Instance.Run.Phase,Is.EqualTo(RunPhase.Results));
            Assert.That(Service().Load().Data.Progress.Runs,Is.EqualTo(1));
            Assert.That(EventSystem.current.currentSelectedGameObject,Is.EqualTo(ResultButton("JOGAR NOVAMENTE").gameObject));
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
            // An open handle does not reliably block replacement on Android.
            // Obstruct only this fixture's destination, preserving its previous file.
            string held = path + ".fixture-held";
            Assert.That(Path.GetDirectoryName(Path.GetFullPath(path)), Is.EqualTo(Path.GetFullPath(directory)));
            Assert.That(Path.GetDirectoryName(Path.GetFullPath(held)), Is.EqualTo(Path.GetFullPath(directory)));
            File.Move(path, held);
            try
            {
                Directory.CreateDirectory(path);
                director.TryAbandon(); yield return null;
                Assert.That(bridge.View.SaveText, Does.Contain("não salvo")); Assert.That(bridge.View.CanRetry, Is.True); Assert.That(bridge.View.CanRestart, Is.True);
                Assert.That(Service().Load().Data.Progress.Runs, Is.Zero);
            }
            finally
            {
                if (Directory.Exists(path)) Directory.Delete(path, false);
                File.Move(held, path);
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
