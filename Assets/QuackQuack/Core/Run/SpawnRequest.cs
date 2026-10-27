using System;
using QuackQuack.Core.Combat;
using UnityEngine;

namespace QuackQuack.Core.Run
{
    public readonly struct SpawnRequest
    {
        public long RequestId { get; }
        public string EncounterId { get; }
        public string EnemyId { get; }
        public int Quantity { get; }
        public Rect Region { get; }
        public float MinimumDistance { get; }
        public EventPriority Priority { get; }
        public long DeadlineTick { get; }
        public SpawnRequest(long requestId, string encounterId, string enemyId, int quantity,
            Rect region, float minimumDistance, EventPriority priority, long deadlineTick)
        { RequestId=requestId; EncounterId=encounterId; EnemyId=enemyId; Quantity=quantity;
            Region=region; MinimumDistance=minimumDistance; Priority=priority; DeadlineTick=deadlineTick; }

        public bool IsValid => RequestId > 0 && !string.IsNullOrWhiteSpace(EncounterId) && !string.IsNullOrWhiteSpace(EnemyId) &&
            Quantity > 0 && ValidRegion(Region) && Finite(MinimumDistance) && MinimumDistance >= 0 &&
            Enum.IsDefined(typeof(EventPriority), Priority) && DeadlineTick >= 0;
        public static bool ValidRegion(Rect value) => Finite(value.xMin) && Finite(value.yMin) &&
            Finite(value.xMax) && Finite(value.yMax) && Finite(value.width) && Finite(value.height) && value.width > 0 && value.height > 0;
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
