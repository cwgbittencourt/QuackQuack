using System;
using QuackQuack.Core.Run;
using UnityEngine;

namespace QuackQuack.Gameplay.Terrain
{
    public sealed class TerrainZone : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new Vector2(10, 12);
        [SerializeField] private float hysteresis = 0.15f;
        [SerializeField] private TerrainType terrain = TerrainType.Water;
        [SerializeField] private int priority;
        public TerrainType Terrain => terrain;

        // Dimensions and hysteresis are measured in local units, following the transform.
        public void Configure(Vector2 dimensions, float margin, TerrainType type = TerrainType.Water, int overlapPriority = 0)
        {
            if (!ValidGeometry(dimensions, margin)) throw new ArgumentOutOfRangeException(nameof(dimensions));
            ValidateTerrain(type);
            size = dimensions; hysteresis = margin; terrain = type; priority = overlapPriority;
        }
        public bool Contains(Vector2 point, TerrainType previous)
        {
            ValidateTerrain(previous);
            if (!isActiveAndEnabled || (terrain != TerrainType.Land && terrain != TerrainType.Water) || !ValidGeometry(size, hysteresis) ||
                !Finite(point.x) || !Finite(point.y) || transform.lossyScale.x == 0 || transform.lossyScale.y == 0)
                return false;
            var local = transform.InverseTransformPoint(point);
            float margin = previous == terrain ? hysteresis : -hysteresis;
            return Mathf.Abs(local.x) <= size.x / 2 + margin && Mathf.Abs(local.y) <= size.y / 2 + margin;
        }

        // Higher priority wins; equal priorities prefer land, independently of array order.
        // Outside all configured active volumes the lake prototype defaults to land.
        public static TerrainType Resolve(TerrainZone[] zones, Vector2 point, TerrainType previous)
        {
            ValidateTerrain(previous);
            TerrainZone selected = null;
            if (zones != null)
                foreach (var zone in zones)
                {
                    if (zone == null || !zone.Contains(point, previous)) continue;
                    if (selected == null || zone.priority > selected.priority ||
                        (zone.priority == selected.priority && zone.terrain == TerrainType.Land)) selected = zone;
                }
            return selected == null ? TerrainType.Land : selected.terrain;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool ValidGeometry(Vector2 dimensions, float margin) =>
            Finite(dimensions.x) && Finite(dimensions.y) && dimensions.x > 0 && dimensions.y > 0 &&
            Finite(margin) && margin >= 0 && margin < Mathf.Min(dimensions.x, dimensions.y) / 2;
        internal static void ValidateTerrain(TerrainType value)
        {
            if (value != TerrainType.Land && value != TerrainType.Water) throw new ArgumentOutOfRangeException(nameof(value));
        }
    }
    public static class TerrainAffinity
    {
        public static float Select(float landValue, float waterValue, TerrainType terrain)
        {
            TerrainZone.ValidateTerrain(terrain);
            ValidateValue(landValue); ValidateValue(waterValue);
            return terrain == TerrainType.Water ? waterValue : landValue;
        }

        // Always recompute from the authored base; do not feed the previous effective value back.
        public static float Apply(float baseValue, TerrainType terrain, float landMultiplier = 1, float waterMultiplier = 1)
        {
            ValidateValue(baseValue);
            float result = baseValue * Select(landMultiplier, waterMultiplier, terrain);
            ValidateValue(result);
            return result;
        }

        private static void ValidateValue(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        }
    }
}
