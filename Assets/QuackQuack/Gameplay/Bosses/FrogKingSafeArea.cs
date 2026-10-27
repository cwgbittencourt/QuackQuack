using UnityEngine;

namespace QuackQuack.Gameplay.Bosses
{
    // Assigned arena refuges, distinct from screen/UI Safe Area.
    [RequireComponent(typeof(Collider2D))]
    public sealed class FrogKingSafeArea : MonoBehaviour
    {
        public bool IsDestroyed { get; private set; }
        public Vector2 Center => GetComponent<Collider2D>().bounds.center;
        public bool Contains(Vector2 position) => !IsDestroyed && isActiveAndEnabled && GetComponent<Collider2D>().OverlapPoint(position);
        public void DestroyArea()
        {
            if (IsDestroyed) return;
            IsDestroyed = true;
            GetComponent<Collider2D>().enabled = false;
            var visual = GetComponent<Renderer>(); if (visual != null) visual.enabled = false;
        }
    }
}
