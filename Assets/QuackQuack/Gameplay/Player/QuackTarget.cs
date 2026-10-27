using QuackQuack.Systems.Pooling;
using UnityEngine;
using System.Collections.Generic;

namespace QuackQuack.Gameplay.Player
{
    [RequireComponent(typeof(CircleCollider2D), typeof(Rigidbody2D))]
    public sealed class QuackTarget : MonoBehaviour, IPoolLifecycle
    {
        public bool IsProjectile { get; set; }
        public bool Light { get; set; } = true;
        public bool Reflectable { get; set; } = true;
        public bool Reflected { get; set; }
        public bool Interruptible { get; set; } = true;
        private float health = 20;
        private System.Func<float> readHealth;
        private System.Func<float, int, long, Vector2, bool> damage;
        private System.Action<Vector2, float> control;
        public bool HasCombatBinding => damage != null;
        public float Health { get => readHealth == null ? health : readHealth(); set => health = value; }
        public void BindCombat(System.Func<float> read, System.Func<float, int, long, Vector2, bool> apply,
            System.Action<Vector2, float> applyControl)
        { readHealth = read; damage = apply; control = applyControl; }
        public bool ApplyBoundDamage(float amount, int source, long tick, Vector2 attacker) => damage != null && damage(amount, source, tick, attacker);
        public void ApplyBoundControl(Vector2 push, float interruption) => control?.Invoke(push, interruption);
        public float InterruptedUntil { get; set; }
        public Vector2 Velocity { get; set; }
        private readonly RaycastHit2D[] contacts = new RaycastHit2D[16];
        private static readonly Dictionary<int, QuackTarget> activeTargets = new Dictionary<int, QuackTarget>(512);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearRegistry() { activeTargets.Clear(); }
        public static bool TryFind(int id, out QuackTarget target) => activeTargets.TryGetValue(id, out target);
        private void OnEnable() { activeTargets[GetInstanceID()] = this; }
        private void OnDisable() { activeTargets.Remove(GetInstanceID()); }
        public bool ApplyKnockback(Vector2 displacement)
        {
            var collider = GetComponent<CircleCollider2D>();
            var filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = 1 << 8 };
            float distance = displacement.magnitude;
            int count = collider.Cast(displacement.normalized, filter, contacts, distance);
            bool collided = false;
            for (int i = 0; i < count; i++)
                if (contacts[i].distance < distance) { distance = Mathf.Max(0, contacts[i].distance - 0.03f); collided = true; }
            transform.position += (Vector3)(displacement.normalized * distance);
            return collided;
        }
        public void OnAcquire() { }
        public void OnRelease() { }
        public void ResetState() { Health = 20; InterruptedUntil = 0; Velocity = Vector2.zero; Reflected = false; }
    }
}
