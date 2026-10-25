using System.Collections.Generic;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Run;
using QuackQuack.Presentation.UI;
using QuackQuack.Presentation.VFX;
using QuackQuack.Systems.Pooling;
using QuackQuack.Systems.Run;
using UnityEngine;

namespace QuackQuack.Gameplay.Player
{
    public sealed class PooledFeedback : MonoBehaviour, IPoolLifecycle
    {
        public QuackFeedbackController View { get; private set; }
        public void Initialize(Material material) { View = gameObject.AddComponent<QuackFeedbackController>(); View.Initialize(material); }
        public void OnAcquire() { }
        public void OnRelease() { }
        public void ResetState() { View.ResetEffect(); }
    }

    public sealed class MvpArena : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private GameplayHud hud;
        [SerializeField] private Sprite circle;
        [SerializeField] private Material lineMaterial;
        [SerializeField] private Transform riskReward;
        private readonly List<QuackTarget> enemies = new List<QuackTarget>(12);
        private readonly List<QuackTarget> projectiles = new List<QuackTarget>(12);
        private readonly RiskRewardState reward = new RiskRewardState();
        private PoolService pools;
        private GameBootstrap bootstrap;
        private AudioSource audioSource;
        private AudioClip quackClip, hitClip;
        private float nextProjectile;
        private int nextPoolId;
        private string enemyKey, projectileKey, effectKey;
        private readonly Color enemyColor = new Color(0.84f, 0.35f, 0.43f);

        private void Start()
        {
            bootstrap = GameBootstrap.Instance;
            pools = bootstrap.Pools;
            string prefix = "mvp-" + GetInstanceID();
            enemyKey = prefix + "-enemy"; projectileKey = prefix + "-projectile"; effectKey = prefix + "-vfx";
            pools.Register(enemyKey, PoolFamily.Enemy, () => CreateTarget(false), 12);
            pools.Register(projectileKey, PoolFamily.Projectile, () => CreateTarget(true), 12);
            pools.Register(effectKey, PoolFamily.Vfx, () =>
            {
                var effect = new GameObject("Pooled feedback").AddComponent<PooledFeedback>();
                effect.Initialize(lineMaterial);
                return effect;
            }, 32);
            for (int i = 0; i < 6; i++)
            {
                var enemy = pools.Acquire<QuackTarget>(enemyKey);
                enemy.transform.position = new Vector2((float)bootstrap.Random.NextDouble() * 20 - 10, 3 + (float)bootstrap.Random.NextDouble() * 3);
                enemies.Add(enemy);
            }
            hud.Bind(player.Snapshot);
            bootstrap.Events.Combat += OnCombat;
            bootstrap.Events.Presentation += OnPresentation;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            quackClip = CreateTone("Quack", 220, 0.16f);
            hitClip = CreateTone("Impact", 620, 0.06f);
        }

        private QuackTarget CreateTarget(bool projectile)
        {
            var obj = new GameObject(projectile ? "Light projectile" : "Target enemy");
            obj.layer = 9;
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = circle; renderer.color = projectile ? new Color(0.81f, 0.62f, 1) : enemyColor;
            renderer.sortingOrder = 3;
            obj.transform.localScale = Vector3.one * (projectile ? 0.28f : 0.7f);
            obj.AddComponent<CircleCollider2D>().isTrigger = true;
            var body = obj.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
            var target = obj.AddComponent<QuackTarget>(); target.IsProjectile = projectile;
            obj.SetActive(false);
            return target;
        }

        private void Update()
        {
            if (bootstrap == null || !bootstrap.Run.IsSimulating) return;
            float dt = Time.deltaTime;
            foreach (var enemy in enemies)
            {
                if (!enemy.gameObject.activeSelf) continue;
                if (enemy.Health <= 0) { pools.Release(enemy); continue; }
                Vector2 difference = player.transform.position - enemy.transform.position;
                if (Time.time >= enemy.InterruptedUntil)
                {
                    enemy.transform.position += (Vector3)(difference.normalized * dt * 0.65f);
                    enemy.GetComponent<SpriteRenderer>().color = enemyColor;
                }
                if (difference.sqrMagnitude < 0.7f) DamagePlayer();
            }
            if (Time.time >= nextProjectile)
            {
                nextProjectile = Time.time + 2.5f;
                var projectile = pools.Acquire<QuackTarget>(projectileKey);
                if (projectile != null)
                {
                    projectile.transform.position = new Vector2(-11, 2);
                    projectile.Velocity = ((Vector2)player.transform.position - (Vector2)projectile.transform.position).normalized * 3;
                    projectiles.Add(projectile);
                }
            }
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                var projectile = projectiles[i];
                projectile.transform.position += (Vector3)(projectile.Velocity * dt);
                bool release = Mathf.Abs(projectile.transform.position.x) > 13 || Mathf.Abs(projectile.transform.position.y) > 8;
                if (!projectile.Reflected && Vector2.Distance(projectile.transform.position, player.transform.position) < 0.55f)
                { DamagePlayer(); release = true; }
                if (release) { pools.Release(projectile); projectiles.RemoveAt(i); }
            }
            if (riskReward != null && !reward.Claimed && Vector2.Distance(player.transform.position, riskReward.position) < 0.8f && reward.TryClaim())
            {
                riskReward.gameObject.SetActive(false);
                player.SetStatus("RECOMPENSA DA ROTA DE RISCO! • recupera Fôlego");
                player.State.Breath.Recover(2, TerrainType.Water);
                Effect(player.transform.position, 2, Color.yellow);
            }
        }

        private void DamagePlayer()
        {
            if (!player.State.TakeDamage(10, bootstrap.Run.ElapsedSeconds, player.Definition.Invulnerability)) return;
            Effect(player.transform.position, 0.8f, Color.red);
            audioSource.PlayOneShot(hitClip, 0.3f);
            if (player.State.Health <= 0) bootstrap.Run.TryTransition(RunPhase.Lost);
        }
        private void OnCombat(CombatEvent value)
        {
            switch (value.Type)
            {
                case CombatEventType.QuackActivated:
                    Effect(value.Position, value.Amount, new Color(0.98f, 0.85f, 0.35f)); audioSource.PlayOneShot(quackClip, 0.35f); break;
                case CombatEventType.ProjectileReflected: Effect(value.Position, 0.65f, Color.cyan); break;
                case CombatEventType.CriticalHit:
                    Effect(value.Position, 0.7f, Color.white);
                    foreach (var enemy in enemies) if (enemy.GetInstanceID() == value.Target) enemy.GetComponent<SpriteRenderer>().color = Color.white;
                    break;
                case CombatEventType.DamageApplied: Effect(value.Position, 0.4f, new Color(1, 0.5f, 0.4f)); break;
                case CombatEventType.CollisionTriggered: Effect(value.Position, 0.65f, Color.yellow); break;
            }
        }
        private void OnPresentation(PresentationEvent value)
        {
            if (value.Type == PresentationEventType.BreathReady) Effect(value.Position, 1, Color.yellow);
        }
        private void Effect(Vector2 position, float radius, Color tint)
        {
            var lease = pools.Acquire<PooledFeedback>(effectKey);
            if (lease == null) return;
            lease.View.Play(position, radius, tint, 0.4f);
            lease.View.Finished = () => pools.Release(lease);
        }
        private static AudioClip CreateTone(string name, float frequency, float duration)
        {
            var samples = new float[(int)(22050 * duration)];
            for (int i = 0; i < samples.Length; i++)
                samples[i] = Mathf.Sin(i * frequency * Mathf.PI * 2 / 22050 * (1 - 0.5f * i / samples.Length)) * (1 - (float)i / samples.Length) * 0.4f;
            var clip = AudioClip.Create(name, samples.Length, 1, 22050, false);
            clip.SetData(samples, 0); return clip;
        }
        private void OnDestroy()
        {
            if (bootstrap != null) { bootstrap.Events.Combat -= OnCombat; bootstrap.Events.Presentation -= OnPresentation; }
            if (quackClip != null) Destroy(quackClip);
            if (hitClip != null) Destroy(hitClip);
        }
    }
}
