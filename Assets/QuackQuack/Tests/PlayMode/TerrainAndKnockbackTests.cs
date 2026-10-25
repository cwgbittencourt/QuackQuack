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
