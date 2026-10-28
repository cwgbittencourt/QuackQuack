using System;
using NUnit.Framework;
using QuackQuack.Core.Shared;
using QuackQuack.Platform.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace QuackQuack.Tests.PlayMode
{
    public class GamepadInputAdapterTests
    {
        private Gamepad gamepad, previousGamepad;
        private Keyboard keyboard, previousKeyboard;
        private InputSettings.BackgroundBehavior background;
        private float deadzoneMin, deadzoneMax;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode editorInput;
#endif

        [SetUp]
        public void SetUp()
        {
            background = InputSystem.settings.backgroundBehavior;
            deadzoneMin = InputSystem.settings.defaultDeadzoneMin;
            deadzoneMax = InputSystem.settings.defaultDeadzoneMax;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.defaultDeadzoneMin = 0.2f;
            InputSystem.settings.defaultDeadzoneMax = 0.8f;
#if UNITY_EDITOR
            editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            previousGamepad = Gamepad.current;
            previousKeyboard = Keyboard.current;
            gamepad = InputSystem.AddDevice<Gamepad>();
        }

        [TearDown]
        public void TearDown()
        {
            if (gamepad != null && gamepad.added) InputSystem.RemoveDevice(gamepad);
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (previousGamepad != null && previousGamepad.added) previousGamepad.MakeCurrent();
            if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
            InputSystem.settings.backgroundBehavior = background;
            InputSystem.settings.defaultDeadzoneMin = deadzoneMin;
            InputSystem.settings.defaultDeadzoneMax = deadzoneMax;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
#endif
        }

        [TestCase(0.1f, 0f)]
        [TestCase(0.5f, 0.5f)]
        [TestCase(0.9f, 1f)]
        public void StickUsesInputSystemDeadzoneAndPreservesAnalogMagnitude(float raw, float expected)
        {
            using (var input = new GamepadInputAdapter())
            {
                InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.right * raw });
                InputSystem.Update();
                var command = input.Read();
                Assert.That(command.Move.x, Is.EqualTo(expected).Within(0.001));
                Assert.That(command.Move.y, Is.EqualTo(0).Within(0.001));
                Assert.That(command.Source, Is.EqualTo(InputSource.Gamepad));
                Assert.That(command.QuackPressed, Is.False);
                Assert.That(command.PausePressed, Is.False);
            }
        }

        [Test]
        public void KeyboardEventsDoNotBecomeGamepadCommands()
        {
            keyboard = InputSystem.AddDevice<Keyboard>();
            using (var input = new GamepadInputAdapter())
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D, Key.Space, Key.Escape));
                InputSystem.Update();
                AssertNeutral(input.Read());
            }
        }

        [Test]
        public void RemovingControllerDiscardsUnreadEdgesAndNewControllerCanBeUsed()
        {
            using (var input = new GamepadInputAdapter())
            {
                InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.right }
                    .WithButton(GamepadButton.South).WithButton(GamepadButton.Start));
                InputSystem.Update();
                // Remove before Read: a disconnected device must not leave a pending activation.
                InputSystem.RemoveDevice(gamepad);
                InputSystem.Update();
                AssertNeutral(input.Read());
                gamepad = InputSystem.AddDevice<Gamepad>();
                InputSystem.Update();
                AssertNeutral(input.Read());
                InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.up }
                    .WithButton(GamepadButton.South).WithButton(GamepadButton.Start));
                InputSystem.Update();
                var command = input.Read();
                Assert.That(command.Move, Is.EqualTo(Vector2.up));
                Assert.That(command.QuackPressed, Is.True);
                Assert.That(command.PausePressed, Is.True);
                Assert.That(input.Read().QuackPressed, Is.False);
                Assert.That(input.Read().PausePressed, Is.False);
            }
        }

        [Test]
        public void ShortButtonTapBetweenReadsIsConsumedOnce()
        {
            using (var input = new GamepadInputAdapter())
            {
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South).WithButton(GamepadButton.Start));
                InputSystem.Update();
                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                InputSystem.Update();
                var command = input.Read();
                Assert.That(command.QuackPressed, Is.True);
                Assert.That(command.PausePressed, Is.True);
                AssertNeutral(input.Read());
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ResetControllerDiscardsUnreadEdges(bool hardReset)
        {
            using (var input = new GamepadInputAdapter())
            {
                InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.right }
                    .WithButton(GamepadButton.South).WithButton(GamepadButton.Start));
                InputSystem.Update();
                InputSystem.ResetDevice(gamepad, hardReset);
                AssertNeutral(input.Read());
            }
        }

        [Test]
        public void DisposeIsIdempotentAndPreventsFurtherReads()
        {
            var input = new GamepadInputAdapter();
            input.Dispose();
            Assert.DoesNotThrow(() => input.Dispose());
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South));
            InputSystem.Update();
            Assert.Throws<ObjectDisposedException>(() => input.Read());
        }

        private static void AssertNeutral(PlayerCommand command)
        {
            Assert.That(command.Move, Is.EqualTo(Vector2.zero));
            Assert.That(command.Source, Is.EqualTo(InputSource.Gamepad));
            Assert.That(command.QuackPressed, Is.False);
            Assert.That(command.PausePressed, Is.False);
        }
    }
}
