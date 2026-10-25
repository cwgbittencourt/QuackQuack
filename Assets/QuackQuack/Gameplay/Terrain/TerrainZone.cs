using QuackQuack.Core.Run;
using UnityEngine;

namespace QuackQuack.Gameplay.Terrain
{
    public sealed class TerrainZone : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new Vector2(10, 12);
        [SerializeField] private float hysteresis = 0.15f;
        public void Configure(Vector2 dimensions, float margin) { size = dimensions; hysteresis = Mathf.Max(0, margin); }
        public bool Contains(Vector2 point, TerrainType previous)
        {
            var local = transform.InverseTransformPoint(point);
            float margin = previous == TerrainType.Water ? hysteresis : -hysteresis;
            return Mathf.Abs(local.x) <= size.x / 2 + margin && Mathf.Abs(local.y) <= size.y / 2 + margin;
        }
    }
    public static class TerrainAffinity
    {
        public static float Apply(float baseValue, TerrainType terrain, float landMultiplier = 1, float waterMultiplier = 1)
            => baseValue * (terrain == TerrainType.Water ? waterMultiplier : landMultiplier);
    }
}
