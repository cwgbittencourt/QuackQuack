using System;
using System.Collections.Generic;
using QuackQuack.Core.Progression;
using UnityEngine;
namespace QuackQuack.Content.Relics
{
    [CreateAssetMenu(menuName = "QuackQuack/Relic catalog")]
    public sealed class PrototypeRelicCatalog : ScriptableObject
    {
        [SerializeField] private RelicDefinition[] relics;
        public IReadOnlyList<RelicDefinition> Relics => Array.AsReadOnly((RelicDefinition[])relics.Clone());
        public bool IsValid(out string reason)
        {
            reason = null;
            if (relics == null || relics.Length != 10) { reason = "Exactly ten relics required."; return false; }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var relic in relics)
            {
                if (relic == null) { reason = "Missing relic."; return false; }
                if (!relic.IsValid(out reason)) return false;
                if (!ids.Add(relic.Id)) { reason = "Duplicate relic ID."; return false; }
            }
            return true;
        }
        public RelicLoadout CreateLoadout()
        {
            if (!IsValid(out string reason)) throw new InvalidOperationException(reason);
            var data = new RelicModifier[relics.Length];
            for (int i = 0; i < data.Length; i++) data[i] = relics[i].Modifier;
            return new RelicLoadout(data);
        }
    }
}
