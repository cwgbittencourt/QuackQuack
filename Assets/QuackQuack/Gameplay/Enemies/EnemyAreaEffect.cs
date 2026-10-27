using System.Threading;
using QuackQuack.Systems.Pooling;
using UnityEngine;
namespace QuackQuack.Gameplay.Enemies
{
    [RequireComponent(typeof(LineRenderer),typeof(AudioSource))]
    public sealed class EnemyAreaEffect : MonoBehaviour,IPoolLifecycle
    {
        private static long nextLease;
        private LineRenderer line;
        private AudioSource audioSource;
        public long LeaseId{get;private set;}
        public bool Warning{get;private set;}
        public float Age{get;internal set;}
        public float Duration{get;private set;}
        public float Radius{get;private set;}
        public float SlowMultiplier{get;private set;}
        public PooledEnemy Owner{get;private set;}
        public long OwnerLease{get;private set;}
        private void Awake(){line=GetComponent<LineRenderer>();audioSource=GetComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=0;}
        internal void Configure(PooledEnemy owner,bool warning,Vector2 position,float radius,float duration,float slow,AudioClip clip,Color color)
        {
            Owner=owner;OwnerLease=owner.LeaseId;Warning=warning;Radius=radius;Duration=duration;SlowMultiplier=slow;Age=0;
            transform.position=position;line.enabled=true;line.useWorldSpace=false;line.loop=true;line.positionCount=32;
            line.startWidth=line.endWidth=0.06f;line.startColor=line.endColor=color;line.sortingOrder=15;
            for(int i=0;i<32;i++){float angle=i*Mathf.PI*2/32;line.SetPosition(i,new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius));}
            audioSource.clip=clip;if(clip!=null) audioSource.Play();
        }
        internal void Pause(bool paused){if(paused)audioSource.Pause();else audioSource.UnPause();}
        public void OnAcquire(){LeaseId=Interlocked.Increment(ref nextLease);}
        public void OnRelease(){if(audioSource!=null)audioSource.Stop();}
        public void ResetState()
        {
            if(line==null)line=GetComponent<LineRenderer>();if(audioSource==null)audioSource=GetComponent<AudioSource>();
            audioSource.Stop();audioSource.clip=null;line.positionCount=0;line.enabled=false;
            Owner=null;OwnerLease=LeaseId=0;Age=Duration=Radius=0;SlowMultiplier=1;Warning=false;
        }
    }
}
