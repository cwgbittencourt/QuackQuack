using System;
using System.Collections;
using System.Collections.Generic;
using QuackQuack.Systems.Upgrades;
using QuackQuack.Gameplay.Pickups;
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

        private UpgradeSystem upgrades;
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
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            for (int i = 0; i < 120 && SceneManager.GetActiveScene().name != "Lake"; i++) yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Lake"));
            player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(player, Is.Not.Null);
            Assert.That(player.State, Is.Not.Null);
            run = GameBootstrap.Instance.Run;
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Playing));
            upgrades = new UpgradeSystem(12345);
            upgrades.InitializeProgression(run, new[] { 10, 20, 30 }, new[] { "a", "b", "c" });
            pools = new PoolService();
            ExperiencePickup.RegisterPool(pools, "experience-test", 1);
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
            Assert.That(upgrades.Experience, Is.EqualTo(4));
            Assert.That(upgrades.Level, Is.EqualTo(1));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Playing));
            Assert.That(pools.GetActiveCount(PoolFamily.Pickup), Is.Zero);
            Assert.That(pickup.gameObject.activeSelf, Is.False);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(upgrades.Experience, Is.EqualTo(4));
            Assert.That(selectionsStarted, Is.Zero);

            var reused = AcquirePickup(3);
            Assert.That(reused, Is.SameAs(pickup));
            yield return CollideWithPlayer(reused);
            Assert.That(upgrades.Experience, Is.EqualTo(7));
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
            UpgradeOffer offer = upgrades.CurrentOffer;
            string id = offer.OfferId;
            string choice = offer.Choices[0];
            Assert.That(upgrades.TryConfirm(id, choice), Is.True);
            Assert.That(upgrades.TryConfirm(id, choice), Is.False);
            Assert.That(upgrades.TrySkip(id), Is.False);
            yield return null;
            yield return new WaitForFixedUpdate();
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Playing));
            Assert.That(selectionsStarted, Is.EqualTo(1));
            Assert.That(playingTransitions, Is.EqualTo(1));
            CollectionAssert.AreEqual(new[] { choice }, upgrades.ConfirmedChoices);
            double resumed = run.ElapsedSeconds;
            yield return new WaitForSeconds(0.1f);
            Assert.That(run.ElapsedSeconds, Is.GreaterThan(resumed));
        }

        [UnityTest]
        public IEnumerator InvalidChoiceKeepsCombatFrozenUntilOneValidSkip()
        {
            yield return CollideWithPlayer(AcquirePickup(10));
            string id = upgrades.CurrentOffer.OfferId;
            Assert.That(upgrades.TryConfirm(id, "not-offered"), Is.False);
            yield return new WaitForFixedUpdate();
            Assert.That(run.Phase, Is.EqualTo(RunPhase.ChoosingUpgrade));
            Assert.That(playingTransitions, Is.Zero);
            Assert.That(upgrades.TrySkip(id), Is.True);
            Assert.That(upgrades.TrySkip(id), Is.False);
            yield return null;
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Playing));
            Assert.That(playingTransitions, Is.EqualTo(1));
            Assert.That(upgrades.ConfirmedChoices, Is.Empty);
            Assert.That(upgrades.RerollsRemaining, Is.EqualTo(2));
            Assert.That(upgrades.BanishRemaining, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator MultipleLevelsQueueDistinctSelectionsWithoutResumingBetweenThem()
        {
            yield return CollideWithPlayer(AcquirePickup(20));
            Assert.That(upgrades.Level, Is.EqualTo(3));
            Assert.That(upgrades.Experience, Is.EqualTo(20));
            UpgradeOffer first = upgrades.CurrentOffer;
            string firstId = first.OfferId;
            Assert.That(upgrades.TrySkip(firstId), Is.True);
            yield return null;
            UpgradeOffer second = upgrades.CurrentOffer;
            string secondId = second.OfferId;
            Assert.That(secondId, Is.Not.EqualTo(firstId));
            Assert.That(second.IsClosed, Is.False);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.ChoosingUpgrade));
            Assert.That(playingTransitions, Is.Zero);
            Assert.That(upgrades.TrySkip(firstId), Is.False);
            Assert.That(upgrades.TrySkip(secondId), Is.True);
            yield return null;
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Playing));
            Assert.That(playingTransitions, Is.EqualTo(1));
            Assert.That(pools.ActiveCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator CallbackAfterAbandonCannotGrantUpgradeOrResumeRun()
        {
            yield return CollideWithPlayer(AcquirePickup(10));
            UpgradeOffer offer = upgrades.CurrentOffer;
            string id = offer.OfferId;
            string choice = offer.Choices[0];
            Assert.That(run.TryTransition(RunPhase.Abandoned), Is.True);
            Assert.That(upgrades.TryConfirm(id, choice), Is.False);
            Assert.That(upgrades.TrySkip(id), Is.False);
            yield return null;
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Abandoned));
            Assert.That(upgrades.ConfirmedChoices, Is.Empty);
            Assert.That(playingTransitions, Is.Zero);
        }

        [UnityTest]
        public IEnumerator PausedOverlapWaitsForResumeBeforeCollecting()
        {
            run.TryTransition(RunPhase.Paused);
            var pickup = AcquirePickup(4);
            yield return CollideWithPlayer(pickup);
            Assert.That(upgrades.Experience, Is.Zero);
            Assert.That(pools.ActiveCount, Is.EqualTo(1));
            run.TryTransition(RunPhase.Playing);
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(upgrades.Experience, Is.EqualTo(4));
            Assert.That(pools.ActiveCount, Is.Zero);
        }

        [Test]
        public void PoolSaturationDoesNotCreateOverflowAndFailedSpawnReturnsItsLease()
        {
            var first = AcquirePickup(4);
            Assert.That(ExperiencePickup.Spawn(pools, "experience-test", upgrades, 4, Vector3.zero), Is.Null);
            Assert.That(pools.ActiveCount, Is.EqualTo(1));
            pools.Release(first);
            Assert.Throws<ArgumentOutOfRangeException>(() => ExperiencePickup.Spawn(pools, "experience-test", upgrades, 0, Vector3.zero));
            Assert.That(pools.ActiveCount, Is.Zero);
            Assert.That(AcquirePickup(4), Is.SameAs(first));
        }

        [Test]
        public void UnownedObjectCannotBeInitializedEvenIfLifecycleIsCalledManually()
        {
            var rogue = new GameObject("Unowned XP test");
            try
            {
                var pickup = rogue.AddComponent<ExperiencePickup>();
                pickup.OnAcquire();
                Assert.Throws<InvalidOperationException>(() => pickup.Initialize(pools, upgrades, 4));
                Assert.That(upgrades.Experience, Is.Zero);
                Assert.That(pools.ActiveCount, Is.Zero);
            }
            finally { Object.DestroyImmediate(rogue); }
        }

        private ExperiencePickup AcquirePickup(int amount) =>
            ExperiencePickup.Spawn(pools, "experience-test", upgrades, amount, new Vector3(100, 100, 0));

        private IEnumerator CollideWithPlayer(Component pickup)
        {
            pickup.transform.position = player.transform.position;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return null;
            yield return new WaitForFixedUpdate();
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
