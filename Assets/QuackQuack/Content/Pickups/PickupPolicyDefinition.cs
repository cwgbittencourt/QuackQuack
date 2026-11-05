using System;
using System.Globalization;
using QuackQuack.Content.Shared;
using QuackQuack.Core.Progression;
using UnityEngine;

namespace QuackQuack.Content.Pickups
{
    [CreateAssetMenu(menuName = "QuackQuack/Pickup policy")]
    public sealed class PickupPolicyDefinition : ContentDefinition
    {
        [SerializeField, Range(0, 1)] private float commonHealChance = 0.1f;
        [SerializeField, Range(0, 1)] private float commonHealFraction = 0.15f;
        [SerializeField, Range(0, 1)] private float miniBossHealFraction = 0.5f;
        [SerializeField] private float captureBodyRadiusMultiplier = 3;
        [SerializeField] private float attractionWaterSpeedMultiplier = 1.25f;

        public float CommonHealChance => commonHealChance;
        public float CommonHealFraction => commonHealFraction;
        public float MiniBossHealFraction => miniBossHealFraction;
        public float CaptureBodyRadiusMultiplier => captureBodyRadiusMultiplier;
        public float AttractionWaterSpeedMultiplier => attractionWaterSpeedMultiplier;

        public override bool IsValid(out string reason)
        {
            if (!base.IsValid(out reason)) return false;
            if (!Finite(commonHealChance) || commonHealChance < 0 || commonHealChance > 1)
                reason = "commonHealChance must be finite in [0,1].";
            else if (!Fraction(commonHealFraction)) reason = "commonHealFraction must be finite in (0,1].";
            else if (!Fraction(miniBossHealFraction)) reason = "miniBossHealFraction must be finite in (0,1].";
            else if (!Finite(captureBodyRadiusMultiplier) || captureBodyRadiusMultiplier <= 0)
                reason = "captureBodyRadiusMultiplier must be finite and positive.";
            else if (!Finite(attractionWaterSpeedMultiplier) || attractionWaterSpeedMultiplier <= 1)
                reason = "attractionWaterSpeedMultiplier must be finite and greater than one.";
            return reason == null;
        }

        public RewardLedger CreateLedger(long runId, Func<double> dropRandom, int defeatCapacity)
        {
            if (!IsValid(out var reason)) throw new InvalidOperationException(reason);
            // Float-to-double widening would turn authored 0.10 into 0.10000000149...
            // Round-trip authoring precision, including tiny nonzero probabilities, without an epsilon.
            // This conversion occurs once at ledger creation, not for each drop.
            double chance = double.Parse(commonHealChance.ToString("R", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            return new RewardLedger(runId, dropRandom, chance, commonHealFraction, miniBossHealFraction, defeatCapacity);
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Fraction(float value) => Finite(value) && value > 0 && value <= 1;
    }
}
