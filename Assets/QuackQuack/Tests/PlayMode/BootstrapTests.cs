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
        }
    }
}
