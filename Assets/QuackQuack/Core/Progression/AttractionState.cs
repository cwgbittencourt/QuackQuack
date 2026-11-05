using System;
using UnityEngine;

namespace QuackQuack.Core.Progression
{
    public enum AttractionPhase { Grounded, Following, Discarded }
    public struct AttractionState
    {
        public long LeaseId { get; private set; }
        public AttractionPhase Phase { get; private set; }
        public bool Captured => Phase == AttractionPhase.Following;
        public void Reset(long lease = 0) { LeaseId = lease; Phase = AttractionPhase.Grounded; }
        public void Capture() { Phase = AttractionPhase.Following; }
        public Vector2 Advance(Vector2 position, Vector2 target, float captureRadius, float speed, float seconds,
            bool simulating, bool alive, bool healing, bool missingHealth)
        {
            if (!Finite(position.x) || !Finite(position.y) || !Finite(target.x) || !Finite(target.y) ||
                !Finite(captureRadius) || captureRadius <= 0 || !Finite(speed) || speed <= 0 || !Finite(seconds) || seconds < 0)
                throw new ArgumentOutOfRangeException("Attraction requires finite positions, positive radius/speed and nonnegative time.");
            if (!alive) { Reset(LeaseId); return position; }
            if (LeaseId <= 0 || Phase == AttractionPhase.Discarded) return position;
            if (!simulating) return position;
            if (!Captured)
            {
                if (healing && !missingHealth) return position;
                // Compare both squared values at the same precision. A float dot product
                // rounded before comparison can otherwise reject exact equality on Mono.
                Vector2 delta = target - position;
                double distanceSquared = (double)delta.x * delta.x + (double)delta.y * delta.y;
                if (distanceSquared > (double)captureRadius * captureRadius) return position;
                Capture();
            }
            if (healing && !missingHealth) { Phase = AttractionPhase.Discarded; return position; }
            Phase = AttractionPhase.Following;
            return Vector2.MoveTowards(position, target, speed * seconds);
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
