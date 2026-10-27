using NUnit.Framework;
using QuackQuack.Content.Enemies;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuackQuack.Tests.EditMode
{
    public class EnemyBehaviorSettingsTests
    {
        [TestCase("contactRadius")] [TestCase("fieldRadius")] [TestCase("preferredRangeFraction")]
        [TestCase("collisionSkin")] [TestCase("knockbackDecay")]
        public void BehavioralNumbersMustBePositiveFinite(string field)
        {
            var settings=ScriptableObject.CreateInstance<EnemyBehaviorSettings>();
            try{Assert.That(settings.IsValid,Is.True);foreach(float value in new[]{0f,-1f,float.NaN,float.PositiveInfinity})
                {var s=new SerializedObject(settings);s.FindProperty(field).floatValue=value;s.ApplyModifiedPropertiesWithoutUndo();Assert.That(settings.IsValid,Is.False);}}
            finally{Object.DestroyImmediate(settings);}
        }
        [TestCase("steeringBudget")] [TestCase("effectCapacityPerType")]
        public void WorkAndPoolBudgetsMustBePositive(string field)
        {
            var settings=ScriptableObject.CreateInstance<EnemyBehaviorSettings>();
            try{var s=new SerializedObject(settings);s.FindProperty(field).intValue=0;s.ApplyModifiedPropertiesWithoutUndo();Assert.That(settings.IsValid,Is.False);}
            finally{Object.DestroyImmediate(settings);}
        }
    }
}
