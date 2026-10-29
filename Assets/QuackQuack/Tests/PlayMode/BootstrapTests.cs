using System.Collections;
using NUnit.Framework;
using QuackQuack.Core.Run;
using QuackQuack.Systems.Run;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace QuackQuack.Tests.PlayMode
{
    public class BootstrapTests
    {
        [TestCase(false, 60)]
        [TestCase(true, 30)]
        [Category("FrameRateRuntime")]
        public void FrameLimitUsesNominalOrOptionalLowRate(bool lowRate, int expected)
        {
            int previous = Application.targetFrameRate;
            if (GameBootstrap.Instance != null) Object.DestroyImmediate(GameBootstrap.Instance.gameObject);
            var root = new GameObject("Frame limit fixture"); root.SetActive(false);
            try
            {
                var bootstrap = root.AddComponent<GameBootstrap>();
                typeof(GameBootstrap).GetField("limitTo30Fps", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(bootstrap, lowRate);
                root.SetActive(true);
                Assert.That(Application.targetFrameRate, Is.EqualTo(expected));
            }
            finally { Object.DestroyImmediate(root); Application.targetFrameRate = previous; }
        }

        [UnityTest]
        public IEnumerator BootstrapLoadsLakeWithServicesAndStableSeed()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            for (int i = 0; i < 120 && SceneManager.GetActiveScene().name != "Lake"; i++) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Lake"));
            var bootstrap = GameBootstrap.Instance;
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(bootstrap.Run.Phase, Is.EqualTo(RunPhase.Playing));
            Assert.That(bootstrap.Random.Next(), Is.EqualTo(new System.Random(bootstrap.Run.Seed).Next()));
            Assert.That(bootstrap.Pools, Is.Not.Null);
            Object.Destroy(bootstrap.gameObject);
            yield return null;
            Assert.That(GameBootstrap.Instance, Is.Null);
            var lake = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("After bootstrap test");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(lake);
        }
    }
}
