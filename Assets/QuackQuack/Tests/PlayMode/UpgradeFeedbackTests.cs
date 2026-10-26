using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Pickups;
using QuackQuack.Gameplay.Player;
using QuackQuack.Gameplay.Upgrades;
using QuackQuack.Presentation.VFX;
using QuackQuack.Systems.Pooling;
using QuackQuack.Systems.Run;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.PlayMode
{
    public class UpgradeFeedbackTests
    {
        private LakeBuildProgression progression;
        private UpgradeFeedbackBridge bridge;
        private GameBootstrap bootstrap;
        private int pickupsDuringRareCue;
        private readonly List<PresentationEvent> cues = new List<PresentationEvent>();
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            cues.Clear(); pickupsDuringRareCue = -1;
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            for (int i = 0; i < 120 && SceneManager.GetActiveScene().name != "Lake"; i++) yield return null;
            for (int i = 0; i < 120; i++)
            {
                progression = Object.FindAnyObjectByType<LakeBuildProgression>();
                bridge = Object.FindAnyObjectByType<UpgradeFeedbackBridge>();
                if (progression != null && progression.Ready && bridge != null && bridge.Ready) break;
                yield return null;
            }
            Assert.That(bridge, Is.Not.Null); Assert.That(bridge.Ready, Is.True);
            bootstrap = GameBootstrap.Instance; bootstrap.Events.Presentation += Capture;
        }
        private void Capture(PresentationEvent value)
        {
            if (value.Type == PresentationEventType.EvolutionObtained || value.Type == PresentationEventType.RareReward) cues.Add(value);
            if (value.Type == PresentationEventType.RareReward) pickupsDuringRareCue = bootstrap.Pools.GetActiveCount(PoolFamily.Pickup);
        }
        private UpgradeFeedbackController ActiveView() => Object.FindAnyObjectByType<UpgradeFeedbackController>();

        [UnityTest]
        public IEnumerator AcceptedEvolutionPublishesOneCueAndNeverReappliesBuildOnReplay()
        {
            var system = progression.Upgrades;
            for (int i = 1; i < 5; i++) system.Build.TryUpgradeWeapon("bumerangue");
            system.Build.TryAddPassive("pena-aerodinamica");
            system.GenerateOffer("evolution-test", new[] { "evolucao-bumerangue" });
            Assert.That(system.TryConfirm("evolution-test", "evolucao-bumerangue"), Is.True);
            Assert.That(system.TryConfirm("evolution-test", "evolucao-bumerangue"), Is.False);
            Assert.That(cues.Count, Is.EqualTo(1));
            Assert.That(cues[0].Type, Is.EqualTo(PresentationEventType.EvolutionObtained));
            Assert.That(bridge.ActiveCount, Is.EqualTo(1));
            var view = ActiveView();
            Assert.That(view.IsEvolution, Is.True); Assert.That(view.Label, Is.EqualTo("EVOLUÇÃO!"));
            Assert.That(view.ActiveClip.name, Is.EqualTo("Evolution")); Assert.That(view.ActiveClip.samples, Is.GreaterThan(0));
            int history = system.Build.History.Count;
            bootstrap.Events.Publish(cues[0]);
            Assert.That(bridge.ActiveCount, Is.EqualTo(1));
            Assert.That(system.Build.History.Count, Is.EqualTo(history));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RarePickupGrantsXpOnceAndWaitsUntilAllCardsResolveBeforePlaying()
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            player.GetComponent<Rigidbody2D>().position = new Vector2(-9, 2);
            yield return new WaitForFixedUpdate(); yield return null;
            Assert.That(progression.Upgrades.Experience, Is.EqualTo(40));
            Assert.That(cues.Count, Is.EqualTo(1)); Assert.That(cues[0].Type, Is.EqualTo(PresentationEventType.RareReward));
            Assert.That(pickupsDuringRareCue, Is.EqualTo(4), "Reward must be returned before notifying presentation consumers.");
            Assert.That(bridge.ActiveCount, Is.Zero); Assert.That(bridge.PendingCount, Is.EqualTo(1));
            yield return new WaitForFixedUpdate();
            Assert.That(progression.Upgrades.Experience, Is.EqualTo(40));
            Assert.That(cues.Count, Is.EqualTo(1));
            for (int i = 0; i < 10 && bootstrap.Run.Phase == RunPhase.ChoosingUpgrade; i++)
                progression.Upgrades.TrySkip(progression.Upgrades.CurrentOffer.OfferId);
            Assert.That(bootstrap.Run.Phase, Is.EqualTo(RunPhase.Playing));
            Assert.That(bridge.PendingCount, Is.Zero); Assert.That(bridge.ActiveCount, Is.EqualTo(1));
            Assert.That(ActiveView().Label, Is.EqualTo("RECOMPENSA RARA"));
            Assert.That(ActiveView().ActiveClip.name, Is.EqualTo("RareReward"));
        }

        [UnityTest]
        public IEnumerator PauseFreezesCueAndExpiryCleansLabelAudioAndGeometry()
        {
            bootstrap.Events.Publish(new PresentationEvent(1, 900, PresentationEventType.RareReward, EventPriority.Feedback, 2, Vector2.zero));
            var view = ActiveView(); float age = view.Age;
            bootstrap.Run.TryTransition(RunPhase.Paused);
            yield return new WaitForSeconds(0.2f);
            Assert.That(view.Age, Is.EqualTo(age));
            bootstrap.Run.TryTransition(RunPhase.Playing);
            yield return new WaitForSeconds(1.4f);
            Assert.That(bridge.ActiveCount, Is.Zero); Assert.That(view.IsPlaying, Is.False);
            Assert.That(view.ActiveClip, Is.Null); Assert.That(view.Label, Is.Empty);
            foreach (var line in view.GetComponentsInChildren<LineRenderer>(true)) Assert.That(line.positionCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator SaturationAndReenableDoNotDuplicateCuesOrChangeRewards()
        {
            int count = Object.FindObjectsByType<UpgradeFeedbackController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            int history = progression.Upgrades.Build.History.Count;
            for (int i = 0; i < 100; i++) bootstrap.Events.Publish(new PresentationEvent(1, 10000 + i, PresentationEventType.RareReward, EventPriority.Feedback, 2, Vector2.zero));
            Assert.That(bridge.ActiveCount, Is.EqualTo(4));
            Assert.That(Object.FindObjectsByType<UpgradeFeedbackController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(count));
            Assert.That(progression.Upgrades.Experience, Is.Zero); Assert.That(progression.Upgrades.Build.History.Count, Is.EqualTo(history));
            bridge.enabled = false; Assert.That(bridge.ActiveCount, Is.Zero);
            var cue = new PresentationEvent(1, 99999, PresentationEventType.RareReward, EventPriority.Feedback, 2, Vector2.zero);
            bootstrap.Events.Publish(cue); Assert.That(bridge.ActiveCount, Is.Zero);
            bridge.enabled = true; bootstrap.Events.Publish(cue); bootstrap.Events.Publish(cue);
            Assert.That(bridge.ActiveCount, Is.EqualTo(1));
            bootstrap.Run.TryTransition(RunPhase.Lost);
            Assert.That(bridge.ActiveCount, Is.Zero); Assert.That(bridge.PendingCount, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OrdinaryUpgradeSkipAndRejectedCommandsNeverPretendToBeEvolution()
        {
            progression.Upgrades.GenerateOffer("ordinary", new[] { "bumerangue" });
            Assert.That(progression.Upgrades.TryConfirm("ordinary", "unknown"), Is.False);
            Assert.That(progression.Upgrades.TryConfirm("ordinary", "bumerangue"), Is.True);
            progression.Upgrades.GenerateOffer("skip", new[] { "ovos" });
            progression.Upgrades.TrySkip("skip");
            Assert.That(cues, Is.Empty); Assert.That(bridge.ActiveCount, Is.Zero);
            bootstrap.Run.TryTransition(RunPhase.Abandoned);
            bootstrap.Events.Publish(new PresentationEvent(1, 777, PresentationEventType.RareReward, EventPriority.Feedback, 2, Vector2.zero));
            Assert.That(bridge.ActiveCount, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReusedRarePickupClearsRarityAndGetsANewCorrelationOnANewRareLease()
        {
            using (var pool = new PoolService())
            {
                ExperiencePickup.RegisterPool(pool, "rare-reset", 1);
                var player = Object.FindAnyObjectByType<PlayerController>();
                var first = ExperiencePickup.SpawnRare(pool, "rare-reset", progression.Upgrades, 1, player.transform.position, bootstrap.Events);
                Physics2D.SyncTransforms(); yield return new WaitForFixedUpdate(); yield return null;
                Assert.That(pool.ActiveCount, Is.Zero); Assert.That(cues.Count, Is.EqualTo(1));
                var second = ExperiencePickup.Spawn(pool, "rare-reset", progression.Upgrades, 1, player.transform.position);
                Assert.That(second, Is.SameAs(first));
                Physics2D.SyncTransforms(); yield return new WaitForFixedUpdate(); yield return null;
                Assert.That(cues.Count, Is.EqualTo(1));
                ExperiencePickup.SpawnRare(pool, "rare-reset", progression.Upgrades, 1, player.transform.position, bootstrap.Events);
                Physics2D.SyncTransforms(); yield return new WaitForFixedUpdate(); yield return null;
                Assert.That(cues.Count, Is.EqualTo(2)); Assert.That(cues[0].CorrelationId, Is.Not.EqualTo(cues[1].CorrelationId));
                Assert.That(progression.Upgrades.Experience, Is.EqualTo(3));
            }
        }

        [UnityTest]
        public IEnumerator QueuedEvolutionPrecedesRareCueWithoutOverlappingTheirLabels()
        {
            progression.Upgrades.GenerateOffer("queue", new[] { "bumerangue" });
            bootstrap.Events.Publish(new PresentationEvent(1, 1234, PresentationEventType.RareReward, EventPriority.Feedback, 2, Vector2.zero));
            bootstrap.Events.Publish(new PresentationEvent(1, 5678, PresentationEventType.EvolutionObtained, EventPriority.Essential, 2, Vector2.zero));
            Assert.That(bridge.PendingCount, Is.EqualTo(2));
            progression.Upgrades.TrySkip("queue");
            Assert.That(bridge.ActiveCount, Is.EqualTo(1)); Assert.That(bridge.PendingCount, Is.EqualTo(1));
            Assert.That(ActiveView().IsEvolution, Is.True);
            yield return new WaitForSeconds(1.4f);
            Assert.That(bridge.ActiveCount, Is.EqualTo(1)); Assert.That(bridge.PendingCount, Is.Zero);
            Assert.That(ActiveView().Label, Is.EqualTo("RECOMPENSA RARA"));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (bootstrap != null) bootstrap.Events.Presentation -= Capture;
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
            yield return null;
            var loaded = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("After upgrade feedback " + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            if (loaded.IsValid() && loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
        }
    }
}
