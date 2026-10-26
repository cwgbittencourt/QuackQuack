using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Player;
using QuackQuack.Systems.Pooling;
using QuackQuack.Systems.Run;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.PlayMode
{
    // Integration contract for T040/T041. No substitute pickup, progression or combat loop.
    public class UpgradeFlowTests
    {
        private Type upgradesType, pickupType;
        private object upgrades;
        private PoolService pools;
        private RunState run;
        private PlayerController player;
        private int selectionsStarted, playingTransitions;
        private Action<RunPhase> onPhaseChanged;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            selectionsStarted = playingTransitions = 0;
            run = null;
            player = null;
            upgrades = null;
            pools = null;
            onPhaseChanged = null;
            upgradesType = typeof(GameBootstrap).Assembly.GetType("QuackQuack.Systems.Upgrades.UpgradeSystem");
            pickupType = typeof(GameBootstrap).Assembly.GetType("QuackQuack.Gameplay.Pickups.ExperiencePickup");
            Assert.That(upgradesType, Is.Not.Null, "T040 pending: UpgradeSystem is missing (T035 red phase).");
            Assert.That(pickupType, Is.Not.Null, "T041 pending: ExperiencePickup is missing (T035 red phase).");
            Assert.That(typeof(Component).IsAssignableFrom(pickupType), Is.True);
            Assert.That(typeof(IPoolLifecycle).IsAssignableFrom(pickupType), Is.True);

            yield return SceneManager.LoadSceneAsync("Bootstrap");
            for (int i = 0; i < 120 && SceneManager.GetActiveScene().name != "Lake"; i++) yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Lake"));
            player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(player, Is.Not.Null);
            Assert.That(player.State, Is.Not.Null);
            run = GameBootstrap.Instance.Run;
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Playing));
            upgrades = Activator.CreateInstance(upgradesType, 12345);
            Invoke(upgrades, "InitializeProgression", run, new[] { 10, 20, 30 }, new[] { "a", "b", "c" });
            pools = new PoolService();
            typeof(UpgradeFlowTests).GetMethod(nameof(RegisterPickup), BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(pickupType).Invoke(this, null);
            onPhaseChanged = phase =>
            {
                if (phase == RunPhase.ChoosingUpgrade) selectionsStarted++;
                if (phase == RunPhase.Playing) playingTransitions++;
            };
            run.Changed += onPhaseChanged;
        }

        [UnityTest]
        public IEnumerator PhysicalCollectionAwardsExperienceOnceAndReturnsPickupToPool()
        {
            var pickup = AcquirePickup(4);
            yield return CollideWithPlayer(pickup);
            Assert.That(Read<int>(upgrades, "Experience"), Is.EqualTo(4));
            Assert.That(Read<int>(upgrades, "Level"), Is.EqualTo(1));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Playing));
            Assert.That(pools.GetActiveCount(PoolFamily.Pickup), Is.Zero);
            Assert.That(pickup.gameObject.activeSelf, Is.False);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(Read<int>(upgrades, "Experience"), Is.EqualTo(4));
            Assert.That(selectionsStarted, Is.Zero);

            var reused = AcquirePickup(3);
            Assert.That(reused, Is.SameAs(pickup));
            yield return CollideWithPlayer(reused);
            Assert.That(Read<int>(upgrades, "Experience"), Is.EqualTo(7));
            Assert.That(pools.ActiveCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator UpgradeSelectionFreezesActualPlayerCombatAndRunClockAcrossFrames()
        {
            player.State.Breath.TryConsume(35, out _);
            yield return CollideWithPlayer(AcquirePickup(10));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.ChoosingUpgrade));
            Assert.That(run.IsSimulating, Is.False);
            var body = player.GetComponent<Rigidbody2D>();
            yield return new WaitForFixedUpdate();
            Vector2 position = body.position;
            float health = player.State.Health;
            float breath = player.State.Breath.Current;
            double elapsed = run.ElapsedSeconds;
            var targets = Object.FindObjectsByType<QuackTarget>(FindObjectsSortMode.None);
            var targetPositions = new Vector3[targets.Length];
            for (int i = 0; i < targets.Length; i++) targetPositions[i] = targets[i].transform.position;
            Assert.That(player.ActivateQuack(), Is.False);
            yield return new WaitForSeconds(0.15f);
            yield return new WaitForFixedUpdate();
            Assert.That(body.position, Is.EqualTo(position));
            Assert.That(player.State.Health, Is.EqualTo(health));
            Assert.That(player.State.Breath.Current, Is.EqualTo(breath));
            Assert.That(run.ElapsedSeconds, Is.EqualTo(elapsed));
            for (int i = 0; i < targets.Length; i++)
                Assert.That(targets[i].transform.position, Is.EqualTo(targetPositions[i]));
            Assert.That(selectionsStarted, Is.EqualTo(1));
            Assert.That(playingTransitions, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ConfirmationResumesOnceAndRepeatedUiCallbacksCannotChooseTwice()
        {
            yield return CollideWithPlayer(AcquirePickup(10));
            object offer = Read<object>(upgrades, "CurrentOffer");
            string id = Read<string>(offer, "OfferId");
            string choice = Read<IReadOnlyList<string>>(offer, "Choices")[0];
            Assert.That((bool)Invoke(upgrades, "TryConfirm", id, choice), Is.True);
            Assert.That((bool)Invoke(upgrades, "TryConfirm", id, choice), Is.False);
            Assert.That((bool)Invoke(upgrades, "TrySkip", id), Is.False);
            yield return null;
            yield return new WaitForFixedUpdate();
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Playing));
            Assert.That(selectionsStarted, Is.EqualTo(1));
            Assert.That(playingTransitions, Is.EqualTo(1));
            CollectionAssert.AreEqual(new[] { choice }, Read<IReadOnlyList<string>>(upgrades, "ConfirmedChoices"));
            double resumed = run.ElapsedSeconds;
            yield return new WaitForSeconds(0.1f);
            Assert.That(run.ElapsedSeconds, Is.GreaterThan(resumed));
        }

        [UnityTest]
        public IEnumerator InvalidChoiceKeepsCombatFrozenUntilOneValidSkip()
        {
            yield return CollideWithPlayer(AcquirePickup(10));
            string id = Read<string>(Read<object>(upgrades, "CurrentOffer"), "OfferId");
            Assert.That((bool)Invoke(upgrades, "TryConfirm", id, "not-offered"), Is.False);
            yield return new WaitForFixedUpdate();
            Assert.That(run.Phase, Is.EqualTo(RunPhase.ChoosingUpgrade));
            Assert.That(playingTransitions, Is.Zero);
            Assert.That((bool)Invoke(upgrades, "TrySkip", id), Is.True);
            Assert.That((bool)Invoke(upgrades, "TrySkip", id), Is.False);
            yield return null;
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Playing));
            Assert.That(playingTransitions, Is.EqualTo(1));
            Assert.That(Read<IReadOnlyList<string>>(upgrades, "ConfirmedChoices"), Is.Empty);
            Assert.That(Read<int>(upgrades, "RerollsRemaining"), Is.EqualTo(2));
            Assert.That(Read<int>(upgrades, "BanishRemaining"), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator MultipleLevelsQueueDistinctSelectionsWithoutResumingBetweenThem()
        {
            yield return CollideWithPlayer(AcquirePickup(20));
            Assert.That(Read<int>(upgrades, "Level"), Is.EqualTo(3));
            Assert.That(Read<int>(upgrades, "Experience"), Is.EqualTo(20));
            object first = Read<object>(upgrades, "CurrentOffer");
            string firstId = Read<string>(first, "OfferId");
            Assert.That((bool)Invoke(upgrades, "TrySkip", firstId), Is.True);
            yield return null;
            object second = Read<object>(upgrades, "CurrentOffer");
            string secondId = Read<string>(second, "OfferId");
            Assert.That(secondId, Is.Not.EqualTo(firstId));
            Assert.That(Read<bool>(second, "IsClosed"), Is.False);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.ChoosingUpgrade));
            Assert.That(playingTransitions, Is.Zero);
            Assert.That((bool)Invoke(upgrades, "TrySkip", firstId), Is.False);
            Assert.That((bool)Invoke(upgrades, "TrySkip", secondId), Is.True);
            yield return null;
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Playing));
            Assert.That(playingTransitions, Is.EqualTo(1));
            Assert.That(pools.ActiveCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator CallbackAfterAbandonCannotGrantUpgradeOrResumeRun()
        {
            yield return CollideWithPlayer(AcquirePickup(10));
            object offer = Read<object>(upgrades, "CurrentOffer");
            string id = Read<string>(offer, "OfferId");
            string choice = Read<IReadOnlyList<string>>(offer, "Choices")[0];
            Assert.That(run.TryTransition(RunPhase.Abandoned), Is.True);
            Assert.That((bool)Invoke(upgrades, "TryConfirm", id, choice), Is.False);
            Assert.That((bool)Invoke(upgrades, "TrySkip", id), Is.False);
            yield return null;
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Abandoned));
            Assert.That(Read<IReadOnlyList<string>>(upgrades, "ConfirmedChoices"), Is.Empty);
            Assert.That(playingTransitions, Is.Zero);
        }

        private void RegisterPickup<T>() where T : Component, IPoolLifecycle
        {
            pools.Register("experience-test", PoolFamily.Pickup, () =>
            {
                var instance = new GameObject("Experience pickup test");
                instance.SetActive(false);
                return instance.AddComponent<T>();
            }, 1);
        }

        private Component AcquirePickup(int amount)
        {
            var pickup = (Component)typeof(PoolService).GetMethod(nameof(PoolService.Acquire))
                .MakeGenericMethod(pickupType).Invoke(pools, new object[] { "experience-test" });
            Assert.That(pickup, Is.Not.Null);
            pickup.transform.position = new Vector3(100, 100, 0);
            Invoke(pickup, "Initialize", pools, upgrades, amount);
            var collider = pickup.GetComponent<Collider2D>();
            Assert.That(collider, Is.Not.Null, "Production pickup must have a trigger collider.");
            Assert.That(collider.isTrigger, Is.True);
            return pickup;
        }

        private IEnumerator CollideWithPlayer(Component pickup)
        {
            pickup.transform.position = player.transform.position;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return null;
            yield return new WaitForFixedUpdate();
        }

        private static T Read<T>(object instance, string name)
        {
            Assert.That(instance, Is.Not.Null);
            var property = instance.GetType().GetProperty(name);
            Assert.That(property, Is.Not.Null, "Missing progression property: " + name);
            return (T)property.GetValue(instance);
        }

        private static object Invoke(object instance, string name, params object[] values)
        {
            var types = new Type[values.Length];
            for (int i = 0; i < values.Length; i++) types[i] = values[i].GetType();
            var method = instance.GetType().GetMethod(name, types);
            Assert.That(method, Is.Not.Null, "Missing progression operation: " + name);
            return method.Invoke(instance, values);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (run != null && onPhaseChanged != null) run.Changed -= onPhaseChanged;
            (upgrades as IDisposable)?.Dispose();
            pools?.Dispose();
            if (player != null)
            {
                if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
                yield return null;
                var loaded = SceneManager.GetActiveScene();
                var empty = SceneManager.CreateScene("After upgrade test " + Guid.NewGuid().ToString("N"));
                SceneManager.SetActiveScene(empty);
                if (loaded.IsValid() && loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
            }
            yield return null;
        }
    }
}
