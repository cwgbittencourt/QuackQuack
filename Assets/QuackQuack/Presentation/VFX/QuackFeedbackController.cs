using System;
using UnityEngine;

namespace QuackQuack.Presentation.VFX
{
    [RequireComponent(typeof(LineRenderer), typeof(AudioSource))]
    public sealed class QuackFeedbackController : MonoBehaviour
    {
        public Action Finished;
        private LineRenderer ring;
        private AudioSource audioSource;
        private float age, radius, duration;
        private Color tint;
        private SpriteRenderer follow;
        private bool outline;
        private static readonly Vector3[] circle = CreateCircle();
        public bool ExternallyDriven { get; set; }
        public bool IsPlaying { get; private set; }
        public bool IsOutline => outline && IsPlaying;
        public float Age => age;
        public void Initialize(Material material)
        {
            ring = GetComponent<LineRenderer>();
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false; audioSource.loop = false; audioSource.spatialBlend = 0;
            ring.sharedMaterial = material;
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 0;
            ring.sortingOrder = 15;
            ring.enabled = false;
        }
        public void Play(Vector2 position, float size, Color color, float seconds)
        {
            if (float.IsNaN(size) || float.IsInfinity(size) || size < 0 ||
                float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            transform.position = position;
            radius = size; tint = color; duration = seconds; age = 0;
            follow = null; outline = false; IsPlaying = true;
            ring.positionCount = circle.Length; ring.enabled = true;
            Draw();
        }

        public void PlayOutline(SpriteRenderer target, float seconds)
        {
            Play(target.bounds.center, Mathf.Max(target.bounds.extents.x, target.bounds.extents.y) + 0.08f, Color.white, seconds);
            follow = target; outline = true; Draw();
        }

        public void PlaySound(AudioClip clip, float volume)
        {
            if (clip == null) return;
            audioSource.clip = clip; audioSource.volume = Mathf.Clamp01(volume); audioSource.Play();
        }

        public void PauseAudio(bool paused) { if (paused) audioSource.Pause(); else if (IsPlaying) audioSource.UnPause(); }

        public void ResetEffect()
        {
            age = radius = duration = 0; IsPlaying = false; follow = null; outline = false; Finished = null;
            if (ring != null) { ring.enabled = false; ring.positionCount = 0; ring.startColor = ring.endColor = Color.clear; }
            if (audioSource != null) { audioSource.Stop(); audioSource.clip = null; }
        }

        private void Update() { if (!ExternallyDriven) Advance(Time.deltaTime); }

        public void Advance(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (!IsPlaying) return;
            if (outline && (follow == null || !follow.gameObject.activeInHierarchy)) follow = null;
            age += seconds;
            if (age >= duration) { Finish(); return; }
            Draw();
        }

        private void Draw()
        {
            float t = Mathf.Clamp01(age / duration);
            if (outline && follow != null) transform.position = follow.bounds.center;
            ring.startWidth = ring.endWidth = outline ? 0.07f : Mathf.Lerp(0.14f, 0.025f, t);
            var color = tint; color.a = 1 - t;
            ring.startColor = ring.endColor = color;
            float size = radius * (outline ? 1 : Mathf.Lerp(0.15f, 1, t));
            for (int i = 0; i < circle.Length; i++) ring.SetPosition(i, circle[i] * size);
        }

        private void Finish()
        {
            IsPlaying = false; ring.enabled = false; audioSource.Stop();
            Finished?.Invoke();
        }

        private static Vector3[] CreateCircle()
        {
            var points = new Vector3[48];
            for (int i = 0; i < points.Length; i++)
            {
                float angle = i * Mathf.PI * 2 / points.Length;
                points[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
            }
            return points;
        }
    }
}
