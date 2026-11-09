using System.Collections;
using NUnit.Framework;
using QuackQuack.Core.Run;
using QuackQuack.Presentation.Terrain;
using UnityEngine;
using UnityEngine.TestTools;

namespace QuackQuack.Tests.PlayMode
{
    public class LakeWaterAnimationTests
    {
        private GameObject root;
        private LakeWaterAnimation water;
        private SpriteRenderer ripple;
        private RunState run;
        [SetUp] public void Setup()
        {
            root = new GameObject("Water fixture"); water = root.AddComponent<LakeWaterAnimation>();
            var child = new GameObject("Surface mark",typeof(SpriteRenderer)); child.transform.SetParent(root.transform,false);
            ripple = child.GetComponent<SpriteRenderer>(); ripple.color = new Color(1,1,1,.3f);
            water.Configure(new[]{ripple}); run = new RunState(42); run.TryTransition(RunPhase.Playing); water.Bind(run);
        }
        [TearDown] public void Cleanup() => Object.DestroyImmediate(root);
        [UnityTest] public IEnumerator SimulationClockFreezesForPauseChoiceAndEnd()
        {
            run.Advance(2); yield return null; var moving = ripple.transform.localPosition;
            Assert.That(moving.x,Is.Not.EqualTo(0));
            foreach(var phase in new[]{RunPhase.Paused,RunPhase.ChoosingUpgrade})
            {
                Assert.That(run.TryTransition(phase),Is.True); run.Advance(10); yield return null;
                Assert.That(ripple.transform.localPosition,Is.EqualTo(moving)); run.TryResume();
            }
            run.Advance(1); yield return null; Assert.That(ripple.transform.localPosition,Is.Not.EqualTo(moving));
            run.TryTransition(RunPhase.Lost); var stopped=ripple.transform.localPosition; run.Advance(20);yield return null;
            Assert.That(ripple.transform.localPosition,Is.EqualTo(stopped));
        }
        [Test] public void SurfaceMotionIsBoundedAndNeverMovesPhysicalParentOrCreatesMaterials()
        {
            var body=root.AddComponent<BoxCollider2D>(); var parent=root.transform.localToWorldMatrix;var material=ripple.sharedMaterial;
            for(int i=0;i<100;i++)
            {
                water.Render(i*.2);Assert.That(Mathf.Abs(ripple.transform.localPosition.x),Is.LessThanOrEqualTo(.0451));
                Assert.That(Mathf.Abs(ripple.transform.localPosition.y),Is.LessThanOrEqualTo(.0181));
                Assert.That(ripple.color.a,Is.InRange(.209f,.301f));Assert.That(ripple.sharedMaterial,Is.SameAs(material));
                Assert.That(root.transform.localToWorldMatrix,Is.EqualTo(parent));Assert.That(body.size,Is.EqualTo(Vector2.one));
            }
        }
        [Test] public void DisableRestoresAuthoredPoseAndNewRunDoesNotInheritTime()
        {
            water.Render(5);water.enabled=false;
            Assert.That(ripple.transform.localPosition,Is.EqualTo(Vector3.zero));Assert.That(ripple.color.a,Is.EqualTo(.3f));
            water.enabled=true;water.Bind(new RunState(8));var first=ripple.transform.localPosition;
            water.Render(0);Assert.That(ripple.transform.localPosition,Is.EqualTo(first));
            Assert.That(()=>water.Render(double.NaN),Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }
    }
}
