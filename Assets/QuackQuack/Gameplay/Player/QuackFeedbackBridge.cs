using System.Collections.Generic;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Run;
using QuackQuack.Systems.Pooling;
using QuackQuack.Systems.Run;
using UnityEngine;

namespace QuackQuack.Gameplay.Player
{
    public sealed class QuackFeedbackBridge : MonoBehaviour
    {
        [SerializeField] private PooledQuackFeedback prefab;
        [SerializeField] private AudioClip quackClip, impactClip, reflectionClip;
        [SerializeField] private int capacity = 32;
        private readonly List<PooledQuackFeedback> active = new List<PooledQuackFeedback>(32);
        private GameBootstrap bootstrap;
        private PoolService pools;
        private string key;
        private bool subscribed;
        public bool Ready { get; private set; }
        public int ActiveCount => active.Count;
        private void Start()
        {
            bootstrap = GameBootstrap.Instance;
            if (bootstrap == null) { enabled = false; return; }
            pools = bootstrap.Pools; key = "quack-feedback-" + GetInstanceID();
            active.Capacity = Mathf.Max(active.Capacity, capacity);
            pools.Register(key, PoolFamily.Vfx, () =>
            {
                var item = Instantiate(prefab, transform); item.Initialize(pools); return item;
            }, capacity);
            Ready = true; Subscribe();
        }
        private void Subscribe()
        {
            if (!Ready || subscribed) return;
            bootstrap.Events.Combat += OnCombat; bootstrap.Run.Changed += OnRunChanged; subscribed = true;
        }
        private void OnCombat(CombatEvent value)
        {
            if (!bootstrap.Run.IsSimulating) return;
            Color color; float radius, seconds = 0.35f; AudioClip clip = null; SpriteRenderer outline = null;
            switch (value.Type)
            {
                case CombatEventType.QuackActivated: color = new Color(1, 0.83f, 0.28f); radius = value.Amount; seconds = 0.45f; clip = quackClip; break;
                case CombatEventType.DamageApplied: color = new Color(1, 0.42f, 0.25f); radius = 0.55f; clip = impactClip; break;
                case CombatEventType.ProjectileReflected: color = Color.cyan; radius = 0.65f; clip = reflectionClip; break;
                case CombatEventType.CollisionTriggered: color = new Color(1, 0.65f, 0.25f); radius = 0.8f; break;
                case CombatEventType.CriticalHit:
                    if (!QuackTarget.TryFind(value.Target, out var target)) return;
                    outline = target.GetComponent<SpriteRenderer>();
                    if (outline == null) return;
                    color = Color.white; radius = 0; seconds = 0.3f; break;
                default: return;
            }
            var effect = pools.Acquire<PooledQuackFeedback>(key);
            if (effect == null) return; // Cosmetic saturation cannot change combat results.
            active.Add(effect);
            if (outline != null) effect.View.PlayOutline(outline, seconds);
            else effect.View.Play(value.Position, radius, color, seconds);
            effect.View.PlaySound(clip, 0.25f);
        }
        private void Update()
        {
            if (!Ready || bootstrap == null || !bootstrap.Run.IsSimulating) return;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                active[i].View.Advance(Time.deltaTime);
                if (!active[i].gameObject.activeSelf) active.RemoveAt(i);
            }
        }
        private void OnRunChanged(RunPhase phase)
        {
            if (!bootstrap.Run.IsSimulating && phase != RunPhase.Paused && phase != RunPhase.ChoosingUpgrade) { ReleaseAll(); return; }
            foreach (var effect in active) effect.View.PauseAudio(!bootstrap.Run.IsSimulating);
        }
        private void ReleaseAll()
        {
            if (pools != null) foreach (var effect in active) pools.Release(effect);
            active.Clear();
        }
        private void OnEnable() { Subscribe(); }
        private void OnDisable()
        {
            if (subscribed && bootstrap != null)
            { bootstrap.Events.Combat -= OnCombat; bootstrap.Run.Changed -= OnRunChanged; }
            subscribed = false; ReleaseAll();
        }
    }
}
