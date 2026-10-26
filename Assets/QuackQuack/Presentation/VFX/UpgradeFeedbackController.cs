using System;
using UnityEngine;

namespace QuackQuack.Presentation.VFX
{
    public sealed class UpgradeFeedbackController : MonoBehaviour
    {
        [SerializeField] private QuackFeedbackController ring;
        [SerializeField] private LineRenderer accent;
        [SerializeField] private TextMesh label;
        [SerializeField] private Color evolutionColor = new Color(1, 0.83f, 0.25f), rareColor = new Color(0.75f, 0.45f, 1);
        private static readonly Vector3[] star = Shape(16, true), diamond = Shape(4, false);
        private float radius, duration;
        private Color color;
        public Action Finished;
        public float Age { get; private set; }
        public bool IsPlaying { get; private set; }
        public bool IsEvolution { get; private set; }
        public string Label => label.text;
        public AudioClip ActiveClip => ring.GetComponent<AudioSource>().clip;
        public void Initialize()
        { ring.Initialize(ring.GetComponent<LineRenderer>().sharedMaterial); ring.ExternallyDriven = true; ResetEffect(); }
        public void Play(bool evolution, Vector2 position, float size, float seconds, AudioClip clip, float volume)
        {
            transform.position = position; radius = size; duration = seconds; Age = 0;
            IsEvolution = evolution; color = evolution ? evolutionColor : rareColor; IsPlaying = true;
            ring.Play(position, size, color, seconds); ring.PlaySound(clip, volume);
            label.text = evolution ? "EVOLUÇÃO!" : "RECOMPENSA RARA"; label.gameObject.SetActive(true);
            accent.enabled = true; Draw();
        }
        public void PauseAudio(bool paused) => ring.PauseAudio(paused);
        public void Advance(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (!IsPlaying) return;
            Age += seconds; ring.Advance(seconds);
            if (Age >= duration)
            { IsPlaying = false; accent.enabled = false; label.gameObject.SetActive(false); Finished?.Invoke(); return; }
            Draw();
        }
        private void Draw()
        {
            float t = Mathf.Clamp01(Age / duration);
            var points = IsEvolution ? star : diamond;
            accent.positionCount = points.Length;
            float size = radius * Mathf.Lerp(0.2f, 0.8f, t);
            for (int i = 0; i < points.Length; i++) accent.SetPosition(i, points[i] * size);
            var tint = color; tint.a = 1 - t;
            accent.startColor = accent.endColor = tint; label.color = tint;
            label.transform.localPosition = Vector3.up * (0.7f + t * 0.7f);
        }
        public void ResetEffect()
        {
            IsPlaying = IsEvolution = false; Age = radius = duration = 0; Finished = null;
            ring.ResetEffect(); accent.positionCount = 0; accent.enabled = false;
            label.text = ""; label.gameObject.SetActive(false);
        }
        private static Vector3[] Shape(int count, bool star)
        {
            var result = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2 / count;
                result[i] = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle)) * (star && i % 2 == 1 ? 0.4f : 1);
            }
            return result;
        }
    }
}
