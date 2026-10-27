using UnityEngine;
namespace QuackQuack.Content.Enemies
{
    [CreateAssetMenu(menuName="QuackQuack/Enemy behavior settings")]
    public sealed class EnemyBehaviorSettings : ScriptableObject
    {
        [SerializeField] private int steeringBudget=64, effectCapacityPerType=16;
        [SerializeField] private float contactRadius=0.6f, fieldRadius=1.5f, preferredRangeFraction=0.8f, collisionSkin=0.02f, knockbackDecay=4;
        [SerializeField] private Color warningColor=new Color(1,0.4f,0.1f), fieldColor=new Color(0.5f,0.3f,0.9f);
        public int SteeringBudget=>steeringBudget;
        public int EffectCapacityPerType=>effectCapacityPerType;
        public float ContactRadius=>contactRadius;
        public float FieldRadius=>fieldRadius;
        public float PreferredRangeFraction=>preferredRangeFraction;
        public float CollisionSkin=>collisionSkin;
        public float KnockbackDecay=>knockbackDecay;
        public Color WarningColor=>warningColor;
        public Color FieldColor=>fieldColor;
        public bool IsValid => steeringBudget>0 && effectCapacityPerType>0 && Positive(contactRadius) && Positive(fieldRadius) &&
            Positive(preferredRangeFraction) && preferredRangeFraction<=1 && Positive(collisionSkin) && Positive(knockbackDecay) &&
            ValidColor(warningColor) && ValidColor(fieldColor);
        private static bool Positive(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v)&&v>0;
        private static bool ValidColor(Color c)=>Finite(c.r)&&Finite(c.g)&&Finite(c.b)&&Finite(c.a)&&c.a>0&&c.a<=1;
        private static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
    }
}
