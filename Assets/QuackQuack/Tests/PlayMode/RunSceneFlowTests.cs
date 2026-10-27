#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Upgrades;
using QuackQuack.Systems.Run;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
namespace QuackQuack.Tests.PlayMode
{
    public class RunSceneFlowTests
    {
        private string directory;
        private Func<string> previousPath;
        [UnitySetUp] public IEnumerator SetUp()
        {
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
            yield return null;
            directory = Path.Combine(Path.GetTempPath(), "QuackQuack-T060Scene-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
            previousPath = RunDirector.SavePathProvider; RunDirector.SavePathProvider = () => Path.Combine(directory, "save.json");
            yield return SceneManager.LoadSceneAsync("RunBootstrap");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var director = Object.FindAnyObjectByType<RunDirector>(); if (director != null) director.TryAbandon();
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
            yield return SceneManager.LoadSceneAsync("Bootstrap"); // Unload LakeRun while the injected path is still owned.
            yield return null;
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
            yield return null; RunDirector.SavePathProvider = previousPath;
            string resolved = Path.GetFullPath(directory), temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Assert.That(resolved.StartsWith(temp, StringComparison.OrdinalIgnoreCase), Is.True); Assert.That(Path.GetFileName(resolved), Does.StartWith("QuackQuack-T060Scene-"));
            if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
        }
        [UnityTest] public IEnumerator FullSceneStartsOnceWithSingleClockControlsAndPersistentRelics()
        {
            RunDirector director = null;
            for (int i = 0; i < 240; i++) { director = Object.FindAnyObjectByType<RunDirector>(); if (director != null && director.Ready) break; yield return null; }
            Assert.That(director, Is.Not.Null); Assert.That(director.Ready, Is.True);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("LakeRun")); Assert.That(GameBootstrap.Instance.ExternalClock, Is.True);
            director.AutomaticTick = false; var run = GameBootstrap.Instance.Run;
            double before = run.ElapsedSeconds; yield return null; yield return null; Assert.That(run.ElapsedSeconds, Is.EqualTo(before));
            var relics = Object.FindAnyObjectByType<LakeRelics>(); relics.Show(true);
            string id = new List<string>(relics.Loadout.UnlockedIds)[0]; Assert.That(relics.Toggle(id), Is.True);
            yield return null; yield return null;
            Assert.That(File.Exists(Path.Combine(directory, "save.json")), Is.True);
            Capture("t060-loadout.png");
            relics.Show(false); yield return null; Capture("t060-scene.png");
            Assert.That(director.TryAbandon(), Is.True);
            Assert.That(director.Result.Outcome, Is.EqualTo(RunPhase.Abandoned)); yield return null;
            Assert.That(director.LastSave.Status, Is.EqualTo(QuackQuack.Systems.Save.SaveStatus.Success));
        }
        private static void Capture(string filename)
        {
            var camera = Camera.main; var target = new RenderTexture(1280, 720, 24); var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previous = RenderTexture.active; var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = new RenderMode[canvases.Length]; var cameras = new Camera[canvases.Length]; float aspect = camera.aspect;
            try
            {
                for (int i = 0; i < canvases.Length; i++) { modes[i] = canvases[i].renderMode; cameras[i] = canvases[i].worldCamera; if (modes[i] == RenderMode.ScreenSpaceOverlay) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = 1; } }
                camera.targetTexture = target; camera.aspect = 1280f / 720; Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../Logs/", filename)); Directory.CreateDirectory(Path.GetDirectoryName(output)); File.WriteAllBytes(output, pixels.EncodeToPNG());
            }
            finally
            {
                for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = modes[i]; canvases[i].worldCamera = cameras[i]; }
                camera.targetTexture = null; camera.aspect = aspect; RenderTexture.active = previous; Object.DestroyImmediate(pixels); Object.DestroyImmediate(target);
            }
        }
    }
}
#endif
