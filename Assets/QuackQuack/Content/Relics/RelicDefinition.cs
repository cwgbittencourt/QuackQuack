using System;
using System.Collections.Generic;
using QuackQuack.Content.Shared;
using QuackQuack.Core.Progression;
using UnityEngine;

namespace QuackQuack.Content.Relics
{
    public enum RelicUnlock { Available, SurviveSeconds, CollectExperience }
    public enum RelicRarity { Common, Uncommon, Rare }
    [CreateAssetMenu(menuName = "QuackQuack/Relic")]
    public sealed class RelicDefinition : ContentDefinition
    {
        [SerializeField] private string description;
        [SerializeField] private RelicAttribute attribute;
        [SerializeField] private RelicTerrain terrain;
        [SerializeField] private float bonus = 0.05f;
        [SerializeField] private RelicUnlock unlock;
        [SerializeField] private int threshold;
        [SerializeField] private RelicRarity rarity;
        public string Description => description;
        public RelicAttribute Attribute => attribute;
        public RelicTerrain Terrain => terrain;
        public float Bonus => bonus;
        public RelicUnlock Unlock => unlock;
        public int Threshold => threshold;
        public RelicRarity Rarity => rarity;
        public RelicModifier Modifier => new RelicModifier(Id, attribute, terrain, bonus);
        public bool CanUnlock(double seconds, int experience) => IsValid(out _) &&
            (unlock == RelicUnlock.Available || (unlock == RelicUnlock.SurviveSeconds && seconds >= threshold) ||
             (unlock == RelicUnlock.CollectExperience && experience >= threshold));
        public override bool IsValid(out string reason)
        {
            if (!base.IsValid(out reason)) return false;
            if (string.IsNullOrWhiteSpace(DisplayName) || string.IsNullOrWhiteSpace(description)) reason = "Relic name and description required.";
            else if (!Enum.IsDefined(typeof(RelicAttribute), attribute) || !Enum.IsDefined(typeof(RelicTerrain), terrain) ||
                !Enum.IsDefined(typeof(RelicUnlock), unlock) || !Enum.IsDefined(typeof(RelicRarity), rarity)) reason = "Invalid relic enum.";
            else if (float.IsNaN(bonus) || float.IsInfinity(bonus) || bonus <= 0 || bonus > 0.1f) reason = "Prototype relic bonuses must be in (0,10%].";
            else if (unlock == RelicUnlock.Available ? threshold != 0 : threshold <= 0) reason = "Invalid unlock threshold.";
            return reason == null;
        }
    }
}
