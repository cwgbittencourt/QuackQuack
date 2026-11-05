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
        private MenuRegressionInput menuInput;
        private static readonly string[] CancelTargets = { "Choose 0", "Banish 0", "Reroll", "Skip" };
        private static readonly int[] CardCounts = { 1, 3 };
        private static readonly Vector2Int[] PreviewSizes = { new Vector2Int(1920, 1080), new Vector2Int(2160, 1080), new Vector2Int(2340, 1080), new Vector2Int(1440, 1080) };
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

        private static float PreviewRadius(object card, string name)
        {
            var property = card.GetType().GetProperty(name);
            Assert.That(property, Is.Not.Null, "T022: numeric area preview is missing.");
            return (float)property.GetValue(card);
        }

        [TestCase(0, 0.2f, 0.232f)] [TestCase(4, 0.328f, 0.36f)]
        public void SoapCardPreviewsActualCurrentAndNextRadiusWithoutMutatingBuild(int current, float before, float after)
        {
            var build = progression.Upgrades.Build;
            Assert.That(build.TryAddWeapon("bolhas"), Is.True);
            if (current > 0)
            {
                Assert.That(build.TryAddPassive("sabao-magico"), Is.True);
                for (int i = 1; i < current; i++) Assert.That(build.TryUpgradePassive("sabao-magico", 5), Is.True);
            }
            int history = build.History.Count;
            Open("sabao-magico");
            var card = view.Displayed.Cards[0];
            Assert.That(PreviewRadius(card, "CurrentRadius"), Is.EqualTo(before).Within(0.00001f));
            Assert.That(PreviewRadius(card, "NextRadius"), Is.EqualTo(after).Within(0.00001f));
            StringAssert.Contains("raio", card.Next); StringAssert.DoesNotContain("área", card.Next);
            Assert.That(build.History.Count, Is.EqualTo(history));
            Assert.That(progression.Bridge.Snapshot(), Is.SameAs(view.Displayed));
            var images = view.GetComponentsInChildren<Image>().Where(i => i.name == "Current radius" || i.name == "Next radius").ToArray();
            Assert.That(images.Length, Is.EqualTo(2), "The generated prefab must contain the preview.");
            Assert.That(images.Single(i => i.name == "Current radius").rectTransform.sizeDelta.x /
                images.Single(i => i.name == "Next radius").rectTransform.sizeDelta.x, Is.EqualTo(before / after).Within(0.0001f));
            Button("Choose 0").onClick.Invoke();
            var bubble = progression.Catalog.Weapons.Single(w => w.Id == "bolhas");
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(progression.Upgrades.ResolveWeaponAttributes(bubble, 1, player.State.Terrain, player.Relics).HitRadius,
                Is.EqualTo(after).Within(0.00001f));
        }

        [Test]
        public void SoapPreviewWithoutBubbleWeaponIsExplicitlyProspective()
        {
            Open("sabao-magico");
            var card = view.Displayed.Cards[0];
            Assert.That(PreviewRadius(card, "NextRadius"), Is.EqualTo(0.232f).Within(0.00001f));
            StringAssert.Contains("Ao adquirir Bolhas", card.Synergy);
            Assert.That(progression.Upgrades.Build.Weapons.ContainsKey("bolhas"), Is.False);
        }

        [Test]
        public void RelicChangesInvalidatePreviewWithoutChangingOfferOrBuild()
        {
            progression.Upgrades.Build.TryAddWeapon("bolhas");
            Open("sabao-magico"); var original = view.Displayed;
            var modifiers = new QuackQuack.Core.Progression.RelicModifier[10];
            for (int i = 0; i < 10; i++) modifiers[i] = new QuackQuack.Core.Progression.RelicModifier("area" + i,
                QuackQuack.Core.Progression.RelicAttribute.Area, QuackQuack.Core.Progression.RelicTerrain.Any, 0.1f);
            var relics = new QuackQuack.Core.Progression.RelicLoadout(modifiers);
            var player = Object.FindAnyObjectByType<PlayerController>(); player.Relics = relics;
            view.Refresh(); var before = view.Displayed;
            relics.Unlock("area0"); relics.Equip("area0"); view.Refresh();
            Assert.That(view.Displayed, Is.Not.SameAs(before));
            Assert.That(view.Displayed.OfferId, Is.EqualTo(original.OfferId));
            Assert.That(PreviewRadius(view.Displayed.Cards[0], "CurrentRadius"), Is.EqualTo(0.22f).Within(0.00001f));
            Assert.That(PreviewRadius(view.Displayed.Cards[0], "NextRadius"), Is.EqualTo(0.2552f).Within(0.00001f));
        }

        [Test]
        public void CardsCommunicateAggregatedActivePassiveEffects()
        {
            progression.Upgrades.Build.TryAddPassive("ninho-reforcado");
            progression.Upgrades.Build.TryAddPassive("oculos-aviador");
            Open("pulmao-poderoso");
            var property = view.Displayed.Cards[0].GetType().GetProperty("ActiveEffects");
            Assert.That(property, Is.Not.Null, "T022: active passive effects are missing.");
            StringAssert.Contains("Dano +16%", (string)property.GetValue(view.Displayed.Cards[0]));
        }

        [UnityTest]
        public IEnumerator DensePassiveCardsAndPreviewFitAndRender([ValueSource(nameof(PreviewSizes))] Vector2Int size)
        {
            var build = progression.Upgrades.Build; build.TryAddWeapon("bolhas");
            foreach (var passive in progression.Catalog.Passives)
            {
                Assert.That(build.TryAddPassive(passive.Id), Is.True);
                for (int i = 1; i < 4; i++) Assert.That(build.TryUpgradePassive(passive.Id, passive.MaxLevel), Is.True);
            }
            Open("sabao-magico", "pulmao-poderoso", "ovos");
            var canvas = view.GetComponent<Canvas>();
            var mode = canvas.renderMode; var previousCamera = canvas.worldCamera;
            var cameraObject = new GameObject("T022 preview camera"); var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false; camera.orthographic = true; camera.transform.position = new Vector3(0, 0, -20);
            var target = new RenderTexture(size.x, size.y, 24); target.Create(); camera.targetTexture = target;
            camera.aspect = (float)size.x / size.y;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10;
            var areas = view.GetComponentsInChildren<SafeAreaController>(true);
            foreach (var area in areas) area.SetViewportProvider(() => new Vector2(size.x, size.y), () => new Rect(0, 0, size.x, size.y));
            try
            {
                yield return null; yield return null; Canvas.ForceUpdateCanvases();
                foreach (var text in view.GetComponentsInChildren<Text>())
                    Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 0.1f), text.name + ": " + text.text);
                foreach (var preview in view.GetComponentsInChildren<UpgradeAreaPreview>())
                {
                    var card = preview.transform.parent.GetComponent<RectTransform>();
                    var previewRect = preview.GetComponent<RectTransform>();
                    var corners = new Vector3[4]; previewRect.GetWorldCorners(corners);
                    foreach (var corner in corners) Assert.That(card.rect.Contains(card.InverseTransformPoint(corner)), Is.True);
                }
                var previous = RenderTexture.active; var pixels = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                try
                {
                    camera.Render(); RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); pixels.Apply();
                    string folder = Application.isEditor ? System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Logs/Polimento002/T022")) :
                        System.IO.Path.Combine(Application.temporaryCachePath, "QuackQuack-T022");
                    System.IO.Directory.CreateDirectory(folder);
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, "preview-" + size.x + "x" + size.y + ".png"), pixels.EncodeToPNG());
                }
                finally { RenderTexture.active = previous; Object.Destroy(pixels); }
            }
            finally
            {
                foreach (var area in areas) area.SetViewportProvider(null, null);
                canvas.renderMode = mode; canvas.worldCamera = previousCamera;
                camera.targetTexture = null; target.Release(); Object.Destroy(target); Object.Destroy(cameraObject);
            }
        }

        [UnityTest]
        public IEnumerator ConfirmedCapacityPassiveReachesRealScenePlayerThroughSharedBuild()
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(player.Upgrades, Is.SameAs(progression.Upgrades));
            Assert.That(progression.Runner.Upgrades, Is.SameAs(progression.Upgrades));
            float baseline = player.Definition.Quack.Capacity;
            Open("pulmao-poderoso");
            Button("Choose 0").onClick.Invoke();
            yield return new WaitForFixedUpdate();
            Assert.That(player.State.Breath.Capacity, Is.EqualTo(baseline * 1.08f).Within(0.001f));
        }
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
#if UNITY_EDITOR
            var editorRouting = InputSystem.settings.editorInputBehaviorInPlayMode;
#endif
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
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
#if UNITY_EDITOR
                InputSystem.settings.editorInputBehaviorInPlayMode = editorRouting;
#endif
            }
        }

        private MenuRegressionInput Controller()
        {
            GameBootstrap.Instance.ExternalClock = true;
            return menuInput ?? (menuInput = new MenuRegressionInput());
        }
        private Transform Modal => Button("Choose 0").transform.parent.parent;

        [UnityTest]
        public IEnumerator CancelCannotImplicitlySkipOrChooseMandatoryOffer([ValueSource(nameof(CancelTargets))] string target)
        {
            var driver = Controller();
            Assert.That(progression.Upgrades.TryCollectExperience(10), Is.True); view.Refresh();
            Assert.That(view.IsVisible, Is.True); Assert.That(GameBootstrap.Instance.Run.Phase, Is.EqualTo(RunPhase.ChoosingUpgrade));
            string id = view.Displayed.OfferId; int history = progression.Upgrades.Build.History.Count, resolved = 0;
            int rerolls = progression.Upgrades.RerollsRemaining, banishes = progression.Upgrades.BanishRemaining;
            progression.Upgrades.OfferResolved += _ => resolved++;
            Button(target).Select(); yield return driver.Press(GamepadButton.East);
            Assert.That(view.IsVisible, Is.True, "Cancel cannot dismiss a mandatory offer as an implicit skip.");
            Assert.That(GameBootstrap.Instance.Run.Phase, Is.EqualTo(RunPhase.ChoosingUpgrade));
            Assert.That(progression.Upgrades.CurrentOffer.OfferId, Is.EqualTo(id)); Assert.That(progression.Upgrades.CurrentOffer.IsClosed, Is.False);
            Assert.That(progression.Upgrades.Build.History.Count, Is.EqualTo(history)); Assert.That(progression.Upgrades.ConfirmedChoices, Is.Empty);
            Assert.That(resolved, Is.Zero); Assert.That(progression.Upgrades.RerollsRemaining, Is.EqualTo(rerolls));
            Assert.That(progression.Upgrades.BanishRemaining, Is.EqualTo(banishes)); MenuRegressionInput.AssertUsableFocus(Modal);
        }

        [UnityTest]
        public IEnumerator CancelFromBanishReturnsToItsCardWithoutResolvingTheOffer()
        {
            var driver = Controller(); Open("bumerangue", "ovos", "pena-aerodinamica");
            string id = view.Displayed.OfferId; Button("Banish 0").Select();
            yield return driver.Press(GamepadButton.East);
            Assert.That(view.IsVisible, Is.True); Assert.That(view.Displayed.OfferId, Is.EqualTo(id));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(Button("Choose 0").gameObject));
        }

        [UnityTest]
        public IEnumerator DirectionalGraphReachesEveryAvailableCardAndResourceAction([ValueSource(nameof(CardCounts))] int count)
        {
            Controller(); Open(count == 1 ? new[] { "bumerangue" } : new[] { "bumerangue", "ovos", "pena-aerodinamica" });
            yield return null; MenuRegressionInput.AssertUsableFocus(Modal);
            MenuRegressionInput.AssertReachableActions(Modal, Button("Choose 0"));
        }

        [UnityTest]
        public IEnumerator ControllerRerollRepairsFocusAndInvalidatesOldOfferOnce()
        {
            var driver = Controller(); Open("bumerangue", "ovos", "pena-aerodinamica");
            string old = view.Displayed.OfferId; int remaining = view.Displayed.Rerolls;
            Button("Reroll").Select(); yield return driver.Press(GamepadButton.South);
            Assert.That(view.Displayed.OfferId, Is.Not.EqualTo(old)); Assert.That(view.Displayed.Rerolls, Is.EqualTo(remaining - 1));
            Assert.That(progression.Upgrades.TryConfirm(old, "bumerangue"), Is.False);
            MenuRegressionInput.AssertUsableFocus(Modal); MenuRegressionInput.AssertReachableActions(Modal, Button("Choose 0"));
        }

        [UnityTest]
        public IEnumerator ExhaustedRerollAndBanishAreSkippedByTheNavigationGraph()
        {
            var driver = Controller(); Open("bumerangue", "ovos", "pena-aerodinamica");
            int rerolls = progression.Upgrades.RerollsRemaining;
            for (int i = 0; i < rerolls; i++) { Button("Reroll").Select(); yield return driver.Press(GamepadButton.South); }
            Assert.That(Button("Reroll").IsInteractable(), Is.False);
            Button("Banish 0").Select(); yield return driver.Press(GamepadButton.South);
            Assert.That(view.IsVisible, Is.True); Assert.That(Button("Banish 0").IsInteractable(), Is.False);
            MenuRegressionInput.AssertUsableFocus(Modal); MenuRegressionInput.AssertReachableActions(Modal, Button("Choose 0"));
        }

        [UnityTest]
        public IEnumerator ControllerConfirmAppliesOneChoiceWithoutQuackLeakingAfterResume()
        {
            var driver = Controller(); Open("bumerangue", "ovos"); int history = progression.Upgrades.Build.History.Count, quacks = 0;
            GameBootstrap.Instance.Events.Combat += e => { if (e.Type == QuackQuack.Core.Combat.CombatEventType.QuackActivated) quacks++; };
            Button("Choose 0").Select(); yield return driver.Press(GamepadButton.South);
            Assert.That(view.IsVisible, Is.False); Assert.That(progression.Upgrades.ConfirmedChoices.Count, Is.EqualTo(1));
            Assert.That(progression.Upgrades.Build.History.Count, Is.EqualTo(history + 1)); Assert.That(quacks, Is.Zero);
            Assert.That(GameBootstrap.Instance.Run.Phase, Is.EqualTo(RunPhase.Playing));
        }

        [UnityTest]
        public IEnumerator ResolvingSelectionRestoresThePreviousAvailableUiFocus()
        {
            var driver = Controller();
            var opener = Object.FindAnyObjectByType<RelicLoadoutView>().GetComponentsInChildren<Button>(true).Single(b => b.name == "Open relics");
            opener.Select(); Open("bumerangue", "ovos");
            yield return driver.Press(GamepadButton.South);
            Assert.That(view.IsVisible, Is.False); Assert.That(opener.isActiveAndEnabled && opener.IsInteractable(), Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(opener.gameObject));
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            menuInput?.Dispose(); menuInput = null;
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
            yield return null;
            var loaded = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("After selection " + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            if (loaded.IsValid() && loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
        }
    }
}
