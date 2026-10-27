using QuackQuack.Systems.Pooling;
using UnityEngine;

namespace QuackQuack.Gameplay.Bosses
{
    // Advanced only by the boss scheduler; never an Update per summoned entity.
    [RequireComponent(typeof(LineRenderer))]
    public sealed class FrogKingHazard : MonoBehaviour, IPoolLifecycle
    {
        public bool IsWave { get; private set; }
        public float Age { get; private set; }
        public float Radius { get; private set; }
        private float speed, lifetime;
        private LineRenderer line;
        public void Configure(bool wave, Vector2 position, float movement, float radius, float duration)
        {
            IsWave = wave; speed = movement; Radius = radius; lifetime = duration; Age = 0;
            transform.position = position;
            line = GetComponent<LineRenderer>(); line.useWorldSpace = false; line.loop = true;
            line.positionCount = 32; line.startWidth = line.endWidth = wave ? 0.12f : 0.08f;
            line.startColor = line.endColor = wave ? Color.cyan : new Color(0.8f, 0.3f, 1);
            line.enabled = true; Draw();
        }
        public bool Step(float seconds, Vector2 target, float width, out bool touches)
        {
            float step = Mathf.Min(seconds, Mathf.Max(0, lifetime - Age));
            Vector2 before = transform.position;
            float oldRadius = Radius;
            if (IsWave) Radius += speed * step;
            else transform.position = Vector2.MoveTowards(before, target, speed * step);
            Age += seconds;
            float distance = Vector2.Distance(target, transform.position);
            if (IsWave) touches = distance >= oldRadius - width && distance <= Radius + width;
            else
            {
                Vector2 delta = (Vector2)transform.position - before;
                float t = delta.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(target - before, delta) / delta.sqrMagnitude) : 0;
                touches = Vector2.Distance(target, before + t * delta) <= Radius;
            }
            Draw(); return Age < lifetime;
        }
        private void Draw()
        {
            for (int i = 0; i < 32; i++)
            {
                float a = i * Mathf.PI * 2 / 32;
                line.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * Radius);
            }
        }
        public void OnAcquire() { }
        public void OnRelease() { }
        public void ResetState()
        {
            if (line == null) line = GetComponent<LineRenderer>();
            line.enabled = false; line.positionCount = 0;
            Age = Radius = speed = lifetime = 0; IsWave = false; transform.localPosition = Vector3.zero;
        }
    }
}
