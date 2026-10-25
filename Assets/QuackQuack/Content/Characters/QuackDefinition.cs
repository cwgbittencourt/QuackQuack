using QuackQuack.Content.Shared;
using QuackQuack.Core.Combat;
using UnityEngine;

namespace QuackQuack.Content.Characters
{
    [CreateAssetMenu(menuName = "QuackQuack/Quack")]
    public sealed class QuackDefinition : ContentDefinition
    {
        [SerializeField] private float capacity = 100, cost = 35, damage = 2, range = 4, push = 2, interruption = 0.6f, fullMultiplier = 2;
        [SerializeField] private float landRecovery = 12, waterRecovery = 24, cooldown = 0.3f;
        [SerializeField] private float reflectionSpeed = 7;
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
        public float ReflectionSpeed => reflectionSpeed;

        // The prototype reflects only projectiles explicitly marked light and reflectable.
        public bool CanReflect(bool light, bool reflectable) => QuackSystem.CanReflect(light, reflectable);

        public override bool IsValid(out string reason)
        {
            if (!base.IsValid(out reason)) return false;
            if (!IsFinite(capacity) || capacity <= 0) reason = "Breath capacity must be finite and positive.";
            else if (!IsFinite(cost) || cost <= 0 || cost > capacity) reason = "Quack cost must be finite, positive and no greater than capacity.";
            else if (!IsFinite(damage) || damage < 0) reason = "Damage must be finite and non-negative.";
            else if (!IsFinite(range) || range <= 0) reason = "Range must be finite and positive.";
            else if (!IsFinite(push) || push < 0) reason = "Knockback must be finite and non-negative.";
            else if (!IsFinite(interruption) || interruption < 0) reason = "Interruption must be finite and non-negative.";
            else if (!IsFinite(fullMultiplier) || fullMultiplier <= 1) reason = "Full breath multiplier must be finite and greater than one.";
            else if (!IsFinite(landRecovery) || landRecovery < 0) reason = "Land recovery must be finite and non-negative.";
            else if (!IsFinite(waterRecovery) || waterRecovery <= landRecovery) reason = "Water recovery must be finite and greater than land recovery.";
            else if (!IsFinite(cooldown) || cooldown <= 0) reason = "Cooldown must be finite and positive.";
            else if (!IsFinite(reflectionSpeed) || reflectionSpeed <= 0) reason = "Reflection speed must be finite and positive.";
            return reason == null;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
