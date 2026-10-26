using System;
using QuackQuack.Content.Shared;
using UnityEngine;

namespace QuackQuack.Content.Passives
{
    public enum PassiveAttribute { Damage, AttackSpeed, MovementSpeed, BreathCapacity, BreathRecovery, AreaRadius }

    [CreateAssetMenu(menuName = "QuackQuack/Passive")]
    public sealed class PassiveDefinition : ContentDefinition
    {
        [SerializeField] private PassiveAttribute attribute;
        [SerializeField] private float modifierPerLevel = 0.1f, modifierLimit = 0.5f;
        [SerializeField] private int maxLevel = 5;
        [SerializeField] private string compatibleWeaponId;
        public PassiveAttribute Attribute => attribute;
        public float ModifierPerLevel => modifierPerLevel;
        public float ModifierLimit => modifierLimit;
        public int MaxLevel => maxLevel;
        public string CompatibleWeaponId => compatibleWeaponId;

        public float GetModifier(int level)
        {
            if (level < 0 || level > maxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            if (!IsValid(out var reason)) throw new InvalidOperationException(reason);
            return (float)Math.Min((double)modifierPerLevel * level, modifierLimit);
        }

        public override bool IsValid(out string reason)
        {
            if (!base.IsValid(out reason)) return false;
            if (!Enum.IsDefined(typeof(PassiveAttribute), attribute)) reason = "Passive attribute is invalid.";
            else if (maxLevel <= 0) reason = "Passive maximum level must be positive.";
            else if (!NormalizedPositive(modifierPerLevel) || !NormalizedPositive(modifierLimit))
                reason = "Passive bonuses and limits must be finite percentages greater than zero and at most one.";
            else if (modifierPerLevel > modifierLimit) reason = "Per-level bonus must not exceed its authored limit.";
            else if (string.IsNullOrWhiteSpace(compatibleWeaponId)) reason = "A compatible weapon ID is required.";
            return reason == null;
        }

        private static bool NormalizedPositive(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value > 0 && value <= 1;
    }
}
