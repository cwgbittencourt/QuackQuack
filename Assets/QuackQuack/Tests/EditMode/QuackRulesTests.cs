using NUnit.Framework;
using QuackQuack.Core.Combat;
using UnityEngine;

namespace QuackQuack.Tests.EditMode
{
    public class QuackRulesTests
    {
        [Test]
        public void RepeatingWaveStartDoesNotAllowDuplicateHit()
        {
            var rules = new QuackSystem();
            rules.BeginWave(1);
            Assert.That(rules.TryHit(10, true, false, true, 2, 4, 0.5f, 2, out _), Is.True);
            rules.BeginWave(1);
            Assert.That(rules.TryHit(10, true, false, true, 2, 4, 0.5f, 2, out _), Is.False);
        }

        [Test]
        public void CollisionRequiresAnAcceptedNonProjectileHit()
        {
            var rules = new QuackSystem();
            rules.BeginWave(1);
            Assert.That(rules.TryCollision(10), Is.False);
            rules.TryHit(10, true, false, false, 2, 4, 0.5f, 2, out _);
            Assert.That(rules.TryCollision(10), Is.True);
            rules.TryHit(20, false, true, false, 2, 4, 0.5f, 2, out _);
            Assert.That(rules.TryCollision(20), Is.False);
        }

        [Test]
        public void InvalidDamageDoesNotReserveTarget()
        {
            var rules = new QuackSystem();
            rules.BeginWave(1);
            Assert.That(rules.TryHit(10, true, false, false, float.NaN, 4, 0.5f, 2, out _), Is.False);
            Assert.That(rules.TryHit(10, true, false, false, 2, 4, 0.5f, 2, out _), Is.True);
        }

        [Test]
        public void WaveAppliesDamageInterruptAndCollisionOnlyOncePerTarget()
        {
            var rules = new QuackSystem();
            rules.BeginWave(1);
            Assert.That(rules.TryHit(10, true, false, true, 2, 4, 0.5f, 2, out var hit), Is.True);
            Assert.That(hit.Damage, Is.EqualTo(4));
            Assert.That(hit.Knockback, Is.EqualTo(8));
            Assert.That(hit.InterruptSeconds, Is.EqualTo(1));
            Assert.That(hit.Critical, Is.True);
            Assert.That(rules.TryHit(10, true, false, true, 2, 4, 0.5f, 2, out _), Is.False);
            Assert.That(rules.TryCollision(10), Is.True);
            Assert.That(rules.TryCollision(10), Is.False);
            rules.BeginWave(2);
            Assert.That(rules.TryHit(10, false, false, false, 2, 4, 0.5f, 2, out hit), Is.True);
            Assert.That(hit.InterruptSeconds, Is.Zero);
            Assert.That(hit.Damage, Is.EqualTo(2));
            Assert.That(hit.Knockback, Is.EqualTo(4));
            Assert.That(hit.Critical, Is.False);
            Assert.That(rules.TryCollision(10), Is.True, "A new wave may apply one new collision.");
            Assert.That(rules.TryHit(11, true, false, false, 2, 4, 0.5f, 2, out hit), Is.True,
                "Deduplication must be per target, not per wave alone.");
            Assert.That(hit.InterruptSeconds, Is.EqualTo(0.5f));
        }

        [Test]
        public void AcceptedHitEmitsEachEventTypeOnceWithCorrelationAndMetadata()
        {
            var rules = new QuackSystem();
            var events = new System.Collections.Generic.List<CombatEvent>();
            rules.Resolved += events.Add;
            rules.BeginWave(7);
            Assert.That(rules.TryEmitHitEvents(10, 20, 3, Vector2.right), Is.False);
            rules.TryHit(10, true, false, true, 2, 4, 0.5f, 2, out _);
            Assert.That(events, Is.Empty, "Events wait until the caller has applied the accepted change.");
            Assert.That(rules.TryEmitHitEvents(10, 20, 3, Vector2.right), Is.True);
            Assert.That(rules.TryEmitHitEvents(10, 21, 3, Vector2.right), Is.False);
            Assert.That(events.Count, Is.EqualTo(3));
            Assert.That(events[0].Type, Is.EqualTo(CombatEventType.DamageApplied));
            Assert.That(events[0].Amount, Is.EqualTo(4));
            Assert.That(events[1].Type, Is.EqualTo(CombatEventType.CriticalHit));
            Assert.That(events[2].Type, Is.EqualTo(CombatEventType.EffectApplied));
            Assert.That(events[2].Amount, Is.EqualTo(1));
            foreach (var value in events)
            {
                Assert.That(value.Tick, Is.EqualTo(20));
                Assert.That(value.CorrelationId, Is.EqualTo(7));
                Assert.That(value.Source, Is.EqualTo(3));
                Assert.That(value.Target, Is.EqualTo(10));
                Assert.That(value.Position, Is.EqualTo(Vector2.right));
            }
            Assert.That(rules.TryEmitCollisionEvent(10, 20, 3, Vector2.right), Is.False);
            rules.TryCollision(10);
            Assert.That(rules.TryEmitCollisionEvent(10, 20, 3, Vector2.right), Is.True);
            Assert.That(rules.TryEmitCollisionEvent(10, 20, 3, Vector2.right), Is.False);
            Assert.That(events[3].Type, Is.EqualTo(CombatEventType.CollisionTriggered));
            rules.BeginWave(8);
            Assert.That(rules.TryEmitHitEvents(10, 21, 3, Vector2.right), Is.False);
            rules.TryHit(10, false, false, false, 2, 4, 0.5f, 2, out _);
            rules.TryEmitHitEvents(10, 21, 3, Vector2.right);
            Assert.That(events.Count, Is.EqualTo(5));
            Assert.That(events[4].CorrelationId, Is.EqualTo(8));
        }

        [Test]
        public void ProjectileEmitsReflectionWithoutDamageCriticalOrInterruption()
        {
            var rules = new QuackSystem();
            var events = new System.Collections.Generic.List<CombatEvent>();
            rules.Resolved += events.Add;
            rules.BeginWave(1);
            rules.TryHit(20, true, true, true, 2, 4, 0.5f, 2, out var hit);
            Assert.That(hit.Damage, Is.Zero);
            Assert.That(hit.Knockback, Is.Zero);
            Assert.That(hit.InterruptSeconds, Is.Zero);
            Assert.That(hit.Critical, Is.False);
            rules.TryEmitHitEvents(20, 1, 3, Vector2.zero);
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Type, Is.EqualTo(CombatEventType.ProjectileReflected));
        }

        [Test]
        public void WaveMustStartAndCannotMoveBackwards()
        {
            var rules = new QuackSystem();
            Assert.That(rules.TryHit(10, false, false, false, 2, 4, 0.5f, 2, out _), Is.False);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => rules.BeginWave(0));
            rules.BeginWave(2);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => rules.BeginWave(1));
            Assert.That(rules.WaveId, Is.EqualTo(2));
        }

        [Test]
        public void OnlyLightReflectableProjectilesReflectAwayFromPlayer()
        {
            Assert.That(QuackSystem.CanReflect(true, true), Is.True);
            Assert.That(QuackSystem.CanReflect(false, true), Is.False);
            Assert.That(QuackSystem.CanReflect(true, false), Is.False);
            Assert.That(QuackSystem.CanReflect(false, false), Is.False);
            Assert.That(QuackSystem.ReflectedVelocity(Vector2.right, Vector2.zero, 3), Is.EqualTo(Vector2.right * 3));
        }

        [Test]
        public void ReflectedProjectilePreservesSpeedWithoutApplyingQuackDamage()
        {
            var rules = new QuackSystem();
            rules.BeginWave(1);
            Assert.That(rules.TryHit(20, false, true, false, 2, 4, 0.5f, 2, out var hit), Is.True);
            Assert.That(hit.Damage, Is.Zero);
            var velocity = QuackSystem.ReflectedVelocity(new Vector2(3, 4), Vector2.zero, 7);
            Assert.That(velocity.magnitude, Is.EqualTo(7).Within(0.0001f));
            Assert.That(Vector2.Dot(velocity.normalized, new Vector2(3, 4).normalized), Is.EqualTo(1).Within(0.0001f));
        }
    }
}
