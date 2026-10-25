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
            if (Id != "pato" || health <= 0 || landSpeed <= 0 || waterSpeed <= landSpeed || quack == null) reason = "Prototype requires Pato and valid movement/Quack.";
            return reason == null;
        }
    }
}
