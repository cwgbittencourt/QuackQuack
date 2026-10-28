using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Content.Characters;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Run;
using QuackQuack.Core.Shared;
using QuackQuack.Gameplay.Player;
using QuackQuack.Gameplay.Terrain;
using QuackQuack.Platform.Input;
using QuackQuack.Systems.Combat;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.PlayMode
{
    // T062 is the red phase for T065/T066. Reflection locates production adapters;
    // it never substitutes an adapter or ignores a missing implementation.
    public class InputParityTests
    {
        private Keyboard keyboard, previousKeyboard;
        private Gamepad gamepad, previousGamepad;
        private InputSettings.BackgroundBehavior background;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode editorInput;
#endif

        [SetUp]
        public void SetUp()
        {
            background = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            previousKeyboard = Keyboard.current;
            previousGamepad = Gamepad.current;
            keyboard = InputSystem.AddDevice<Keyboard>();
            gamepad = InputSystem.AddDevice<Gamepad>();
        }

        [TearDown]
        public void TearDown()
        {
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (gamepad != null && gamepad.added) InputSystem.RemoveDevice(gamepad);
            if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
            if (previousGamepad != null && previousGamepad.added) previousGamepad.MakeCurrent();
            InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
#endif
        }

        [TestCase(InputSource.Keyboard)]
        [TestCase(InputSource.Gamepad)]
        [TestCase(InputSource.Touch)]
        public void EquivalentSequencePreservesMovementAndConsumesButtonEdgesOnce(InputSource source)
        {
            using (var driver = new Driver(this, source))
            {
                var states = new[] {
                    new Sample(Vector2.zero), new Sample(Vector2.right),
                    new Sample(Vector2.one.normalized, true), new Sample(Vector2.one.normalized, true),
                    new Sample(Vector2.left), new Sample(Vector2.left, true),
                    new Sample(Vector2.zero, false, true), new Sample(Vector2.zero, false, true),
                    new Sample(Vector2.zero), new Sample(Vector2.zero, false, true),
                    new Sample(Vector2.zero)
                };
                bool quackHeld = false, pauseHeld = false;
                foreach (var state in states)
                {
                    var command = driver.Read(state);
                    AssertCommand(command, state.Move, state.Quack && !quackHeld, state.Pause && !pauseHeld, source);
                    AssertCommand(driver.Input.Read(), state.Move, false, false, source);
                    quackHeld = state.Quack;
                    pauseHeld = state.Pause;
                }
            }
        }

        [TestCase(InputSource.Keyboard)]
        [TestCase(InputSource.Gamepad)]
        [TestCase(InputSource.Touch)]
        public void EquivalentSequenceHasSameTerrainBreathVelocityPauseAndQuackEvents(InputSource source)
        {
            // Compare every checkpoint with the real keyboard path, not just a final state.
            var keyboardTrace = CaptureSequence(InputSource.Keyboard);
            var actualTrace = CaptureSequence(source);
            Assert.That(actualTrace.Count, Is.EqualTo(keyboardTrace.Count));
            for (int i = 0; i < actualTrace.Count; i++) actualTrace[i].AssertEquivalent(keyboardTrace[i], i);
        }

        [TestCase(InputSource.Keyboard, RunPhase.Won)]
        [TestCase(InputSource.Keyboard, RunPhase.Lost)]
        [TestCase(InputSource.Keyboard, RunPhase.Abandoned)]
        [TestCase(InputSource.Gamepad, RunPhase.Won)]
        [TestCase(InputSource.Gamepad, RunPhase.Lost)]
        [TestCase(InputSource.Gamepad, RunPhase.Abandoned)]
        [TestCase(InputSource.Touch, RunPhase.Won)]
        [TestCase(InputSource.Touch, RunPhase.Lost)]
        [TestCase(InputSource.Touch, RunPhase.Abandoned)]
        public void TerminalAndResultsDiscardAllDeviceCommands(InputSource source, RunPhase outcome)
        {
            using (var driver = new Driver(this, source))
            using (var world = new World(driver.Input))
            {
                world.Step(driver.Read(new Sample(Vector2.right, true)));
                Assert.That(world.Quacks, Is.EqualTo(1));
                driver.Read(new Sample(Vector2.zero));
                Assert.That(world.Run.TryTransition(RunPhase.Boss), Is.True);
                Assert.That(world.Run.TryTransition(outcome), Is.True);
                var before = world.Snapshot();
                world.Step(driver.Read(new Sample(Vector2.one.normalized, true, true)));
                world.Snapshot().AssertEquivalent(before, 0);
                Assert.That(world.Body.linearVelocity, Is.EqualTo(Vector2.zero));
                Assert.That(world.Run.Outcome, Is.EqualTo(outcome));
                Assert.That(world.Run.TryTransition(RunPhase.Results), Is.True);
                before = world.Snapshot();
                driver.Read(new Sample(Vector2.zero));
                world.Step(driver.Read(new Sample(Vector2.left, true, true)));
                world.Snapshot().AssertEquivalent(before, 1);
                Assert.That(world.Run.Outcome, Is.EqualTo(outcome));
            }
        }

        private List<Checkpoint> CaptureSequence(InputSource source)
        {
            using (var driver = new Driver(this, source))
            using (var world = new World(driver.Input))
            {
                var trace = new List<Checkpoint>();
                Action<Sample> step = sample => { world.Step(driver.Read(sample)); trace.Add(world.Snapshot()); };
                step(new Sample(Vector2.right));
                Assert.That(world.Body.linearVelocity.x, Is.EqualTo(world.Character.LandSpeed).Within(0.001));
                world.EnterWater(); // Enter the same water volume at a controlled checkpoint.
                step(new Sample(Vector2.one.normalized, true));
                Assert.That(world.Player.State.Terrain, Is.EqualTo(TerrainType.Water));
                Assert.That(world.Body.linearVelocity.magnitude, Is.EqualTo(world.Character.WaterSpeed).Within(0.001));
                Assert.That(world.Quacks, Is.EqualTo(1));
                Assert.That(world.Player.State.Breath.Current, Is.EqualTo(world.Quack.Capacity - world.Quack.Cost).Within(0.001));
                step(new Sample(Vector2.one.normalized, true));
                Assert.That(world.Quacks, Is.EqualTo(1), "Holding must not consume Folego again.");
                step(new Sample(Vector2.right));
                step(new Sample(Vector2.right, true, true));
                Assert.That(world.Run.Phase, Is.EqualTo(RunPhase.Paused));
                Assert.That(world.Quacks, Is.EqualTo(1), "Simultaneous pause and Quack must prioritize pause.");
                var paused = world.Snapshot();
                step(new Sample(Vector2.right, true, true));
                world.Snapshot().AssertEquivalent(paused, trace.Count - 1);
                step(new Sample(Vector2.zero));
                step(new Sample(Vector2.right, false, true));
                Assert.That(world.Run.Phase, Is.EqualTo(RunPhase.Playing));
                Assert.That(world.Body.linearVelocity, Is.EqualTo(Vector2.zero), "Resume must not leak movement from the pause command.");
                for (int i = 0; i < Mathf.CeilToInt(world.Quack.Cooldown / Time.fixedDeltaTime) + 1; i++)
                    step(new Sample(Vector2.zero));
                step(new Sample(Vector2.up, true));
                Assert.That(world.Quacks, Is.EqualTo(2), "Release and press after cooldown must activate again.");
                world.EnterLand();
                step(new Sample(Vector2.left));
                Assert.That(world.Player.State.Terrain, Is.EqualTo(TerrainType.Land));
                Assert.That(world.Body.linearVelocity.x, Is.EqualTo(-world.Character.LandSpeed).Within(0.001));
                step(new Sample(Vector2.zero));
                Assert.That(world.Body.linearVelocity, Is.EqualTo(Vector2.zero));
                return trace;
            }
        }

        private static void AssertCommand(PlayerCommand value, Vector2 move, bool quack, bool pause, InputSource source)
        {
            Assert.That(Vector2.Distance(value.Move, move), Is.LessThan(0.001f));
            Assert.That(value.Move.magnitude, Is.LessThanOrEqualTo(1.0001f));
            Assert.That(value.QuackPressed, Is.EqualTo(quack));
            Assert.That(value.PausePressed, Is.EqualTo(pause));
            Assert.That(value.Source, Is.EqualTo(source));
        }

        private readonly struct Sample
        {
            public readonly Vector2 Move;
            public readonly bool Quack, Pause;
            public Sample(Vector2 move, bool quack = false, bool pause = false) { Move = move; Quack = quack; Pause = pause; }
        }

        private sealed class Driver : IDisposable
        {
            public IPlayerInput Input { get; }
            private readonly InputParityTests fixture;
            private readonly InputSource source;
            private readonly GameObject host;
            private readonly MethodInfo setMove, setQuack, setPause;
            public Driver(InputParityTests fixture, InputSource source)
            {
                this.fixture = fixture;
                this.source = source;
                if (source == InputSource.Keyboard) { Input = new KeyboardInputAdapter(); return; }
                string typeName = source == InputSource.Gamepad ? "GamepadInputAdapter" : "MobileInputAdapter";
                var type = typeof(IPlayerInput).Assembly.GetType("QuackQuack.Platform.Input." + typeName);
                Assert.That(type, Is.Not.Null, (source == InputSource.Gamepad ? "T065" : "T066") + " pending: production " + typeName + " is missing (T062 red phase).");
                Assert.That(typeof(IPlayerInput).IsAssignableFrom(type), Is.True, typeName + " must implement IPlayerInput.");
                // Check the callback contract before allocating a component, so a failing constructor leaks nothing.
                if (source == InputSource.Touch)
                {
                    setMove = RequireMethod(type, "SetMove", typeof(Vector2));
                    setQuack = RequireMethod(type, "SetQuack", typeof(bool));
                    setPause = RequireMethod(type, "SetPause", typeof(bool));
                }
                if (typeof(Component).IsAssignableFrom(type))
                {
                    host = new GameObject("T062 input adapter");
                    try { Input = (IPlayerInput)host.AddComponent(type); }
                    catch { Object.DestroyImmediate(host); throw; }
                }
                else Input = (IPlayerInput)Activator.CreateInstance(type);
            }
            public PlayerCommand Read(Sample sample)
            {
                if (source == InputSource.Keyboard)
                {
                    var keys = new List<Key>();
                    if (sample.Move.x > 0) keys.Add(Key.D);
                    if (sample.Move.x < 0) keys.Add(Key.A);
                    if (sample.Move.y > 0) keys.Add(Key.W);
                    if (sample.Move.y < 0) keys.Add(Key.S);
                    if (sample.Quack) keys.Add(Key.Space);
                    if (sample.Pause) keys.Add(Key.Escape);
                    InputSystem.QueueStateEvent(fixture.keyboard, new KeyboardState(keys.ToArray()));
                }
                else if (source == InputSource.Gamepad)
                {
                    var state = new GamepadState { leftStick = sample.Move };
                    if (sample.Quack) state = state.WithButton(GamepadButton.South);
                    if (sample.Pause) state = state.WithButton(GamepadButton.Start);
                    InputSystem.QueueStateEvent(fixture.gamepad, state);
                }
                else
                {
                    setMove.Invoke(Input, new object[] { sample.Move });
                    setQuack.Invoke(Input, new object[] { sample.Quack });
                    setPause.Invoke(Input, new object[] { sample.Pause });
                }
                InputSystem.Update();
                return Input.Read();
            }
            public void Dispose()
            {
                (Input as IDisposable)?.Dispose();
                if (host != null) Object.DestroyImmediate(host);
            }
            private static MethodInfo RequireMethod(Type type, string name, Type argument)
            {
                var method = type.GetMethod(name, new[] { argument });
                Assert.That(method, Is.Not.Null, "T066 callback contract: " + name + "(" + argument.Name + ") is required.");
                return method;
            }
        }

        private sealed class World : IDisposable
        {
            public readonly CharacterDefinition Character;
            public readonly QuackDefinition Quack;
            public readonly RunState Run = new RunState(62);
            public readonly PlayerController Player;
            public readonly Rigidbody2D Body;
            public int Quacks;
            private readonly GameObject playerObject, waterObject;
            private static readonly Vector2 Origin = new Vector2(10000, 10000);
            public World(IPlayerInput input)
            {
                Quack = ScriptableObject.CreateInstance<QuackDefinition>();
                SetField(Quack, "id", "quack");
                Character = ScriptableObject.CreateInstance<CharacterDefinition>();
                SetField(Character, "id", "pato");
                SetField(Character, "quack", Quack);
                waterObject = new GameObject("T062 water");
                waterObject.transform.position = Origin;
                var water = waterObject.AddComponent<TerrainZone>();
                water.Configure(new Vector2(4, 4), 0.15f);
                playerObject = new GameObject("T062 Pato");
                playerObject.SetActive(false);
                Player = playerObject.AddComponent<PlayerController>();
                SetField(Player, "water", new[] { water });
                var events = new GameplayEventBus();
                events.Combat += value => { if (value.Type == CombatEventType.QuackActivated) Quacks++; };
                Player.Initialize(Character, input, Run, events);
                Run.TryTransition(RunPhase.Playing);
                playerObject.SetActive(true);
                Body = Player.GetComponent<Rigidbody2D>();
                EnterLand();
            }
            public void EnterWater() => Body.position = Origin;
            public void EnterLand() => Body.position = Origin + new Vector2(10, 0);
            public void Step(PlayerCommand command)
            {
                Player.ApplyCommand(command);
                // Controlled component tick: no wall-clock frame race or replacement gameplay rule.
                Player.SendMessage("FixedUpdate");
                Run.Advance(Time.fixedDeltaTime);
            }
            public Checkpoint Snapshot() => new Checkpoint(Body.linearVelocity, Player.State.Breath.Current, Player.State.Health,
                Player.State.Terrain, Run.Phase, Run.ElapsedSeconds, Quacks);
            public void Dispose()
            {
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(waterObject);
                Object.DestroyImmediate(Character);
                Object.DestroyImmediate(Quack);
            }
        }

        private readonly struct Checkpoint
        {
            private readonly Vector2 velocity;
            private readonly float breath, health;
            private readonly TerrainType terrain;
            private readonly RunPhase phase;
            private readonly double elapsed;
            private readonly int quacks;
            public Checkpoint(Vector2 velocity, float breath, float health, TerrainType terrain, RunPhase phase, double elapsed, int quacks)
            { this.velocity = velocity; this.breath = breath; this.health = health; this.terrain = terrain; this.phase = phase; this.elapsed = elapsed; this.quacks = quacks; }
            public void AssertEquivalent(Checkpoint other, int index)
            {
                string context = "Checkpoint " + index;
                Assert.That(Vector2.Distance(velocity, other.velocity), Is.LessThan(0.001), context);
                Assert.That(breath, Is.EqualTo(other.breath).Within(0.001), context);
                Assert.That(health, Is.EqualTo(other.health), context);
                Assert.That(terrain, Is.EqualTo(other.terrain), context);
                Assert.That(phase, Is.EqualTo(other.phase), context);
                Assert.That(elapsed, Is.EqualTo(other.elapsed).Within(0.000001), context);
                Assert.That(quacks, Is.EqualTo(other.quacks), context);
            }
        }

        private static void SetField(object target, string name, object value)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field == null) continue;
                field.SetValue(target, value);
                return;
            }
            throw new MissingFieldException(target.GetType().FullName, name);
        }
    }
}
