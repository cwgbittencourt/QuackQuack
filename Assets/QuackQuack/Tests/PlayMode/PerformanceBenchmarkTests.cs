#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NUnit.Framework;
using QuackQuack.Systems.Diagnostics;
using QuackQuack.Systems.Pooling;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.PlayMode
{
    public class PerformanceBenchmarkTests
    {
        private PerformanceBenchmark benchmark;
        private float previousTimeScale;
        [SetUp]
        public void SetUp() { previousTimeScale = Time.timeScale; Time.timeScale = 0; }
        private IEnumerator Load()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/QuackQuack/Scenes/PerformanceBenchmark.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            benchmark = Object.FindAnyObjectByType<PerformanceBenchmark>();
            Assert.That(benchmark, Is.Not.Null);
            benchmark.AutomaticTick = false;
            Assert.That(benchmark.Ready, Is.True);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = previousTimeScale;
            if (benchmark != null) Object.Destroy(benchmark.gameObject);
            yield return null;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("After benchmark"));
            if (old.IsValid() && old.isLoaded) yield return SceneManager.UnloadSceneAsync(old);
        }

        [UnityTest]
        public IEnumerator BothLoadsSaturateAndRecycleWithoutCreatingMoreObjectsOrSavingProgress()
        {
            yield return Load();
            var pools = benchmark.Pools;
            int created = benchmark.CreatedObjects;
            int objects = benchmark.GetComponentsInChildren<Transform>(true).Length;
            Assert.That(benchmark.Director.ActiveCount, Is.EqualTo(200));
            AssertSaturated();
            while (benchmark.Tick < PerformanceBenchmark.WarmupTicks) benchmark.Step();
            Capture("t071-200.png");
            for (int i = benchmark.Tick; i < PerformanceBenchmark.WarmupTicks + PerformanceBenchmark.StageTicks; i++) benchmark.Step();
            Assert.That(benchmark.Director.ActiveCount, Is.EqualTo(400));
            Assert.That(benchmark.CreatedObjects, Is.EqualTo(created));
            Assert.That(benchmark.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(objects));
            AssertSaturated();
            Capture("t071-400.png");
            while (!benchmark.Completed) benchmark.Step();
            Assert.That(benchmark.Director.ActiveCount, Is.EqualTo(400));
            Assert.That(benchmark.CreatedObjects, Is.EqualTo(created));
            Assert.That(benchmark.RecycledObjects, Is.GreaterThan(50000));
            Assert.That(benchmark.SaturationChecks, Is.EqualTo(benchmark.Tick + 1));
            int tick = benchmark.Tick; benchmark.Step(); Assert.That(benchmark.Tick, Is.EqualTo(tick));
            Assert.That(Object.FindAnyObjectByType<QuackQuack.Systems.Run.RunDirector>(), Is.Null);
            Assert.That(Object.FindAnyObjectByType<QuackQuack.Systems.Run.GameBootstrap>(), Is.Null);
            Object.Destroy(benchmark.gameObject); yield return null;
            Assert.That(pools.IsDisposed, Is.True);
        }

        [UnityTest]
        public IEnumerator ReloadWithSameSeedAndTicksReproducesEnemyPositions()
        {
            yield return Load();
            // Reload after disabling automatic advancement, to start both samples at tick zero.
            Object.Destroy(benchmark.gameObject); yield return null;
            yield return Load();
            int initialTick = benchmark.Tick;
            for (int i = 0; i < 25; i++) benchmark.Step();
            var positions = new Vector2[200];
            for (int i = 0; i < positions.Length; i++)
            { Assert.That(benchmark.Director.TryGetActive(i, out var enemy, out _), Is.True); positions[i] = enemy.Body.position; }
            yield return Load();
            Assert.That(benchmark.Tick, Is.EqualTo(initialTick));
            for (int i = 0; i < 25; i++) benchmark.Step();
            for (int i = 0; i < positions.Length; i++)
            { Assert.That(benchmark.Director.TryGetActive(i, out var enemy, out _), Is.True); Assert.That(Vector2.Distance(enemy.Body.position, positions[i]), Is.LessThan(0.00001f)); }
        }

        private void AssertSaturated()
        {
            Assert.That(benchmark.Pools.GetActiveCount(PoolFamily.Projectile), Is.EqualTo(512));
            Assert.That(benchmark.Pools.GetActiveCount(PoolFamily.Pickup), Is.EqualTo(512));
            Assert.That(benchmark.Pools.GetActiveCount(PoolFamily.Vfx), Is.GreaterThanOrEqualTo(128));
        }

        [UnityTest]
        public IEnumerator SceneRunsAutomaticallyOnFixedSteps()
        {
            yield return Load();
            int before = benchmark.Tick;
            benchmark.AutomaticTick = true;
            Time.timeScale = 1;
            yield return new WaitForFixedUpdate(); yield return null;
            Assert.That(benchmark.Tick, Is.GreaterThan(before));
            Assert.That(benchmark.Director.ActiveCount, Is.EqualTo(200));
            AssertSaturated();
            benchmark.AutomaticTick = false;
            Time.timeScale = 0;
        }

        private static void Capture(string name)
        {
            var camera = Camera.main;
            var canvas = Object.FindAnyObjectByType<Canvas>();
            var target = new RenderTexture(1280, 720, 24); target.Create();
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                camera.targetTexture = target; camera.aspect = 16f / 9;
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                var path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../Logs", name));
                Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null;
                camera.targetTexture = null; RenderTexture.active = previous;
                Object.Destroy(pixels); target.Release(); Object.Destroy(target);
            }
        }
    }
}
#endif
