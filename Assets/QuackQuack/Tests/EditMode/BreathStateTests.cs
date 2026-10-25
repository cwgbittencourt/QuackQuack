using NUnit.Framework;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Run;

namespace QuackQuack.Tests.EditMode
{
    public class BreathStateTests
    {
        [Test]
        public void PlayerTerrainIsExclusiveAndRejectsUnknownValues()
        {
            var player = new PlayerState(100, 100, 10, 20);
            Assert.That(player.Terrain, Is.EqualTo(TerrainType.Land));
            player.Terrain = TerrainType.Water;
            player.Terrain = TerrainType.Water;
            Assert.Throws<System.ArgumentOutOfRangeException>(() => player.Terrain = (TerrainType)99);
            Assert.That(player.Terrain, Is.EqualTo(TerrainType.Water));
            player.Terrain = TerrainType.Land;
            player.Breath.TryConsume(100, out _);
            player.Breath.Recover(1, player.Terrain);
            Assert.That(player.Breath.Current, Is.EqualTo(10));
        }

        [Test]
        public void LethalDamageClampsHealthAndDeadPlayerRejectsDamage()
        {
            var player = new PlayerState(100, 100, 10, 20);
            Assert.That(player.TakeDamage(150, 0, 0), Is.True);
            Assert.That(player.Health, Is.Zero);
            Assert.That(player.MaxHealth, Is.EqualTo(100));
            Assert.That(player.TakeDamage(10, 1, 0), Is.False);
        }

        [Test]
        public void RejectedDamagePreservesHealthAndInvulnerabilityDeadline()
        {
            var player = new PlayerState(100, 100, 10, 20);
            Assert.That(player.TakeDamage(10, 0, 0.5f), Is.True);
            Assert.That(player.TakeDamage(50, 0.25, 10), Is.False);
            Assert.That(player.Health, Is.EqualTo(90));
            Assert.That(player.InvulnerableUntil, Is.EqualTo(0.5));
            Assert.That(player.TakeDamage(10, 0.5, 0), Is.True);
            Assert.That(player.TakeDamage(10, 0.5, 0), Is.True);
        }

        [Test]
        public void InvalidNumbersCannotCorruptPlayerState()
        {
            foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                Assert.Throws<System.ArgumentOutOfRangeException>(() => new PlayerState(invalid, 100, 10, 20));
                Assert.Throws<System.ArgumentOutOfRangeException>(() => new PlayerState(100, invalid, 10, 20));
                Assert.Throws<System.ArgumentOutOfRangeException>(() => new PlayerState(100, 100, invalid, 20));
                Assert.Throws<System.ArgumentOutOfRangeException>(() => new PlayerState(100, 100, 10, invalid));
                var player = new PlayerState(100, 100, 10, 20);
                Assert.That(player.TakeDamage(invalid, 0, 1), Is.False);
                Assert.That(player.TakeDamage(10, invalid, 1), Is.False);
                Assert.That(player.TakeDamage(10, 0, invalid), Is.False);
                Assert.That(player.Breath.TryConsume(invalid, out _), Is.False);
                Assert.That(player.Health, Is.EqualTo(100));
                Assert.That(player.InvulnerableUntil, Is.Zero);
                Assert.That(player.Breath.Current, Is.EqualTo(100));
            }
            var state = new PlayerState(100, 100, 10, 20);
            Assert.That(state.TakeDamage(10, -1, 1), Is.False);
            Assert.That(state.TakeDamage(10, 0, -1), Is.False);
            Assert.That(state.TakeDamage(0, 0, 1), Is.False);
            Assert.That(state.TakeDamage(-10, 0, 1), Is.False);
            Assert.That(state.Health, Is.EqualTo(100));
        }

        [TestCase(TerrainType.Land)]
        [TestCase(TerrainType.Water)]
        public void RepeatedConsumptionAndRecoveryStayWithinCapacity(TerrainType terrain)
        {
            var breath = new BreathSystem(100, 10, 20);
            for (int i = 0; i < 100; i++)
            {
                breath.TryConsume(35, out _);
                Assert.That(breath.Current, Is.InRange(0f, 100f));
                breath.Recover(0.1f, terrain);
                Assert.That(breath.Current, Is.InRange(0f, 100f));
            }
            breath.Recover(1000, terrain);
            Assert.That(breath.Current, Is.EqualTo(100));
            Assert.That(breath.IsFull, Is.True);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(101f)]
        public void InvalidCostDoesNotConsumeBreath(float cost)
        {
            var breath = new BreathSystem(100, 10, 20);
            Assert.That(breath.TryConsume(cost, out var enhanced), Is.False);
            Assert.That(enhanced, Is.False);
            Assert.That(breath.Current, Is.EqualTo(100));
        }

        [Test]
        public void AcceptedActivationCanDrainToZeroButNextOneIsRejected()
        {
            var breath = new BreathSystem(100, 10, 20);
            Assert.That(breath.TryConsume(100, out var full), Is.True);
            Assert.That(full, Is.True);
            Assert.That(breath.Current, Is.Zero);
            Assert.That(breath.TryConsume(100, out _), Is.False);
            breath.Recover(1000, TerrainType.Water);
            Assert.That(breath.Current, Is.EqualTo(100));
        }

        [Test]
        public void WaterRecoversFasterAndPartialActivationUsesRemainingBreath()
        {
            var breath = new BreathSystem(100, 10, 20);
            breath.TryConsume(100, out _);
            breath.Recover(1, TerrainType.Land);
            Assert.That(breath.Current, Is.EqualTo(10));
            Assert.That(breath.TryConsume(25, out var full), Is.True);
            Assert.That(full, Is.False);
            Assert.That(breath.Current, Is.Zero);
            breath.Recover(1, TerrainType.Water);
            Assert.That(breath.Current, Is.EqualTo(20));
        }

        [Test]
        public void DamageWindowRejectsRepeatedContact()
        {
            var player = new PlayerState(100, 100, 10, 20);
            Assert.That(player.TakeDamage(10, 1, 0.5f), Is.True);
            Assert.That(player.TakeDamage(10, 1.1f, 0.5f), Is.False);
            Assert.That(player.TakeDamage(10, 1.5f, 0.5f), Is.True);
            Assert.That(player.Health, Is.EqualTo(80));
        }
    }
}
