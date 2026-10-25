using NUnit.Framework;
using QuackQuack.Systems.Pooling;
using UnityEngine;

namespace QuackQuack.Tests.PlayMode
{
    public sealed class PoolProbe : MonoBehaviour, IPoolLifecycle
    {
        public int Value;
        public int ResetCount;
        public void OnAcquire() { }
        public void OnRelease() { }
        public void ResetState() { Value = 0; ResetCount++; }
    }

    public class PoolServiceTests
    {
        [TestCase(PoolFamily.Enemy)]
        [TestCase(PoolFamily.Projectile)]
        [TestCase(PoolFamily.Pickup)]
        [TestCase(PoolFamily.Vfx)]
        public void BoundedPoolNeverSharesActiveInstanceAndResetsOnReturn(PoolFamily family)
        {
            using (var pool = new PoolService())
            {
                pool.Register(family.ToString(), family, () => new GameObject("probe").AddComponent<PoolProbe>(), 1);
                var first = pool.Acquire<PoolProbe>(family.ToString());
                first.Value = 99;
                Assert.That(first.gameObject.activeSelf, Is.True);
                Assert.That(pool.Acquire<PoolProbe>(family.ToString()), Is.Null);
                Assert.That(pool.Release(first), Is.True);
                Assert.That(pool.Release(first), Is.False);
                Assert.That(first.Value, Is.Zero);
                Assert.That(first.gameObject.activeSelf, Is.False);
                var reused = pool.Acquire<PoolProbe>(family.ToString());
                Assert.That(reused, Is.SameAs(first));
                Assert.That(reused.ResetCount, Is.GreaterThanOrEqualTo(1));
                Assert.That(pool.ActiveCount, Is.EqualTo(1));
            }
        }
    }
}
