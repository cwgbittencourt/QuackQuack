using NUnit.Framework;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Terrain;
using QuackQuack.Gameplay.Player;
using UnityEngine;

namespace QuackQuack.Tests.PlayMode
{
    public class TerrainAndKnockbackTests
    {
        [Test]
        public void OverlappingLandAndWaterResolveOneTerrainIndependentOfOrder()
        {
            var lake = new GameObject("lake");
            var island = new GameObject("island");
            try
            {
                var water = lake.AddComponent<TerrainZone>();
                var land = island.AddComponent<TerrainZone>();
                water.Configure(new Vector2(10, 10), 0.2f);
                land.Configure(new Vector2(2, 2), 0.2f, TerrainType.Land);
                Assert.That(TerrainZone.Resolve(new[] { water, land }, Vector2.zero, TerrainType.Water), Is.EqualTo(TerrainType.Land));
                Assert.That(TerrainZone.Resolve(new[] { land, water }, Vector2.zero, TerrainType.Water), Is.EqualTo(TerrainType.Land));
                water.Configure(new Vector2(10, 10), 0.2f, TerrainType.Water, 1);
                Assert.That(TerrainZone.Resolve(new[] { land, water }, Vector2.zero, TerrainType.Land), Is.EqualTo(TerrainType.Water));
                water.enabled = false;
                land.enabled = false;
                Assert.That(TerrainZone.Resolve(new[] { water, land, null }, Vector2.zero, TerrainType.Water), Is.EqualTo(TerrainType.Land));
                Assert.That(TerrainZone.Resolve(null, Vector2.zero, TerrainType.Water), Is.EqualTo(TerrainType.Land));
            }
            finally { Object.DestroyImmediate(lake); Object.DestroyImmediate(island); }
        }

        [Test]
        public void TransformedVolumeUsesLocalBoundsAndHysteresis()
        {
            var obj = new GameObject("transformed water");
            try
            {
                var zone = obj.AddComponent<TerrainZone>();
                zone.Configure(new Vector2(4, 4), 0.2f);
                obj.transform.position = new Vector2(10, 5);
                obj.transform.rotation = Quaternion.Euler(0, 0, 30);
                obj.transform.localScale = new Vector3(2, 3, 1);
                Vector2 point = obj.transform.TransformPoint(new Vector2(1.9f, 0));
                Assert.That(zone.Contains(point, TerrainType.Land), Is.False);
                Assert.That(zone.Contains(point, TerrainType.Water), Is.True);
                Assert.That(zone.Contains(new Vector2(float.NaN, 0), TerrainType.Water), Is.False);
            }
            finally { Object.DestroyImmediate(obj); }
        }

        [Test]
        public void RepeatedTerrainAffinityRecomputesFromBaseWithoutAccumulation()
        {
            const float authoredBase = 10;
            for (int i = 0; i < 100; i++)
            {
                Assert.That(TerrainAffinity.Apply(authoredBase, TerrainType.Water, 1, 1.5f), Is.EqualTo(15));
                Assert.That(TerrainAffinity.Apply(authoredBase, TerrainType.Land, 1, 1.5f), Is.EqualTo(10));
                Assert.That(TerrainAffinity.Apply(authoredBase, TerrainType.Water), Is.EqualTo(10));
            }
        }

        [Test]
        public void DisabledVolumeDoesNotContainPoints()
        {
            var obj = new GameObject("disabled water");
            try
            {
                var zone = obj.AddComponent<TerrainZone>();
                zone.enabled = false;
                Assert.That(zone.Contains(Vector2.zero, TerrainType.Land), Is.False);
            }
            finally { Object.DestroyImmediate(obj); }
        }

        [Test]
        public void InvalidGeometryIsRejectedBeforeReplacingValidVolume()
        {
            var obj = new GameObject("water");
            try
            {
                var zone = obj.AddComponent<TerrainZone>();
                zone.Configure(new Vector2(4, 4), 0.2f);
                Assert.Throws<System.ArgumentOutOfRangeException>(() => zone.Configure(Vector2.zero, 0));
                Assert.Throws<System.ArgumentOutOfRangeException>(() => zone.Configure(new Vector2(4, 4), float.NaN));
                Assert.That(zone.Contains(Vector2.zero, TerrainType.Land), Is.True);
            }
            finally { Object.DestroyImmediate(obj); }
        }

        [Test]
        public void AffinityRejectsInvalidTerrainAndNonFiniteMultipliers()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => TerrainAffinity.Apply(10, (TerrainType)99));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => TerrainAffinity.Apply(10, TerrainType.Water, 1, float.NaN));
        }

        [Test]
        public void ShorelineHysteresisPreventsOscillationAndAffinityDoesNotMutateBase()
        {
            var obj = new GameObject("water");
            var zone = obj.AddComponent<TerrainZone>();
            try
            {
                zone.Configure(new Vector2(4, 4), 0.2f);
                Assert.That(zone.Contains(new Vector2(1.9f, 0), TerrainType.Land), Is.False);
                Assert.That(zone.Contains(new Vector2(1.7f, 0), TerrainType.Land), Is.True);
                Assert.That(zone.Contains(new Vector2(2.1f, 0), TerrainType.Water), Is.True);
                Assert.That(zone.Contains(new Vector2(2.3f, 0), TerrainType.Water), Is.False);
                Assert.That(TerrainAffinity.Apply(10, TerrainType.Water, 1, 1.5f), Is.EqualTo(15));
                Assert.That(TerrainAffinity.Apply(10, TerrainType.Land, 1, 1.5f), Is.EqualTo(10));
                Assert.That(TerrainAffinity.Apply(10, TerrainType.Water), Is.EqualTo(10), "Equipment without affinity keeps its base value.");
                var current = TerrainType.Water;
                for (int i = 0; i < 100; i++)
                    current = zone.Contains(new Vector2(i % 2 == 0 ? 1.99f : 2.01f, 0), current) ? TerrainType.Water : TerrainType.Land;
                Assert.That(current, Is.EqualTo(TerrainType.Water));
            }
            finally { Object.DestroyImmediate(obj); }
        }

        [Test]
        public void KnockbackStopsBeforeObstacleAndRiskRewardIsOneShot()
        {
            var wall = new GameObject("wall");
            wall.layer = 8;
            wall.transform.position = Vector2.right * 2;
            wall.AddComponent<BoxCollider2D>();
            var target = new GameObject("target");
            try
            {
                target.AddComponent<CircleCollider2D>().radius = 0.3f;
                var body = target.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                var actor = target.AddComponent<QuackTarget>();
                Physics2D.SyncTransforms();
                Assert.That(actor.ApplyKnockback(Vector2.right * 4), Is.True);
                Assert.That(target.transform.position.x, Is.LessThan(1.3f));
                Assert.That(target.transform.position.x, Is.GreaterThan(0), "A collision must still allow movement up to the wall.");
                Physics2D.SyncTransforms();
                float before = target.transform.position.x;
                actor.ApplyKnockback(Vector2.left * 0.5f);
                Assert.That(target.transform.position.x, Is.LessThan(before), "The target must not remain stuck against the wall.");
                var reward = new RiskRewardState();
                Assert.That(reward.TryClaim(), Is.True);
                Assert.That(reward.TryClaim(), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(wall);
            }
        }
    }
}
