using NUnit.Framework;
using QuackQuack.Core.Combat;
using UnityEngine;

namespace QuackQuack.Tests.EditMode
{
    public class QuackRulesTests
    {
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
