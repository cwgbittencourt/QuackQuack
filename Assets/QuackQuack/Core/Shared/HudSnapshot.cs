using System;

namespace QuackQuack.Core.Shared
{
    public readonly struct HudSnapshot
    {
        public readonly float Health, MaxHealth, Breath, Capacity;
        public readonly string Terrain, Status, QuackState;
        public readonly bool QuackReady;
        public readonly double Time;
        public readonly ProgressSnapshot Progress;
        public float Health01 => Normalize(Health, MaxHealth);
        public float Breath01 => Normalize(Breath, Capacity);
        public HudSnapshot(float health, float maxHealth, float breath, float capacity, string terrain, string status, double time,
            bool quackReady = false, string quackState = "", ProgressSnapshot progress = default)
        { Health = health; MaxHealth = maxHealth; Breath = breath; Capacity = capacity; Terrain = terrain; Status = status; Time = time;
            QuackReady = quackReady; QuackState = quackState; Progress = progress; }

        private static float Normalize(float current, float maximum)
        {
            if (float.IsNaN(current) || float.IsInfinity(current) || float.IsNaN(maximum) ||
                float.IsInfinity(maximum) || maximum <= 0) return 0;
            return (float)Math.Max(0d, Math.Min(1d, (double)current / maximum));
        }
    }
}
