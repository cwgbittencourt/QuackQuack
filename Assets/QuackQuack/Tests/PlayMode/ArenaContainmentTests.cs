using System;
using System.Collections;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using QuackQuack.Gameplay.Enemies;
using QuackQuack.Systems.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.PlayMode
{
    // Instrumentation fixture only (T002). The production horde reproduction belongs to T003.
    public sealed class ArenaContainmentTests
    {
        private Scene scene;
        private PhysicsScene2D physics;
        private Rigidbody2D body;
        private CircleCollider2D player;
        private BoxCollider2D wall;
        private ArenaPressureDiagnostics capture;

        [SetUp]
        public void SetUp()
        {
            scene = SceneManager.CreateScene("Arena diagnostic fixture", new CreateSceneParameters(LocalPhysicsMode.Physics2D));
            physics = scene.GetPhysicsScene2D();
            var root = Item("Observed body"); body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0; body.freezeRotation = true; player = root.AddComponent<CircleCollider2D>(); player.radius = 0.5f;
            wall = Item("Wall").AddComponent<BoxCollider2D>(); wall.size = new Vector2(0.5f, 4); wall.transform.position = new Vector2(0.6f, 0);
            capture = Item("Opt-in observer").AddComponent<ArenaPressureDiagnostics>();
            Physics2D.SyncTransforms();
        }

        private GameObject Item(string name)
        {
            var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, scene); return go;
        }
        private void Begin(int capacity = 4, int pairs = 8) => capture.Begin(73, body, player, new Collider2D[] { wall }, capacity, pairs, false);
        private void Step(long tick)
        { physics.Simulate(0.02f); capture.Capture(tick, tick * 0.02, ArenaCapturePhase.ExplicitAfterSimulation); }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [Test]
        public void ComponentIsDormantUntilExplicitBeginAndStopRetainsSamples()
        {
            Assert.That(capture.enabled, Is.False); Assert.That(capture.Capacity, Is.Zero);
            Assert.That(capture.Capture(0, 0, ArenaCapturePhase.ExplicitAfterSimulation), Is.False);
            Begin(); Step(1); capture.Stop();
            Assert.That(capture.Capture(2, 0.04, ArenaCapturePhase.ExplicitAfterSimulation), Is.False);
            Assert.That(capture.Count, Is.EqualTo(1)); Assert.That(capture.IsCapturing, Is.False);
        }

        [Test]
        public void CapturesContactsAndSignedPenetrationWithoutChangingBodies()
        {
            Begin();
            var before = body.position;
            capture.Capture(0, 0, ArenaCapturePhase.FixedBeforeSimulation);
            Assert.That(body.position, Is.EqualTo(before));
            Assert.That(capture.TryGetOverlap(0, 0, out var overlap), Is.True);
            Assert.That(overlap.Valid, Is.True); Assert.That(overlap.Penetrating, Is.True);
            Assert.That(overlap.SignedDistance, Is.LessThan(0)); Assert.That(overlap.ColliderId, Is.EqualTo(wall.GetInstanceID()));
            Step(1);
            capture.TryGetFrame(1, out var frame);
            Assert.That(frame.Contacts, Is.GreaterThan(0));
            Assert.That(capture.TryGetContact(1, 0, out var contact), Is.True);
            Assert.That(contact.ColliderId == wall.GetInstanceID() || contact.OtherColliderId == wall.GetInstanceID(), Is.True);
            Assert.That(frame.Phase, Is.EqualTo(ArenaCapturePhase.ExplicitAfterSimulation));
            Assert.That(frame.Tick, Is.EqualTo(1)); Assert.That(capture.Seed, Is.EqualTo(73));
        }

        [Test]
        public void RingRetainsOrderedValueCopiesAndSignalsOverwrites()
        {
            Begin(2);
            for (int i = 0; i < 4; i++) capture.Capture(i, i * 0.02, ArenaCapturePhase.ExplicitAfterSimulation);
            Assert.That(capture.Written, Is.EqualTo(4)); Assert.That(capture.Overwritten, Is.EqualTo(2));
            capture.TryGetFrame(0, out var first); Assert.That(first.Tick, Is.EqualTo(2));
            first.Tick = 999; capture.TryGetFrame(0, out var unchanged); Assert.That(unchanged.Tick, Is.EqualTo(2));
            Assert.That(capture.TryGetFrame(-1, out _), Is.False); Assert.That(capture.TryGetActor(0, 1, out _), Is.False);
            Assert.That(capture.TryGetContact(0, 999, out _), Is.False); Assert.That(capture.TryGetOverlap(0, 999, out _), Is.False);
        }

        [Test]
        public void FullPairBufferIsExplicitRatherThanSilentlyComplete()
        {
            var second = Item("Second wall").AddComponent<BoxCollider2D>(); second.transform.position = new Vector2(-0.6f, 0);
            Physics2D.SyncTransforms(); Begin(2, 1);
            capture.Capture(0, 0, ArenaCapturePhase.ExplicitAfterSimulation);
            capture.TryGetFrame(0, out var frame);
            Assert.That(frame.OverlapBufferFull, Is.True); Assert.That(frame.Overlaps, Is.EqualTo(1));
        }

        [Test]
        public void ActorLeaseChangesAreRecordedAcrossReuseAndCallerArrayIsNotRetained()
        {
            var enemy = Item("Lease probe").AddComponent<PooledEnemy>(); enemy.OnAcquire();
            var input = new Collider2D[] { enemy.GetComponent<CircleCollider2D>() };
            capture.Begin(9, body, player, input, 4, 8, false); input[0] = wall;
            capture.Capture(0, 0, ArenaCapturePhase.ExplicitAfterSimulation);
            capture.TryGetActor(0, 0, out var first);
            enemy.OnRelease(); enemy.ResetState(); enemy.OnAcquire();
            capture.Capture(1, 0.02, ArenaCapturePhase.ExplicitAfterSimulation);
            capture.TryGetActor(1, 0, out var second);
            Assert.That(first.ColliderId, Is.EqualTo(second.ColliderId));
            Assert.That(first.LeaseId, Is.GreaterThan(0)); Assert.That(second.LeaseId, Is.GreaterThan(first.LeaseId));
            Assert.That(first.ColliderId, Is.Not.EqualTo(wall.GetInstanceID()));
        }

        [Test]
        public void PausingComponentStopsCaptureAndNewBeginResetsIdentityAndBuffers()
        {
            Begin(); Step(1); capture.enabled = false;
            Assert.That(capture.Capture(2, 0.04, ArenaCapturePhase.ExplicitAfterSimulation), Is.False);
            capture.enabled = true;
            Assert.That(capture.IsCapturing, Is.False, "Re-enabling alone must not opt in.");
            capture.Begin(99, body, player, Array.Empty<Collider2D>(), 2, 1, false);
            Assert.That(capture.Count, Is.Zero); Assert.That(capture.Written, Is.Zero); Assert.That(capture.Seed, Is.EqualTo(99));
        }

        [Test]
        public void DormantAndWarmedSamplingAllocateNoManagedBytes()
        {
            for (int i = 0; i < 64; i++) capture.Capture(0, 0, ArenaCapturePhase.ExplicitAfterSimulation);
            GC.GetAllocatedBytesForCurrentThread(); long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++) capture.Capture(i, 0, ArenaCapturePhase.ExplicitAfterSimulation);
            long disabledBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(disabledBytes, Is.Zero);
            Begin(); for (int i = 0; i < 64; i++) capture.Capture(i, 0, ArenaCapturePhase.ExplicitAfterSimulation);
            before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++) capture.Capture(i, 0, ArenaCapturePhase.ExplicitAfterSimulation);
            long enabledBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(enabledBytes, Is.Zero);
        }

        [Test]
        public void ExportRequiresStopAndUsesInvariantValuesWithSeedAndOverflowMetadata()
        {
            string folder = Path.Combine(Application.temporaryCachePath, "ArenaDiagnosticTest-" + Guid.NewGuid().ToString("N"));
            string prefix = Path.Combine(folder, "capture");
            var previous = CultureInfo.CurrentCulture;
            try
            {
                Begin(1); Step(1); Step(2);
                Assert.Throws<InvalidOperationException>(() => capture.ExportCsv(prefix));
                capture.Stop(); CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR"); capture.ExportCsv(prefix);
                string csv = File.ReadAllText(prefix + ".frames.csv");
                Assert.That(csv, Does.Contain("73,1,2,0.04,ExplicitAfterSimulation"));
                Assert.That(File.ReadAllText(prefix + ".metadata.txt"), Does.Contain("overwritten=1"));
                foreach (string suffix in new[] { ".actors.csv", ".contacts.csv", ".overlaps.csv" }) Assert.That(File.Exists(prefix + suffix), Is.True);
            }
            finally { CultureInfo.CurrentCulture = previous; if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }

        [UnityTest]
        public IEnumerator AutomaticSamplingLabelsPrePhysicsAndCanBeSuspended()
        {
            capture.Begin(73, body, player, new Collider2D[] { wall }, 8, 8, true);
            yield return new WaitForFixedUpdate();
            Assert.That(capture.Count, Is.GreaterThan(0));
            capture.TryGetFrame(capture.Count - 1, out var frame);
            Assert.That(frame.Phase, Is.EqualTo(ArenaCapturePhase.FixedBeforeSimulation));
            Assert.That(frame.FixedStep, Is.EqualTo(Time.fixedDeltaTime));
            Assert.That(frame.Tick, Is.GreaterThan(0));
            capture.AutomaticSampling = false;
            int count = capture.Count;
            yield return new WaitForFixedUpdate();
            Assert.That(capture.Count, Is.EqualTo(count));
            capture.Stop();
        }
        [Test]
        public void RejectsInvalidConfigurationWithoutStartingCapture()
        {
            Assert.Throws<ArgumentException>(() => capture.Begin(1, null, player, Array.Empty<Collider2D>()));
            Assert.Throws<ArgumentException>(() => capture.Begin(1, body, wall, Array.Empty<Collider2D>()));
            Assert.Throws<ArgumentOutOfRangeException>(() => capture.Begin(1, body, player, Array.Empty<Collider2D>(), 0));
            Assert.Throws<ArgumentException>(() => capture.Begin(1, body, player, new Collider2D[] { null }));
            Assert.That(capture.IsCapturing, Is.False);
            Begin();
            Assert.Throws<ArgumentOutOfRangeException>(() => capture.Capture(-1, 0, ArenaCapturePhase.ExplicitAfterSimulation));
        }
    }
}
