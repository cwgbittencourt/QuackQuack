using QuackQuack.Content.Shared;
using UnityEngine;

namespace QuackQuack.Content.Characters
{
    [CreateAssetMenu(menuName = "QuackQuack/Quack")]
    public sealed class QuackDefinition : ContentDefinition
    {
        [SerializeField] private float capacity = 100, cost = 35, damage = 2, range = 4, push = 2, interruption = 0.6f, fullMultiplier = 2;
        [SerializeField] private float landRecovery = 12, waterRecovery = 24, cooldown = 0.3f;
        public float Capacity => capacity;
        public float Cost => cost;
        public float Damage => damage;
        public float Range => range;
        public float Push => push;
        public float Interruption => interruption;
        public float FullMultiplier => fullMultiplier;
        public float LandRecovery => landRecovery;
        public float WaterRecovery => waterRecovery;
        public float Cooldown => cooldown;
        public override bool IsValid(out string reason)
        {
            if (!base.IsValid(out reason)) return false;
            if (capacity <= 0 || cost <= 0 || cost > capacity || range <= 0 || damage < 0 || push < 0 || interruption < 0 || fullMultiplier <= 1 || landRecovery < 0 || waterRecovery <= landRecovery || cooldown <= 0)
                reason = "Invalid Quack balance.";
            return reason == null;
        }
    }
}
