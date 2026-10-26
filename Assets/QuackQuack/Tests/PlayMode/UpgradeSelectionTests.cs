#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Player;
using QuackQuack.Gameplay.Upgrades;
using QuackQuack.Presentation.UI;
using QuackQuack.Systems.Run;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.PlayMode
{
    public class UpgradeSelectionTests
    {
        private LakeBuildProgression progression;
        private UpgradeSelectionView view;
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            for (int i = 0; i < 120 && SceneManager.GetActiveScene().name != "Lake"; i++) yield return null;
            progression = Object.FindAnyObjectByType<LakeBuildProgression>();
            for (int i = 0; i < 120 && (progression == null || !progression.Ready); i++)
            { yield return null; progression = Object.FindAnyObjectByType<LakeBuildProgression>(); }
            Assert.That(progression, Is.Not.Null); Assert.That(progression.Ready, Is.True);
            view = Object.FindAnyObjectByType<UpgradeSelectionView>();
            Assert.That(view.IsVisible, Is.False);
        }
        private Button Button(string name) => view.GetComponentsInChildren<Button>(true).Single(b => b.name == name);
        private void Open(params string[] ids)
        {
            progression.Upgrades.GenerateOffer("test", ids); view.Refresh();
            Assert.That(view.IsVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator PhysicalXpCollectionOpensCardsAndAButtonAppliesExactlyOneChoice()
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            player.GetComponent<Rigidbody2D>().position = new Vector2(-6, -4);
            yield return new WaitForFixedUpdate(); yield return null;
            Assert.That(view.IsVisible, Is.True);
            Assert.That(progression.Upgrades.Experience, Is.EqualTo(10));
            Assert.That(view.Displayed.Cards.Count, Is.EqualTo(3));
            Assert.That(GameBootstrap.Instance.Run.Phase, Is.EqualTo(RunPhase.ChoosingUpgrade));
            int history = progression.Upgrades.Build.History.Count;
            Button("Choose 0").onClick.Invoke(); Button("Choose 0").onClick.Invoke();
            Assert.That(view.IsVisible, Is.False);
            Assert.That(progression.Upgrades.Build.History.Count, Is.EqualTo(history + 1));
            Assert.That(progression.Upgrades.ConfirmedChoices.Count, Is.EqualTo(1));
            Assert.That(GameBootstrap.Instance.Run.Phase, Is.EqualTo(RunPhase.Playing));
        }

        [UnityTest]
        public IEnumerator CardsDescribeTypeCurrentNextSynergyAndRequirementsWithoutMutatingBuild()
        {
            Open("bumerangue", "pena-aerodinamica", "ovos");
            var snapshot = view.Displayed;
            Assert.That(progression.Bridge.Snapshot(), Is.SameAs(snapshot));
            var weapon = snapshot.Cards.Single(c => c.Id == "bumerangue");
            Assert.That(weapon.Kind, Is.EqualTo("MELHORIA DE ARMA"));
            StringAssert.Contains("N1", weapon.Current); StringAssert.Contains("N2", weapon.Next);
            StringAssert.Contains("Pena aerodinâmica", weapon.Synergy); StringAssert.Contains("N5", weapon.Requirement);
            var passive = snapshot.Cards.Single(c => c.Id == "pena-aerodinamica");
            StringAssert.Contains("PASSIVA", passive.Kind); StringAssert.Contains("8%", passive.Next);
            Assert.That(snapshot.Cards.Single(c => c.Id == "ovos").Kind, Is.EqualTo("NOVA ARMA"));
            Assert.That(progression.Upgrades.Build.Weapons.Count, Is.EqualTo(1));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(Button("Choose 0").gameObject));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RerollBanishSkipAndRebindingDoNotDuplicateCommandsOrResources()
        {
            Open("bumerangue", "pena-aerodinamica", "ovos");
            string original = view.Displayed.OfferId;
            Button("Reroll").onClick.Invoke(); Button("Reroll").onClick.Invoke();
            Assert.That(view.Displayed.OfferId, Is.Not.EqualTo(original));
            Assert.That(view.Displayed.Rerolls, Is.EqualTo(1));
            Assert.That(progression.Upgrades.TryConfirm(original, "ovos"), Is.False);
            yield return null;
            Button("Banish 0").onClick.Invoke();
            Assert.That(view.Displayed.Banishes, Is.Zero);
            Assert.That(view.Displayed.Cards.Count, Is.EqualTo(2));
            Assert.That(Button("Banish 0").interactable, Is.False);
            yield return null;
            Button("Skip").onClick.Invoke();
            Assert.That(view.IsVisible, Is.False);
            Assert.That(progression.Upgrades.ConfirmedChoices, Is.Empty);
            progression.Bridge.enabled = false; progression.Bridge.enabled = true;
            yield return null;
            progression.Upgrades.GenerateOffer("after-rebind", new[] { "bumerangue" }); view.Refresh();
            Button("Choose 0").onClick.Invoke();
            Assert.That(progression.Upgrades.ConfirmedChoices.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SingleCardHidesUnusedSlotsAndLastBanishClosesTheModal()
        {
            Open("pena-aerodinamica");
            Assert.That(view.Displayed.Cards.Count, Is.EqualTo(1));
            Assert.That(Button("Choose 1").gameObject.activeInHierarchy, Is.False);
            Button("Banish 0").onClick.Invoke();
            Assert.That(view.IsVisible, Is.False);
            Assert.That(GameBootstrap.Instance.Run.Phase, Is.EqualTo(RunPhase.Playing));
            yield return null;
        }

        [UnityTest]
        public IEnumerator EvolutionCardCommunicatesReplacementAndPreservesSlots()
        {
            var build = progression.Upgrades.Build;
            for (int i = 1; i < 5; i++) build.TryUpgradeWeapon("bumerangue");
            build.TryAddPassive("pena-aerodinamica");
            Open("evolucao-bumerangue");
            var card = view.Displayed.Cards[0];
            Assert.That(card.Kind, Is.EqualTo("EVOLUÇÃO"));
            StringAssert.Contains("Tornado", card.Title); StringAssert.Contains("mesmo slot", card.Requirement);
            Assert.That(card.Next, Is.Not.EqualTo(card.Current));
            Button("Choose 0").onClick.Invoke();
            Assert.That(build.Weapons.Count, Is.EqualTo(1));
            Assert.That(build.Weapons.ContainsKey("tornado-penas"), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator KeyboardSubmitSelectsFocusedCardAndTerminalStateHidesTheModal()
        {
            Open("bumerangue");
            var background = InputSystem.settings.backgroundBehavior;
            var editorRouting = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); InputSystem.Update();
                var module = EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Assert.That(module.submit.action.enabled, Is.True);
                Assert.That(keyboard.enterKey.isPressed, Is.True);
                module.Process();
                yield return null; yield return null;
                Assert.That(progression.Upgrades.ConfirmedChoices.Count, Is.EqualTo(1));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                progression.Upgrades.GenerateOffer("terminal", new[] { "ovos" }); view.Refresh();
                GameBootstrap.Instance.Run.TryTransition(RunPhase.Abandoned);
                yield return null;
                Assert.That(view.IsVisible, Is.False);
                Button("Choose 0").onClick.Invoke();
                Assert.That(progression.Upgrades.ConfirmedChoices.Count, Is.EqualTo(1));
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard); InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editorRouting;
            }
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
            yield return null;
            var loaded = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("After selection " + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            if (loaded.IsValid() && loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
        }
    }
}
#endif
