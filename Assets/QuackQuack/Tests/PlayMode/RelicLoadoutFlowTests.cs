using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Core.Shared;
using QuackQuack.Gameplay.Player;
using QuackQuack.Gameplay.Upgrades;
using QuackQuack.Presentation.UI;
using QuackQuack.Systems.Run;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace QuackQuack.Tests.PlayMode
{
    public class RelicLoadoutFlowTests
    {
        private MenuRegressionInput menuInput;
        private static readonly bool[] PauseStates = { false, true };
        private LakeRelics relics;
        private RelicLoadoutView view;
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            for (int i=0;i<240;i++) { relics=Object.FindAnyObjectByType<LakeRelics>(); if(relics!=null && relics.Ready) break; yield return null; }
            Assert.That(relics, Is.Not.Null); Assert.That(relics.Ready, Is.True);
            view=Object.FindAnyObjectByType<RelicLoadoutView>();
        }
        [UnityTest]
        public IEnumerator RealButtonsPauseEquipRemoveAndResumeWithoutDuplicating()
        {
            var run=GameBootstrap.Instance.Run;
            view.transform.Find("UI Safe Area/Open relics").GetComponent<Button>().onClick.Invoke();
            Assert.That(run.Phase,Is.EqualTo(RunPhase.Paused)); Assert.That(view.IsVisible,Is.True);
            relics.Show(true); // Reopening an already open panel must retain pause ownership.
            var toggle=view.transform.Find("UI Safe Area/Panel/Reference content/Relic 2/Toggle").GetComponent<Button>();
            toggle.onClick.Invoke(); Assert.That(relics.Loadout.EquippedIds.Count,Is.EqualTo(1));
            Assert.That(relics.Loadout.ApplyEffects(100),Is.EqualTo(105).Within(0.001));
            toggle.onClick.Invoke(); Assert.That(relics.Loadout.EquippedIds,Is.Empty);
            Assert.That(relics.Loadout.ApplyEffects(100),Is.EqualTo(100));
            view.transform.Find("UI Safe Area/Panel/Reference content/Close").GetComponent<Button>().onClick.Invoke();
            Assert.That(run.Phase,Is.EqualTo(RunPhase.Playing)); Assert.That(view.IsVisible,Is.False);
            Assert.That(relics.Toggle("pena-de-combate"),Is.False);
            yield return null;
        }
        [UnityTest]
        public IEnumerator UnlockProgressAndThreeSlotLimitAreVisibleAndTerminalClosesPanel()
        {
            GameBootstrap.Instance.Run.Advance(60); yield return null;
            Assert.That(relics.Loadout.IsUnlocked("apito-veloz"),Is.True);
            relics.Show(true);
            Assert.That(relics.Toggle("botas-do-lago"),Is.True); Assert.That(relics.Toggle("garras-de-junco"),Is.True);
            Assert.That(relics.Toggle("pena-de-combate"),Is.True); Assert.That(relics.Toggle("apito-veloz"),Is.False);
            Assert.That(relics.Loadout.EquippedIds.Count,Is.EqualTo(3));
            GameBootstrap.Instance.Run.TryTransition(RunPhase.Abandoned);
            Assert.That(view.IsVisible,Is.False); Assert.That(relics.Toggle("pena-de-combate"),Is.False);
        }
        [UnityTest]
        public IEnumerator MovementEffectUsesRealPlayerAndReturnsToBaselineAfterRemoval()
        {
            var player=Object.FindAnyObjectByType<PlayerController>();
            var command=new PlayerCommand(Vector2.right,false,false,InputSource.Keyboard);
            player.ApplyCommand(command); player.SendMessage("FixedUpdate");
            float baseline=player.GetComponent<Rigidbody2D>().linearVelocity.magnitude;
            Assert.That(baseline,Is.GreaterThan(0));
            relics.Show(true); relics.Toggle("garras-de-junco"); relics.Show(false);
            player.ApplyCommand(command); player.SendMessage("FixedUpdate");
            Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.magnitude,Is.EqualTo(baseline*1.05f).Within(0.001));
            relics.Show(true); relics.Toggle("garras-de-junco"); relics.Show(false);
            player.ApplyCommand(command); player.SendMessage("FixedUpdate");
            Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.magnitude,Is.EqualTo(baseline).Within(0.001));
            yield return null;
        }
        private MenuRegressionInput Controller()
        {
            GameBootstrap.Instance.ExternalClock = true;
            return menuInput ?? (menuInput = new MenuRegressionInput());
        }
        private Button Control(string name) => view.GetComponentsInChildren<Button>(true).Single(b => b.name == name);
        private Transform Panel => view.transform.Find("UI Safe Area/Panel");

        [UnityTest]
        public IEnumerator OpeningRelicsMovesFocusIntoTheModal()
        {
            Controller(); Control("Open relics").Select(); relics.Show(true); yield return null;
            MenuRegressionInput.AssertUsableFocus(Panel);
        }

        [UnityTest]
        public IEnumerator CancelClosesRelicsAndRestoresItsPauseOwner([ValueSource(nameof(PauseStates))] bool alreadyPaused)
        {
            var driver = Controller(); var run = GameBootstrap.Instance.Run;
            if (alreadyPaused) Assert.That(run.TryTransition(RunPhase.Paused), Is.True);
            Control("Open relics").Select(); relics.Show(true); Control("Close").Select();
            yield return driver.Press(GamepadButton.East);
            Assert.That(view.IsVisible, Is.False, "East/Cancel must close relics.");
            Assert.That(run.Phase, Is.EqualTo(alreadyPaused ? RunPhase.Paused : RunPhase.Playing));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(Control("Open relics").gameObject));
        }

        [UnityTest]
        public IEnumerator CloseButtonRestoresThePreviousFocus([ValueSource(nameof(PauseStates))] bool alreadyPaused)
        {
            Controller(); if (alreadyPaused) GameBootstrap.Instance.Run.TryTransition(RunPhase.Paused);
            var opener = Control("Open relics"); opener.Select(); relics.Show(true); Control("Close").Select();
            Control("Close").onClick.Invoke(); yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(opener.gameObject));
        }

        [UnityTest]
        public IEnumerator ClosingOnlyResumesPauseOwnedByRelics([ValueSource(nameof(PauseStates))] bool alreadyPaused)
        {
            Controller(); var run = GameBootstrap.Instance.Run;
            if (alreadyPaused) run.TryTransition(RunPhase.Paused);
            relics.Show(true); relics.Show(true); yield return null;
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Paused));
            Control("Close").onClick.Invoke(); yield return null;
            Assert.That(view.IsVisible, Is.False);
            Assert.That(run.Phase, Is.EqualTo(alreadyPaused ? RunPhase.Paused : RunPhase.Playing));
        }

        [UnityTest]
        public IEnumerator RelicsShortcutTogglesOncePerPressWithoutChangingPauseOwnership([ValueSource(nameof(PauseStates))] bool alreadyPaused)
        {
            var driver = Controller(); var run = GameBootstrap.Instance.Run;
            if (alreadyPaused) run.TryTransition(RunPhase.Paused);
            yield return driver.Press(GamepadButton.Select);
            Assert.That(view.IsVisible, Is.True, "Select/View must open relics without a pointer.");
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Paused));
            yield return driver.Press(GamepadButton.Select);
            Assert.That(view.IsVisible, Is.False);
            Assert.That(run.Phase, Is.EqualTo(alreadyPaused ? RunPhase.Paused : RunPhase.Playing));
        }

        [UnityTest]
        public IEnumerator TerminalContextIgnoresRelicsShortcut()
        {
            var driver = Controller(); var run = GameBootstrap.Instance.Run;
            Assert.That(run.TryTransition(RunPhase.Abandoned), Is.True);
            yield return driver.Press(GamepadButton.Select);
            Assert.That(view.IsVisible, Is.False); Assert.That(run.Phase, Is.EqualTo(RunPhase.Abandoned));
        }

        [UnityTest]
        public IEnumerator SlotLimitMovesFocusOffARelicThatBecomesUnavailable()
        {
            Controller(); GameBootstrap.Instance.Run.Advance(60); relics.RefreshUnlocks(); relics.Show(true);
            Assert.That(relics.Toggle("botas-do-lago"), Is.True); Assert.That(relics.Toggle("garras-de-junco"), Is.True); view.Refresh();
            var apito = view.transform.Find("UI Safe Area/Panel/Reference content/Relic 5/Toggle").GetComponent<Button>();
            Assert.That(apito.IsInteractable(), Is.True); apito.Select();
            Assert.That(relics.Toggle("pena-de-combate"), Is.True); view.Refresh(); yield return null;
            Assert.That(relics.Loadout.EquippedIds.Count, Is.EqualTo(3)); Assert.That(apito.IsInteractable(), Is.False);
            MenuRegressionInput.AssertUsableFocus(Panel);
        }

        [UnityTest]
        public IEnumerator SouthSubmitsOneRelicToggleWithoutQuackingBehindTheModal()
        {
            var driver = Controller(); int quacks = 0;
            GameBootstrap.Instance.Events.Combat += e => { if (e.Type == QuackQuack.Core.Combat.CombatEventType.QuackActivated) quacks++; };
            relics.Show(true);
            view.transform.Find("UI Safe Area/Panel/Reference content/Relic 2/Toggle").GetComponent<Button>().Select();
            yield return driver.Press(GamepadButton.South);
            Assert.That(relics.Loadout.EquippedIds.Count, Is.EqualTo(1)); Assert.That(quacks, Is.Zero);
            yield return driver.Press(GamepadButton.South);
            Assert.That(relics.Loadout.EquippedIds, Is.Empty); Assert.That(quacks, Is.Zero);
            Assert.That(GameBootstrap.Instance.Run.Phase, Is.EqualTo(RunPhase.Paused));
        }

        [UnityTest]
        public IEnumerator RelicDirectionalGraphReachesUnlockedActionsAndCloseOnly()
        {
            Controller(); relics.Show(true); yield return null;
            var first = Panel.GetComponentsInChildren<Button>().First(b => b.name == "Toggle" && b.IsInteractable());
            MenuRegressionInput.AssertReachableActions(Panel, first);
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            menuInput?.Dispose(); menuInput = null;
            if(GameBootstrap.Instance!=null) Object.Destroy(GameBootstrap.Instance.gameObject);
            yield return null;
            var loaded=SceneManager.GetActiveScene();
            var empty=SceneManager.CreateScene("After relics "+System.Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            if(loaded.IsValid() && loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
        }
    }

    internal sealed class MenuRegressionInput : IDisposable
    {
        private readonly List<InputDevice> disabled = new List<InputDevice>();
        private readonly Gamepad pad, previousPad;
        private readonly Keyboard previousKeyboard;
        private readonly InputSettings.BackgroundBehavior background;
#if UNITY_EDITOR
        private readonly InputSettings.EditorInputBehaviorInPlayMode routing;
#endif
        public MenuRegressionInput()
        {
            background = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            routing = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            previousPad = Gamepad.current; previousKeyboard = Keyboard.current;
            foreach (var d in InputSystem.devices) if (d.enabled && (d is Gamepad || d is Keyboard)) disabled.Add(d);
            foreach (var d in disabled) InputSystem.DisableDevice(d);
            pad = InputSystem.AddDevice<Gamepad>();
        }
        public IEnumerator Press(GamepadButton button)
        {
            // Real InputSystemUIInputModule dispatch, not Button.onClick or a fake Cancel handler.
            InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(button));
            yield return null; yield return null; // Hold for two frames: one edge must remain one action.
            InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null;
        }
        public void Dispose()
        {
            if (pad.added) InputSystem.RemoveDevice(pad);
            foreach (var d in disabled) if (d.added) InputSystem.EnableDevice(d);
            if (previousPad != null && previousPad.added) previousPad.MakeCurrent();
            if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
            InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = routing;
#endif
        }
        public static void AssertUsableFocus(Transform modal)
        {
            Assert.That(EventSystem.current, Is.Not.Null);
            var selected = EventSystem.current.currentSelectedGameObject;
            Assert.That(selected, Is.Not.Null, "Menu must acquire/repair focus.");
            Assert.That(selected.transform.IsChildOf(modal), Is.True, "Focus escaped the modal.");
            var control = selected.GetComponent<Selectable>();
            Assert.That(control != null && control.isActiveAndEnabled && control.IsInteractable(), Is.True, "Focus points to an unavailable control.");
            bool cue = control.transition == Selectable.Transition.ColorTint ? control.colors.selectedColor != control.colors.normalColor :
                control.transition == Selectable.Transition.SpriteSwap ? control.spriteState.selectedSprite != null :
                control.transition == Selectable.Transition.Animation && !string.IsNullOrEmpty(control.animationTriggers.selectedTrigger);
            Assert.That(cue, Is.True, "Focused action needs a visual selection cue.");
            var outline = control.GetComponent<Outline>(); if (outline != null) Assert.That(outline.enabled, Is.True, "Selected control must show its focus outline.");
        }
        public static void AssertReachableActions(Transform modal, Selectable first)
        {
            Canvas.ForceUpdateCanvases();
            var expected = modal.GetComponentsInChildren<Button>().Where(b => b.isActiveAndEnabled && b.IsInteractable()).ToArray();
            var visited = new HashSet<Selectable>(); var queue = new Queue<Selectable>(); queue.Enqueue(first);
            while (queue.Count > 0) {
                var current = queue.Dequeue(); if (!visited.Add(current)) continue;
                foreach (var next in new[] { current.FindSelectableOnUp(), current.FindSelectableOnDown(), current.FindSelectableOnLeft(), current.FindSelectableOnRight() }) {
                    if (next == null) continue;
                    Assert.That(next.transform.IsChildOf(modal) && next.isActiveAndEnabled && next.IsInteractable(), Is.True,
                        "Directional navigation reached a hidden/disabled/background action from " + current.name);
                    if (!visited.Contains(next)) queue.Enqueue(next);
                }
            }
            foreach (var button in expected) Assert.That(visited.Contains(button), Is.True, "Unreachable menu action: " + button.name);
        }
    }}
