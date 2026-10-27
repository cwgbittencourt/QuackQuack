using System;
using System.Collections.Generic;
using QuackQuack.Content.Enemies;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Enemies;
using QuackQuack.Systems.Combat;
using QuackQuack.Systems.Pooling;
using UnityEngine;

namespace QuackQuack.Systems.Spawning
{
    // One caller drives Process with simulation ticks and the current camera bounds.
    public sealed class SpawnDirector : IDisposable
    {
        private struct Pending { public SpawnRequest Request; public int Remaining; }
        private struct Lease { public PooledEnemy Enemy; public long Id; }
        private readonly RunState run;
        private readonly PoolService pools;
        private readonly GameplayEventBus events;
        private readonly Transform target;
        private readonly Action<EnemyReward> reward;
        private readonly Dictionary<string, EnemyDefinition> definitions = new Dictionary<string, EnemyDefinition>(StringComparer.Ordinal);
        private readonly Pending[] pending;
        private readonly Lease[] active;
        private readonly int budget, attempts;
        private readonly float exclusionPadding;
        private int pendingCount;
        private long lastRequest, lastTick = -1;
        private uint random;
        private bool disposed, processing;
        public int Capacity => active.Length;
        private int activeLimit;
        public int ActiveLimit { get => activeLimit; set { if (value <= 0 || value > active.Length) throw new ArgumentOutOfRangeException(nameof(value)); activeLimit = value; } }
        public int PendingCount => pendingCount;
        public long SpawnedCount { get; private set; }
        public long ExpiredCount { get; private set; }
        public long ReducedCount { get; private set; }
        public int ActiveCount { get { int count=0; for(int i=0;i<active.Length;i++) if(Live(active[i])) count++; return count; } }

        public SpawnDirector(RunState run, PoolService pools, GameplayEventBus events, Transform target,
            IEnumerable<EnemyDefinition> definitions, int activeLimit, int queueCapacity, int spawnBudget,
            int attemptsPerSpawn, float exclusionPadding, int seed, Action<EnemyReward> reward)
        {
            this.run=run ?? throw new ArgumentNullException(nameof(run)); this.pools=pools ?? throw new ArgumentNullException(nameof(pools));
            this.events=events ?? throw new ArgumentNullException(nameof(events)); this.target=target != null ? target : throw new ArgumentNullException(nameof(target));
            this.reward=reward ?? throw new ArgumentNullException(nameof(reward));
            if(definitions==null || activeLimit<=0 || queueCapacity<=0 || spawnBudget<=0 || attemptsPerSpawn<=0 ||
                float.IsNaN(exclusionPadding) || float.IsInfinity(exclusionPadding) || exclusionPadding<0) throw new ArgumentException("Invalid bounded spawn configuration.");
            foreach(var definition in definitions)
            {
                if(definition==null || !definition.IsValid(out _) || !pools.IsRegistered<PooledEnemy>(definition.PoolKey,PoolFamily.Enemy))
                    throw new ArgumentException("Each valid enemy definition requires a registered PooledEnemy pool.");
                this.definitions.Add(definition.Id,definition);
            }
            if(this.definitions.Count==0) throw new ArgumentException("Enemy catalog is empty.");
            pending=new Pending[queueCapacity]; active=new Lease[activeLimit]; this.activeLimit=activeLimit; budget=spawnBudget; attempts=attemptsPerSpawn;
            this.exclusionPadding=exclusionPadding; random=unchecked((uint)seed); if(random==0) random=0x9e3779b9;
        }

        public bool TryEnqueue(SpawnRequest request)
        {
            if(disposed || pools.IsDisposed || target==null || run.Outcome.HasValue || !request.IsValid || request.RequestId<=lastRequest || pendingCount==pending.Length ||
                !definitions.ContainsKey(request.EnemyId)) return false;
            pending[pendingCount++]=new Pending{Request=request,Remaining=request.Quantity}; lastRequest=request.RequestId; return true;
        }

        public int Process(long simulationTick, Rect visibleArea)
        {
            if(disposed || processing) return 0;
            if(run.Outcome.HasValue || pools.IsDisposed || target==null) { Clear(); return 0; }
            if(!run.IsSimulating) return 0;
            if(simulationTick<0 || simulationTick<lastTick || !SpawnRequest.ValidRegion(visibleArea)) throw new ArgumentException("Current bounds and monotonic simulation tick required.");
            if(simulationTick==lastTick) return 0;
            lastTick=simulationTick; processing=true;
            int spawned=0, work=0;
            try
            {
                Prune();
                // Expiration precedes priority and capacity checks, so blocked requests cannot survive their deadline.
                for(int i=pendingCount-1;i>=0;i--)
                    if(simulationTick>pending[i].Request.DeadlineTick) { ExpiredCount+=pending[i].Remaining; Remove(i); }
                while(pendingCount>0 && work<budget)
                {
                    int index=Best(); var item=pending[index];
                    if(pools.GetActiveCount(PoolFamily.Enemy)>=activeLimit) { ReducedCount+=item.Remaining; Remove(index); continue; }
                    work++;
                    if(!TryPosition(item.Request,visibleArea,out Vector2 position)) { ReducedCount+=item.Remaining; Remove(index); continue; }
                    int slot=FreeSlot();
                    if(slot<0) { ReducedCount+=item.Remaining; Remove(index); continue; }
                    var enemy=PooledEnemy.SpawnPrepared(pools,definitions[item.Request.EnemyId],run,events,target,position,reward);
                    if(enemy==null) { ReducedCount+=item.Remaining; Remove(index); continue; }
                    active[slot]=new Lease{Enemy=enemy,Id=enemy.LeaseId}; spawned++; SpawnedCount++;
                    item.Remaining--;
                    if(item.Remaining==0) Remove(index); else pending[index]=item;
                }
            }
            finally { processing=false; }
            return spawned;
        }

        public bool TryGetActive(int slot,out PooledEnemy enemy,out long leaseId)
        {
            enemy=null; leaseId=0;
            if(slot<0 || slot>=active.Length || !Live(active[slot])) return false;
            enemy=active[slot].Enemy; leaseId=active[slot].Id; return true;
        }
        private bool Live(Lease value) => value.Enemy!=null && value.Enemy.LeaseId==value.Id && pools.IsActive(value.Enemy,PoolFamily.Enemy);
        private void Prune() { for(int i=0;i<active.Length;i++) if(!Live(active[i])) active[i]=default; }
        private int FreeSlot() { for(int i=0;i<active.Length;i++) if(!Live(active[i])) return i; return -1; }
        private int Best()
        {
            int best=0;
            for(int i=1;i<pendingCount;i++)
                if(pending[i].Request.Priority>pending[best].Request.Priority || (pending[i].Request.Priority==pending[best].Request.Priority &&
                    pending[i].Request.RequestId<pending[best].Request.RequestId)) best=i;
            return best;
        }
        private void Remove(int index) { pendingCount--; pending[index]=pending[pendingCount]; pending[pendingCount]=default; }
        private bool TryPosition(SpawnRequest request,Rect visible,out Vector2 position)
        {
            position=default;
            for(int i=0;i<attempts;i++)
            {
                var point=new Vector2(request.Region.xMin+Unit()*request.Region.width,request.Region.yMin+Unit()*request.Region.height);
                Vector2 player=target.position;
                if(float.IsNaN(player.x) || float.IsNaN(player.y) || float.IsInfinity(player.x) || float.IsInfinity(player.y)) return false;
                if(point.x>=visible.xMin-exclusionPadding && point.x<=visible.xMax+exclusionPadding &&
                    point.y>=visible.yMin-exclusionPadding && point.y<=visible.yMax+exclusionPadding) continue;
                double dx=(double)point.x-player.x, dy=(double)point.y-player.y;
                if(dx*dx+dy*dy<(double)request.MinimumDistance*request.MinimumDistance) continue;
                position=point; return true;
            }
            return false;
        }
        private float Unit() { random^=random<<13; random^=random>>17; random^=random<<5; return (random>>8)*(1f/16777216f); }
        private void Clear()
        {
            for(int i=0;i<active.Length;i++) { var value=active[i]; if(Live(value)) value.Enemy.Despawn(value.Id); active[i]=default; }
            Array.Clear(pending,0,pending.Length); pendingCount=0;
        }
        public void Dispose() { if(disposed) return; Clear(); disposed=true; }
    }
}
