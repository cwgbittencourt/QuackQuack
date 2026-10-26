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
        internal void Begin(WeaponDefinition definition, WeaponLevel stats, ProjectileMotion motion,
            Vector2 position, Vector2 direction, QuackTarget target)
        {
            Definition = definition; Stats = stats; Motion = motion; Target = target;
            Direction = direction; transform.position = position; Age = 0; hitCount = 0;
            GetComponent<SpriteRenderer>().sprite = definition.ProjectileSprite;
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
            Age = 0; hitCount = 0;
            GetComponent<SpriteRenderer>().sprite = null;
        }
    }
}
