using QuackQuack.Content.Characters;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Core.Shared;
using QuackQuack.Gameplay.Terrain;
using QuackQuack.Platform.Input;
using QuackQuack.Systems.Run;
using QuackQuack.Systems.Combat;
using UnityEngine;

namespace QuackQuack.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private CharacterDefinition definition;
        // Keep the serialized name; this list now accepts both land and water volumes.
        [SerializeField] private TerrainZone[] water;
        private IPlayerInput input;
        private KeyboardInputAdapter ownedInput;
        private RunState run;
        private GameplayEventBus events;
        private Rigidbody2D body;
        private Vector2 move;
        private bool requestedQuack;
        private double nextQuack, simulationSeconds;
        private RunPhase previousPhase;
        private float statusUntil;
        private long wave, tick;
        private readonly Collider2D[] overlaps = new Collider2D[128];
        private readonly QuackSystem quack = new QuackSystem();
        public PlayerState State { get; private set; }
        public RelicLoadout Relics { get; set; }
        public float EnemySlowMultiplier {get;private set;}=1;
        public void SetEnemySlow(float multiplier)
        {
            if(float.IsNaN(multiplier)||float.IsInfinity(multiplier)||multiplier<=0||multiplier>1)throw new System.ArgumentOutOfRangeException(nameof(multiplier));
            EnemySlowMultiplier=multiplier;
        }
        public string Status { get; private set; } = "Explore o lago e experimente o Quack.";
        public CharacterDefinition Definition => definition;
        public bool QuackReady => CanSimulate && simulationSeconds >= nextQuack && State.Breath.Current > 0;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void Start()
        {
            if (State != null) return;
            var bootstrap = GameBootstrap.Instance;
            if (bootstrap == null) { Debug.LogError("PlayerController requires run services.", this); enabled = false; return; }
            ownedInput = new KeyboardInputAdapter();
            Initialize(definition, ownedInput, bootstrap.Run, bootstrap.Events);
        }

        // Injected input remains owned by the caller; only the default keyboard is disposed here.
        public void Initialize(CharacterDefinition character, IPlayerInput playerInput, RunState runState, GameplayEventBus eventBus)
        {
            if (State != null) throw new System.InvalidOperationException("Player is already initialized.");
            if (character == null) throw new System.ArgumentNullException(nameof(character));
            if (!character.IsValid(out var reason) || !character.Quack.IsValid(out reason))
                throw new System.ArgumentException(reason, nameof(character));
            input = playerInput ?? throw new System.ArgumentNullException(nameof(playerInput));
            run = runState ?? throw new System.ArgumentNullException(nameof(runState));
            previousPhase = run.Phase;
            events = eventBus ?? throw new System.ArgumentNullException(nameof(eventBus));
            definition = character;
            var q = character.Quack;
            State = new PlayerState(character.Health, q.Capacity, q.LandRecovery, q.WaterRecovery);
            quack.Resolved += ForwardCombat;
            run.Changed += OnRunChanged;
        }

        private void Update()
        {
            if (State == null) return;
            ApplyCommand(input.Read());
        }

        public void ApplyCommand(PlayerCommand command)
        {
            if (State == null || !isActiveAndEnabled) return;
            if (command.PausePressed)
            {
                if (run.IsSimulating) run.TryTransition(RunPhase.Paused);
                else if (run.Phase == RunPhase.Paused) run.TryResume();
                ClearCommands();
                return;
            }
            if (!CanSimulate) { ClearCommands(); return; }
            move = command.Move;
            requestedQuack |= command.QuackPressed;
        }

        private void FixedUpdate()
        {
            if (!CanSimulate) { ClearCommands(); return; }
            simulationSeconds += Time.fixedDeltaTime;
            tick++;
            var terrain = TerrainZone.Resolve(water, body.position, State.Terrain);
            if (terrain != State.Terrain)
            {
                State.Terrain = terrain;
                events.Publish(new PresentationEvent(tick, wave, PresentationEventType.TerrainChanged, EventPriority.Essential, 1, body.position));
            }
            body.linearVelocity = move * TerrainAffinity.Select(definition.LandSpeed, definition.WaterSpeed, terrain);
            if (Relics != null) body.linearVelocity *= 1 + Relics.GetBonus(RelicAttribute.MovementSpeed, terrain);
            body.linearVelocity*=EnemySlowMultiplier;
            bool wasFull = State.Breath.IsFull;
            State.Breath.Recover(Time.fixedDeltaTime * (1 + (Relics?.GetBonus(RelicAttribute.BreathRecovery, terrain) ?? 0)), terrain);
            if (!wasFull && State.Breath.IsFull)
                events.Publish(new PresentationEvent(tick, wave, PresentationEventType.BreathReady, EventPriority.Essential, 1, body.position));
            if (requestedQuack) { ActivateQuack(); requestedQuack = false; }
        }

        public bool ActivateQuack()
        {
            if (!CanSimulate) return false;
            var q = definition.Quack;
            if (simulationSeconds < nextQuack)
            { SetStatus("Quack recarregando."); return false; }
            if (!State.Breath.TryConsume(q.Cost, out var full))
            { SetStatus("Sem Fôlego para usar o Quack."); return false; }
            nextQuack = simulationSeconds + q.Cooldown;
            quack.BeginWave(++wave);
            SetStatus(full ? "QUACK APRIMORADO!" : "Quack!");
            int source = GetInstanceID();
            events.Publish(new CombatEvent(tick, wave, source, source, body.position, q.Range, CombatEventType.QuackActivated));
            events.Publish(new PresentationEvent(tick, wave, PresentationEventType.BreathConsumed, EventPriority.Essential, 0.5f, body.position));
            var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = 1 << 9 };
            int count = Physics2D.OverlapCircle(body.position, q.Range, filter, overlaps);
            for (int i = 0; i < count; i++)
            {
                var target = overlaps[i].GetComponentInParent<QuackTarget>();
                if (target == null || target.Health <= 0) continue;
                if (target.IsProjectile && !q.CanReflect(target.Light, target.Reflectable)) continue;
                int id = target.GetInstanceID();
                if (!quack.TryHit(id, target.Interruptible, target.IsProjectile, full, q.Damage, q.Push, q.Interruption, q.FullMultiplier, out var hit)) continue;
                if (target.IsProjectile)
                {
                    target.Velocity = QuackSystem.ReflectedVelocity(target.transform.position, body.position, q.ReflectionSpeed);
                    target.Reflected = true;
                }
                else
                {
                    if (target.HasCombatBinding)
                    {
                        Vector2 position = target.transform.position;
                        var push = (position - body.position).normalized * hit.Knockback;
                        target.ApplyBoundControl(push, hit.InterruptSeconds);
                        if (target.ApplyBoundDamage(hit.Damage, source, tick, body.position) && full)
                            events.Publish(new CombatEvent(tick, wave, source, id, position, 0, CombatEventType.CriticalHit));
                        if (!CanSimulate) break;
                        continue; // Bound actors publish only the accepted damage; no duplicate Quack damage event.
                    }
                    target.Health = Mathf.Max(0, target.Health - hit.Damage);
                    if (hit.InterruptSeconds > 0)
                        target.InterruptedUntil = Mathf.Max(target.InterruptedUntil, (float)run.ElapsedSeconds + hit.InterruptSeconds);
                    var delta = (Vector2)target.transform.position - body.position;
                    if (target.ApplyKnockback(delta.normalized * hit.Knockback) && quack.TryCollision(id))
                        quack.TryEmitCollisionEvent(id, tick, source, target.transform.position);
                }
                quack.TryEmitHitEvents(id, tick, source, target.transform.position);
            }
            return true;
        }

        public void SetStatus(string value) { Status = value; statusUntil = Time.time + 2; }
        public HudSnapshot Snapshot()
        {
            string status = run.Phase == RunPhase.Paused ? "PAUSADO • Esc para continuar" : run.Outcome.HasValue ?
                (run.Outcome == RunPhase.Won ? "VITÓRIA • Sapo-Rei derrotado" : run.Outcome == RunPhase.Lost ? "DERROTA • Vida esgotada" : "PARTIDA ABANDONADA") : Time.time < statusUntil ? Status : "Use a água para recuperar Fôlego mais rápido.";
            string quackState = !CanSimulate ? "QUACK INDISPONÍVEL" : simulationSeconds < nextQuack ? "RECARREGANDO" :
                State.Breath.Current <= 0 ? "SEM FÔLEGO" : State.Breath.IsFull ? "PRONTO • APRIMORADO" : "PRONTO • QUACK";
            return new HudSnapshot(State.Health, State.MaxHealth, State.Breath.Current, State.Breath.Capacity,
                State.Terrain == TerrainType.Water ? "ÁGUA • mais velocidade e recuperação" : "TERRA • use as margens para se posicionar", status, run.ElapsedSeconds,
                QuackReady, quackState);
        }
        private bool CanSimulate => State != null && isActiveAndEnabled && run.IsSimulating && State.Health > 0;
        private void ForwardCombat(CombatEvent value) => events.Publish(value);
        private void OnRunChanged(RunPhase phase)
        {
            if (!run.IsSimulating) ClearCommands();
            if (previousPhase == RunPhase.ChoosingUpgrade && run.IsSimulating)
            { input?.Read(); ClearCommands(); }
            previousPhase = phase;
        }
        private void ClearCommands()
        {
            move = Vector2.zero; requestedQuack = false;
            if (body != null) body.linearVelocity = Vector2.zero;
        }
        private void OnDisable() { ClearCommands(); }
        private void OnEnable()
        {
            // Discard edges accumulated by an adapter while gameplay was disabled.
            input?.Read();
        }
        private void OnDestroy()
        {
            if (run != null) run.Changed -= OnRunChanged;
            quack.Resolved -= ForwardCombat;
            ownedInput?.Dispose();
        }
    }
}
