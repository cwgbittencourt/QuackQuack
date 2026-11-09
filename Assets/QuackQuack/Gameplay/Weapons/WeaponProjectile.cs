using QuackQuack.Content.Weapons;
using QuackQuack.Gameplay.Player;
using QuackQuack.Systems.Pooling;
using UnityEngine;

namespace QuackQuack.Gameplay.Weapons
{
    public enum ProjectileMotion { Returning, Trap, Homing, Straight }
    public enum ProjectilePassage { Outbound, Inbound }

    // A pooled state/view, deliberately without Update or physics callbacks.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class WeaponProjectile : MonoBehaviour, IPoolLifecycle
    {
        private readonly int[] hitIds = new int[512];
        private readonly long[] hitLeases = new long[512];
        private int hitCount;
        private long emissionSequence;
        private SpriteRenderer image;
        private Material fallbackMaterial;
        private void Awake(){image=GetComponent<SpriteRenderer>();fallbackMaterial=image.sharedMaterial;}
        public long LeaseId { get; private set; }
        public ProjectilePassage Passage { get; private set; }
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
            LeaseId = ++emissionSequence;
            Passage = ProjectilePassage.Outbound;
            HitRadius = hitRadius ?? definition.HitRadius;
            Direction = direction; transform.position = position; Age = 0; hitCount = 0;
            var renderer = GetComponent<SpriteRenderer>();
            renderer.sprite = definition.ProjectileSprite;
            renderer.sharedMaterial=definition.ProjectileMaterial!=null?definition.ProjectileMaterial:fallbackMaterial;
            renderer.color=Color.white;renderer.sortingOrder=14;
            transform.rotation=Quaternion.identity;
            transform.localScale = Vector3.one;
            if (definition.Behavior == WeaponBehavior.Bubbles && renderer.sprite != null)
            {
                var size = renderer.sprite.bounds.size;
                var scale = transform.lossyScale;
                // CircleCast uses world units; compensate the sprite's units and pool hierarchy.
                transform.localScale = new Vector3(2 * HitRadius / (size.x * Mathf.Abs(scale.x)),
                    2 * HitRadius / (size.y * Mathf.Abs(scale.y)), 1);
            }
            else if(renderer.sprite!=null)
            {
                var size=renderer.sprite.bounds.size;var scale=transform.lossyScale;
                float diameter=2*HitRadius;
                float factor=diameter/Mathf.Max(size.x*Mathf.Abs(scale.x),size.y*Mathf.Abs(scale.y));
                transform.localScale=Vector3.one*factor;
            }
            Present();
        }
        internal void Present()
        {
            float angle=Motion==ProjectileMotion.Returning?Age*720:
                Motion==ProjectileMotion.Homing?Mathf.Atan2(Direction.y,Direction.x)*Mathf.Rad2Deg:0;
            transform.rotation=Quaternion.Euler(0,0,angle);
        }
        internal void BeginInbound()
        {
            if (Motion != ProjectileMotion.Returning || Passage == ProjectilePassage.Inbound) return;
            Passage = ProjectilePassage.Inbound;
            hitCount = 0;
        }
        internal bool MarkHit(int id, long targetLease = 0)
        {
            for (int i = 0; i < hitCount; i++) if (hitIds[i] == id && hitLeases[i] == targetLease) return false;
            if (hitCount == hitIds.Length) return false;
            hitIds[hitCount] = id;
            hitLeases[hitCount++] = targetLease;
            return true;
        }
        public void OnAcquire() { }
        public void OnRelease() { }
        public void ResetState()
        {
            Definition = null; Stats = default; Target = null; Direction = Vector2.zero;
            Age = 0; hitCount = 0; HitRadius = 0;
            LeaseId = 0; Passage = ProjectilePassage.Outbound;
            transform.localScale = Vector3.one;
            transform.rotation=Quaternion.identity;
            GetComponent<SpriteRenderer>().sprite = null;
            if(image!=null){image.sharedMaterial=fallbackMaterial;image.color=Color.white;}
        }
    }
}
