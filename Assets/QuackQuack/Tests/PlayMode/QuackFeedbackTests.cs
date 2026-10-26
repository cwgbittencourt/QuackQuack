using System.Collections;
using NUnit.Framework;
using QuackQuack.Gameplay.Player;
using QuackQuack.Presentation.VFX;
using QuackQuack.Systems.Run;
using QuackQuack.Core.Run;
using QuackQuack.Core.Combat;
using QuackQuack.Systems.Pooling;
using QuackQuack.Gameplay.Weapons;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace QuackQuack.Tests.PlayMode
{
    public class QuackFeedbackTests
    {
        [UnityTest]
        public IEnumerator FinishedIsEmittedOnceAndResetClearsVisuals()
        {
            var obj = new GameObject("Temporary feedback");
            var material = new Material(Shader.Find("Sprites/Default"));
            try
            {
                var view = obj.AddComponent<QuackFeedbackController>();
                view.Initialize(material);
                int finished = 0;
                view.Finished = () => finished++;
                view.Play(Vector2.zero, 1, Color.white, 0.01f);
                yield return new WaitForSeconds(0.12f);
                Assert.That(finished, Is.EqualTo(1));
                view.ResetEffect();
                Assert.That(obj.GetComponent<LineRenderer>().enabled, Is.False);
            }
            finally { Object.DestroyImmediate(obj); Object.DestroyImmediate(material); }
        }

        [UnityTest]
        public IEnumerator AcceptedQuackCreatesPooledVisualAndTemporaryAudio()
        {
            yield return LoadArena();
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(player.ActivateQuack(), Is.True);
            var view = Object.FindAnyObjectByType<QuackFeedbackController>();
            Assert.That(view, Is.Not.Null, "An accepted Quack must create visible pooled feedback.");
            Assert.That(view.GetComponent<LineRenderer>().enabled, Is.True);
            Assert.That(view.GetComponent<AudioSource>().clip, Is.Not.Null);
            yield return new WaitForSeconds(0.8f);
            Assert.That(GameBootstrap.Instance.Pools.GetActiveCount(QuackQuack.Systems.Pooling.PoolFamily.Vfx), Is.Zero);
        }

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
            yield return null;
            var scene = SceneManager.GetActiveScene();
            if (scene.name == "After feedback test") yield break;
            SceneManager.SetActiveScene(SceneManager.CreateScene("After feedback test"));
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        }

        private static IEnumerator LoadArena()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            for (int i = 0; i < 120 && SceneManager.GetActiveScene().name != "Lake"; i++) yield return null;
            yield return null; yield return null;
            // Isolate manual Quack feedback from autonomous weapon impact effects.
            foreach (var weapon in Object.FindObjectsByType<WeaponRunner>(FindObjectsSortMode.None)) weapon.enabled = false;
            var feedback = Object.FindAnyObjectByType<QuackFeedbackBridge>();
            feedback.enabled = false; feedback.enabled = true;
        }

        [UnityTest]
        public IEnumerator CriticalOutlineFollowsTargetWithoutChangingItsHealth()
        {
            yield return LoadArena();
            var player = Object.FindAnyObjectByType<PlayerController>();
            QuackTarget enemy = null;
            foreach (var target in Object.FindObjectsByType<QuackTarget>(FindObjectsSortMode.None))
                if (target.gameObject.layer == 9 && !target.IsProjectile) { enemy = target; break; }
            Assert.That(enemy, Is.Not.Null);
            enemy.transform.position = player.transform.position + Vector3.right * 2;
            Physics2D.SyncTransforms();
            Assert.That(player.ActivateQuack(), Is.True);
            QuackFeedbackController outline = null;
            foreach (var view in Object.FindObjectsByType<QuackFeedbackController>(FindObjectsSortMode.None))
                if (view.IsOutline) { outline = view; break; }
            Assert.That(outline, Is.Not.Null);
            Assert.That(outline.GetComponent<LineRenderer>().startColor, Is.EqualTo(Color.white));
            float health = enemy.Health;
            enemy.transform.position += Vector3.left * 0.25f;
            yield return null; yield return null;
            Assert.That(Vector2.Distance(outline.transform.position, enemy.GetComponent<SpriteRenderer>().bounds.center), Is.LessThan(0.01f));
            Assert.That(enemy.Health, Is.EqualTo(health));
        }

        [UnityTest]
        public IEnumerator PauseFreezesEffectAndResumeReturnsItCleanToPool()
        {
            yield return LoadArena();
            var bootstrap = GameBootstrap.Instance;
            Object.FindAnyObjectByType<PlayerController>().ActivateQuack();
            var view = Object.FindAnyObjectByType<QuackFeedbackController>();
            Assert.That(view, Is.Not.Null);
            bootstrap.Run.TryTransition(RunPhase.Paused);
            float age = view.Age;
            yield return new WaitForSeconds(0.2f);
            Assert.That(view.Age, Is.EqualTo(age));
            bootstrap.Run.TryTransition(RunPhase.Playing);
            yield return new WaitForSeconds(0.7f);
            Assert.That(bootstrap.Pools.GetActiveCount(PoolFamily.Vfx), Is.Zero);
            Assert.That(view.IsPlaying, Is.False);
            Assert.That(view.GetComponent<AudioSource>().clip, Is.Null);
            Assert.That(view.GetComponent<LineRenderer>().positionCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator SaturationDropsCosmeticsAndReenableDoesNotDuplicateSubscriptions()
        {
            yield return LoadArena();
            var bootstrap = GameBootstrap.Instance;
            var bridge = Object.FindAnyObjectByType<QuackFeedbackBridge>();
            Assert.That(bridge.Ready, Is.True);
            int count = Object.FindObjectsByType<QuackFeedbackController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            var player = Object.FindAnyObjectByType<PlayerController>();
            float health = player.State.Health, breath = player.State.Breath.Current;
            var value = new CombatEvent(1, 1, 0, 0, Vector2.zero, 4, CombatEventType.QuackActivated);
            for (int i = 0; i < 100; i++) bootstrap.Events.Publish(value);
            Assert.That(bootstrap.Pools.GetActiveCount(PoolFamily.Vfx), Is.EqualTo(32));
            Assert.That(Object.FindObjectsByType<QuackFeedbackController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(count));
            Assert.That(player.State.Health, Is.EqualTo(health));
            Assert.That(player.State.Breath.Current, Is.EqualTo(breath));
            bridge.enabled = false;
            Assert.That(bootstrap.Pools.GetActiveCount(PoolFamily.Vfx), Is.Zero);
            bootstrap.Events.Publish(value);
            Assert.That(bootstrap.Pools.GetActiveCount(PoolFamily.Vfx), Is.Zero);
            bridge.enabled = true;
            bootstrap.Events.Publish(value);
            Assert.That(bootstrap.Pools.GetActiveCount(PoolFamily.Vfx), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ReflectionPulseUsesCyanAndImportedSoundWithoutReapplyingRules()
        {
            yield return LoadArena();
            var player = Object.FindAnyObjectByType<PlayerController>();
            float before = player.State.Breath.Current;
            GameBootstrap.Instance.Events.Publish(new CombatEvent(1, 1, 0, 0, Vector2.zero, 0, CombatEventType.ProjectileReflected));
            var view = Object.FindAnyObjectByType<QuackFeedbackController>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.GetComponent<LineRenderer>().startColor, Is.EqualTo(Color.cyan));
            Assert.That(view.GetComponent<AudioSource>().clip.name, Is.EqualTo("Reflection"));
            Assert.That(view.GetComponent<AudioSource>().clip.samples, Is.GreaterThan(0));
            Assert.That(player.State.Breath.Current, Is.EqualTo(before));
        }
    }
}
