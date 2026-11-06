using System;
using UnityEngine;

namespace QuackQuack.Core.Shared
{
    public enum SpecialEnemyCategory { MiniBoss, Boss }

    public readonly struct SpecialEnemySnapshot
    {
        public int InstanceId { get; }
        public long LeaseId { get; }
        public string EncounterId { get; }
        public string DisplayName { get; }
        public SpecialEnemyCategory Category { get; }
        public Vector3 Anchor { get; }
        public float Health01 { get; }
        public bool IsValid => InstanceId != 0 && LeaseId > 0 && !string.IsNullOrEmpty(EncounterId) &&
            !string.IsNullOrEmpty(DisplayName) && Health01 > 0;

        public SpecialEnemySnapshot(int instanceId, long leaseId, string encounterId, string displayName,
            SpecialEnemyCategory category, Vector3 anchor, float health, float maximum)
        {
            if (instanceId == 0 || leaseId <= 0 || string.IsNullOrWhiteSpace(encounterId) || string.IsNullOrWhiteSpace(displayName) ||
                (category != SpecialEnemyCategory.MiniBoss && category != SpecialEnemyCategory.Boss) || !Finite(anchor.x) || !Finite(anchor.y) || !Finite(anchor.z) ||
                !Finite(health) || !Finite(maximum) || health <= 0 || maximum <= 0)
                throw new ArgumentException("A live special enemy snapshot requires finite health, anchor and lease identity.");
            InstanceId = instanceId; LeaseId = leaseId; EncounterId = encounterId; DisplayName = displayName;
            Category = category; Anchor = anchor; Health01 = Mathf.Clamp((float)((double)health / maximum), float.Epsilon, 1);
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    // Rebuilt from live leases in place. No actor/Transform references cross into Presentation.
    public sealed class SpecialEnemyRegistry
    {
        private readonly SpecialEnemySnapshot[] entries;
        public int Capacity => entries.Length;
        public int Count { get; private set; }
        public SpecialEnemySnapshot this[int index] => index >= 0 && index < Count ? entries[index] : throw new ArgumentOutOfRangeException(nameof(index));
        public SpecialEnemyRegistry(int capacity)
        { if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity)); entries = new SpecialEnemySnapshot[capacity]; }
        public void Clear() { Array.Clear(entries, 0, Count); Count = 0; }
        public bool TryAdd(SpecialEnemySnapshot value)
        {
            if (!value.IsValid) return false;
            for (int i = 0; i < Count; i++) if (entries[i].InstanceId == value.InstanceId) return false;
            if (Count == entries.Length) return false;
            entries[Count++] = value; return true;
        }
    }
}
