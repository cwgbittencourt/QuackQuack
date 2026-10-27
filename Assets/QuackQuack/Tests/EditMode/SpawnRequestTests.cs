using NUnit.Framework;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Run;
using UnityEngine;

namespace QuackQuack.Tests.EditMode
{
    public class SpawnRequestTests
    {
        [Test]
        public void RequestCapturesIdentityBoundsBudgetAndInclusiveDeadline()
        {
            var area=new Rect(10,20,30,40);
            var request=new SpawnRequest(1,"wave","ant",3,area,5,EventPriority.Danger,10);
            Assert.That(request.IsValid,Is.True); Assert.That(request.Region,Is.EqualTo(area));
            Assert.That(request.RequestId,Is.EqualTo(1)); Assert.That(request.EncounterId,Is.EqualTo("wave"));
            Assert.That(request.EnemyId,Is.EqualTo("ant")); Assert.That(request.Quantity,Is.EqualTo(3));
            Assert.That(request.MinimumDistance,Is.EqualTo(5)); Assert.That(request.DeadlineTick,Is.EqualTo(10));
            Assert.That(request.Priority,Is.EqualTo(EventPriority.Danger));
        }
        [TestCase(0)] [TestCase(-1)]
        public void RequestIdAndQuantityMustBePositive(int value)
        {
            Assert.That(new SpawnRequest(value,"wave","ant",1,new Rect(0,0,1,1),0,EventPriority.Cosmetic,0).IsValid,Is.False);
            Assert.That(new SpawnRequest(1,"wave","ant",value,new Rect(0,0,1,1),0,EventPriority.Cosmetic,0).IsValid,Is.False);
        }
        [TestCase(null)] [TestCase("")] [TestCase(" ")]
        public void BothStableIdsAreRequired(string value)
        {
            Assert.That(new SpawnRequest(1,value,"ant",1,new Rect(0,0,1,1),0,EventPriority.Cosmetic,0).IsValid,Is.False);
            Assert.That(new SpawnRequest(1,"wave",value,1,new Rect(0,0,1,1),0,EventPriority.Cosmetic,0).IsValid,Is.False);
        }
        [TestCase(-1f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void DistanceCannotBeNegativeOrNonfinite(float value) =>
            Assert.That(new SpawnRequest(1,"wave","ant",1,new Rect(0,0,1,1),value,EventPriority.Cosmetic,0).IsValid,Is.False);
        [Test]
        public void InvalidGeometryPriorityDeadlineAndDefaultAreRejected()
        {
            Assert.That(default(SpawnRequest).IsValid,Is.False);
            foreach(var region in new[] {new Rect(0,0,0,1),new Rect(0,0,-1,1),new Rect(float.NaN,0,1,1),new Rect(0,0,float.PositiveInfinity,1)})
                Assert.That(new SpawnRequest(1,"wave","ant",1,region,0,EventPriority.Cosmetic,0).IsValid,Is.False);
            Assert.That(new SpawnRequest(1,"wave","ant",1,new Rect(0,0,1,1),0,(EventPriority)99,0).IsValid,Is.False);
            Assert.That(new SpawnRequest(1,"wave","ant",1,new Rect(0,0,1,1),0,EventPriority.Danger,-1).IsValid,Is.False);
        }
    }
}
