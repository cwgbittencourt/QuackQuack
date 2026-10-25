using QuackQuack.Content.Characters;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Run;
using QuackQuack.Core.Shared;
using QuackQuack.Gameplay.Terrain;
using QuackQuack.Platform.Input;
using QuackQuack.Systems.Run;
using UnityEngine;

namespace QuackQuack.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private CharacterDefinition definition;
        [SerializeField] private TerrainZone[] water;
        private KeyboardInputAdapter input;
        private Rigidbody2D body;
        private Vector2 move;
        private bool requestedQuack;
        private float nextQuack, statusUntil;
        private long wave;
        private readonly Collider2D[] overlaps = new Collider2D[128];
        private readonly QuackSystem quack = new QuackSystem();
        public PlayerState State { get; private set; }
        public string Status { get; private set; } = "Explore o lago e experimente o Quack.";
        public CharacterDefinition Definition => definition;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.freezeRotation = true;
            var q = definition.Quack;
            State = new PlayerState(definition.Health, q.Capacity, q.LandRecovery, q.WaterRecovery);
            input = new KeyboardInputAdapter();
        }

        private void Update()
        {
            var command = input.Read();
            var run = GameBootstrap.Instance?.Run;
            if (run == null) return;
            if (command.PausePressed)
            {
                if (run.Phase == RunPhase.Playing) run.TryTransition(RunPhase.Paused);
                else if (run.Phase == RunPhase.Paused) run.TryTransition(RunPhase.Playing);
            }
            if (!run.IsSimulating) { move = Vector2.zero; requestedQuack = false; return; }
            move = command.Move;
            requestedQuack |= command.QuackPressed;
        }

        private void FixedUpdate()
        {
            var bootstrap = GameBootstrap.Instance;
            if (bootstrap == null || !bootstrap.Run.IsSimulating) { body.linearVelocity = Vector2.zero; return; }
            var terrain = TerrainType.Land;
            foreach (var zone in water) if (zone.Contains(body.position, State.Terrain)) { terrain = TerrainType.Water; break; }
            if (terrain != State.Terrain)
            {
                State.Terrain = terrain;
                bootstrap.Events.Publish(new PresentationEvent(wave, wave, PresentationEventType.TerrainChanged, EventPriority.Essential, 1, body.position));
            }
            body.linearVelocity = move * (terrain == TerrainType.Water ? definition.WaterSpeed : definition.LandSpeed);
            bool wasFull = State.Breath.IsFull;
            State.Breath.Recover(Time.fixedDeltaTime, terrain);
            if (!wasFull && State.Breath.IsFull)
                bootstrap.Events.Publish(new PresentationEvent(wave, wave, PresentationEventType.BreathReady, EventPriority.Essential, 1, body.position));
            if (requestedQuack) { ActivateQuack(); requestedQuack = false; }
        }

        public bool ActivateQuack()
        {
            var bootstrap = GameBootstrap.Instance;
            if (bootstrap == null || !bootstrap.Run.IsSimulating) return false;
            var q = definition.Quack;
            if (Time.time < nextQuack || !State.Breath.TryConsume(q.Cost, out var full))
            { SetStatus("Sem fôlego ou Quack recarregando."); return false; }
            nextQuack = Time.time + q.Cooldown;
            quack.BeginWave(++wave);
            SetStatus(full ? "QUACK APRIMORADO!" : "Quack!");
            bootstrap.Events.Publish(new CombatEvent(wave, wave, 0, 0, body.position, q.Range, CombatEventType.QuackActivated));
            bootstrap.Events.Publish(new PresentationEvent(wave, wave, PresentationEventType.BreathConsumed, EventPriority.Essential, 0.5f, body.position));
            var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = 1 << 9 };
            int count = Physics2D.OverlapCircle(body.position, q.Range, filter, overlaps);
            for (int i = 0; i < count; i++)
            {
                var target = overlaps[i].GetComponent<QuackTarget>();
                if (target == null) continue;
                int id = target.GetInstanceID();
                if (!quack.TryHit(id, target.Interruptible, target.IsProjectile, full, q.Damage, q.Push, q.Interruption, q.FullMultiplier, out var hit)) continue;
                if (target.IsProjectile)
                {
                    if (!QuackSystem.CanReflect(target.Light, target.Reflectable)) continue;
                    target.Velocity = QuackSystem.ReflectedVelocity(target.transform.position, body.position, 7);
                    target.Reflected = true;
                    bootstrap.Events.Publish(new CombatEvent(wave, wave, 0, id, target.transform.position, 0, CombatEventType.ProjectileReflected));
                }
                else
                {
                    target.Health = Mathf.Max(0, target.Health - hit.Damage);
                    target.InterruptedUntil = Time.time + hit.InterruptSeconds;
                    var delta = (Vector2)target.transform.position - body.position;
                    if (target.ApplyKnockback(delta.normalized * hit.Knockback) && quack.TryCollision(id))
                        bootstrap.Events.Publish(new CombatEvent(wave, wave, 0, id, target.transform.position, 0, CombatEventType.CollisionTriggered));
                    bootstrap.Events.Publish(new CombatEvent(wave, wave, 0, id, target.transform.position, hit.Damage, CombatEventType.DamageApplied));
                    if (hit.Critical) bootstrap.Events.Publish(new CombatEvent(wave, wave, 0, id, target.transform.position, hit.Damage, CombatEventType.CriticalHit));
                }
            }
            return true;
        }

        public void SetStatus(string value) { Status = value; statusUntil = Time.time + 2; }
        public HudSnapshot Snapshot()
        {
            var run = GameBootstrap.Instance.Run;
            string status = run.Phase == RunPhase.Paused ? "PAUSADO • Esc para continuar" : run.Phase == RunPhase.Lost ? "FIM DO TESTE • Abra Bootstrap para reiniciar" : Time.time < statusUntil ? Status : "WASD / setas: mover    Espaço: Quack    Esc: pausa";
            return new HudSnapshot(State.Health, State.MaxHealth, State.Breath.Current, State.Breath.Capacity,
                State.Terrain == TerrainType.Water ? "ÁGUA • mais velocidade e recuperação" : "TERRA • use as margens para se posicionar", status, run.ElapsedSeconds);
        }
        private void OnDestroy() { input?.Dispose(); }
    }
}
