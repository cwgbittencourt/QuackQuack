using System;
using System.Collections;
using System.Collections.Generic;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Systems.Combat;
using QuackQuack.Systems.Pooling;
using QuackQuack.Systems.Run;
using QuackQuack.Systems.Upgrades;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuackQuack.Gameplay.Upgrades
{
    public sealed class UpgradeFeedbackBridge : MonoBehaviour
    {
        [SerializeField] private PooledUpgradeFeedback prefab;
        [SerializeField] private AudioClip evolutionClip, rareClip;
        [SerializeField] private int capacity = 4;
        [SerializeField] private float evolutionRadius = 2, rareRadius = 1.2f, evolutionDuration = 1.2f, rareDuration = 1, volume = 0.3f;
        private readonly HashSet<(PresentationEventType, long)> seen = new HashSet<(PresentationEventType, long)>(1024);
        private List<PooledUpgradeFeedback> active;
        private List<PresentationEvent> pending;
        private UpgradeSystem system;
        private GameplayEventBus events;
        private RunState run;
        private PoolService pools;
        private Transform focus;
        private string key;
        private bool subscribed;
        private long nextEvolution;
        public bool Ready { get; private set; }
        public int ActiveCount => active == null ? 0 : active.Count;
        public int PendingCount => pending == null ? 0 : pending.Count;
        private IEnumerator Start()
        {
            if (Ready) yield break;
            LakeBuildProgression progression = null;
            for (int i = 0; i < 120 && (progression == null || !progression.Ready); i++)
            { progression = Object.FindAnyObjectByType<LakeBuildProgression>(); yield return null; }
            var bootstrap = GameBootstrap.Instance;
            var player = Object.FindAnyObjectByType<Player.PlayerController>();
            if (progression == null || !progression.Ready || bootstrap == null || player == null) { enabled = false; yield break; }
            Initialize(progression.Upgrades, bootstrap.Events, bootstrap.Run, bootstrap.Pools, player.transform);
        }
        public void Initialize(UpgradeSystem system, GameplayEventBus events, RunState run, PoolService pools, Transform focus)
        {
            if (Ready) throw new InvalidOperationException("Upgrade feedback is already initialized.");
            if (system == null || events == null || run == null || pools == null || focus == null || prefab == null || capacity <= 0)
                throw new ArgumentException("Upgrade feedback requires services and a bounded prefab pool.");
            if (!Positive(evolutionRadius) || !Positive(rareRadius) || !Positive(evolutionDuration) || !Positive(rareDuration) ||
                float.IsNaN(volume) || float.IsInfinity(volume) || volume < 0 || volume > 1)
                throw new ArgumentException("Upgrade feedback profiles must be finite and valid.");
            this.system = system; this.events = events; this.run = run; this.pools = pools; this.focus = focus;
            active = new List<PooledUpgradeFeedback>(capacity); pending = new List<PresentationEvent>(capacity);
            key = "upgrade-feedback-" + GetInstanceID();
            pools.Register(key, PoolFamily.Vfx, () => { var item = Instantiate(prefab, transform); item.Initialize(pools); return item; }, capacity);
            Ready = true; Subscribe();
        }
        private void Subscribe()
        {
            if (!Ready || subscribed || !isActiveAndEnabled) return;
            system.ChoiceConfirmed += OnConfirmed; events.Presentation += OnPresentation; run.Changed += OnRunChanged; subscribed = true;
        }
        private void OnConfirmed(UpgradeConfirmation value)
        {
            if (!value.IsEvolution || focus == null) return;
            long id = ++nextEvolution;
            events.Publish(new PresentationEvent(id, id, PresentationEventType.EvolutionObtained, EventPriority.Essential, evolutionDuration, focus.position));
        }
        private void OnPresentation(PresentationEvent value)
        {
            if (pools.IsDisposed || !(run.IsSimulating || run.Phase == RunPhase.ChoosingUpgrade) ||
                (value.Type != PresentationEventType.EvolutionObtained && value.Type != PresentationEventType.RareReward) ||
                value.CorrelationId <= 0 || !Positive(value.MaxDuration) ||
                float.IsNaN(value.Position.x) || float.IsNaN(value.Position.y) || float.IsInfinity(value.Position.x) || float.IsInfinity(value.Position.y) || seen.Count >= 1024 ||
                !seen.Add((value.Type, value.CorrelationId))) return;
            if (run.Phase == RunPhase.ChoosingUpgrade)
            { if (pending.Count < capacity) pending.Add(value); return; }
            Play(value);
        }
        private void Play(PresentationEvent value)
        {
            if (active.Count >= capacity) return;
            var effect = pools.Acquire<PooledUpgradeFeedback>(key); if (effect == null) return;
            bool evolution = value.Type == PresentationEventType.EvolutionObtained;
            effect.View.Play(evolution, value.Position, evolution ? evolutionRadius : rareRadius,
                Mathf.Min(value.MaxDuration, evolution ? evolutionDuration : rareDuration), evolution ? evolutionClip : rareClip, volume);
            active.Add(effect);
        }
        private void Update()
        {
            if (!Ready) return;
            if (pools.IsDisposed) { ReleaseAll(); return; }
            if (!run.IsSimulating) return;
            for (int i = active.Count - 1; i >= 0; i--)
            { active[i].View.Advance(Time.deltaTime); if (!active[i].gameObject.activeSelf) active.RemoveAt(i); }
            FlushNext();
        }
        private void OnRunChanged(RunPhase phase)
        {
            if (run.IsSimulating)
            {
                foreach (var item in active) item.View.PauseAudio(false);
                FlushNext();
            }
            else if (phase == RunPhase.Paused || phase == RunPhase.ChoosingUpgrade)
                foreach (var item in active) item.View.PauseAudio(true);
            else ReleaseAll();
        }
        private void ReleaseAll()
        {
            if (active == null) return;
            foreach (var item in active) pools.Release(item);
            active.Clear(); pending.Clear();
        }
        private void FlushNext()
        {
            if (active.Count != 0 || pending.Count == 0) return;
            int index = 0;
            for (int i = 0; i < pending.Count; i++)
                if (pending[i].Type == PresentationEventType.EvolutionObtained) { index = i; break; }
            var value = pending[index]; pending.RemoveAt(index); Play(value);
        }
        private void OnEnable() { Subscribe(); }
        private static bool Positive(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value > 0;
        private void OnDisable()
        {
            if (subscribed) { system.ChoiceConfirmed -= OnConfirmed; events.Presentation -= OnPresentation; run.Changed -= OnRunChanged; }
            subscribed = false; ReleaseAll();
        }
    }
}
