using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QuackQuack.Core.Progression
{
    public enum BuildChangeKind { WeaponAcquired, PassiveAcquired, WeaponUpgraded, PassiveUpgraded, WeaponEvolved }

    public readonly struct BuildChange
    {
        public BuildChangeKind Kind { get; }
        public string ItemId { get; }
        public string PreviousId { get; }
        public int Level { get; }
        public BuildChange(BuildChangeKind kind, string itemId, int level, string previousId = null)
        { Kind = kind; ItemId = itemId; Level = level; PreviousId = previousId; }
    }

    // Runtime state only. Callers resolve content IDs and evolution recipes before applying them.
    public sealed class BuildState
    {
        public const int SlotLimit = 6;
        public const int WeaponMaxLevel = 5;
        private readonly Dictionary<string, int> weapons = new Dictionary<string, int>(SlotLimit, StringComparer.Ordinal);
        private readonly Dictionary<string, int> passives = new Dictionary<string, int>(SlotLimit, StringComparer.Ordinal);
        private readonly Dictionary<string, string> evolutions = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly HashSet<string> evolvedResults = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<BuildChange> history = new List<BuildChange>();
        public IReadOnlyDictionary<string, int> Weapons { get; }
        public IReadOnlyDictionary<string, int> Passives { get; }
        public IReadOnlyDictionary<string, string> Evolutions { get; }
        public IReadOnlyList<BuildChange> History { get; }
        public int RerollsRemaining { get; private set; } = 2;
        public int BanishRemaining { get; private set; } = 1;

        public BuildState()
        {
            Weapons = new ReadOnlyDictionary<string, int>(weapons);
            Passives = new ReadOnlyDictionary<string, int>(passives);
            Evolutions = new ReadOnlyDictionary<string, string>(evolutions);
            History = history.AsReadOnly();
        }

        public bool TryAddWeapon(string id)
        {
            if (!ValidId(id) || weapons.Count >= SlotLimit || weapons.ContainsKey(id) || evolutions.ContainsKey(id)) return false;
            weapons.Add(id, 1);
            history.Add(new BuildChange(BuildChangeKind.WeaponAcquired, id, 1));
            return true;
        }

        public bool TryAddPassive(string id)
        {
            if (!ValidId(id) || passives.Count >= SlotLimit || passives.ContainsKey(id)) return false;
            passives.Add(id, 1);
            history.Add(new BuildChange(BuildChangeKind.PassiveAcquired, id, 1));
            return true;
        }

        public bool TryUpgradeWeapon(string id)
        {
            if (!ValidId(id) || !weapons.TryGetValue(id, out int level) || level >= WeaponMaxLevel || evolvedResults.Contains(id)) return false;
            weapons[id] = level + 1;
            history.Add(new BuildChange(BuildChangeKind.WeaponUpgraded, id, level + 1));
            return true;
        }

        // Passive maximum comes from its validated authored definition, not a weapon constant.
        public bool TryUpgradePassive(string id, int maxLevel)
        {
            if (!ValidId(id) || maxLevel <= 0 || !passives.TryGetValue(id, out int level) || level >= maxLevel) return false;
            passives[id] = level + 1;
            history.Add(new BuildChange(BuildChangeKind.PassiveUpgraded, id, level + 1));
            return true;
        }

        public bool TryUseReroll()
        {
            if (RerollsRemaining == 0) return false;
            RerollsRemaining--;
            return true;
        }

        public bool TryUseBanish()
        {
            if (BanishRemaining == 0) return false;
            BanishRemaining--;
            return true;
        }

        public bool TryEvolveWeapon(string baseId, string requiredPassiveId, string resultId)
        {
            if (!ValidId(baseId) || !ValidId(requiredPassiveId) || !ValidId(resultId) ||
                !weapons.TryGetValue(baseId, out int level) || level != WeaponMaxLevel ||
                !passives.ContainsKey(requiredPassiveId) || weapons.ContainsKey(resultId) ||
                evolutions.ContainsKey(baseId) || evolutions.ContainsKey(resultId) || evolvedResults.Contains(baseId)) return false;
            weapons.Remove(baseId);
            // An evolved weapon is the completed form, retaining the max-level slot.
            weapons.Add(resultId, WeaponMaxLevel);
            evolutions.Add(baseId, resultId);
            evolvedResults.Add(resultId);
            history.Add(new BuildChange(BuildChangeKind.WeaponEvolved, resultId, WeaponMaxLevel, baseId));
            return true;
        }

        private static bool ValidId(string id) => !string.IsNullOrWhiteSpace(id);
    }
}
