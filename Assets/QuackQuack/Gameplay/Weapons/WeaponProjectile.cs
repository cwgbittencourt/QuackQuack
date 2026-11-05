using QuackQuack.Content.Weapons;
using QuackQuack.Gameplay.Player;
using QuackQuack.Systems.Pooling;
using UnityEngine;

namespace QuackQuack.Gameplay.Weapons
{
    public enum ProjectileMotion { Returning, Trap, Homing, Straight }

    // A pooled state/view, deliberately without Update or physics callbacks.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class WeaponProjectile : MonoBehaviour, IPoolLifecycle
    {
        private readonly int[] hitIds = new int[512];
        private int hitCount;
        public WeaponDefinition Definition { get; private set; }
        public WeaponLevel Stats { get; private set; }
        public ProjectileMotion Motion { get; private set; }
        public Vector2 Direction { get; internal set; }
        public float Age { get; internal set; }
        public QuackTarget Target { get; internal set; }
        public float HitRadius { get; private set; }
        internal void Begin(WeaponDefinition definition, WeaponLevel stats, ProjectileMotion motion,
            Vector2 position, Vector2 direction, QuackTarget target, float? hitRadius = null)
        {
            Definition = definition; Stats = stats; Motion = motion; Target = target;
            HitRadius = hitRadius ?? definition.HitRadius;
            Direction = direction; transform.position = position; Age = 0; hitCount = 0;
            var renderer = GetComponent<SpriteRenderer>();
            renderer.sprite = definition.ProjectileSprite;
            transform.localScale = Vector3.one;
            if (definition.Behavior == WeaponBehavior.Bubbles && renderer.sprite != null)
            {
                var size = renderer.sprite.bounds.size;
                var scale = transform.lossyScale;
                // CircleCast uses world units; compensate the sprite's units and pool hierarchy.
                transform.localScale = new Vector3(2 * HitRadius / (size.x * Mathf.Abs(scale.x)),
                    2 * HitRadius / (size.y * Mathf.Abs(scale.y)), 1);
            }
        }
        internal bool MarkHit(int id)
        {
            for (int i = 0; i < hitCount; i++) if (hitIds[i] == id) return false;
            if (hitCount == hitIds.Length) return false;
            hitIds[hitCount++] = id;
            return true;
        }
        public void OnAcquire() { }
        public void OnRelease() { }
        public void ResetState()
        {
            Definition = null; Stats = default; Target = null; Direction = Vector2.zero;
            Age = 0; hitCount = 0; HitRadius = 0;
            transform.localScale = Vector3.one;
            GetComponent<SpriteRenderer>().sprite = null;
        }
    }
}
