using System;
using UnityEngine;

namespace QuackQuack.Presentation.Weapons
{
    // Bounded persistent views driven by the runner's simulation clock, without Update.
    public sealed class WeaponVisuals : MonoBehaviour
    {
        private SpriteRenderer[] auras;

        public void Initialize(int slots)
        {
            if (auras != null) throw new InvalidOperationException("Weapon visuals are already initialized.");
            if (slots <= 0) throw new ArgumentOutOfRangeException(nameof(slots));
            auras = new SpriteRenderer[slots];
            for (int i = 0; i < slots; i++)
            {
                var view = new GameObject("Aura " + i);
                view.transform.SetParent(transform, false);
                auras[i] = view.AddComponent<SpriteRenderer>();
                auras[i].sortingOrder = -1;
                auras[i].enabled = false;
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
            if (auras == null) return;
            for (int i = 0; i < auras.Length; i++) if (auras[i] != null) auras[i].enabled = false;
        }
        private void OnDisable() => HideAll();
    }
}
