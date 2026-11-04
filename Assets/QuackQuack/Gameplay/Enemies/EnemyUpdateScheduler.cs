using System;
using System.Collections.Generic;
using QuackQuack.Content.Enemies;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Player;
using QuackQuack.Systems.Combat;
using QuackQuack.Systems.Pooling;
using QuackQuack.Systems.Spawning;
using UnityEngine;
using Unity.Profiling;

namespace QuackQuack.Gameplay.Enemies
{
    public sealed class EnemyUpdateScheduler:MonoBehaviour
    {
        public const string ProfilerMarkerName = "QuackQuack.EnemyScheduler.Advance";
        private static readonly ProfilerMarker AdvanceMarker = new ProfilerMarker(ProfilerMarkerName);
        public double TotalAdvanceMilliseconds { get; private set; }
        public long AdvanceCount { get; private set; }
        private struct Slot
        {
            public PooledEnemy Enemy;public long Lease;public Vector2 Heading,LockedPoint;
            public double NextAttack,NextContact,WarningUntil,ChargeFrom,ChargeUntil;
            public EnemyAreaEffect Warning;public long WarningLease;
        }
        private Slot[] slots;
        private EnemyAreaEffect[] effects;
        private readonly Dictionary<string,string> effectKeys=new Dictionary<string,string>(StringComparer.Ordinal);
        private readonly RaycastHit2D[] contacts=new RaycastHit2D[16];
        private RunState run;private SpawnDirector director;private PoolService pools;private GameplayEventBus events;
        private PlayerController player;private Rigidbody2D playerBody;private CircleCollider2D playerShape;private EnemyBehaviorSettings settings;
        private int cursor;private double clock;private long tick,correlation;private bool ready,advancing;
        public bool AutomaticTick{get;set;}=true;
        public int LastSteeringCount{get;private set;}
        public int SuppressedAttacks{get;private set;}
        public int ActiveEffectCount{get{int count=0;if(effects!=null)foreach(var e in effects)if(e!=null&&e.LeaseId>0)count++;return count;}}

        public void Initialize(RunState run,SpawnDirector director,PoolService pools,GameplayEventBus events,
            PlayerController player,EnemyBehaviorSettings settings,IEnumerable<EnemyDefinition> definitions)
        {
            if(ready)throw new InvalidOperationException("Scheduler already initialized.");
            if(run==null||director==null||pools==null||events==null||player==null||player.State==null||settings==null||!settings.IsValid||definitions==null)
                throw new ArgumentException("Valid scheduler services, player and settings required.");
            this.run=run;this.director=director;this.pools=pools;this.events=events;this.player=player;playerBody=player.GetComponent<Rigidbody2D>();playerShape=player.GetComponent<CircleCollider2D>();this.settings=settings;
            int types=0;
            foreach(var d in definitions)
            {
                if(d==null||!d.IsValid(out _))throw new ArgumentException("Invalid enemy definition.");
                if(!d.HasTelegraphedAttack)continue;
                string key="enemy-field/"+GetInstanceID()+"/"+d.Id;var prefab=d.TelegraphVisual;
                pools.Register(key,PoolFamily.Vfx,()=>
                {
                    var root=Instantiate(prefab);root.SetActive(false);
                    var effect=root.GetComponent<EnemyAreaEffect>();if(effect==null)effect=root.AddComponent<EnemyAreaEffect>();return effect;
                },settings.EffectCapacityPerType);
                effectKeys.Add(d.Id,key);types++;
            }
            slots=new Slot[director.Capacity];effects=new EnemyAreaEffect[types*settings.EffectCapacityPerType];
            run.Changed+=PhaseChanged;ready=true;
        }
        private void FixedUpdate(){if(AutomaticTick)Advance(Time.fixedDeltaTime);}
        public void Advance(float seconds)
        {
            if(!ready||advancing||!isActiveAndEnabled)return;
            if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));
            if(pools.IsDisposed||run.Outcome.HasValue||player==null){Clear();return;}
            if(!run.IsSimulating)return;
            using var scope = AdvanceMarker.Auto();
            long started = System.Diagnostics.Stopwatch.GetTimestamp();
            advancing=true;clock+=seconds;tick++;
            try
            {
                UpdateFields(seconds);
                LastSteeringCount=0;
                for(int i=0;i<slots.Length;i++)
                {
                    if(!director.TryGetActive(i,out var enemy,out long lease)){CancelWarning(ref slots[i]);slots[i]=default;continue;}
                    if(slots[i].Enemy!=enemy||slots[i].Lease!=lease)
                    {
                        CancelWarning(ref slots[i]);slots[i]=new Slot{Enemy=enemy,Lease=lease,Heading=((Vector2)player.transform.position-enemy.Body.position).normalized};
                    }
                }
                int count=Math.Min(settings.SteeringBudget,slots.Length);
                for(int n=0;n<count;n++)
                {
                    int index=(cursor+n)%slots.Length;ref var slot=ref slots[index];
                    if(slot.Enemy==null)continue;
                    if(slot.WarningUntil<=0&&slot.ChargeUntil<=clock)
                    {slot.Heading=((Vector2)player.transform.position-slot.Enemy.Body.position).normalized;LastSteeringCount++;}
                }
                cursor=(cursor+count)%slots.Length;
                for(int i=0;i<slots.Length;i++)
                {
                    ref var slot=ref slots[i];if(slot.Enemy==null)continue;
                    Step(ref slot,seconds);
                    if(!run.IsSimulating||pools.IsDisposed)break;
                }
            }
            finally
            {
                advancing=false;
                TotalAdvanceMilliseconds += (System.Diagnostics.Stopwatch.GetTimestamp() - started) * (1000.0 / System.Diagnostics.Stopwatch.Frequency);
                AdvanceCount++;
            }
        }
        private void Step(ref Slot slot,float seconds)
        {
            var enemy=slot.Enemy;var d=enemy.Definition;
            if(d==null||!enemy.IsReady||enemy.LeaseId!=slot.Lease)return;
            if(enemy.State.InterruptionRemaining>0||enemy.State.Knockback.sqrMagnitude>0)
            {
                CancelWarning(ref slot);slot.ChargeUntil=0;
                Move(enemy,enemy.State.Knockback*seconds);
                enemy.State.TrySetControl(slot.Lease,Vector2.MoveTowards(enemy.State.Knockback,Vector2.zero,settings.KnockbackDecay*seconds),
                    Mathf.Max(0,enemy.State.InterruptionRemaining-seconds));return;
            }
            float distance=Vector2.Distance(enemy.Body.position,player.transform.position);
            float movementSeconds=seconds;
            if(slot.WarningUntil>0&&clock>=slot.WarningUntil)
            {
                double deadline=slot.WarningUntil;movementSeconds=(float)Math.Min(seconds,clock-deadline);
                bool signaled=slot.Warning!=null&&slot.Warning.LeaseId==slot.WarningLease;
                CancelWarning(ref slot);
                if(signaled)
                {
                    if(d.Role==EnemyRole.Charger){slot.ChargeFrom=deadline;slot.ChargeUntil=deadline+d.ChargeDuration;slot.Heading=(slot.LockedPoint-enemy.Body.position).normalized;}
                    else
                    {
                        SpawnField(enemy,false,slot.LockedPoint,d.EffectDuration,d.SlowMultiplier);
                        if(d.Role==EnemyRole.WebCaster&&Vector2.Distance(slot.LockedPoint,player.transform.position)<=settings.FieldRadius)DamagePlayer(enemy,d.AttackDamage);
                    }
                }
            }
            if(!run.IsSimulating)return;
            bool charging=slot.ChargeUntil>clock;
            float chargeSeconds=(float)Math.Max(0,Math.Min(clock,slot.ChargeUntil)-Math.Max(clock-seconds,slot.ChargeFrom));
            chargeSeconds=Mathf.Min(chargeSeconds,movementSeconds);
            if(d.HasTelegraphedAttack&&slot.WarningUntil<=0&&!charging&&chargeSeconds<=0&&clock>=slot.NextAttack&&distance<=d.AttackRange&&
                (d.Role==EnemyRole.SlowingTrail||Physics2D.Linecast(enemy.Body.position,player.transform.position,1<<8).collider==null))
            {
                slot.NextAttack=clock+d.AttackInterval;
                slot.LockedPoint=d.Role==EnemyRole.SlowingTrail?enemy.Body.position:(Vector2)player.transform.position;
                var warning=SpawnField(enemy,true,slot.LockedPoint,d.TelegraphSeconds,1);
                if(warning!=null)
                {
                    slot.Warning=warning;slot.WarningLease=warning.LeaseId;slot.WarningUntil=clock+d.TelegraphSeconds;
                    events.Publish(new PresentationEvent(tick,warning.LeaseId,PresentationEventType.DangerTelegraphed,EventPriority.Danger,d.TelegraphSeconds,slot.LockedPoint));
                }
                else SuppressedAttacks++;
            }
            if(!run.IsSimulating||!enemy.IsReady||enemy.LeaseId!=slot.Lease)return;
            bool holding=slot.WarningUntil>0 || (d.Role==EnemyRole.WebCaster&&distance<=d.AttackRange*settings.PreferredRangeFraction);
            if(slot.Heading.sqrMagnitude>0)enemy.Body.rotation=Mathf.Atan2(slot.Heading.y,slot.Heading.x)*Mathf.Rad2Deg;
            Vector2 previous=enemy.Body.position;
            if(!holding)Move(enemy,slot.Heading*(d.MoveSpeed*(movementSeconds-chargeSeconds)+d.ChargeSpeed*chargeSeconds));
            float reach=settings.ContactRadius+enemy.Shape.radius*Mathf.Max(Mathf.Abs(enemy.transform.lossyScale.x),Mathf.Abs(enemy.transform.lossyScale.y));
            if(SegmentDistance(previous,enemy.Body.position,player.transform.position)<=reach&&clock>=slot.NextContact&&
                Physics2D.Linecast(enemy.Body.position,player.transform.position,1<<8).collider==null)
            {slot.NextContact=clock+d.AttackInterval;DamagePlayer(enemy,chargeSeconds>0?d.AttackDamage:d.ContactDamage);}
        }
        private static float SegmentDistance(Vector2 start,Vector2 end,Vector2 point)
        {var delta=end-start;float along=delta.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(point-start,delta)/delta.sqrMagnitude):0;return Vector2.Distance(start+delta*along,point);}
        private void Move(PooledEnemy enemy,Vector2 displacement)
        {
            float distance=displacement.magnitude;
            if(distance>0)
            {
                var filter=new ContactFilter2D{useTriggers=false,useLayerMask=true,layerMask=(1<<8)|(1<<player.gameObject.layer)};
                bool approachingPlayer=false;
                int found=enemy.Shape.Cast(displacement.normalized,filter,contacts,distance);
                if(found==contacts.Length)distance=0;
                else for(int i=0;i<found;i++)
                {
                    var hit=contacts[i];
                    if(hit.rigidbody==playerBody)
                    {
                        // Keep physical pressure, but never place an enemy arbitrarily deep
                        // into the player on a long step. Moving away must remain possible.
                        if(Vector2.Dot(displacement,enemy.Body.position-playerBody.position)>=0)continue;
                        approachingPlayer=true;
                        distance=Mathf.Min(distance,Mathf.Max(0,hit.distance+Physics2D.defaultContactOffset));
                    }
                    else distance=Mathf.Min(distance,Mathf.Max(0,hit.distance-settings.CollisionSkin));
                }
                enemy.Body.position+=displacement.normalized*distance;
                if(approachingPlayer)
                {
                    // Shape casts include solver tolerances. Enforce the skin against the
                    // actual collider distance, not only the approximate time of impact.
                    var separation=enemy.Shape.Distance(playerShape);
                    if(separation.isValid&&separation.distance < -Physics2D.defaultContactOffset)
                        enemy.Body.position+=separation.normal*(separation.distance+Physics2D.defaultContactOffset);
                }
            }
            enemy.Body.linearVelocity=Vector2.zero;
            // The director performs physics queries again in this same frame. Rigidbody2D.position
            // is immediate for physics but its Transform otherwise waits for the next physics step.
            enemy.transform.SetPositionAndRotation(enemy.Body.position,Quaternion.Euler(0,0,enemy.Body.rotation));
            enemy.State.TrySetPosition(enemy.LeaseId,enemy.Body.position);
        }
        public bool ApplyHit(PooledEnemy enemy,long lease,float damage,Vector2 attacker,int source,long combatTick)
        {
            if(!ready||!run.IsSimulating||enemy==null||!enemy.IsReady||enemy.LeaseId!=lease||
                float.IsNaN(attacker.x)||float.IsNaN(attacker.y)||float.IsInfinity(attacker.x)||float.IsInfinity(attacker.y))return false;
            for(int i=0;i<slots.Length;i++)if(slots[i].Enemy==enemy&&slots[i].Lease==lease)
            {
                var d=enemy.Definition;
                if(d.Role==EnemyRole.FrontalArmored&&Vector2.Angle(slots[i].Heading,attacker-enemy.Body.position)<=d.FrontalArcDegrees*0.5f)damage*=d.FrontalDamageMultiplier;
                return enemy.TakeDamage(lease,damage,source,combatTick);
            }
            return false;
        }
        public bool ApplyControl(PooledEnemy enemy,long lease,Vector2 push,float interruption)
        {
            if(!ready||!run.IsSimulating||enemy==null||!enemy.IsReady||enemy.LeaseId!=lease||interruption<0||float.IsNaN(interruption)||float.IsInfinity(interruption))return false;
            for(int i=0;i<slots.Length;i++)if(slots[i].Enemy==enemy&&slots[i].Lease==lease)
                return enemy.State.TrySetControl(lease,push*(1-enemy.Definition.KnockbackResistance),interruption*(1-enemy.Definition.InterruptionResistance));
            return false;
        }
        private void DamagePlayer(PooledEnemy enemy,float damage)
        {
            damage = (float)Math.Min(float.MaxValue, (double)damage * enemy.DamageMultiplier);
            float before=player.State.Health;
            if(!player.State.TakeDamage(damage,run.ElapsedSeconds,player.Definition.Invulnerability))return;
            events.Publish(new CombatEvent(tick,++correlation,enemy.GetInstanceID(),player.GetInstanceID(),player.transform.position,before-player.State.Health,CombatEventType.DamageApplied));
            if(player.State.Health<=0)run.TryTransition(RunPhase.Lost);
        }
        private EnemyAreaEffect SpawnField(PooledEnemy enemy,bool warning,Vector2 position,float duration,float slow)
        {
            if(!effectKeys.TryGetValue(enemy.Definition.Id,out string key))return null;
            int slot=-1;for(int i=0;i<effects.Length;i++)if(effects[i]==null||effects[i].LeaseId==0){slot=i;break;}
            if(slot<0)return null;var effect=pools.Acquire<EnemyAreaEffect>(key);if(effect==null)return null;
            effect.Configure(enemy,warning,position,settings.FieldRadius,duration,slow,warning?enemy.Definition.TelegraphAudio:null,
                warning?settings.WarningColor:settings.FieldColor);effects[slot]=effect;return effect;
        }
        private void UpdateFields(float seconds)
        {
            float slow=1;
            for(int i=0;i<effects.Length;i++)
            {
                var effect=effects[i];if(effect==null||effect.LeaseId==0){effects[i]=null;continue;}
                effect.Age+=seconds;
                // Warning retirement is owned by the attack slot, after its execution/cancellation decision.
                if(!effect.Warning&&effect.Age>=effect.Duration){ReleaseField(effect);continue;}
                if(effect.Warning&&(effect.Owner==null||!effect.Owner.IsReady||effect.Owner.LeaseId!=effect.OwnerLease)){ReleaseField(effect);continue;}
                if(!effect.Warning&&Vector2.Distance(effect.transform.position,player.transform.position)<=effect.Radius)slow=Mathf.Min(slow,effect.SlowMultiplier);
            }
            player.SetEnemySlow(slow);
        }
        private void CancelWarning(ref Slot slot)
        {
            if(slot.Warning!=null&&slot.Warning.LeaseId==slot.WarningLease)ReleaseField(slot.Warning);
            slot.Warning=null;slot.WarningLease=0;slot.WarningUntil=0;
        }
        private void PhaseChanged(RunPhase phase)
        {
            if(run.Outcome.HasValue){Clear();return;}
            bool paused=!run.IsSimulating;
            if(effects!=null)foreach(var effect in effects)if(effect!=null&&effect.LeaseId>0)effect.Pause(paused);
        }
        private void Clear()
        {
            if(effects!=null)for(int i=0;i<effects.Length;i++){if(effects[i]!=null)ReleaseField(effects[i]);effects[i]=null;}
            if(slots!=null)Array.Clear(slots,0,slots.Length);if(player!=null)player.SetEnemySlow(1);
        }
        private void ReleaseField(EnemyAreaEffect effect)
        {for(int i=0;i<effects.Length;i++)if(effects[i]==effect)effects[i]=null;pools.Release(effect);}
        private void OnDisable(){if(ready)Clear();}
        private void OnDestroy(){if(run!=null)run.Changed-=PhaseChanged;}
    }
}
