using System;
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
        private readonly Dictionary<int, QuackHit> hits = new Dictionary<int, QuackHit>(512);
        private readonly HashSet<int> projectiles = new HashSet<int>(512);
        private readonly HashSet<int> collisions = new HashSet<int>(512);
        private readonly HashSet<int> emittedHits = new HashSet<int>(512);
        private readonly HashSet<int> emittedCollisions = new HashSet<int>(512);
        public long WaveId { get; private set; }
        public event Action<CombatEvent> Resolved;

        public void BeginWave(long id)
        {
            if (id <= 0 || id < WaveId) throw new ArgumentOutOfRangeException(nameof(id));
            if (id == WaveId) return;
            WaveId = id;
            hits.Clear(); projectiles.Clear(); collisions.Clear();
            emittedHits.Clear(); emittedCollisions.Clear();
        }
        public bool TryHit(int target, bool interruptible, bool projectile, bool enhanced, float damage, float push, float interrupt, float fullMultiplier, out QuackHit hit)
        {
            hit = default;
            if (WaveId == 0 || hits.ContainsKey(target) ||
                !NonNegative(damage) || !NonNegative(push) || !NonNegative(interrupt) ||
                !NonNegative(fullMultiplier) || fullMultiplier <= 1) return false;
            float multiplier = enhanced ? fullMultiplier : 1;
            float resolvedDamage = projectile ? 0 : damage * multiplier;
            float resolvedPush = projectile ? 0 : push * multiplier;
            float resolvedInterrupt = !projectile && interruptible ? interrupt * multiplier : 0;
            if (!NonNegative(resolvedDamage) || !NonNegative(resolvedPush) || !NonNegative(resolvedInterrupt)) return false;
            hit = new QuackHit(resolvedDamage, resolvedPush, resolvedInterrupt, !projectile && enhanced);
            hits.Add(target, hit);
            if (projectile) projectiles.Add(target);
            return true;
        }
        public bool TryCollision(int target) => hits.ContainsKey(target) && !projectiles.Contains(target) && collisions.Add(target);

        /// <summary>
        /// Call after applying the accepted hit (or eligible reflection) to runtime state.
        /// Subscribe the event bus here instead of publishing these events again in the caller.
        /// </summary>
        public bool TryEmitHitEvents(int target, long tick, int source, Vector2 position)
        {
            if (!hits.TryGetValue(target, out var hit) || !emittedHits.Add(target)) return false;
            long wave = WaveId;
            if (projectiles.Contains(target))
                Emit(CombatEventType.ProjectileReflected, 0, wave, tick, source, target, position);
            else
            {
                Emit(CombatEventType.DamageApplied, hit.Damage, wave, tick, source, target, position);
                if (hit.Critical) Emit(CombatEventType.CriticalHit, hit.Damage, wave, tick, source, target, position);
                if (hit.InterruptSeconds > 0) Emit(CombatEventType.EffectApplied, hit.InterruptSeconds, wave, tick, source, target, position);
            }
            return true;
        }

        private void Emit(CombatEventType type, float amount, long wave, long tick, int source, int target, Vector2 position) =>
            Resolved?.Invoke(new CombatEvent(tick, wave, source, target, position, amount, type));

        /// <summary>Call only after physics confirms a collision for an accepted hit.</summary>
        public bool TryEmitCollisionEvent(int target, long tick, int source, Vector2 position)
        {
            if (!collisions.Contains(target) || !emittedCollisions.Add(target)) return false;
            Resolved?.Invoke(new CombatEvent(tick, WaveId, source, target, position, 0, CombatEventType.CollisionTriggered));
            return true;
        }

        private static bool NonNegative(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0;
        public static bool CanReflect(bool light, bool reflectable) => light && reflectable;
        public static Vector2 ReflectedVelocity(Vector2 position, Vector2 source, float speed)
        {
            var direction = position - source;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector2.up;
            return direction.normalized * speed;
        }
    }
}
