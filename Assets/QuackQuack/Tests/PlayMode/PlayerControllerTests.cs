using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Content.Characters;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Run;
using QuackQuack.Core.Shared;
using QuackQuack.Gameplay.Player;
using QuackQuack.Platform.Input;
using QuackQuack.Systems.Combat;
using UnityEngine;
using UnityEngine.TestTools;

namespace QuackQuack.Tests.PlayMode
{
    public class PlayerControllerTests
    {
        private GameObject playerObject, wall, targetObject;
        private CharacterDefinition character;
        private QuackDefinition definition;
        private PlayerController player;
        private RunState run;
        private GameplayEventBus events;
        private Commands input;
        private sealed class Commands : IPlayerInput
        {
            public PlayerCommand Next;
            public PlayerCommand Read()
            {
                var result = Next;
                Next = new PlayerCommand(result.Move, source: result.Source);
                return result;
            }
        }

        [SetUp]
        public void SetUp()
        {
            definition = ScriptableObject.CreateInstance<QuackDefinition>();
            SetField(definition, "id", "quack");
            character = ScriptableObject.CreateInstance<CharacterDefinition>();
            SetField(character, "id", "pato");
            SetField(character, "quack", definition);
            input = new Commands();
            run = new RunState(42);
            run.TryTransition(RunPhase.Playing);
            events = new GameplayEventBus();
            playerObject = new GameObject("Test Pato");
            playerObject.SetActive(false);
            player = playerObject.AddComponent<PlayerController>();
            var initialize = typeof(PlayerController).GetMethod("Initialize");
            Assert.That(initialize, Is.Not.Null, "Controller must accept common input and explicit run services.");
            initialize.Invoke(player, new object[] { character, input, run, events });
            playerObject.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            if (playerObject != null) Object.DestroyImmediate(playerObject);
            if (wall != null) Object.DestroyImmediate(wall);
            if (targetObject != null) Object.DestroyImmediate(targetObject);
            if (character != null) Object.DestroyImmediate(character);
            if (definition != null) Object.DestroyImmediate(definition);
        }

        [UnityTest]
        public IEnumerator CommonCommandsMoveThroughPhysicsAndStopAtWall()
        {
            wall = new GameObject("Solid wall");
            wall.transform.position = new Vector2(2, 0);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(1, 10);
            Physics2D.SyncTransforms();
            input.Next = new PlayerCommand(Vector2.right, source: InputSource.Touch);
            for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
            var body = player.GetComponent<Rigidbody2D>();
            Assert.That(body.position.x, Is.GreaterThan(0.2f));
            Assert.That(body.position.x, Is.LessThan(1.1f), "Pato must remain on the near side of the wall.");
            input.Next = new PlayerCommand(Vector2.zero);
            yield return null;
            yield return new WaitForFixedUpdate();
            Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
        }

        [UnityTest]
        public IEnumerator PauseAndTerminalStateDiscardQueuedMovementAndQuack()
        {
            input.Next = new PlayerCommand(Vector2.right, true, true);
            yield return null;
            yield return new WaitForFixedUpdate();
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Paused));
            Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity, Is.EqualTo(Vector2.zero));
            Assert.That(player.State.Breath.Current, Is.EqualTo(definition.Capacity));
            input.Next = new PlayerCommand(Vector2.zero, false, true);
            yield return null;
            yield return new WaitForFixedUpdate();
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Playing));
            Assert.That(player.State.Breath.Current, Is.EqualTo(definition.Capacity));
            run.TryTransition(RunPhase.Lost);
            input.Next = new PlayerCommand(Vector2.right, true, true);
            yield return null;
            yield return new WaitForFixedUpdate();
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Lost));
            Assert.That(player.ActivateQuack(), Is.False);
            Assert.That(player.State.Breath.Current, Is.EqualTo(definition.Capacity));
            Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity, Is.EqualTo(Vector2.zero));
        }

        [UnityTest]
        public IEnumerator QuackUsesAuthoredReflectionAndEmitsOneEventPerTarget()
        {
            SetField(definition, "reflectionSpeed", 11f);
            targetObject = new GameObject("Projectile with two colliders");
            targetObject.layer = 9;
            targetObject.transform.position = Vector2.right;
            var target = targetObject.AddComponent<QuackTarget>();
            target.GetComponent<CircleCollider2D>().isTrigger = true;
            targetObject.AddComponent<BoxCollider2D>().isTrigger = true;
            target.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            target.IsProjectile = true;
            var received = new List<CombatEvent>();
            events.Combat += received.Add;
            yield return null;
            Physics2D.SyncTransforms();
            Assert.That(player.ActivateQuack(), Is.True);
            Assert.That(target.Reflected, Is.True);
            Assert.That(target.Velocity.magnitude, Is.EqualTo(11).Within(0.001f));
            Assert.That(received.FindAll(e => e.Type == CombatEventType.ProjectileReflected).Count, Is.EqualTo(1));
            Assert.That(player.ActivateQuack(), Is.False);
            float after = player.State.Breath.Current;
            run.TryTransition(RunPhase.Paused);
            yield return new WaitForSeconds(definition.Cooldown + 0.1f);
            Assert.That(player.State.Breath.Current, Is.EqualTo(after));
            run.TryTransition(RunPhase.Playing);
            Assert.That(player.ActivateQuack(), Is.False, "Paused wall time must not expire combat cooldown.");
        }

        private static void SetField(object target, string name, object value)
        {
            var type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                type = type.BaseType;
            }
            field.SetValue(target, value);
        }

        [UnityTest]
        public IEnumerator MultipleCollidersDoNotDuplicateDamageOrFeedback()
        {
            targetObject = new GameObject("Enemy with two colliders");
            targetObject.layer = 9;
            targetObject.transform.position = Vector2.right;
            var target = targetObject.AddComponent<QuackTarget>();
            target.GetComponent<CircleCollider2D>().isTrigger = true;
            targetObject.AddComponent<BoxCollider2D>().isTrigger = true;
            target.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var received = new List<CombatEvent>();
            events.Combat += received.Add;
            yield return null;
            Physics2D.SyncTransforms();
            Assert.That(player.ActivateQuack(), Is.True);
            Assert.That(target.Health, Is.EqualTo(20 - definition.Damage * definition.FullMultiplier));
            Assert.That(received.FindAll(e => e.Type == CombatEventType.DamageApplied).Count, Is.EqualTo(1));
            Assert.That(received.FindAll(e => e.Type == CombatEventType.CriticalHit).Count, Is.EqualTo(1));
            Assert.That(received.FindAll(e => e.Type == CombatEventType.EffectApplied).Count, Is.EqualTo(1));
            Assert.That(target.InterruptedUntil, Is.GreaterThan(Time.time));
        }

        [UnityTest]
        public IEnumerator DisabledControllerStopsAndDropsPendingQuack()
        {
            input.Next = new PlayerCommand(Vector2.right);
            yield return null;
            yield return new WaitForFixedUpdate();
            player.enabled = false;
            Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity, Is.EqualTo(Vector2.zero));
            input.Next = new PlayerCommand(Vector2.zero, true);
            Assert.That(player.ActivateQuack(), Is.False);
            player.enabled = true;
            yield return null;
            yield return new WaitForFixedUpdate();
            Assert.That(player.State.Breath.Current, Is.EqualTo(definition.Capacity));
        }
    }
}
