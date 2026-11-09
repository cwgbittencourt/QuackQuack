using System;
using QuackQuack.Core.Run;
using UnityEngine;

namespace QuackQuack.Presentation.Terrain
{
    // A single driver for collider-free surface marks. The run owns time, including pause.
    public sealed class LakeWaterAnimation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] ripples = Array.Empty<SpriteRenderer>();
        private Vector3[] positions, scales;
        private Color[] colors;
        private RunState run;
        private double previous = -1;
        public bool IsBound => run != null;
        public int RippleCount => ripples.Length;
        public void Configure(SpriteRenderer[] views)
        {
            Restore(); ripples = views ?? Array.Empty<SpriteRenderer>();
            positions = null; Cache(); previous = -1;
        }
        public void Bind(RunState state) { run = state; Cache(); previous = -1; Render(state?.ElapsedSeconds ?? 0); }
        private void Awake() => Cache();
        private void Cache()
        {
            if (positions != null) return;
            positions = new Vector3[ripples.Length]; scales = new Vector3[ripples.Length]; colors = new Color[ripples.Length];
            for (int i = 0; i < ripples.Length; i++) if (ripples[i] != null)
            { positions[i] = ripples[i].transform.localPosition; scales[i] = ripples[i].transform.localScale; colors[i] = ripples[i].color; }
        }
        private void LateUpdate() { if (run != null) Render(run.ElapsedSeconds); }
        public void Render(double simulationSeconds)
        {
            if (double.IsNaN(simulationSeconds) || double.IsInfinity(simulationSeconds) || simulationSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(simulationSeconds));
            if (simulationSeconds == previous) return;
            Cache(); previous = simulationSeconds;
            float time = (float)(simulationSeconds % (Math.PI * 2 / .65));
            for (int i = 0; i < ripples.Length; i++)
            {
                var image = ripples[i]; if (image == null) continue;
                float wave = Mathf.Sin(time * .65f + i * 2.399f);
                image.transform.localPosition = positions[i] + new Vector3(wave * .045f, Mathf.Cos(time * .65f + i) * .018f, 0);
                image.transform.localScale = scales[i] * (1 + wave * .025f);
                var color = colors[i]; color.a *= .85f + wave * .15f; image.color = color;
            }
        }
        private void Restore()
        {
            if (positions == null) return;
            for (int i = 0; i < ripples.Length; i++) if (ripples[i] != null)
            { ripples[i].transform.localPosition = positions[i]; ripples[i].transform.localScale = scales[i]; ripples[i].color = colors[i]; }
            previous = -1;
        }
        private void OnDisable() => Restore();
    }
}
