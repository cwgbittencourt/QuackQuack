using QuackQuack.Gameplay.Enemies;
using QuackQuack.Gameplay.Bosses;
using QuackQuack.Core.Run;
using UnityEngine;

namespace QuackQuack.Gameplay.Player
{
    // Presentation on a collider-free child; never changes the actor's physical transform.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CartoonActorVisual : MonoBehaviour
    {
        private PlayerController player;
        private PooledEnemy enemy;
        private FrogKingController boss;
        private SpriteRenderer image;
        private Vector3 restScale, previousPosition;
        private float phase, lastHealth, flash;
        private long lease;
        private bool sampled;
        private bool restFlip;
        public Vector3 RaiseHudAnchor(Vector3 physicalAnchor)
        {
            if(image!=null&&image.enabled&&image.sprite!=null)
                physicalAnchor.y=Mathf.Max(physicalAnchor.y,image.bounds.max.y);
            return physicalAnchor;
        }

        private void Awake()
        {
            image=GetComponent<SpriteRenderer>(); restScale=transform.localScale;restFlip=image.flipX;
            player=GetComponentInParent<PlayerController>();enemy=GetComponentInParent<PooledEnemy>();
            boss=GetComponentInParent<FrogKingController>();
        }
        private void OnEnable() { sampled=false; phase=flash=0; lease=0; }
        private void OnDisable()
        { if(image==null)return;image.color=Color.white;image.flipX=restFlip;transform.localScale=restScale;sampled=false; }

        private void LateUpdate()
        {
            bool simulating=player!=null?player.VisualSimulationActive:enemy!=null?enemy.VisualSimulationActive:boss!=null&&boss.VisualSimulationActive;
            if(!simulating)return;
            float health=player!=null?player.State.Health:enemy!=null?enemy.State.Health:boss.Health;
            long currentLease=enemy!=null?enemy.State.LeaseId:0;
            Vector3 position=transform.parent.position;
            if(!sampled||currentLease!=lease)
            { phase=flash=0;lastHealth=health;previousPosition=position;lease=currentLease;sampled=true;image.color=Color.white;image.flipX=restFlip;transform.localScale=restScale; }
            var delta=position-previousPosition;previousPosition=position;
            bool moving=delta.sqrMagnitude>.000001f;
            bool swimming=player!=null&&player.State.Terrain==TerrainType.Water;
            phase+=Time.deltaTime*(moving?10:3);
            float amount=moving?(swimming?.018f:.035f):.008f;
            float pulse=Mathf.Sin(phase)*amount;
            transform.localScale=new Vector3(restScale.x*(1-pulse*.5f),restScale.y*(1+pulse),restScale.z);
            if(player!=null&&Mathf.Abs(delta.x)>.0001f)image.flipX=delta.x<0;
            if(health<lastHealth)flash=.14f;
            lastHealth=health;flash=Mathf.Max(0,flash-Time.deltaTime);
            image.color=flash>0?new Color(1,.55f,.55f):Color.white;
        }
    }
}
