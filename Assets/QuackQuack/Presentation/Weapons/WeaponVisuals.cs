using System;
using UnityEngine;

namespace QuackQuack.Presentation.Weapons
{
    // Bounded persistent views driven by the runner's simulation clock, without Update.
    public sealed class WeaponVisuals : MonoBehaviour
    {
        private SpriteRenderer[] auras;
        private LineRenderer[] frontals;
        private SpriteRenderer[] frontalArt;
        private Material defaultSpriteMaterial;
        private Vector3[][] frontalPoints;
        private double[] frontalStarted;
        private LineRenderer[] bursts;
        private Vector3[][] burstPoints;
        private double[] burstStarted;
        private bool[] rocketBursts;
        private int nextBurst;
        private const int BurstSegments = 48;
        private const double BurstDuration = 0.45;
        private const int FrontalSegments = 20;
        private const double FrontalDuration = 0.25;

        // Both circles share a scale; the diagram compares radii, not area percentages.
        public static float PreviewDiameter(float radius, float maxRadius, float maxPixels) =>
            2 * radius * (maxPixels / (2 * maxRadius));

        public void Initialize(int slots, int burstCapacity = 1)
        {
            if (auras != null) throw new InvalidOperationException("Weapon visuals are already initialized.");
            if (slots <= 0) throw new ArgumentOutOfRangeException(nameof(slots));
            if (burstCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(burstCapacity));
            auras = new SpriteRenderer[slots];
            frontals = new LineRenderer[slots];
            frontalArt = new SpriteRenderer[slots];
            frontalPoints = new Vector3[slots][];
            frontalStarted = new double[slots];
            for (int i = 0; i < slots; i++)
            {
                var view = new GameObject("Aura " + i);
                view.transform.SetParent(transform, false);
                auras[i] = view.AddComponent<SpriteRenderer>();
                auras[i].sortingOrder = -1;
                if(i==0)defaultSpriteMaterial=auras[i].sharedMaterial;
                auras[i].enabled = false;
                var cone = new GameObject("Frontal " + i);
                cone.transform.SetParent(transform, false);
                var line = cone.AddComponent<LineRenderer>();
                line.sharedMaterial = auras[i].sharedMaterial;
                line.useWorldSpace = true; line.loop = false; line.sortingOrder = 14;
                line.startWidth = line.endWidth = 0.045f; line.enabled = false;
                frontals[i] = line; frontalPoints[i] = new Vector3[FrontalSegments + 3];
                var art=new GameObject("Frontal art "+i);art.transform.SetParent(transform,false);
                frontalArt[i]=art.AddComponent<SpriteRenderer>();frontalArt[i].sortingOrder=13;frontalArt[i].enabled=false;
            }
            bursts = new LineRenderer[burstCapacity];
            burstPoints = new Vector3[burstCapacity][];
            burstStarted = new double[burstCapacity];
            rocketBursts = new bool[burstCapacity];
            for (int i = 0; i < burstCapacity; i++)
            {
                var view = new GameObject("Egg burst " + i);
                view.transform.SetParent(transform, false);
                var line = view.AddComponent<LineRenderer>();
                line.sharedMaterial = auras[0].sharedMaterial;
                line.useWorldSpace = true; line.loop = true; line.sortingOrder = 14;
                line.startWidth = line.endWidth = 0.055f; line.enabled = false;
                bursts[i] = line; burstPoints[i] = new Vector3[BurstSegments];
            }
        }

        public void PulseEggArea(Vector2 origin, float radius, double time, bool rocket=false)
        {
            if (!isActiveAndEnabled) return;
            int slot = nextBurst; nextBurst = (nextBurst + 1) % bursts.Length;
            var points = burstPoints[slot];
            for (int i = 0; i < BurstSegments; i++)
            {
                float angle = 2 * Mathf.PI * i / BurstSegments;
                points[i] = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * (rocket && i%2==1?.9f:1);
            }
            var line = bursts[slot]; line.positionCount = points.Length; line.SetPositions(points);
            rocketBursts[slot]=rocket;
            line.startColor = line.endColor = rocket?new Color(1,.3f,.15f,.95f):new Color(1, 0.65f, 0.2f, 0.95f);
            burstStarted[slot] = time; line.enabled = true;
        }

        public void PulseFrontal(int slot, Vector2 origin, Vector2 direction, float radius, float angle, double time,
            Sprite sprite=null,Material material=null)
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
            var art=frontalArt[slot];art.sprite=sprite;art.sharedMaterial=material!=null?material:defaultSpriteMaterial;
            art.enabled=sprite!=null;
            if(sprite!=null)
            {
                art.transform.position=origin+direction.normalized*radius*.65f;
                art.transform.rotation=Quaternion.Euler(0,0,facing);art.transform.localScale=Vector3.one;
                var size=sprite.bounds.size;var scale=art.transform.lossyScale;
                float height=radius*Mathf.Min(.6f,.8f*Mathf.Tan(Mathf.Min(angle,120)*Mathf.Deg2Rad*.5f));
                art.transform.localScale=new Vector3(radius*.4f/(size.x*Mathf.Abs(scale.x)),height/(size.y*Mathf.Abs(scale.y)),1);
                art.color=new Color(1,1,1,.8f);
            }
        }

        public void Advance(double time)
        {
            if (bursts != null)
                for (int i = 0; i < bursts.Length; i++)
                {
                    var line = bursts[i]; if (!line.enabled) continue;
                    double elapsed = time - burstStarted[i];
                    if (elapsed >= BurstDuration) { line.enabled = false; continue; }
                    // Keep the damage boundary fixed; only opacity animates.
                    var tint=rocketBursts[i]?new Color(1,.3f,.15f):new Color(1,.65f,.2f);
                    tint.a=.95f*(1-(float)(elapsed/BurstDuration));line.startColor=line.endColor=tint;
                }
            if (frontals == null) return;
            for (int i = 0; i < frontals.Length; i++)
            {
                var line = frontals[i]; if (!line.enabled) continue;
                double elapsed = time - frontalStarted[i];
                if (elapsed >= FrontalDuration) { line.enabled = false;frontalArt[i].enabled=false; continue; }
                line.startColor = line.endColor = new Color(1, 0.86f, 0.3f, 0.9f * (1 - (float)(elapsed / FrontalDuration)));
                frontalArt[i].color=new Color(1,1,1,.8f*(1-(float)(elapsed/FrontalDuration)));
            }
        }

        public void ShowAura(int slot, Vector2 position, float radius, Sprite sprite, double simulationTime,Material material=null)
        {
            if (!isActiveAndEnabled) return;
            if (auras == null || slot < 0 || slot >= auras.Length) throw new ArgumentOutOfRangeException(nameof(slot));
            if (float.IsNaN(radius) || float.IsInfinity(radius) || radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
            var view = auras[slot];
            if (sprite == null) { view.enabled = false; return; }
            view.sprite = sprite;
            view.sharedMaterial=material!=null?material:defaultSpriteMaterial;
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
            if (bursts != null)
                for (int i = 0; i < bursts.Length; i++) if (bursts[i] != null) bursts[i].enabled = false;
            if (frontals == null) return;
            for (int i = 0; i < frontals.Length; i++)
            {if (frontals[i] != null) frontals[i].enabled = false;if(frontalArt[i]!=null)frontalArt[i].enabled=false;}
        }
        public void HideAuras()
        {
            if (auras == null) return;
            for (int i = 0; i < auras.Length; i++) if (auras[i] != null) auras[i].enabled = false;
        }
        private void OnDisable() => HideAll();
    }
}
