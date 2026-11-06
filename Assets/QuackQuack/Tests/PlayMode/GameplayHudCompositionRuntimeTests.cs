using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Player;
using QuackQuack.Gameplay.Weapons;
using QuackQuack.Presentation.UI;
using QuackQuack.Systems.Run;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.PlayMode
{
    [Category("HudCompositionRuntime")]
    public sealed class GameplayHudCompositionRuntimeTests
    {
        private string directory;
        private Func<string> previousPath;
        private RunDirector director;
        private GameplayHudComposition composition;
        private GameplayHud hud;
        private static T Field<T>(Object target, string name) => (T)target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
        private IEnumerator Load(string entry)
        {
            directory = Path.Combine(Application.temporaryCachePath, "QuackQuack-T038-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
            previousPath = RunDirector.SavePathProvider; RunDirector.SavePathProvider = () => Path.Combine(directory,"save.json");
            yield return SceneManager.LoadSceneAsync(entry);
            for (int i = 0; i < 300; i++)
            {
                composition = Object.FindAnyObjectByType<GameplayHudComposition>();
                if (composition != null && composition.Ready) break; yield return null;
            }
            Assert.That(composition != null && composition.Ready, Is.True);
            hud = composition.GetComponent<GameplayHud>(); director = Object.FindAnyObjectByType<RunDirector>();
            GameBootstrap.Instance.ExternalClock = true;
            if (director != null) { director.AutomaticTick = false; director.Scheduler.enabled = false; }
            foreach (var runner in Object.FindObjectsByType<WeaponRunner>(FindObjectsSortMode.None)) runner.enabled = false;
        }
        private static readonly string[] Entries = { "Bootstrap", "RunBootstrap" };
        [UnityTest] public IEnumerator SceneCompositionBindsResourcesAndHintsWithoutFallbackObjects([ValueSource(nameof(Entries))] string entry)
        {
            yield return Load(entry); var run = GameBootstrap.Instance.Run;
            var hints = Field<ContextualHintsView>(composition,"hints");
            Assert.That(hints.IsVisible, Is.True); Assert.That(Field<Text>(hints,"label").text, Does.Contain("MOVER").And.Contain("RELÍQUIAS"));
            Assert.That(Field<Text>(hud,"healthLabel"), Is.Null); Assert.That(Field<Text>(hud,"terrainLabel"), Is.Null);
            int count = hud.GetComponentsInChildren<RectTransform>(true).Length;
            for (int i = 0; i < 100; i++) hud.Render(Field<PlayerController>(composition,"player").Snapshot());
            Assert.That(hud.GetComponentsInChildren<RectTransform>(true).Length, Is.EqualTo(count));
            Assert.That(run.TryTransition(RunPhase.Paused), Is.True); double before = run.ElapsedSeconds;
            run.Advance(100); yield return null; Assert.That(hints.IsVisible, Is.False); Assert.That(run.ElapsedSeconds, Is.EqualTo(before));
            Assert.That(Field<Text>(hud,"statusLabel"), Is.Null, "T040: no redundant status footer, including while paused.");
            Assert.That(run.TryResume(), Is.True); run.Advance(Math.Max(0,10-run.ElapsedSeconds)); yield return null;
            Assert.That(hints.IsVisible, Is.False); composition.enabled = false; composition.enabled = true; yield return null;
            Assert.That(hints.IsVisible, Is.False); Assert.That(hud.GetComponentsInChildren<RectTransform>(true).Length, Is.EqualTo(count));
        }
        [UnityTest] public IEnumerator AuthoredSpecialViewReceivesDirectorBossHealthAndTerminalCleanup()
        {
            yield return Load("RunBootstrap"); var run = GameBootstrap.Instance.Run;
            run.Advance(Math.Max(0,900-run.ElapsedSeconds)); director.Advance(0,new Rect(-4,-3,8,6));
            Assert.That(director.Boss, Is.Not.Null);
            var player = Field<PlayerController>(composition,"player");
            director.Boss.transform.position = player.transform.position + Vector3.right * 2;
            Physics2D.SyncTransforms(); yield return null; yield return null;
            var view = Field<SpecialEnemyBarsView>(composition,"specials");
            Assert.That(view.VisibleCount, Is.EqualTo(1));
            Assert.That(view.TryGetBar(director.Boss.GetInstanceID(),director.Boss.LeaseId,out var bar), Is.True);
            director.Boss.TakeDamage(director.Boss.MaxHealth/4,player.GetInstanceID(),0); yield return null;
            Assert.That(bar.Find("Health track/Health fill").GetComponent<RectTransform>().anchorMax.x, Is.EqualTo(.75f));
            run.TryTransition(RunPhase.Paused); yield return null; Assert.That(view.VisibleCount, Is.EqualTo(1));
            director.TryAbandon(); yield return null; Assert.That(view.VisibleCount, Is.Zero);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (director != null) director.TryAbandon();
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
            yield return null;
            var scene = SceneManager.GetActiveScene(); SceneManager.SetActiveScene(SceneManager.CreateScene("After HUD composition"));
            if(scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            RunDirector.SavePathProvider = previousPath;
            string resolved = Path.GetFullPath(directory), prefix = Path.GetFullPath(Application.temporaryCachePath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Assert.That(resolved.StartsWith(prefix,StringComparison.OrdinalIgnoreCase) && Path.GetFileName(resolved).StartsWith("QuackQuack-T038-"), Is.True);
            if (Directory.Exists(resolved)) Directory.Delete(resolved,true);
        }
    }
}
