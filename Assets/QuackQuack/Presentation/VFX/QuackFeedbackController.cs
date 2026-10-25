using System;
using UnityEngine;

namespace QuackQuack.Presentation.VFX
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class QuackFeedbackController : MonoBehaviour
    {
        public Action Finished;
        private LineRenderer ring;
        private float age, radius, duration;
        private Color tint;
        public void Initialize(Material material)
        {
            ring = GetComponent<LineRenderer>();
            ring.sharedMaterial = material;
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 48;
            ring.sortingOrder = 15;
        }
        public void Play(Vector2 position, float size, Color color, float seconds)
        {
            transform.position = position;
            radius = size; tint = color; duration = seconds; age = 0;
        }
        public void ResetEffect() { age = 0; Finished = null; }
        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / duration);
            ring.startWidth = ring.endWidth = Mathf.Lerp(0.14f, 0.025f, t);
            var color = tint; color.a = 1 - t;
            ring.startColor = ring.endColor = color;
            for (int i = 0; i < 48; i++)
            {
                float angle = i * Mathf.PI * 2 / 48;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius * Mathf.Lerp(0.15f, 1, t));
            }
            if (t >= 1) Finished?.Invoke();
        }
    }
}
