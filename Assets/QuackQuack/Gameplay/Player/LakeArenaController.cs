using System.Collections.Generic;
using QuackQuack.Core.Run;
using QuackQuack.Systems.Pooling;
using QuackQuack.Systems.Run;
using UnityEngine;

namespace QuackQuack.Gameplay.Player
{
    // Small target range for US1. HUD and recurring feedback are integrated separately.
    public sealed class LakeArenaController : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private QuackTarget enemyPrefab, projectilePrefab, rewardPrefab;
        [SerializeField] private Transform[] enemySpawns;
        [SerializeField] private Transform projectileSpawn, rewardSpawn;
        [SerializeField] private float contactDamage = 10, projectileSpeed = 3, projectileInterval = 2.5f;
        [SerializeField] private float rewardRecoverySeconds = 2;
        private readonly List<QuackTarget> enemies = new List<QuackTarget>(4);
        private readonly List<QuackTarget> projectiles = new List<QuackTarget>(12);
        private readonly RiskRewardState reward = new RiskRewardState();
        private GameBootstrap bootstrap;
        private PoolService pools;
        private QuackTarget rewardTarget;
        private string enemyKey, projectileKey, rewardKey;
        private double nextProjectile;
        public bool Ready { get; private set; }
        public bool RewardClaimed => reward.Claimed;
        public PlayerController Player => player;

        private void Start()
        {
            bootstrap = GameBootstrap.Instance;
            if (bootstrap == null) { enabled = false; return; }
            pools = bootstrap.Pools;
            string prefix = "lake-arena-" + GetInstanceID();
            enemyKey = prefix + "-enemy"; projectileKey = prefix + "-projectile"; rewardKey = prefix + "-reward";
            pools.Register(enemyKey, PoolFamily.Enemy, () => Instantiate(enemyPrefab, transform), enemySpawns.Length);
            pools.Register(projectileKey, PoolFamily.Projectile, () => Instantiate(projectilePrefab, transform), 12);
            pools.Register(rewardKey, PoolFamily.Pickup, () => Instantiate(rewardPrefab, transform), 1);
            foreach (var spawn in enemySpawns)
            {
                var target = pools.Acquire<QuackTarget>(enemyKey);
                target.transform.position = spawn.position;
                enemies.Add(target);
            }
            rewardTarget = pools.Acquire<QuackTarget>(rewardKey);
            rewardTarget.transform.position = rewardSpawn.position;
            Ready = true;
        }

        private void Update()
        {
            if (!Ready || bootstrap == null || !bootstrap.Run.IsSimulating || player.State == null) return;
            double now = bootstrap.Run.ElapsedSeconds;
            foreach (var enemy in enemies)
            {
                if (!enemy.gameObject.activeSelf) continue;
                if (enemy.Health <= 0) { pools.Release(enemy); continue; }
                if (now >= enemy.InterruptedUntil && Vector2.Distance(enemy.transform.position, player.transform.position) < 0.8f)
                    DamagePlayer();
            }
            if (now >= nextProjectile)
            {
                nextProjectile = now + projectileInterval;
                var projectile = pools.Acquire<QuackTarget>(projectileKey);
                if (projectile != null)
                {
                    projectile.transform.position = projectileSpawn.position;
                    projectile.IsProjectile = true;
                    projectile.Velocity = ((Vector2)player.transform.position - (Vector2)projectileSpawn.position).normalized * projectileSpeed;
                    projectiles.Add(projectile);
                }
            }
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                var projectile = projectiles[i];
                Vector2 from = projectile.transform.position;
                Vector2 to = from + projectile.Velocity * Time.deltaTime;
                bool release = Mathf.Abs(to.x) > 12.5f || Mathf.Abs(to.y) > 7 || Physics2D.Linecast(from, to, 1 << 8).collider != null;
                projectile.transform.position = to;
                if (!projectile.Reflected && Vector2.Distance(to, player.transform.position) < 0.6f)
                { DamagePlayer(); release = true; }
                if (release) { pools.Release(projectile); projectiles.RemoveAt(i); }
            }
            if (!reward.Claimed && Vector2.Distance(player.transform.position, rewardSpawn.position) < 0.8f && reward.TryClaim())
            {
                pools.Release(rewardTarget);
                player.State.Breath.Recover(rewardRecoverySeconds, TerrainType.Water);
                player.SetStatus("Recompensa da rota de risco: Fôlego recuperado.");
            }
        }

        private void DamagePlayer()
        {
            if (!player.State.TakeDamage(contactDamage, bootstrap.Run.ElapsedSeconds, player.Definition.Invulnerability)) return;
            if (player.State.Health <= 0) bootstrap.Run.TryTransition(RunPhase.Lost);
        }

        private void OnDestroy()
        {
            if (pools == null) return;
            foreach (var enemy in enemies) pools.Release(enemy);
            foreach (var projectile in projectiles) pools.Release(projectile);
            pools.Release(rewardTarget);
        }
    }
}
