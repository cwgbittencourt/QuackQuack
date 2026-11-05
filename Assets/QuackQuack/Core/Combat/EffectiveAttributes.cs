using System;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;

namespace QuackQuack.Core.Combat
{
    /// <summary>Immutable projection recomputed from authored base values, never from a previous result.</summary>
    public readonly struct EffectiveAttributes
    {
        public float Damage { get; }
        public float AttackInterval { get; }
        public float MovementSpeed { get; }
        public float BreathCapacity { get; }
        public float BreathRecovery { get; }
        public float Range { get; }
        public float AreaRadius { get; }
        public float HitRadius { get; }
        public float ProjectileSpeed { get; }

        private EffectiveAttributes(float damage, float attackInterval, float movementSpeed,
            float breathCapacity, float breathRecovery, float range, float areaRadius,
            float hitRadius, float projectileSpeed)
        {
            Damage = damage;
            AttackInterval = attackInterval;
            MovementSpeed = movementSpeed;
            BreathCapacity = breathCapacity;
            BreathRecovery = breathRecovery;
            Range = range;
            AreaRadius = areaRadius;
            HitRadius = hitRadius;
            ProjectileSpeed = projectileSpeed;
        }

        /// <summary>
        /// Bases already include authored terrain affinity. Bonuses are obtained from
        /// PassiveDefinition.GetModifier(level), including its authored cap, then summed
        /// by attribute: damage, attack speed, movement, capacity, recovery, bubble radius.
        /// No balancing constants or Content dependencies belong in this projection.
        /// Relics retain their existing additive internal sum and multiply after passives.
        /// </summary>
        public static EffectiveAttributes Resolve(float damage, float attackInterval, float movementSpeed,
            float breathCapacity, float breathRecovery, float range, float areaRadius,
            float hitRadius, float projectileSpeed, bool isBubbles, float[] passiveBonuses,
            RelicLoadout relics, TerrainType terrain)
        {
            Positive(damage, nameof(damage));
            Positive(attackInterval, nameof(attackInterval));
            Positive(movementSpeed, nameof(movementSpeed));
            Positive(breathCapacity, nameof(breathCapacity));
            NonNegative(breathRecovery, nameof(breathRecovery));
            Positive(range, nameof(range));
            NonNegative(areaRadius, nameof(areaRadius));
            Positive(hitRadius, nameof(hitRadius));
            Positive(projectileSpeed, nameof(projectileSpeed));
            if (terrain != TerrainType.Land && terrain != TerrainType.Water)
                throw new ArgumentOutOfRangeException(nameof(terrain));
            if (passiveBonuses == null) throw new ArgumentNullException(nameof(passiveBonuses));
            if (passiveBonuses.Length != 6)
                throw new ArgumentException("Exactly six attribute bonuses are required.", nameof(passiveBonuses));
            for (int i = 0; i < passiveBonuses.Length; i++)
                NonNegative(passiveBonuses[i], nameof(passiveBonuses));

            return new EffectiveAttributes(
                Scale(damage, passiveBonuses[0], Bonus(relics, RelicAttribute.Damage, terrain)),
                Interval(attackInterval, passiveBonuses[1], Bonus(relics, RelicAttribute.AttackSpeed, terrain)),
                Scale(movementSpeed, passiveBonuses[2], Bonus(relics, RelicAttribute.MovementSpeed, terrain)),
                Scale(breathCapacity, passiveBonuses[3], 0),
                Scale(breathRecovery, passiveBonuses[4], Bonus(relics, RelicAttribute.BreathRecovery, terrain)),
                Scale(range, 0, Bonus(relics, RelicAttribute.Range, terrain)),
                Scale(areaRadius, 0, Bonus(relics, RelicAttribute.Area, terrain)),
                isBubbles ? Scale(hitRadius, passiveBonuses[5], Bonus(relics, RelicAttribute.Area, terrain)) : hitRadius,
                projectileSpeed);
        }

        private static float Bonus(RelicLoadout relics, RelicAttribute attribute, TerrainType terrain) =>
            relics?.GetBonus(attribute, terrain) ?? 0;

        private static float Scale(float baseline, float passive, float relic) =>
            Result((double)baseline * (1d + passive) * (1d + relic), baseline == 0);

        private static float Interval(float baseline, float passive, float relic) =>
            Result((double)baseline / (1d + passive) / (1d + relic), false);

        private static float Result(double value, bool allowZero)
        {
            float result = (float)value;
            if (double.IsNaN(value) || double.IsInfinity(value) || value > float.MaxValue ||
                float.IsNaN(result) || float.IsInfinity(result) || result < 0 || (!allowZero && result == 0))
                throw new ArgumentOutOfRangeException("attributes", "Effective attributes must be finite and representable; positive bases must remain positive.");
            return result;
        }

        private static void Positive(float value, string name)
        {
            NonNegative(value, name);
            if (value == 0) throw new ArgumentOutOfRangeException(name);
        }

        private static void NonNegative(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0)
                throw new ArgumentOutOfRangeException(name);
        }
    }
}
