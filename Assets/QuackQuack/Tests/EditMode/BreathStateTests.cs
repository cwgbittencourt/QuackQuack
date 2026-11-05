using NUnit.Framework;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Run;

namespace QuackQuack.Tests.EditMode
{
    public class BreathStateTests
    {
        [Test]
        public void CapacityReconfigurationPreservesReserveAndClampsOnlyWhenShrinking()
        {
            var breath = new BreathSystem(100, 10, 20);
            breath.TryConsume(35, out _);
            breath.SetCapacity(140);
            Assert.That(breath.Current, Is.EqualTo(65));
            breath.RecoverAtRate(1, 24);
            Assert.That(breath.Current, Is.EqualTo(89));
            breath.SetCapacity(80);
            Assert.That(breath.Current, Is.EqualTo(80));
            Assert.That(breath.IsFull, Is.True);
        }

        [TestCase(0f)] [TestCase(-1f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void InvalidEffectiveCapacityDoesNotMutateReserve(float value)
        {
            var breath = new BreathSystem(100, 10, 20);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => breath.SetCapacity(value));
            Assert.That(breath.Capacity, Is.EqualTo(100));
            Assert.That(breath.Current, Is.EqualTo(100));
        }

        [Test]
        public void EffectiveRecoveryRejectsInvalidInputsAndCapsLargeFiniteProduct()
        {
            var breath = new BreathSystem(100, 10, 20);
            breath.TryConsume(100, out _);
            foreach (float value in new[] { -1f, float.NaN, float.PositiveInfinity })
            {
                Assert.Throws<System.ArgumentOutOfRangeException>(() => breath.RecoverAtRate(value, 1));
                Assert.Throws<System.ArgumentOutOfRangeException>(() => breath.RecoverAtRate(1, value));
                Assert.That(breath.Current, Is.Zero);
            }
            breath.RecoverAtRate(0, 0); Assert.That(breath.Current, Is.Zero);
            breath.RecoverAtRate(float.MaxValue, float.MaxValue);
            Assert.That(breath.Current, Is.EqualTo(100));
        }
        [Test]
        public void EnhancedActivationRequiresFullBreathAndCanReturnAfterRecovery()
        {
            var breath = new BreathSystem(100, 10, 20);
            Assert.That(breath.IsFull, Is.True);
            Assert.That(breath.TryConsume(35, out var enhanced), Is.True);
            Assert.That(enhanced, Is.True);
            Assert.That(breath.Current, Is.EqualTo(65));
            Assert.That(breath.IsFull, Is.False);
            Assert.That(breath.TryConsume(35, out enhanced), Is.True);
            Assert.That(enhanced, Is.False);
            breath.Recover(3.5f, TerrainType.Water);
            Assert.That(breath.Current, Is.EqualTo(100));
            Assert.That(breath.IsFull, Is.True);
            Assert.That(breath.TryConsume(35, out enhanced), Is.True);
            Assert.That(enhanced, Is.True);
        }

        [TestCase(TerrainType.Land, 10f)]
        [TestCase(TerrainType.Water, 20f)]
        public void RecoveryUsesElapsedTimeAndClampsAtCapacity(TerrainType terrain, float rate)
        {
            var breath = new BreathSystem(100, 10, 20);
            breath.TryConsume(100, out _);
            breath.Recover(0.25f, terrain);
            Assert.That(breath.Current, Is.EqualTo(rate * 0.25f));
            Assert.That(breath.IsFull, Is.False);
            breath.Recover(float.MaxValue, terrain);
            Assert.That(breath.Current, Is.EqualTo(100));
            Assert.That(breath.IsFull, Is.True);
        }

        [Test]
        public void InvalidRecoveryLeavesPartialReserveUnchanged()
        {
            var breath = new BreathSystem(100, 10, 20);
            breath.TryConsume(35, out _);
            foreach (float seconds in new[] { -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                Assert.Throws<System.ArgumentOutOfRangeException>(() => breath.Recover(seconds, TerrainType.Land));
                Assert.That(breath.Current, Is.EqualTo(65));
                Assert.That(breath.IsFull, Is.False);
            }
            Assert.Throws<System.ArgumentOutOfRangeException>(() => breath.Recover(1, (TerrainType)99));
            Assert.That(breath.Current, Is.EqualTo(65));
        }

        [Test]
        public void ZeroElapsedTimeAndZeroLandRateDoNotRecoverBreath()
        {
            var breath = new BreathSystem(100, 0, 20);
            breath.TryConsume(35, out _);
            breath.Recover(0, TerrainType.Water);
            breath.Recover(float.MaxValue, TerrainType.Land);
            Assert.That(breath.Current, Is.EqualTo(65));
            Assert.That(breath.IsFull, Is.False);
        }

        [Test]
        public void ReplayingCommandsProducesIdenticalStateAtEveryStep()
        {
            var first = new BreathSystem(100, 10, 20);
            var replay = new BreathSystem(100, 10, 20);
            for (int step = 0; step < 200; step++)
            {
                float cost = step % 3 == 0 ? 100 : 35;
                Assert.That(first.TryConsume(cost, out var firstEnhanced),
                    Is.EqualTo(replay.TryConsume(cost, out var replayEnhanced)));
                Assert.That(firstEnhanced, Is.EqualTo(replayEnhanced));
                var terrain = step % 2 == 0 ? TerrainType.Land : TerrainType.Water;
                first.Recover(0.125f, terrain);
                replay.Recover(0.125f, terrain);
                Assert.That(first.Current, Is.EqualTo(replay.Current));
                Assert.That(first.IsFull, Is.EqualTo(replay.IsFull));
            }
        }

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
