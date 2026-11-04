using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Run;
using QuackQuack.Core.Shared;
using QuackQuack.Gameplay.Player;
using QuackQuack.Platform.Input;
using QuackQuack.Systems.Run;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.PlayMode
{
    [Category("UnifiedInputRuntime")]
    public class UnifiedPlayerInputTests
    {
        private readonly List<Gamepad> disabledOriginalPads = new List<Gamepad>();
        private Keyboard keyboard, previousKeyboard;
        private Gamepad gamepad, previousGamepad;
        private Touchscreen touchscreen, previousTouchscreen;
        private UnifiedPlayerInput input;
        private bool sceneLoaded;
        private InputSettings.BackgroundBehavior background;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode editorInput;
#endif
        [SetUp] public void SetUp()
        {
            sceneLoaded = false; background = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            previousKeyboard = Keyboard.current; previousGamepad = Gamepad.current; previousTouchscreen = Touchscreen.current;
            disabledOriginalPads.Clear();
            foreach (var original in Gamepad.all) if (original.enabled) disabledOriginalPads.Add(original);
            foreach (var original in disabledOriginalPads) InputSystem.DisableDevice(original);
            keyboard = InputSystem.AddDevice<Keyboard>(); gamepad = InputSystem.AddDevice<Gamepad>();
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            input?.Dispose(); input = null;
            if (sceneLoaded)
            {
                if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
                yield return null;
                var scene = SceneManager.GetActiveScene(); SceneManager.SetActiveScene(SceneManager.CreateScene("After unified input"));
                if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            }
            yield return null;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (gamepad != null && gamepad.added) InputSystem.RemoveDevice(gamepad);
            if (touchscreen != null && touchscreen.added) InputSystem.RemoveDevice(touchscreen);
            foreach (var original in disabledOriginalPads) if (original.added) InputSystem.EnableDevice(original);
            if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
            if (previousGamepad != null && previousGamepad.added) previousGamepad.MakeCurrent();
            if (previousTouchscreen != null && previousTouchscreen.added) previousTouchscreen.MakeCurrent();
            InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
#endif
        }

        [Test] public void LatestActivitySelectsOneSourceWithoutAddingMovementOrReplayingEdges()
        {
            input = new UnifiedPlayerInput();
            Assert.That(PlayerInputConfiguration.Load().Actions.enabled, Is.False, "Runtime consumers must not enable the authored asset.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D)); InputSystem.Update();
            Assert.That(input.Read().Move, Is.EqualTo(Vector2.right));
            InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.left }); InputSystem.Update();
            var command = input.Read(); Assert.That(command.Source, Is.EqualTo(InputSource.Gamepad)); Assert.That(command.Move, Is.EqualTo(Vector2.left));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.Space)); InputSystem.Update();
            command = input.Read(); Assert.That(command.Source, Is.EqualTo(InputSource.Keyboard));
            Assert.That(command.Move, Is.EqualTo(Vector2.right)); Assert.That(command.QuackPressed, Is.True);
            Assert.That(input.Read().QuackPressed, Is.False);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
            Assert.That(input.Read().Move, Is.EqualTo(Vector2.zero), "An older held device must not resume on its own.");
        }

        [Test] public void MenusDiscardHeldButtonsAndRequireNeutralMovementBeforeReturning()
        {
            input = new UnifiedPlayerInput(); input.SetContext(false, false);
            Pad(new GamepadState { leftStick = Vector2.up }.WithButton(GamepadButton.South).WithButton(GamepadButton.Start));
            AssertNeutral(input.Read()); input.SetContext(true, false); InputSystem.Update();
            AssertNeutral(input.Read()); Pad(new GamepadState()); AssertNeutral(input.Read());
            Pad(new GamepadState { leftStick = Vector2.up }.WithButton(GamepadButton.South));
            var command = input.Read(); Assert.That(command.Move, Is.EqualTo(Vector2.up)); Assert.That(command.QuackPressed, Is.True);
        }

        [Test] public void PausedContextAllowsOnlyPauseAndSuspensionAllowsNothing()
        {
            input = new UnifiedPlayerInput(); input.SetContext(false, true);
            Pad(new GamepadState { leftStick = Vector2.up }.WithButton(GamepadButton.South).WithButton(GamepadButton.Start));
            var command = input.Read(); Assert.That(command.PausePressed, Is.True);
            Assert.That(command.QuackPressed, Is.False); Assert.That(command.Move, Is.EqualTo(Vector2.zero));
            input.SetSuspended(true); Pad(new GamepadState()); Pad(new GamepadState().WithButton(GamepadButton.Start)); AssertNeutral(input.Read());
            input.SetSuspended(false); InputSystem.Update(); AssertNeutral(input.Read());
        }

        [Test] public void TouchUsesAuthoredActionsWithoutBecomingAPhysicalGamepad()
        {
            if (gamepad.added) InputSystem.RemoveDevice(gamepad); // Touch-only scenario.
            touchscreen = InputSystem.AddDevice<Touchscreen>(); input = new UnifiedPlayerInput();
            Assert.That(input.Mobile, Is.Not.Null);
            input.Read(); input.Mobile.SetMove(new Vector2(0.3f, 0.4f)); input.Mobile.SetQuack(true);
            var command = input.Read(); Assert.That(command.Source, Is.EqualTo(InputSource.Touch));
            Assert.That(Vector2.Distance(command.Move, new Vector2(0.3f, 0.4f)), Is.LessThan(0.001)); Assert.That(command.QuackPressed, Is.True);
            using (var physical = new GamepadInputAdapter()) { InputSystem.Update(); AssertNeutral(physical.Read()); }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); InputSystem.Update();
            Assert.That(input.Read().Source, Is.EqualTo(InputSource.Keyboard));
        }

        [Test] public void FocusPolicyAllowsNewBackgroundInputButCannotOverrideApplicationSuspension()
        {
            input = new UnifiedPlayerInput(); input.SetApplicationPaused(true); input.SetFocused(true);
            Pad(new GamepadState { leftStick = Vector2.up }); AssertNeutral(input.Read());
            input.SetApplicationPaused(false); Pad(new GamepadState()); input.Read();
            input.SetFocused(false); Pad(new GamepadState()); input.Read();
            Pad(new GamepadState { leftStick = Vector2.up }.WithButton(GamepadButton.South));
            var command = input.Read(); Assert.That(command.Move, Is.EqualTo(Vector2.up)); Assert.That(command.QuackPressed, Is.True);
            input.SetApplicationPaused(true); input.SetFocused(true); AssertNeutral(input.Read());
        }

        [UnityTest] public IEnumerator DefaultSceneControllerUsesGamepadMovementQuackAndPause()
        {
            yield return LoadLake();
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(InputOf(player), Is.InstanceOf<UnifiedPlayerInput>());
            // Use an authored pause-menu action; unrelated background buttons are excluded by T013.
            var selected = Object.FindAnyObjectByType<QuackQuack.Presentation.UI.RelicLoadoutView>().OpenButton;
            int clicks = 0; selected.onClick.AddListener(() => clicks++);
            EventSystem.current.SetSelectedGameObject(selected.gameObject);
            int quacks = 0; GameBootstrap.Instance.Events.Combat += value => { if (value.Type == CombatEventType.QuackActivated) quacks++; };
            InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.up }.WithButton(GamepadButton.South));
            yield return null; yield return null; yield return new WaitForFixedUpdate();
            Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.y, Is.GreaterThan(0)); Assert.That(quacks, Is.EqualTo(1));
            Assert.That(clicks, Is.EqualTo(0), "Quack must not submit the UI control left selected during gameplay.");
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.Start));
            yield return null; yield return null;
            Assert.That(GameBootstrap.Instance.Run.Phase, Is.EqualTo(RunPhase.Paused));
            Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity, Is.EqualTo(Vector2.zero));
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); yield return null;
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South)); yield return null; yield return null;
            Assert.That(clicks, Is.EqualTo(1), "Submit must still work while the game is paused.");
            Assert.That(quacks, Is.EqualTo(1));
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); yield return null;
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.Start)); yield return null; yield return null;
            Assert.That(GameBootstrap.Instance.Run.Phase, Is.EqualTo(RunPhase.Playing)); Assert.That(quacks, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator DefaultSceneCreatesTouchControlsAndHidesThemDuringSelection()
        {
            InputSystem.RemoveDevice(gamepad); // Touch-only scenario.
            touchscreen = InputSystem.AddDevice<Touchscreen>(); yield return LoadLake();
            var player = Object.FindAnyObjectByType<PlayerController>(); var owned = (UnifiedPlayerInput)InputOf(player);
            Assert.That(owned.Mobile, Is.Not.Null);
            owned.Mobile.SetMove(Vector2.right); owned.Mobile.SetQuack(true);
            yield return null; yield return null; yield return new WaitForFixedUpdate();
            Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.x, Is.GreaterThan(0));
            Assert.That(player.State.Breath.Current, Is.LessThan(player.Definition.Quack.Capacity));
            GameBootstrap.Instance.Run.TryTransition(RunPhase.ChoosingUpgrade); yield return null;
            Assert.That(owned.Mobile.gameObject.activeSelf, Is.False);
            Assert.That(EventSystem.current, Is.Not.Null);
            Assert.That(EventSystem.current.isActiveAndEnabled, Is.True, "Modal UI must keep an active EventSystem when gameplay controls hide.");
            Assert.That(EventSystem.current.sendNavigationEvents, Is.True);
            owned.Mobile.SetQuack(true); GameBootstrap.Instance.Run.TryResume(); yield return null;
            Assert.That(owned.Mobile.gameObject.activeSelf, Is.True); AssertNeutral(owned.Read());
        }

        private IEnumerator LoadLake()
        {
            sceneLoaded = true; yield return SceneManager.LoadSceneAsync("Bootstrap");
            for (int i = 0; i < 120 && SceneManager.GetActiveScene().name != "Lake"; i++) yield return null;
            yield return null; yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Lake"));
        }
        private void Pad(GamepadState state) { InputSystem.QueueStateEvent(gamepad, state); InputSystem.Update(); }
        private static IPlayerInput InputOf(PlayerController player) => (IPlayerInput)typeof(PlayerController).GetField("input", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player);
        private static void AssertNeutral(PlayerCommand command)
        { Assert.That(command.Move, Is.EqualTo(Vector2.zero)); Assert.That(command.QuackPressed || command.PausePressed, Is.False); }
    }
}
