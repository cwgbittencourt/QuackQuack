using NUnit.Framework;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Shared;
using QuackQuack.Platform.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QuackQuack.Tests.PlayMode
{
    public class KeyboardPlayerInputTests
    {
        [TestCase(Key.W, Key.D)]
        [TestCase(Key.UpArrow, Key.RightArrow)]
        [TestCase(Key.W, Key.RightArrow)]
        [TestCase(Key.UpArrow, Key.D)]
        public void KeyboardProducesEquivalentMovementAndSingleQuackEdge(Key up, Key right)
        {
            var settings = InputSystem.settings;
            var previousBackground = settings.backgroundBehavior;
#if UNITY_EDITOR
            var previousEditorInput = settings.editorInputBehaviorInPlayMode;
#endif
            Keyboard keyboard = null;
            try
            {
                // Batchmode has no focused Game View. Route synthetic device events into the game.
                settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
                settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
                keyboard = InputSystem.AddDevice<Keyboard>();
                using (var input = new KeyboardInputAdapter())
                {
                    InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(up, right, Key.Space));
                    InputSystem.Update();
                    var first = input.Read();
                    Assert.That(first.Move.magnitude, Is.EqualTo(1).Within(0.0001));
                    Assert.That(first.Move.x, Is.GreaterThan(0));
                    Assert.That(first.Move.y, Is.GreaterThan(0));
                    Assert.That(first.QuackPressed, Is.True);
                    Assert.That(first.Source, Is.EqualTo(InputSource.Keyboard));
                    var expected = new PlayerCommand(Vector2.one, true);
                    Assert.That(Vector2.Distance(first.Move, expected.Move), Is.LessThan(0.0001f));
                    var actualBreath = new BreathSystem(100, 10, 20);
                    var expectedBreath = new BreathSystem(100, 10, 20);
                    Assert.That(first.QuackPressed && actualBreath.TryConsume(35, out _),
                        Is.EqualTo(expected.QuackPressed && expectedBreath.TryConsume(35, out _)));
                    Assert.That(actualBreath.Current, Is.EqualTo(expectedBreath.Current));
                    Assert.That(input.Read().QuackPressed, Is.False);
                    InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
                    InputSystem.Update();
                    Assert.That(input.Read().Move, Is.EqualTo(Vector2.zero));
                    InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.Space, Key.Escape));
                    InputSystem.Update();
                    var pressedAgain = input.Read();
                    Assert.That(pressedAgain.QuackPressed, Is.True, "Releasing and pressing again must create a new edge.");
                    Assert.That(pressedAgain.PausePressed, Is.True);
                    Assert.That(input.Read().PausePressed, Is.False);
                    InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.Space, Key.Escape));
                    InputSystem.Update();
                    var held = input.Read();
                    Assert.That(held.QuackPressed, Is.False, "Holding Space across updates must not repeat the edge.");
                    Assert.That(held.PausePressed, Is.False, "Holding Escape across updates must not repeat the edge.");
                    InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.W, Key.DownArrow, Key.A, Key.RightArrow));
                    InputSystem.Update();
                    Assert.That(input.Read().Move, Is.EqualTo(Vector2.zero), "Opposing aliases must cancel each other.");
                }
            }
            finally
            {
                if (keyboard != null) InputSystem.RemoveDevice(keyboard);
                settings.backgroundBehavior = previousBackground;
#if UNITY_EDITOR
                settings.editorInputBehaviorInPlayMode = previousEditorInput;
#endif
            }
        }
    }
}
