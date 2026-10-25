using UnityEngine;

namespace QuackQuack.Core.Combat
{
    public enum CombatEventType { DamageApplied, CriticalHit, EntityDefeated, QuackActivated, ProjectileReflected, EffectApplied, CollisionTriggered }
    public enum EventPriority { Cosmetic, Feedback, Essential, Danger }
    public readonly struct CombatEvent
    {
        public long Tick { get; }
        public long CorrelationId { get; }
        public int Source { get; }
        public int Target { get; }
        public Vector2 Position { get; }
        public float Amount { get; }
        public CombatEventType Type { get; }
        public EventPriority Priority { get; }
        public CombatEvent(long tick, long correlationId, int source, int target, Vector2 position, float amount, CombatEventType type, EventPriority priority = EventPriority.Feedback)
        { Tick = tick; CorrelationId = correlationId; Source = source; Target = target; Position = position; Amount = amount; Type = type; Priority = priority; }
    }
    public enum PresentationEventType { TerrainChanged, BreathReady, BreathConsumed, DangerTelegraphed, BossPresented, EvolutionObtained, RareReward, ResultsAvailable }
    public readonly struct PresentationEvent
    {
        public long Tick { get; }
        public long CorrelationId { get; }
        public PresentationEventType Type { get; }
        public EventPriority Priority { get; }
        public float MaxDuration { get; }
        public Vector2 Position { get; }
        public PresentationEvent(long tick, long correlationId, PresentationEventType type, EventPriority priority, float maxDuration, Vector2 position)
        { Tick = tick; CorrelationId = correlationId; Type = type; Priority = priority; MaxDuration = Mathf.Max(0, maxDuration); Position = position; }
    }
}
