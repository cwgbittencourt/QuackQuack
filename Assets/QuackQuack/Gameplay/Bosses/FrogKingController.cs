using System;
using System.Threading;
using QuackQuack.Content.Enemies;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Player;
using QuackQuack.Systems.Combat;
using QuackQuack.Systems.Pooling;
using UnityEngine;

namespace QuackQuack.Gameplay.Bosses
{
    public enum FrogKingPhase { Opening, Summoning, Destruction, Defeated }

    public sealed class FrogKingController : MonoBehaviour, IPoolLifecycle
    {
        public bool AutomaticTick { get; set; } = true;
        public bool AutomaticPatterns { get; set; }
        public FrogKingPhase Phase { get; private set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        private float damageMultiplier = 1;
        public int ActiveHazards { get; private set; }
        public int SuppressedHazards { get; private set; }
        public event Action<string, long> AttackCommitted;
        private static long correlations;
        private RunState run;
        private GameplayEventBus events;
        private LineRenderer marker;
        private AudioSource audioSource;
        private AudioClip clip;
        private float lead, remaining, recovery;
        private string pending;
        private Vector2 lockedTarget;
        private long correlation, tick;
        private int pattern;
        private FrogKingSettings settings;
        private PoolService pools;
        private PlayerController player;
        private FrogKingSafeArea[] safeAreas;
        private FrogKingSafeArea pendingArea;
        private FrogKingHazard[] hazards;
        private string waveKey, tadpoleKey;
        private bool configured;

        public void Initialize(RunState state, GameplayEventBus eventBus, LineRenderer visual,
            AudioSource source, AudioClip warning, float leadSeconds)
        {
            if (run != null) throw new InvalidOperationException("Already initialized.");
            if (state == null || eventBus == null || visual == null || source == null || warning == null)
                throw new ArgumentNullException("Frog King warning dependencies");
            if (!Finite(leadSeconds) || leadSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(leadSeconds));
            run = state; events = eventBus; marker = visual; audioSource = source; clip = warning; lead = leadSeconds;
            run.Changed += PhaseChanged; HideWarning();
        }

        // Full encounter composition. The small Initialize overload remains useful for isolated signal tests.
        public void InitializeEncounter(RunState state, GameplayEventBus eventBus, PoolService poolService,
            PlayerController target, FrogKingSettings definition, LineRenderer visual, AudioSource source,
            FrogKingSafeArea[] refuges, float healthMultiplier = 1, float damageMultiplier = 1)
        {
            if (run != null) throw new InvalidOperationException("Already initialized.");
            if (definition == null || !definition.IsValid(out _) || target == null || target.State == null
                || poolService == null || poolService.IsDisposed || refuges == null
                || definition.HazardPrefab.GetComponent<FrogKingHazard>() == null)
                throw new ArgumentException("Invalid Frog King encounter dependencies.");
            for (int i = 0; i < refuges.Length; i++)
            {
                if (refuges[i] == null) throw new ArgumentException("Missing refuge.");
                for (int j = 0; j < i; j++) if (refuges[j] == refuges[i]) throw new ArgumentException("Duplicate refuge.");
            }
            if (visual == null || source == null || state == null || eventBus == null) throw new ArgumentNullException("Encounter presentation");
            if (!Finite(healthMultiplier) || healthMultiplier <= 0 || !Finite(damageMultiplier) || damageMultiplier <= 0
                || (double)definition.Health * healthMultiplier > float.MaxValue) throw new ArgumentException("Invalid boss encounter multipliers.");
            settings = definition; pools = poolService; player = target;
            safeAreas = (FrogKingSafeArea[])refuges.Clone(); hazards = new FrogKingHazard[definition.HazardCapacity];
            string key = "frog-king/" + GetInstanceID();
            waveKey = key + "/wave"; tadpoleKey = key + "/tadpole";
            if (!pools.IsRegistered<FrogKingHazard>(tadpoleKey, PoolFamily.Enemy))
                pools.Register(tadpoleKey, PoolFamily.Enemy,
                    () => Instantiate(definition.HazardPrefab).GetComponent<FrogKingHazard>(), definition.HazardCapacity);
            if (!pools.IsRegistered<FrogKingHazard>(waveKey, PoolFamily.Projectile))
                pools.Register(waveKey, PoolFamily.Projectile,
                    () => Instantiate(definition.HazardPrefab).GetComponent<FrogKingHazard>(), definition.HazardCapacity);
            visual.sharedMaterial = definition.MarkerMaterial;
            Initialize(state, eventBus, visual, source, definition.WarningAudio, definition.TelegraphSeconds);
            Health = MaxHealth = definition.Health * healthMultiplier; this.damageMultiplier = damageMultiplier;
            Phase = FrogKingPhase.Opening; configured = true;
            if (state.Phase == RunPhase.Boss) events.Publish(new PresentationEvent(0, Interlocked.Increment(ref correlations),
                PresentationEventType.BossPresented, EventPriority.Essential, 3, transform.position));
        }

        private bool CanSimulate => run != null && run.Phase == RunPhase.Boss && isActiveAndEnabled
            && (!configured || (player != null && player.State != null && !pools.IsDisposed));
        public bool TryBeginAttack(string attackId, Vector2 target)
        {
            if (!CanSimulate || pending != null || !Finite(target.x) || !Finite(target.y)) return false;
            if (attackId != "jump" && attackId != "wave" && attackId != "tadpoles" && attackId != "destroy-safe-area") return false;
            pendingArea = null;
            if (configured)
            {
                if (Health <= 0 || (attackId == "tadpoles" && Phase == FrogKingPhase.Opening)) return false;
                if (attackId == "destroy-safe-area")
                {
                    if (Phase != FrogKingPhase.Destruction) return false;
                    pendingArea = NearestArea(target);
                    if (pendingArea == null) return false;
                    target = pendingArea.Center;
                }
            }
            pending = attackId; lockedTarget = target; remaining = lead;
            correlation = Interlocked.Increment(ref correlations);
            float radius = configured && attackId == "jump" ? settings.JumpRadius : 1;
            marker.gameObject.SetActive(true); marker.enabled = true; marker.useWorldSpace = true; marker.loop = true;
            marker.positionCount = 32; marker.startWidth = marker.endWidth = 0.1f;
            marker.startColor = marker.endColor = attackId == "wave" ? Color.cyan : attackId == "tadpoles" ? Color.magenta : new Color(1, 0.35f, 0.1f);
            for (int i = 0; i < 32; i++)
            {
                float a = i * Mathf.PI * 2 / 32;
                marker.SetPosition(i, (Vector3)(target + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius));
            }
            audioSource.enabled = true; audioSource.gameObject.SetActive(true); audioSource.clip = clip;
            audioSource.loop = true; audioSource.mute = false; audioSource.volume = 1; audioSource.Play();
            events.Publish(new PresentationEvent(tick, correlation, PresentationEventType.DangerTelegraphed,
                EventPriority.Danger, lead, target));
            return true;
        }

        public void Advance(float seconds)
        {
            if (!Finite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (!CanSimulate) { if (configured && (run.Outcome.HasValue || player == null || pools.IsDisposed)) Cancel(); return; }
            tick++;
            // Existing hazards consume this step; newly committed ones start on the following step.
            if (configured) AdvanceHazards(seconds);
            if (!CanSimulate) { if (configured) Cancel(); return; }
            if (pending != null)
            {
                remaining -= seconds;
                if (remaining > 0) return;
                string id = pending; long committedCorrelation = correlation; Vector2 target = lockedTarget;
                var area = pendingArea;
                pending = null; pendingArea = null; HideWarning();
                recovery = configured ? settings.RecoverySeconds : 0;
                if (configured) Commit(id, target, area, committedCorrelation);
                AttackCommitted?.Invoke(id, committedCorrelation);
                return; // Never catch up multiple attacks in a long frame.
            }
            if (!configured || (!AutomaticTick && !AutomaticPatterns) || seconds == 0) return;
            recovery = Mathf.Max(0, recovery - seconds);
            if (recovery > 0) return;
            int count = Phase == FrogKingPhase.Opening ? 2 : Phase == FrogKingPhase.Summoning ? 3 : 4;
            string next = (pattern++ % count) switch { 0 => "jump", 1 => "wave", 2 => "tadpoles", _ => "destroy-safe-area" };
            Vector2 aim = next == "wave" || next == "tadpoles" ? (Vector2)transform.position : (Vector2)player.transform.position;
            if (!TryBeginAttack(next, aim)) recovery = settings.RecoverySeconds;
        }

        public bool TakeDamage(float amount, int source, long eventTick)
        {
            if (!CanSimulate || !configured || Health <= 0 || !Finite(amount) || amount <= 0) return false;
            float applied = Mathf.Min(Health, amount); Health -= applied;
            Phase = Health <= 0 ? FrogKingPhase.Defeated : Health <= MaxHealth * settings.DestructionPhaseHealth
                ? FrogKingPhase.Destruction : Health <= MaxHealth * settings.SummonPhaseHealth ? FrogKingPhase.Summoning : FrogKingPhase.Opening;
            long id = Interlocked.Increment(ref correlations);
            // Retire hazards, then publish final facts before the terminal snapshot freezes.
            bool defeated = Health <= 0;
            if (defeated) Cancel();
            try
            {
                events.Publish(new CombatEvent(eventTick, id, source, GetInstanceID(), transform.position, applied, CombatEventType.DamageApplied));
                if (defeated) events.Publish(new CombatEvent(eventTick, id, source, GetInstanceID(), transform.position, 0, CombatEventType.EntityDefeated));
            }
            finally { if (defeated) run.TryTransition(RunPhase.Won); }
            return true;
        }
        private void Commit(string id, Vector2 target, FrogKingSafeArea area, long attackCorrelation)
        {
            if (id == "jump")
            {
                transform.position = target;
                if (Vector2.Distance(player.transform.position, target) <= settings.JumpRadius) DamagePlayer(settings.JumpDamage, attackCorrelation);
            }
            else if (id == "wave") SpawnHazard(true, target);
            else if (id == "tadpoles")
            {
                for (int i = 0; i < settings.TadpoleCount; i++)
                {
                    float a = i * Mathf.PI * 2 / settings.TadpoleCount;
                    SpawnHazard(false, target + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * settings.JumpRadius);
                }
            }
            else if (area != null && !area.IsDestroyed) area.DestroyArea();
        }
        private void SpawnHazard(bool wave, Vector2 position)
        {
            if (pools.IsDisposed) { SuppressedHazards++; return; }
            if (ActiveHazards >= hazards.Length) { SuppressedHazards++; return; }
            var hazard = pools.Acquire<FrogKingHazard>(wave ? waveKey : tadpoleKey);
            if (hazard == null) { SuppressedHazards++; return; }
            hazard.Configure(wave, position, wave ? settings.WaveSpeed : settings.TadpoleSpeed,
                wave ? 0 : settings.TadpoleRadius, wave ? settings.WaveLifetime : settings.TadpoleLifetime);
            for (int i = 0; i < hazards.Length; i++) if (hazards[i] == null) { hazards[i] = hazard; ActiveHazards++; return; }
            pools.Release(hazard); SuppressedHazards++;
        }
        private void AdvanceHazards(float seconds)
        {
            if (seconds == 0) return;
            if (pools.IsDisposed) { ClearHazards(); return; }
            Vector2 target = player.transform.position;
            for (int i = 0; i < hazards.Length && CanSimulate; i++)
            {
                var hazard = hazards[i]; if (hazard == null) continue;
                bool alive = hazard.Step(seconds, target, settings.WaveWidth, out bool touches);
                if (touches && (!hazard.IsWave || !IsProtected(target)))
                {
                    DamagePlayer(hazard.IsWave ? settings.WaveDamage : settings.TadpoleDamage, Interlocked.Increment(ref correlations));
                    if (!hazard.IsWave) alive = false;
                }
                if (!alive && hazards[i] != null) { hazards[i] = null; ActiveHazards--; pools.Release(hazard); }
            }
        }
        private bool IsProtected(Vector2 position)
        {
            foreach (var area in safeAreas) if (area != null && area.Contains(position)) return true;
            return false;
        }
        private FrogKingSafeArea NearestArea(Vector2 target)
        {
            FrogKingSafeArea best = null; float distance = float.PositiveInfinity;
            foreach (var area in safeAreas)
            {
                if (area == null || area.IsDestroyed || !area.isActiveAndEnabled) continue;
                float next = (area.Center - target).sqrMagnitude;
                if (next < distance) { distance = next; best = area; }
            }
            return best;
        }
        private void DamagePlayer(float damage, long id)
        {
            damage = (float)Math.Min(float.MaxValue, (double)damage * damageMultiplier);
            float before = player.State.Health;
            player.State.TakeDamage(damage, run.ElapsedSeconds, player.Definition.Invulnerability);
            float applied = before - player.State.Health;
            if (applied <= 0) return;
            events.Publish(new CombatEvent(tick, id, GetInstanceID(), player.GetInstanceID(), player.transform.position, applied, CombatEventType.DamageApplied));
            if (player.State.Health <= 0) run.TryTransition(RunPhase.Lost);
        }
        private void FixedUpdate() { if (AutomaticTick) Advance(Time.fixedDeltaTime); }
        private void HideWarning() { if (marker != null) marker.enabled = false; if (audioSource != null) audioSource.Stop(); }
        private void ClearHazards()
        {
            if (hazards == null) return;
            for (int i = 0; i < hazards.Length; i++) { var item = hazards[i]; hazards[i] = null; if (item != null) pools.Release(item); }
            ActiveHazards = 0;
        }
        private void Cancel() { pending = null; pendingArea = null; HideWarning(); ClearHazards(); }
        private void PhaseChanged(RunPhase phase)
        {
            if (phase == RunPhase.Paused || phase == RunPhase.ChoosingUpgrade) { if (audioSource != null) audioSource.Pause(); }
            else if (phase == RunPhase.Boss) { if (pending != null && audioSource != null) audioSource.UnPause(); }
            else if (run.Outcome.HasValue) Cancel();
        }
        private void OnDisable() { Cancel(); }
        private void OnDestroy() { if (run != null) run.Changed -= PhaseChanged; Cancel(); }
        public void OnAcquire() { enabled = true; }
        public void OnRelease() { Cancel(); }
        public void ResetState()
        {
            if (run != null) run.Changed -= PhaseChanged;
            Cancel(); run = null; events = null; marker = null; audioSource = null; clip = null;
            settings = null; pools = null; player = null; safeAreas = null; hazards = null; waveKey = tadpoleKey = null;
            configured = false; AutomaticTick = true; AutomaticPatterns = false; Health = MaxHealth = 0; damageMultiplier = 1; Phase = FrogKingPhase.Opening;
            remaining = recovery = lead = 0; tick = correlation = 0; pattern = SuppressedHazards = 0;
            AttackCommitted = null; transform.localPosition = Vector3.zero;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
