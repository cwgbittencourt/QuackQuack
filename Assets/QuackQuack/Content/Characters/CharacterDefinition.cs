using System;
using System.Collections.Generic;
using QuackQuack.Content.Shared;
using UnityEngine;

namespace QuackQuack.Content.Characters
{
    [CreateAssetMenu(menuName = "QuackQuack/Character")]
    public sealed class CharacterDefinition : ContentDefinition
    {
        [SerializeField] private float health = 100, landSpeed = 4, waterSpeed = 6, invulnerability = 0.7f;
        [SerializeField] private QuackDefinition quack;
        public float Health => health;
        public float LandSpeed => landSpeed;
        public float WaterSpeed => waterSpeed;
        public float Invulnerability => invulnerability;
        public QuackDefinition Quack => quack;
        public override bool IsValid(out string reason)
        {
            if (!base.IsValid(out reason)) return false;
            if (Id != "pato") reason = "Only the canonical character ID 'pato' is allowed in the prototype.";
            else if (!IsFinite(health) || health <= 0) reason = "Health must be finite and positive.";
            else if (!IsFinite(landSpeed) || landSpeed <= 0) reason = "Land speed must be finite and positive.";
            else if (!IsFinite(waterSpeed) || waterSpeed <= landSpeed) reason = "Water speed must be finite and greater than land speed.";
            else if (!IsFinite(invulnerability) || invulnerability < 0) reason = "Invulnerability must be finite and non-negative.";
            else if (quack == null) reason = "A Quack definition is required.";
            return reason == null;
        }

        // Validate the complete selection together: individual assets cannot detect duplicate IDs.
        public static bool IsValidSelection(IEnumerable<CharacterDefinition> characters, out string reason)
        {
            reason = null;
            if (characters == null) { reason = "Character selection is required."; return false; }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var character in characters)
            {
                if (character == null) { reason = "Character selection contains a missing definition."; return false; }
                if (!character.IsValid(out reason)) return false;
                if (!ids.Add(character.Id)) { reason = "Duplicate character ID: " + character.Id; return false; }
            }
            if (ids.Count != 1) reason = "Prototype selection must contain exactly one Pato.";
            return reason == null;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
