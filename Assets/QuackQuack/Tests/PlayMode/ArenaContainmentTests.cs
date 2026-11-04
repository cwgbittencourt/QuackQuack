using System;
using System.Collections;
using System.Collections.Generic;
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

namespace QuackQuack.Tests.PlayMode
{
    // Diagnostic evidence, not the containment regression/acceptance contract of T004/T007.
    [Category("ArenaPressureReproduction")]
    public sealed class ArenaLakeRunReproductionTests
    {
        private Func<string> previousSave;
        private string saveDirectory;
        private SimulationMode2D previousMode;
        private float previousStep;
        private QuackQuack.Systems.Run.RunDirector director;
        private QuackQuack.Systems.Run.GameBootstrap bootstrap;

        [UnitySetUp]
        public IEnumerator LoadProductionArena()
        {
            previousMode = Physics2D.simulationMode; previousStep = Time.fixedDeltaTime;
            previousSave = QuackQuack.Systems.Run.RunDirector.SavePathProvider;
            saveDirectory = Path.Combine(Application.temporaryCachePath, "ArenaRepro-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(saveDirectory);
            QuackQuack.Systems.Run.RunDirector.SavePathProvider = () => Path.Combine(saveDirectory, "save.json");
            if (QuackQuack.Systems.Run.GameBootstrap.Instance != null) Object.Destroy(QuackQuack.Systems.Run.GameBootstrap.Instance.gameObject);
            yield return null;
            yield return SceneManager.LoadSceneAsync("RunBootstrap");
            double deadline = Time.realtimeSinceStartupAsDouble + 30;
            do {
                director = Object.FindAnyObjectByType<QuackQuack.Systems.Run.RunDirector>();
                if (director != null && director.Ready) break;
                yield return null;
            } while (Time.realtimeSinceStartupAsDouble < deadline);
            Assert.That(director != null && director.Ready, Is.True);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("LakeRun"));
            bootstrap = QuackQuack.Systems.Run.GameBootstrap.Instance;
            director.AutomaticTick = false;
            director.Scheduler.AutomaticTick = false;
            foreach (var runner in Object.FindObjectsByType<QuackQuack.Gameplay.Weapons.WeaponRunner>(FindObjectsSortMode.None)) runner.AutomaticTick = false;
            Physics2D.simulationMode = SimulationMode2D.Script; Time.fixedDeltaTime = 0.02f;
        }

        [UnityTearDown]
        public IEnumerator CleanupProductionArena()
        {
            Physics2D.simulationMode = previousMode; Time.fixedDeltaTime = previousStep;
            if (director != null && director.Ready) director.TryAbandon();
            if (bootstrap != null) Object.Destroy(bootstrap.gameObject);
            yield return null;
            var loaded = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("After arena reproduction"); SceneManager.SetActiveScene(empty);
            if (loaded.IsValid() && loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
            QuackQuack.Systems.Run.RunDirector.SavePathProvider = previousSave;
            string resolved = Path.GetFullPath(saveDirectory);
            string cache = Path.GetFullPath(Application.temporaryCachePath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Assert.That(resolved.StartsWith(cache, StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(Path.GetFileName(resolved), Does.StartWith("ArenaRepro-"));
            if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
        }

        // T004: normative assertions. These intentionally remain red until T005/T006.
        private static readonly string[] RegressionCases = {
            "east", "west", "north", "south", "north-east", "north-west", "south-east", "south-west",
            "training-rock", "south-rock", "east-long", "south-west-long", "spawn-return",
            "boss-east", "boss-west", "boss-north", "boss-south-long", "pressure"
        };

        [UnityTest]
        [Category("ArenaContainmentRegression")]
        public IEnumerator WholeBodyStaysWalkableAndMovementResumesAfterPressure(
            [ValueSource(nameof(RegressionCases))] string scenario)
        {
            var player = Object.FindAnyObjectByType<QuackQuack.Gameplay.Player.PlayerController>();
            var body = player.GetComponent<Rigidbody2D>();
            var shape = player.GetComponent<CircleCollider2D>();
            var east = GameObject.Find("East bank").GetComponent<BoxCollider2D>();
            var west = GameObject.Find("West bank").GetComponent<BoxCollider2D>();
            var north = GameObject.Find("North bank").GetComponent<BoxCollider2D>();
            var south = GameObject.Find("South bank").GetComponent<BoxCollider2D>();
            var rocks = new[] { GameObject.Find("Training rock").GetComponent<BoxCollider2D>(),
                GameObject.Find("South rock").GetComponent<BoxCollider2D>() };
            var inner = Rect.MinMaxRect(west.bounds.max.x, south.bounds.max.y, east.bounds.min.x, north.bounds.min.y);
            const float tolerance = 0.02f; // Solver skin only; never permit traversing a bank/rock.
            float step = scenario.EndsWith("long", StringComparison.Ordinal) ? 0.1f : 0.02f;
            Time.fixedDeltaTime = step;
            bool boss = scenario.StartsWith("boss-", StringComparison.Ordinal);
            bool pressureOnly = scenario == "pressure";
            Vector2 outward = new Vector2(scenario.Contains("east") ? 1 : scenario.Contains("west") ? -1 : 0,
                scenario.Contains("north") ? 1 : scenario.Contains("south") ? -1 : 0).normalized;
            float radius = shape.bounds.extents.x;
            Vector2 start = new Vector2(outward.x == 0 ? 0 : Mathf.Sign(outward.x) * (inner.xMax - radius - 0.03f),
                outward.y == 0 ? 0 : Mathf.Sign(outward.y) * (inner.yMax - radius - 0.03f));
            if (scenario == "spawn-return") { start = body.position; outward = Vector2.left; }
            if (scenario.Contains("rock")) {
                var rock = scenario == "training-rock" ? rocks[0] : rocks[1];
                start = new Vector2(rock.bounds.min.x - radius - 0.03f, rock.bounds.center.y);
                outward = Vector2.right;
            }
            if (pressureOnly) { start = new Vector2(0, -2); outward = Vector2.right; }
            player.State.TakeDamage(1, 0, float.MaxValue);
            for (int i = 0; i < director.Spawns.Capacity; i++)
                if (director.Spawns.TryGetActive(i, out var initial, out long lease)) initial.Despawn(lease);
            var enemies = new List<PooledEnemy>();
            if (boss) {
                // Invoke the actual encounter transition, not a hand-built substitute arena.
                director.SendMessage("BeginBoss");
                Assert.That(bootstrap.Run.Phase, Is.EqualTo(QuackQuack.Core.Run.RunPhase.Boss));
                Assert.That(director.Boss, Is.Not.Null);
            } else {
                director.Spawns.ActiveLimit = 200;
                Assert.That(director.Spawns.TryEnqueue(new QuackQuack.Core.Run.SpawnRequest(1000000,
                    "arena-regression", "formiga", 64, new Rect(-9,-5,1,1), 0,
                    QuackQuack.Core.Combat.EventPriority.Feedback, long.MaxValue)), Is.True);
                for (int i = 0; i < 20 && director.Spawns.ActiveCount < 64; i++)
                    director.Spawns.Process(1000000+i, new Rect(-100,-100,1,1));
                Assert.That(director.Spawns.ActiveCount, Is.EqualTo(64));
                for (int i = 0; i < director.Spawns.Capacity; i++)
                    if (director.Spawns.TryGetActive(i, out var enemy, out _)) enemies.Add(enemy);
                Vector2 tangent = new Vector2(-outward.y, outward.x);
                for (int i = 0; i < enemies.Count; i++) {
                    var enemy = enemies[i];
                    Vector2 position = start - outward * (0.9f + (i % 8) * 0.45f) + tangent * ((i / 8 - 3.5f) * 0.3f);
                    enemy.Body.position = position; enemy.transform.position = position;
                    enemy.Body.linearVelocity = Vector2.zero;
                    Assert.That((enemy.Shape.excludeLayers.value & (1 << player.gameObject.layer)), Is.Zero,
                        "Regression must retain authored enemy/player contacts.");
                }
            }
            body.position = start; body.transform.position = start; body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            Assert.That(IsWalkable(shape, inner, rocks, tolerance), Is.True, "Invalid initial player placement.");
            int firstInvalid = -1, contactSteps = 0;
            float maxPressureDisplacement = 0;
            var contacts = new ContactPoint2D[128];
            int steps = Mathf.RoundToInt((pressureOnly ? 1 : 12) / step);
            for (int tick = 0; tick < steps; tick++) {
                player.ApplyCommand(new QuackQuack.Core.Shared.PlayerCommand(boss ? outward : Vector2.zero));
                player.SendMessage("FixedUpdate"); bootstrap.Run.Advance(step);
                if (boss) director.Boss.Advance(step); else director.Scheduler.Advance(step);
                Physics2D.Simulate(step);
                if (!IsWalkable(shape, inner, rocks, tolerance) && firstInvalid < 0) firstInvalid = tick;
                int count = shape.GetContacts(contacts);
                for (int i = 0; i < count; i++)
                    if (contacts[i].collider.GetComponent<PooledEnemy>() != null ||
                        contacts[i].otherCollider.GetComponent<PooledEnemy>() != null) { contactSteps++; break; }
                maxPressureDisplacement = Mathf.Max(maxPressureDisplacement, Vector2.Dot(body.position - start, outward));
            }
            Vector2 releasedAt = body.position;
            foreach (var enemy in enemies) enemy.Despawn(enemy.LeaseId);
            if (boss) director.Boss.gameObject.SetActive(false); // Remove the solid boss too: recovery requires free space.
            Vector2 retreat = scenario.Contains("rock") || pressureOnly ? -outward : (-releasedAt).normalized;
            for (int tick = 0; tick < Mathf.RoundToInt(2.4f / step); tick++) {
                player.ApplyCommand(new QuackQuack.Core.Shared.PlayerCommand(retreat));
                player.SendMessage("FixedUpdate"); bootstrap.Run.Advance(step); Physics2D.Simulate(step);
            }
            float retreatDistance = Vector2.Dot(body.position - releasedAt, retreat);
            TestContext.WriteLine(FormattableString.Invariant($"scenario={scenario}; seed={bootstrap.Run.Seed}; dt={step}; firstInvalid={firstInvalid}; contacts={contactSteps}; pressure={maxPressureDisplacement}; released={releasedAt}; returned={body.position}; retreat={retreatDistance}"));
            var failures = new List<string>();
            if (firstInvalid >= 0) failures.Add("Whole body crossed walkable boundary/obstacle at tick " + firstInvalid);
            if (!IsWalkable(shape, inner, rocks, tolerance)) failures.Add("Player failed to return to walkable space after horde release");
            if (retreatDistance <= 0.5f) failures.Add("Movement did not resume toward free space");
            if (pressureOnly && contactSteps == 0) failures.Add("Enemy/player physical contact was removed");
            if (pressureOnly && maxPressureDisplacement <= 0.05f) failures.Add("Idle player must still yield to normal enemy pressure");
            Assert.That(failures, Is.Empty, string.Join("; ", failures));
            yield return null;
        }

        private static bool IsWalkable(CircleCollider2D shape, Rect inner, BoxCollider2D[] rocks, float tolerance)
        {
            var bounds = shape.bounds;
            if (bounds.min.x < inner.xMin - tolerance || bounds.max.x > inner.xMax + tolerance ||
                bounds.min.y < inner.yMin - tolerance || bounds.max.y > inner.yMax + tolerance) return false;
            foreach (var rock in rocks) {
                var distance = shape.Distance(rock);
                if (!distance.isValid || distance.distance < -tolerance) return false;
            }
            return true;
        }
        private static readonly string[] Cases = { "grid64-a", "grid64-b", "grid64-no-player-contacts", "grid64-frozen", "no-enemies", "spawn64-right-a", "spawn64-right-b" };

        [UnityTest]
        public IEnumerator DiagnosticLakeRunPressureWritesEvidence([ValueSource(nameof(Cases))] string scenario)
        {
            var player = Object.FindAnyObjectByType<QuackQuack.Gameplay.Player.PlayerController>();
            var body = player.GetComponent<Rigidbody2D>(); var shape = player.GetComponent<CircleCollider2D>();
            var bank = GameObject.Find("East bank").GetComponent<BoxCollider2D>();
            Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Dynamic));
            Assert.That(shape.radius, Is.EqualTo(0.42f).Within(0.001));
            Assert.That(bank.bounds.size.x, Is.EqualTo(0.5f).Within(0.001));
            player.State.TakeDamage(1, 0, float.MaxValue); // Isolate displacement from death; physical properties stay authored.
            for (int i = 0; i < director.Spawns.Capacity; i++)
                if (director.Spawns.TryGetActive(i, out var initial, out long lease)) initial.Despawn(lease);
            director.Spawns.ActiveLimit = 200;
            int enemyCount = scenario == "no-enemies" ? 0 : 64;
            if(enemyCount > 0) Assert.That(director.Spawns.TryEnqueue(new QuackQuack.Core.Run.SpawnRequest(1000000,"arena-diagnostic","formiga",64,
                new Rect(-9,-5,1,1),0,QuackQuack.Core.Combat.EventPriority.Feedback,long.MaxValue)), Is.True);
            for (int i = 0; i < 20 && director.Spawns.ActiveCount < enemyCount; i++) director.Spawns.Process(1000000+i,new Rect(-100,-100,1,1));
            Assert.That(director.Spawns.ActiveCount, Is.EqualTo(enemyCount));
            var enemies = new List<PooledEnemy>();
            for (int i = 0; i < director.Spawns.Capacity; i++) if (director.Spawns.TryGetActive(i,out var enemy,out _)) enemies.Add(enemy);
            bool atSpawn = scenario.StartsWith("spawn", StringComparison.Ordinal);
            Vector2 start = atSpawn ? body.position : new Vector2(bank.bounds.min.x - shape.radius - 0.03f, 0);
            body.position = start; body.transform.position = start; body.linearVelocity = Vector2.zero;
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                Vector2 p = start + new Vector2((atSpawn ? 1 : -1)*(0.9f+(i%8)*0.45f), ((i/8)-3.5f)*0.3f);
                enemy.Body.position = p; enemy.transform.position = p; enemy.Body.linearVelocity = Vector2.zero;
                Assert.That(enemy.Body.bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
                if(scenario == "grid64-no-player-contacts") enemy.Shape.excludeLayers |= 1 << player.gameObject.layer;
            }
            Physics2D.SyncTransforms();
            var south = GameObject.Find("South bank").GetComponent<BoxCollider2D>();
            var north = GameObject.Find("North bank").GetComponent<BoxCollider2D>();
            var west = GameObject.Find("West bank").GetComponent<BoxCollider2D>();
            var inner = Rect.MinMaxRect(west.bounds.max.x,south.bounds.max.y,bank.bounds.min.x,north.bounds.min.y);
            var outer = Rect.MinMaxRect(west.bounds.min.x,south.bounds.min.y,bank.bounds.max.x,north.bounds.max.y);
            var observed = new List<Collider2D>(); foreach (var enemy in enemies) observed.Add(enemy.Shape);
            observed.Add(bank); observed.Add(south); observed.Add(north); observed.Add(west);
            var observer = new GameObject("LakeRun pressure observer").AddComponent<ArenaPressureDiagnostics>();
            observer.Begin(bootstrap.Run.Seed,body,shape,observed.ToArray(),1400,128,false);
            int firstIntrusion = -1, firstCenterOutside = -1, firstFullyOutside = -1, maxContacts = 0, fullBuffers = 0;
            float maxX = body.position.x;
            for (int tick = 0; tick < 600; tick++)
            {
                player.ApplyCommand(new QuackQuack.Core.Shared.PlayerCommand(Vector2.zero));
                player.SendMessage("FixedUpdate");
                bootstrap.Run.Advance(0.02f);
                if(scenario != "grid64-frozen") director.Scheduler.Advance(0.02f);
                observer.Capture(tick,tick*0.02,ArenaCapturePhase.FixedBeforeSimulation);
                Physics2D.Simulate(0.02f);
                observer.Capture(tick,tick*0.02+0.02,ArenaCapturePhase.ExplicitAfterSimulation);
                observer.TryGetFrame(observer.Count-1,out var sample);
                maxContacts=Math.Max(maxContacts,sample.Contacts);
                if(sample.ContactBufferFull||sample.OverlapBufferFull)fullBuffers++;
                maxX=Math.Max(maxX,body.position.x);
                if(firstIntrusion<0&&(shape.bounds.max.x>inner.xMax+0.02f||shape.bounds.min.x<inner.xMin-0.02f||shape.bounds.max.y>inner.yMax+0.02f||shape.bounds.min.y<inner.yMin-0.02f))firstIntrusion=tick;
                if(firstCenterOutside<0&&!outer.Contains(body.position))firstCenterOutside=tick;
                if(firstFullyOutside<0&&(shape.bounds.min.x>outer.xMax||shape.bounds.max.x<outer.xMin||shape.bounds.min.y>outer.yMax||shape.bounds.max.y<outer.yMin))firstFullyOutside=tick;
            }
            observer.Stop();
            string output=Environment.GetEnvironmentVariable("QUACK_ARENA_OUTPUT");
            if(string.IsNullOrWhiteSpace(output))output=Path.Combine(Application.temporaryCachePath,"ArenaPressureT003");
            Directory.CreateDirectory(output);
            string prefix=Path.Combine(output,scenario); observer.ExportCsv(prefix);
            Vector2 beforeRelease=body.position;
            foreach(var enemy in enemies)enemy.Despawn(enemy.LeaseId);
            for(int tick=0;tick<120;tick++) {
                player.ApplyCommand(new QuackQuack.Core.Shared.PlayerCommand((-beforeRelease).normalized)); player.SendMessage("FixedUpdate");
                bootstrap.Run.Advance(0.02f); Physics2D.Simulate(0.02f);
            }
            bool quackAccepted = player.ActivateQuack();
            string summary=FormattableString.Invariant($"scenario={scenario}\nseed={bootstrap.Run.Seed}\nstep=0.02\nenemies={enemyCount}\nstart_x={start.x:R}\nstart_y={start.y:R}\nbank_inner={bank.bounds.min.x:R}\nbank_outer={bank.bounds.max.x:R}\nfirst_intrusion={firstIntrusion}\nfirst_center_outside={firstCenterOutside}\nfirst_fully_outside={firstFullyOutside}\nmax_x={maxX:R}\nbefore_release_x={beforeRelease.x:R}\nbefore_release_y={beforeRelease.y:R}\nafter_return_x={body.position.x:R}\nafter_return_y={body.position.y:R}\ninner_ymin={inner.yMin:R}\nouter_ymin={outer.yMin:R}\nquack_accepted_after_return={quackAccepted}\nphase_after_return={bootstrap.Run.Phase}\nvelocity_iterations={Physics2D.velocityIterations}\nposition_iterations={Physics2D.positionIterations}\nmax_contacts={maxContacts}\nfull_buffers={fullBuffers}\noverwritten={observer.Overwritten}");
            File.WriteAllText(prefix+".summary.txt",summary);
            TestContext.WriteLine(summary);
            Assert.That(observer.Count, Is.EqualTo(1200)); Assert.That(observer.Overwritten, Is.Zero);
            Assert.That(fullBuffers, Is.Zero);
            Object.DestroyImmediate(observer.gameObject);
            yield return null;
        }
    }
}