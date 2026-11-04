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
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.PlayMode
{
    [Category("RelicsInputRuntime")]
    public sealed class RelicsInputTests
    {
        private static readonly string[] Layouts = {
            "Gamepad", "XInputController", "DualShockGamepad",
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
            "DualSenseGamepadHID"
#endif
        };
        private static readonly string[] Contexts = { "Gameplay", "Paused", "Relics", "ChoosingUpgrade", "Results" };
        private static readonly string[] BlockedTransitions = { "Paused", "ChoosingUpgrade", "Results" };
        private static readonly string[] Losses = { "Removed", "Disabled", "Reset" };
        private readonly List<InputDevice> disabled = new List<InputDevice>();
        private Keyboard keyboard, previousKeyboard;
        private Gamepad pad, previousPad;
        private UnifiedPlayerInput input;
        private bool queueOnly;
        private GameObject ui;
        private InputSettings.BackgroundBehavior background;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode routing;
#endif
        [SetUp]
        public void SetUp()
        {
            queueOnly = false; disabled.Clear(); previousPad = Gamepad.current; previousKeyboard = Keyboard.current;
            background = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            routing = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            foreach (var d in InputSystem.devices) if (d.enabled && (d is Keyboard || d is Gamepad)) disabled.Add(d);
            foreach (var d in disabled) InputSystem.DisableDevice(d);
            keyboard = InputSystem.AddDevice<Keyboard>();
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            input?.Dispose(); input = null;
            if (ui != null) Object.Destroy(ui);
            yield return null;
            if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            foreach (var d in disabled) if (d.added) InputSystem.EnableDevice(d);
            if (previousPad != null && previousPad.added) previousPad.MakeCurrent();
            if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
            InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = routing;
#endif
        }
        private void Compose(string layout = "Gamepad")
        {
            pad = (Gamepad)InputSystem.AddDevice(layout); input = new UnifiedPlayerInput(); input.Read();
        }
        private void Change<T>(InputControl<T> control, T value) where T : struct
        {
            // Full state events also support bitfield buttons and preprocessed HID layouts.
            using (StateEvent.From(control.device, out var evt)) {
                control.WriteValueIntoEvent(value, evt); InputSystem.QueueEvent(evt);
            }
            if (!queueOnly) InputSystem.Update();
        }        private static void Neutral(PlayerCommand value)
        {
            Assert.That(value.Move, Is.EqualTo(Vector2.zero));
            Assert.That(value.QuackPressed || value.PausePressed || value.RelicsPressed, Is.False);
        }
        private void Context(string name) => input.SetContext(name == "Gameplay", name == "Paused" || name == "Relics");

        [Test]
        public void NormalizedControllerBindingsProduceSeparateEdges([ValueSource(nameof(Layouts))] string layout)
        {
            Compose(layout);
            Change(pad.selectButton, 1f);
            var command = input.Read();
            Assert.That(command.Source, Is.EqualTo(InputSource.Gamepad)); Assert.That(command.RelicsPressed, Is.True);
            Assert.That(command.PausePressed || command.QuackPressed, Is.False); Neutral(input.Read());
            InputSystem.Update(); Neutral(input.Read());
            Change(pad.selectButton, 0f); Change(pad.selectButton, 1f);
            Assert.That(input.Read().RelicsPressed, Is.True);
            Change(pad.selectButton, 0f); Change(pad.startButton, 1f);
            command = input.Read(); Assert.That(command.PausePressed, Is.True); Assert.That(command.RelicsPressed, Is.False);
            Change(pad.startButton, 0f); Change(pad.buttonSouth, 1f);
            command = input.Read(); Assert.That(command.QuackPressed, Is.True); Assert.That(command.RelicsPressed || command.PausePressed, Is.False);
            Assert.That(PlayerInputConfiguration.Load().Actions.enabled, Is.False, "Consumers must keep the authored asset disabled.");
        }

        [Test]
        public void KeyboardRProducesOnlyOneRelicsEdgePerPress()
        {
            Compose(); Change(keyboard.rKey, 1f);
            var command = input.Read(); Assert.That(command.RelicsPressed, Is.True); Assert.That(command.Source, Is.EqualTo(InputSource.Keyboard));
            Assert.That(command.PausePressed || command.QuackPressed, Is.False); Neutral(input.Read());
            InputSystem.Update(); Neutral(input.Read()); Change(keyboard.rKey, 0f); Change(keyboard.rKey, 1f);
            Assert.That(input.Read().RelicsPressed, Is.True);
        }

        [Test]
        public void RelicsAvailabilityMatchesTheInputContext([ValueSource(nameof(Contexts))] string context)
        {
            Compose(); Context(context); input.Read(); Change(pad.selectButton, 1f);
            var command = input.Read();
            Assert.That(command.RelicsPressed, Is.EqualTo(context == "Gameplay" || context == "Paused" || context == "Relics"));
            Assert.That(command.QuackPressed || command.PausePressed, Is.False); Assert.That(command.Move, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void ContextSwitchDiscardsUnconsumedRelicsEdge([ValueSource(nameof(BlockedTransitions))] string destination)
        {
            Compose(); Change(pad.selectButton, 1f); Context(destination); Neutral(input.Read());
            Change(pad.selectButton, 0f); Change(pad.selectButton, 1f);
            Assert.That(input.Read().RelicsPressed, Is.EqualTo(destination == "Paused"));
        }

        [Test]
        public void ButtonsHeldInMandatoryChoiceCannotReplayOnResume()
        {
            Compose(); input.SetContext(false, false);
            Change(pad.selectButton, 1f); Change(pad.buttonSouth, 1f); Neutral(input.Read());
            input.SetContext(true, false); InputSystem.Update(); Neutral(input.Read());
            Change(pad.selectButton, 0f); Change(pad.buttonSouth, 0f); input.Read();
            Change(pad.selectButton, 1f); Assert.That(input.Read().RelicsPressed, Is.True);
        }

        [Test]
        public void DeviceLossDiscardsPendingRelics([ValueSource(nameof(Losses))] string loss)
        {
            Compose(); Change(pad.selectButton, 1f);
            if (loss == "Removed") InputSystem.RemoveDevice(pad);
            else if (loss == "Disabled") InputSystem.DisableDevice(pad);
            else InputSystem.ResetDevice(pad);
            Neutral(input.Read());
        }

        [Test]
        public void RelicsActivityParticipatesInUnifiedSourceSelection()
        {
            Compose(); Change(keyboard.dKey, 1f); Assert.That(input.Read().Source, Is.EqualTo(InputSource.Keyboard));
            Change(pad.selectButton, 1f);
            var command = input.Read(); Assert.That(command.Source, Is.EqualTo(InputSource.Gamepad));
            Assert.That(command.RelicsPressed, Is.True); Assert.That(command.Move, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void RuntimeClonesConsumeTheirOwnEdgeWithoutMutatingTheAuthoredAsset()
        {
            Compose(); using (var other = new UnifiedPlayerInput()) {
                Change(pad.selectButton, 1f);
                Assert.That(input.Read().RelicsPressed, Is.True); Assert.That(input.Read().RelicsPressed, Is.False);
                Assert.That(other.Read().RelicsPressed, Is.True); Assert.That(other.Read().RelicsPressed, Is.False);
                Assert.That(PlayerInputConfiguration.Load().Actions.enabled, Is.False);
            }
        }

        [UnityTest]
        public IEnumerator UiSouthConfirmsEastReturnsAndNavigationDoesNotLeakGameplay([ValueSource(nameof(Layouts))] string layout)
        {
            queueOnly = true;
            ui = new GameObject("T010 UI", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var target = new GameObject("T010 selected action", typeof(RectTransform), typeof(EventTrigger)); target.transform.SetParent(ui.transform);
            int submits = 0, cancels = 0, moves = 0;
            var trigger = target.GetComponent<EventTrigger>();
            AddEvent(trigger, EventTriggerType.Submit, () => submits++); AddEvent(trigger, EventTriggerType.Cancel, () => cancels++);
            AddEvent(trigger, EventTriggerType.Move, () => moves++);
            Compose(layout); input.SetContext(false, true); input.Read(); EventSystem.current.SetSelectedGameObject(target);
            yield return null;
            Change(pad.buttonSouth, 1f); yield return null; yield return null;
            Assert.That(submits, Is.EqualTo(1)); Neutral(input.Read()); Change(pad.buttonSouth, 0f); yield return null;
            Change(pad.buttonEast, 1f); yield return null; yield return null;
            Assert.That(cancels, Is.EqualTo(1)); Neutral(input.Read()); Change(pad.buttonEast, 0f); yield return null;
            UiStick(true); yield return null; yield return null;
            Assert.That(pad.leftStick.ReadValue().y, Is.GreaterThan(0.9f), "Fixture must deliver normalized stick input.");
            Assert.That(moves, Is.GreaterThan(0)); Neutral(input.Read());
            UiStick(false); yield return null; int previousMoves = moves;
            UiDpadRight(); yield return null; yield return null;
            Assert.That(pad.dpad.ReadValue().x, Is.GreaterThan(0.9f));
            Assert.That(moves, Is.GreaterThan(previousMoves)); Neutral(input.Read());
        }
        private void UiStick(bool pressed)
        {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
            if (pad.layout == "DualSenseGamepadHID") { HidNavigation(pressed, false); return; }
#endif
            Change(pad.leftStick, pressed ? Vector2.up : Vector2.zero);
        }
        private void UiDpadRight()
        {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
            if (pad.layout == "DualSenseGamepadHID") { HidNavigation(false, true); return; }
#endif
            Change(pad.dpad, Vector2.right);
        }
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
        private void HidNavigation(bool up, bool right)
        {
            // HID sticks use byte axes and the hat uses an encoded direction, not float vectors.
            var state = new UnityEngine.InputSystem.DualShock.LowLevel.DualSenseHIDInputReport {
                leftStickX = 128, leftStickY = (byte)(up ? 0 : 128), rightStickX = 128, rightStickY = 128,
                buttons0 = (byte)(right ? 2 : 8)
            };
            InputSystem.QueueStateEvent(pad, state);
            if (!queueOnly) InputSystem.Update();
        }
#endif
        private static void AddEvent(EventTrigger trigger, EventTriggerType type, Action action)
        {
            var entry = new EventTrigger.Entry { eventID = type }; entry.callback.AddListener(_ => action()); trigger.triggers.Add(entry);
        }

        [Test]
        public void HeldUiSubmitCannotBecomeQuackWhenGameplayResumes()
        {
            Compose(); input.SetContext(false, true); Change(pad.buttonSouth, 1f); Neutral(input.Read());
            input.SetContext(true, false); InputSystem.Update(); Neutral(input.Read());
            Change(pad.buttonSouth, 0f); input.Read(); Change(pad.buttonSouth, 1f);
            Assert.That(input.Read().QuackPressed, Is.True);
        }
    }
}
