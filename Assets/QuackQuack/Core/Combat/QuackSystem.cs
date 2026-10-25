using System.Collections.Generic;
using UnityEngine;

namespace QuackQuack.Core.Combat
{
    public readonly struct QuackHit
    {
        public float Damage { get; }
        public float Knockback { get; }
        public float InterruptSeconds { get; }
        public bool Critical { get; }
        public QuackHit(float damage, float knockback, float interruptSeconds, bool critical)
        { Damage = damage; Knockback = knockback; InterruptSeconds = interruptSeconds; Critical = critical; }
    }
    public sealed class QuackSystem
    {
        private readonly HashSet<int> hits = new HashSet<int>(512);
        private readonly HashSet<int> collisions = new HashSet<int>(512);
        public long WaveId { get; private set; }
        public void BeginWave(long id) { WaveId = id; hits.Clear(); collisions.Clear(); }
        public bool TryHit(int target, bool interruptible, bool projectile, bool enhanced, float damage, float push, float interrupt, float fullMultiplier, out QuackHit hit)
        {
            hit = default;
            if (!hits.Add(target)) return false;
            float multiplier = enhanced ? fullMultiplier : 1;
            hit = new QuackHit(projectile ? 0 : damage * multiplier, push * multiplier,
                interruptible ? interrupt * multiplier : 0, enhanced);
            return true;
        }
        public bool TryCollision(int target) => collisions.Add(target);
        public static bool CanReflect(bool light, bool reflectable) => light && reflectable;
        public static Vector2 ReflectedVelocity(Vector2 position, Vector2 source, float speed)
        {
            var direction = position - source;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector2.up;
            return direction.normalized * speed;
        }
    }
}
