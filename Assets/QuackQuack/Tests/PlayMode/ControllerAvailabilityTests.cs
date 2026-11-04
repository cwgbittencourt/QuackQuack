using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using QuackQuack.Core.Shared;
using QuackQuack.Platform.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.PlayMode
{
    // FR-013: availability is independent of last activity. Runtime APIs only;
    // a synthetic touchscreen exercises mobile composition on desktop test runners too.
    [Category("ControllerAvailabilityRuntime")]
    public sealed class ControllerAvailabilityTests
    {
        private readonly List<InputDevice> created = new List<InputDevice>();
        private readonly List<InputDevice> temporarilyDisabled = new List<InputDevice>();
        private readonly List<InputDeviceChange> changes = new List<InputDeviceChange>();
        private Gamepad previousPad;
        private Keyboard previousKeyboard, keyboard;
        private Touchscreen previousTouch;
        private UnifiedPlayerInput input;
        private GameObject ui;
        private EventSystem events;
        private InputSettings.BackgroundBehavior background;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode editorInput;
#endif
        [SetUp]
        public void SetUp()
        {
            created.Clear(); temporarilyDisabled.Clear(); changes.Clear();
            background = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            previousPad = Gamepad.current; previousKeyboard = Keyboard.current; previousTouch = Touchscreen.current;
            // Isolate the runner's InputSystem, not OS devices or another game process.
            foreach (var device in InputSystem.devices)
                if ((device is Gamepad || device is Keyboard || device is Touchscreen) && device.enabled)
                    temporarilyDisabled.Add(device);
            foreach (var device in temporarilyDisabled) InputSystem.DisableDevice(device);
            keyboard = InputSystem.AddDevice<Keyboard>(); created.Add(keyboard);
            created.Add(InputSystem.AddDevice<Touchscreen>());
            ui = new GameObject("T008 menu event system", typeof(EventSystem)); events = ui.GetComponent<EventSystem>();
            InputSystem.onDeviceChange += DeviceChanged;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            InputSystem.onDeviceChange -= DeviceChanged;
            input?.Dispose(); input = null;
            if (ui != null) Object.Destroy(ui);
            yield return null; // Complete disposal of cloned actions/mobile devices before restoring originals.
            foreach (var device in created) if (device.added) InputSystem.RemoveDevice(device);
            foreach (var device in temporarilyDisabled) if (device.added) InputSystem.EnableDevice(device);
            if (previousPad != null && previousPad.added) previousPad.MakeCurrent();
            if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
            if (previousTouch != null && previousTouch.added) previousTouch.MakeCurrent();
            InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
#endif
        }

        private void DeviceChanged(InputDevice device, InputDeviceChange change)
        { if (device is Gamepad) changes.Add(change); }
        private Gamepad AddPad()
        { var pad = InputSystem.AddDevice<Gamepad>(); created.Add(pad); return pad; }
        private void Compose()
        {
            input = new UnifiedPlayerInput();
            Assert.That(input.Mobile, Is.Not.Null, "The synthetic touchscreen must compose the real mobile prefab.");
            Pump();
        }
        private PlayerCommand Pump() { InputSystem.Update(); return input.Read(); }
        private void Pad(Gamepad pad, GamepadState state) { InputSystem.QueueStateEvent(pad, state); InputSystem.Update(); }
        private static void Neutral(PlayerCommand command)
        {
            Assert.That(command.Move, Is.EqualTo(Vector2.zero));
            Assert.That(command.QuackPressed || command.PausePressed, Is.False, "A lifecycle change must not replay a command edge.");
        }
        private GameObject Stick => input.Mobile.GetComponentInChildren<MobileJoystick>(true).gameObject;
        private GameObject Quack
        {
            get {
                foreach (var button in input.Mobile.GetComponentsInChildren<MobileActionButton>(true))
                    if (button.Action == MobileAction.Quack) return button.gameObject;
                throw new InvalidOperationException("Mobile prefab has no Quack control.");
            }
        }
        private static bool Visible(GameObject control)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var graphic in control.GetComponentsInChildren<Graphic>(true)) {
                if (!graphic.isActiveAndEnabled || graphic.color.a <= 0 || graphic.canvas == null || !graphic.canvas.isActiveAndEnabled) continue;
                if (graphic.canvasRenderer.GetInheritedAlpha() > 0.001f) return true;
            }
            return false;
        }
        private void TouchVisibility(bool expected)
        {
            Assert.That(Visible(Stick), Is.EqualTo(expected), "Joystick visibility must follow usable controller availability.");
            Assert.That(Visible(Quack), Is.EqualTo(expected), "Quack touch visibility must follow usable controller availability.");
        }
        private void Lose(Gamepad pad, string loss)
        {
            changes.Clear();
            if (loss == "Disabled") InputSystem.DisableDevice(pad);
            else if (loss == "Removed") InputSystem.RemoveDevice(pad);
            else {
                var remove = DeviceRemoveEvent.Create(pad.deviceId);
                InputSystem.QueueEvent(ref remove); InputSystem.Update();
            }
            Assert.That(changes, Does.Contain(loss == "Disabled" ? InputDeviceChange.Disabled : InputDeviceChange.Removed),
                "Verify the actual package lifecycle event before asserting product behavior.");
        }

        [Test]
        public void NoControllerShowsTouchAndAcceptsFreshCommands()
        {
            Compose(); TouchVisibility(true);
            input.Mobile.SetMove(Vector2.right); input.Mobile.SetQuack(true);
            var command = Pump();
            Assert.That(command.Source, Is.EqualTo(InputSource.Touch));
            Assert.That(command.Move, Is.EqualTo(Vector2.right)); Assert.That(command.QuackPressed, Is.True);
            Assert.That(input.Read().QuackPressed, Is.False);
        }

        [Test]
        public void ControllerPresentBeforeCompositionHidesTouchWithoutAnyActivity()
        { AddPad(); Compose(); TouchVisibility(false); }

        [Test]
        public void AddedIdleControllerHidesTouchDespiteOlderTouchSource()
        {
            Compose(); input.Mobile.SetMove(Vector2.left); Assert.That(Pump().Source, Is.EqualTo(InputSource.Touch));
            input.Mobile.SetMove(Vector2.zero); Pump();
            var pad = AddPad(); Pad(pad, new GamepadState());
            Assert.That(changes, Does.Contain(InputDeviceChange.Added));
            Assert.That(Pump().Source, Is.EqualTo(InputSource.Touch), "No controller activity was generated.");
            TouchVisibility(false);
        }

        [Test]
        public void KeyboardActivityDoesNotRevealTouchWhileControllerIsAvailable()
        {
            AddPad(); Compose();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            Assert.That(Pump().Source, Is.EqualTo(InputSource.Keyboard)); TouchVisibility(false);
        }

        [Test]
        public void DisabledControllerPresentBeforeCompositionDoesNotSuppressTouch()
        { var pad = AddPad(); InputSystem.DisableDevice(pad); Compose(); TouchVisibility(true); }

        [Test]
        public void EnablingAnIdleControllerHidesTouchWithoutRequiringAnAction()
        {
            var pad = AddPad(); InputSystem.DisableDevice(pad); Compose(); TouchVisibility(true);
            changes.Clear(); InputSystem.EnableDevice(pad); Pump();
            Assert.That(changes, Does.Contain(InputDeviceChange.Enabled)); TouchVisibility(false);
        }

        [TestCase("Removed")] [TestCase("Disabled")] [TestCase("RuntimeRemoveEvent")]
        public void LosingLastControllerRestoresTouchWithoutResumingOldHeldTouch(string loss)
        {
            Compose();
            // Leave an old touch gesture held when the controller takes over.
            input.Mobile.SetMove(Vector2.left); input.Mobile.SetQuack(true); input.Mobile.SetPause(true); Pump();
            var pad = AddPad();
            Pad(pad, new GamepadState { leftStick = Vector2.right }.WithButton(GamepadButton.South).WithButton(GamepadButton.Start));
            Assert.That(input.Read().Source, Is.EqualTo(InputSource.Gamepad));
            Lose(pad, loss); Neutral(Pump()); TouchVisibility(true);
            input.Mobile.SetQuack(false); input.Mobile.SetQuack(true);
            var fresh = Pump();
            Assert.That(fresh.QuackPressed, Is.True, "Fresh touch must work without restarting the input consumer.");
            Assert.That(fresh.Move, Is.EqualTo(Vector2.zero), "New Quack must not resurrect the old touch drag.");
            Assert.That(fresh.PausePressed, Is.False);
            input.Mobile.SetMove(Vector2.up); Assert.That(Pump().Move, Is.EqualTo(Vector2.up));
        }

        [TestCase("Gameplay", "Removed")] [TestCase("Gameplay", "Disabled")]
        [TestCase("Paused", "Removed")] [TestCase("Paused", "Disabled")]
        [TestCase("Relics", "Removed")] [TestCase("Relics", "Disabled")]
        [TestCase("ChoosingUpgrade", "Removed")] [TestCase("ChoosingUpgrade", "Disabled")]
        [TestCase("Results", "Removed")] [TestCase("Results", "Disabled")]
        public void HotplugKeepsContextAndMenuEventSystemAlive(string context, string loss)
        {
            Compose();
            bool play = context == "Gameplay", pause = context == "Paused" || context == "Relics";
            input.SetContext(play, pause); Neutral(Pump());
            bool stickBefore = Visible(Stick), quackBefore = Visible(Quack), navigationBefore = events.sendNavigationEvents;
            var pad = AddPad(); Pump();
            bool hiddenWhenAvailable = !Visible(Stick) && !Visible(Quack);
            Pad(pad, new GamepadState { leftStick = Vector2.up }.WithButton(GamepadButton.South).WithButton(GamepadButton.Start));
            input.Read(); Lose(pad, loss); Neutral(Pump());
            var issues = new List<string>();
            if (!hiddenWhenAvailable) issues.Add("Connected idle controller did not hide joystick/Quack in " + context);
            if (Visible(Stick) != stickBefore || Visible(Quack) != quackBefore) issues.Add("Loss did not restore previous context visibility");
            if (EventSystem.current != events || !events.isActiveAndEnabled) issues.Add("Hotplug disabled or replaced menu EventSystem");
            if (events.sendNavigationEvents != navigationBefore) issues.Add("Hotplug changed navigation context");
            input.Mobile.SetMove(Vector2.right); input.Mobile.SetQuack(true); input.Mobile.SetPause(true);
            var command = Pump();
            if (command.Move != (play ? Vector2.right : Vector2.zero)) issues.Add("Move context was not preserved");
            if (command.QuackPressed != play) issues.Add("Quack context was not preserved");
            if (command.PausePressed != (play || pause)) issues.Add("Pause context was not preserved");
            Assert.That(issues, Is.Empty, string.Join("; ", issues));
        }

        [TestCase("Removed")] [TestCase("Disabled")]
        public void OneRemainingEnabledControllerStillSuppressesTouch(string loss)
        {
            var first = AddPad(); var second = AddPad(); Compose();
            Lose(first, loss); Pump(); TouchVisibility(false);
            Lose(second, loss); Pump(); TouchVisibility(true);
        }

        [Test]
        public void ReaddedNeutralControllerDoesNotReplayRemovedControllerCommands()
        {
            Compose(); var pad = AddPad();
            Pad(pad, new GamepadState { leftStick = Vector2.up }.WithButton(GamepadButton.South).WithButton(GamepadButton.Start));
            Lose(pad, "Removed"); Neutral(Pump());
            InputSystem.AddDevice(pad); Pad(pad, new GamepadState()); Neutral(Pump());
            Pad(pad, new GamepadState { leftStick = Vector2.right }.WithButton(GamepadButton.South));
            var command = input.Read(); Assert.That(command.Move, Is.EqualTo(Vector2.right)); Assert.That(command.QuackPressed, Is.True);
        }

        [Test]
        public void DisablingAndEnablingClearsPendingEdgesBeforeAFreshPress()
        {
            Compose(); var pad = AddPad();
            Pad(pad, new GamepadState().WithButton(GamepadButton.South).WithButton(GamepadButton.Start));
            Lose(pad, "Disabled"); InputSystem.EnableDevice(pad); Pad(pad, new GamepadState()); Neutral(Pump());
            Pad(pad, new GamepadState().WithButton(GamepadButton.South));
            Assert.That(input.Read().QuackPressed, Is.True); Assert.That(input.Read().QuackPressed, Is.False);
        }

        [UnityTest]
        public IEnumerator DisposedConsumerDoesNotReactToFurtherHotplug()
        {
            Compose(); var mobile = input.Mobile; input.Dispose(); input = null;
            yield return null;
            var pad = AddPad(); InputSystem.DisableDevice(pad); InputSystem.EnableDevice(pad); InputSystem.RemoveDevice(pad);
            yield return null;
            Assert.That(mobile == null, Is.True); Assert.That(events.isActiveAndEnabled, Is.True);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
