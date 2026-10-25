using System;
using QuackQuack.Core.Combat;

namespace QuackQuack.Systems.Combat
{
    // Events describe accepted changes. Subscribers receive values, never mutable rule state.
    public sealed class GameplayEventBus
    {
        public event Action<CombatEvent> Combat;
        public event Action<PresentationEvent> Presentation;
        public void Publish(CombatEvent value) => Combat?.Invoke(value);
        public void Publish(PresentationEvent value) => Presentation?.Invoke(value);
        public void Clear() { Combat = null; Presentation = null; }
    }
}
