using System;
using UnityEngine;

namespace QuackQuack.Presentation.Weapons
{
    // Bounded persistent views driven by the runner's simulation clock, without Update.
    public sealed class WeaponVisuals : MonoBehaviour
    {
        private SpriteRenderer[] auras;
        private LineRenderer[] frontals;
        private Vector3[][] frontalPoints;
        private double[] frontalStarted;
        private const int FrontalSegments = 20;
        private const double FrontalDuration = 0.25;

        // Both circles share a scale; the diagram compares radii, not area percentages.
        public static float PreviewDiameter(float radius, float maxRadius, float maxPixels) =>
            2 * radius * (maxPixels / (2 * maxRadius));

        public void Initialize(int slots)
        {
            if (auras != null) throw new InvalidOperationException("Weapon visuals are already initialized.");
            if (slots <= 0) throw new ArgumentOutOfRangeException(nameof(slots));
            auras = new SpriteRenderer[slots];
            frontals = new LineRenderer[slots];
            frontalPoints = new Vector3[slots][];
            frontalStarted = new double[slots];
            for (int i = 0; i < slots; i++)
            {
                var view = new GameObject("Aura " + i);
                view.transform.SetParent(transform, false);
                auras[i] = view.AddComponent<SpriteRenderer>();
                auras[i].sortingOrder = -1;
                auras[i].enabled = false;
                var cone = new GameObject("Frontal " + i);
                cone.transform.SetParent(transform, false);
                var line = cone.AddComponent<LineRenderer>();
                line.sharedMaterial = auras[i].sharedMaterial;
                line.useWorldSpace = true; line.loop = false; line.sortingOrder = 14;
                line.startWidth = line.endWidth = 0.045f; line.enabled = false;
                frontals[i] = line; frontalPoints[i] = new Vector3[FrontalSegments + 3];
            }
        }

        public void PulseFrontal(int slot, Vector2 origin, Vector2 direction, float radius, float angle, double time)
        {
            if (!isActiveAndEnabled) return;
            var points = frontalPoints[slot];
            points[0] = origin; points[points.Length - 1] = origin;
            float facing = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            for (int i = 0; i <= FrontalSegments; i++)
            {
                float radians = (facing - angle * 0.5f + angle * i / FrontalSegments) * Mathf.Deg2Rad;
                points[i + 1] = origin + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
            }
            var line = frontals[slot]; line.positionCount = points.Length; line.SetPositions(points);
            line.startColor = line.endColor = new Color(1, 0.86f, 0.3f, 0.9f);
            frontalStarted[slot] = time; line.enabled = true;
        }

        public void Advance(double time)
        {
            if (frontals == null) return;
            for (int i = 0; i < frontals.Length; i++)
            {
                var line = frontals[i]; if (!line.enabled) continue;
                double elapsed = time - frontalStarted[i];
                if (elapsed >= FrontalDuration) { line.enabled = false; continue; }
                line.startColor = line.endColor = new Color(1, 0.86f, 0.3f, 0.9f * (1 - (float)(elapsed / FrontalDuration)));
            }
        }

        public void ShowAura(int slot, Vector2 position, float radius, Sprite sprite, double simulationTime)
        {
            if (!isActiveAndEnabled) return;
            if (auras == null || slot < 0 || slot >= auras.Length) throw new ArgumentOutOfRangeException(nameof(slot));
            if (float.IsNaN(radius) || float.IsInfinity(radius) || radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
            var view = auras[slot];
            if (sprite == null) { view.enabled = false; return; }
            view.sprite = sprite;
            view.transform.position = position;
            view.transform.localScale = Vector3.one;
            var parentScale = view.transform.lossyScale;
            var size = sprite.bounds.size;
            view.transform.localScale = new Vector3(2 * radius / (size.x * Mathf.Abs(parentScale.x)),
                2 * radius / (size.y * Mathf.Abs(parentScale.y)), 1);
            // Animate opacity, never the radius that communicates the damage boundary.
            float opacity = 0.14f + 0.04f * Mathf.Sin((float)(simulationTime % (Math.PI * 2)) * 2);
            view.color = new Color(0.35f, 0.85f, 1f, opacity);
            view.enabled = true;
        }

        public void HideAll()
        {
            HideAuras();
            if (frontals == null) return;
            for (int i = 0; i < frontals.Length; i++) if (frontals[i] != null) frontals[i].enabled = false;
        }
        public void HideAuras()
        {
            if (auras == null) return;
            for (int i = 0; i < auras.Length; i++) if (auras[i] != null) auras[i].enabled = false;
        }
        private void OnDisable() => HideAll();
    }
}
