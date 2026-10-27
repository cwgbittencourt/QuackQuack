using QuackQuack.Gameplay.Bosses;
using QuackQuack.Gameplay.Player;
using UnityEngine;
namespace QuackQuack.Gameplay.Enemies
{
    // Stable delegates per pooled actor; every call resolves its current lease.
    [RequireComponent(typeof(QuackTarget))]
    public sealed class CombatTargetBinding : MonoBehaviour
    {
        public EnemyUpdateScheduler Scheduler { get; set; }
        private long configuredLease;
        public void Configure(EnemyUpdateScheduler scheduler, float healthMultiplier, float damageMultiplier)
        {
            Scheduler = scheduler;
            if (enemy != null && configuredLease != enemy.LeaseId && enemy.ConfigureEncounter(healthMultiplier, damageMultiplier)) configuredLease = enemy.LeaseId;
        }
        private PooledEnemy enemy;
        private FrogKingController boss;
        private void Awake()
        {
            enemy = GetComponent<PooledEnemy>(); boss = GetComponent<FrogKingController>();
            var target = GetComponent<QuackTarget>(); target.Interruptible = enemy != null;
            target.BindCombat(ReadHealth, Damage, Control);
        }
        private float ReadHealth() => enemy != null ? (enemy.IsReady ? enemy.State.Health : 0) : boss != null ? boss.Health : 0;
        private bool Damage(float amount, int source, long tick, Vector2 attacker) => enemy != null
            ? Scheduler != null && Scheduler.ApplyHit(enemy, enemy.LeaseId, amount, attacker, source, tick)
            : boss != null && boss.TakeDamage(amount, source, tick);
        private void Control(Vector2 push, float interruption)
        { if (enemy != null && Scheduler != null) Scheduler.ApplyControl(enemy, enemy.LeaseId, push, interruption); }
    }
}
