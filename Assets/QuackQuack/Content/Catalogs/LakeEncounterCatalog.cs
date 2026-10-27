using System;
using System.Collections.Generic;
using QuackQuack.Content.Enemies;
using QuackQuack.Content.Encounters;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Enemies;
using QuackQuack.Systems.Combat;
using QuackQuack.Systems.Pooling;
using QuackQuack.Systems.Spawning;
using UnityEngine;

namespace QuackQuack.Content.Catalogs
{
    [CreateAssetMenu(menuName="QuackQuack/Lake encounter catalog")]
    public sealed class LakeEncounterCatalog:ScriptableObject
    {
        public const string FinalBossId="sapo-rei";
        private static readonly string[] CommonIds={"formiga","aranha","vespa","besouro","caramujo-aquatico"};
        [SerializeField]private EnemyDefinition[] commonEnemies,miniBosses;
        [SerializeField]private EncounterDefinition[] waves,miniBossEncounters;
        [SerializeField]private EncounterDefinition finalBossEncounter;
        [SerializeField]private EnemyBehaviorSettings behaviorSettings;
        [SerializeField]private int activeLimit=200,queueCapacity=32,spawnBudget=8,positionAttempts=16;
        [SerializeField]private float exclusionPadding=1.5f;
        public IReadOnlyList<EnemyDefinition> CommonEnemies=>Array.AsReadOnly((EnemyDefinition[])commonEnemies.Clone());
        public IReadOnlyList<EnemyDefinition> MiniBosses=>Array.AsReadOnly((EnemyDefinition[])miniBosses.Clone());
        public IReadOnlyList<EncounterDefinition> Waves=>Array.AsReadOnly((EncounterDefinition[])waves.Clone());
        public IReadOnlyList<EncounterDefinition> MiniBossEncounters=>Array.AsReadOnly((EncounterDefinition[])miniBossEncounters.Clone());
        public EncounterDefinition FinalBossEncounter=>finalBossEncounter;
        public EnemyBehaviorSettings BehaviorSettings=>behaviorSettings;
        public int ActiveLimit=>activeLimit;
        public int QueueCapacity=>queueCapacity;
        public int SpawnBudget=>spawnBudget;
        public int PositionAttempts=>positionAttempts;
        public float ExclusionPadding=>exclusionPadding;
        public EnemyDefinition[] GetAllEnemies()
        {
            var result=new EnemyDefinition[commonEnemies.Length+miniBosses.Length];
            Array.Copy(commonEnemies,result,commonEnemies.Length);Array.Copy(miniBosses,0,result,commonEnemies.Length,miniBosses.Length);return result;
        }
        public EncounterDefinition[] GetAllEncounters()
        {
            var result=new EncounterDefinition[waves.Length+miniBossEncounters.Length+1];
            Array.Copy(waves,result,waves.Length);Array.Copy(miniBossEncounters,0,result,waves.Length,miniBossEncounters.Length);result[result.Length-1]=finalBossEncounter;return result;
        }
        public bool IsValid(out string reason)
        {
            reason=null;
            if(commonEnemies==null||commonEnemies.Length!=5||miniBosses==null||miniBosses.Length!=2||waves==null||waves.Length==0||
                miniBossEncounters==null||miniBossEncounters.Length!=2||finalBossEncounter==null||behaviorSettings==null||!behaviorSettings.IsValid)
            {reason="Lake requires five common enemies, two minibosses, waves, three milestones and valid settings.";return false;}
            if(activeLimit<=0||activeLimit>400||queueCapacity<=0||spawnBudget<=0||positionAttempts<=0||float.IsNaN(exclusionPadding)||float.IsInfinity(exclusionPadding)||exclusionPadding<0)
            {reason="Invalid lake spawn budgets.";return false;}
            var all=GetAllEnemies();if(!EnemyDefinition.IsValidCatalog(all,out reason))return false;
            var known=new List<string>(8);var common=new HashSet<string>(StringComparer.Ordinal);
            for(int i=0;i<commonEnemies.Length;i++)
            {
                var d=commonEnemies[i];
                if(d.Id!=CommonIds[i]||d.Role!=(EnemyRole)i){reason="Common lake roster/order/tactical roles must match the five authored species.";return false;}
                common.Add(d.Id);
            }
            if(miniBosses[0].Id!="libelula-alfa"||miniBosses[0].Role!=EnemyRole.Charger||miniBosses[1].Id!="sapo-cururu"||miniBosses[1].Role!=EnemyRole.WebCaster)
            {reason="Lake miniboss identities and tactical roles are invalid.";return false;}
            foreach(var d in all)
            {
                if(d.Prefab.GetComponent<PooledEnemy>()==null){reason="Lake enemy prefab requires PooledEnemy.";return false;}
                if(d.HasTelegraphedAttack&&d.TelegraphVisual.GetComponent<EnemyAreaEffect>()==null){reason="Lake special attacks require a pooled visual/audio effect prefab.";return false;}
                known.Add(d.Id);
            }
            known.Add(FinalBossId); // External boss controller is supplied by T057/T060, not a common enemy prefab.
            if(miniBossEncounters[0]==null||miniBossEncounters[0].Kind!=EncounterKind.AlphaDragonfly||miniBossEncounters[1]==null||miniBossEncounters[1].Kind!=EncounterKind.CururuToad||
                finalBossEncounter.Kind!=EncounterKind.FrogKing){reason="Lake encounter milestone kinds are invalid.";return false;}
            if(!EncounterDefinition.IsValidTimeline(GetAllEncounters(),known,out reason))return false;
            var seen=new HashSet<string>(StringComparer.Ordinal);
            double end=0;float previousInterval=float.MaxValue;int previousLimit=0;
            foreach(var wave in waves)
            {
                if(wave.Kind!=EncounterKind.Wave||wave.StartSeconds!=end||wave.SpawnInterval>previousInterval||wave.MaxActive<previousLimit||wave.MaxActive>activeLimit)
                {reason="Waves must partition the timeline in order with nondecreasing pressure inside the global limit.";return false;}
                foreach(var id in wave.EnemyIds){if(!common.Contains(id)){reason="Regular waves may only contain common species.";return false;}seen.Add(id);}
                end=wave.EndSeconds;previousInterval=wave.SpawnInterval;previousLimit=wave.MaxActive;
            }
            if(end!=EncounterDefinition.LakeDurationSeconds||seen.Count!=5){reason="Wave progression must cover 15 minutes and all five species.";return false;}
            return true;
        }
        public EncounterSchedule CreateSchedule(RunState run)
        {
            RequireValid();var known=new List<string>();foreach(var enemy in GetAllEnemies())known.Add(enemy.Id);known.Add(FinalBossId);
            return EncounterDefinition.CreateSchedule(run,GetAllEncounters(),known);
        }
        public void RegisterPools(PoolService pools,Transform parent=null)
        {RequireValid();foreach(var enemy in GetAllEnemies())PooledEnemy.RegisterPool(pools,enemy,parent);}
        public SpawnDirector CreateDirector(RunState run,PoolService pools,GameplayEventBus events,Transform target,int seed,Action<EnemyReward> reward)
        {RequireValid();return new SpawnDirector(run,pools,events,target,GetAllEnemies(),activeLimit,queueCapacity,spawnBudget,positionAttempts,exclusionPadding,seed,reward);}
        private void RequireValid(){if(!IsValid(out string reason))throw new InvalidOperationException(reason);}
    }
}
