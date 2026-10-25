namespace QuackQuack.Core.Shared
{
    public readonly struct HudSnapshot
    {
        public readonly float Health, MaxHealth, Breath, Capacity;
        public readonly string Terrain, Status;
        public readonly double Time;
        public HudSnapshot(float health, float maxHealth, float breath, float capacity, string terrain, string status, double time)
        { Health = health; MaxHealth = maxHealth; Breath = breath; Capacity = capacity; Terrain = terrain; Status = status; Time = time; }
    }
}
