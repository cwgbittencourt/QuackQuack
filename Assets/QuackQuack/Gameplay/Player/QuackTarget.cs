using QuackQuack.Systems.Pooling;
using UnityEngine;

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
        public float Health { get; set; } = 20;
        public float InterruptedUntil { get; set; }
        public Vector2 Velocity { get; set; }
        private readonly RaycastHit2D[] contacts = new RaycastHit2D[16];
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
