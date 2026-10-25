namespace QuackQuack.Core.Shared
{
    public readonly struct HudSnapshot
    {
        public readonly float Health, MaxHealth, Breath, Capacity;
        public readonly string Terrain, Status, QuackState;
        public readonly bool QuackReady;
        public readonly double Time;
        public HudSnapshot(float health, float maxHealth, float breath, float capacity, string terrain, string status, double time,
            bool quackReady = false, string quackState = "")
        { Health = health; MaxHealth = maxHealth; Breath = breath; Capacity = capacity; Terrain = terrain; Status = status; Time = time;
            QuackReady = quackReady; QuackState = quackState; }
    }
}
