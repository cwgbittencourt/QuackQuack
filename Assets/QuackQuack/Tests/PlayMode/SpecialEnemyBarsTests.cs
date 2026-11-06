using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Run;
using QuackQuack.Core.Shared;
using QuackQuack.Gameplay.Enemies;
using QuackQuack.Gameplay.Bosses;
using QuackQuack.Gameplay.Player;
using QuackQuack.Gameplay.Weapons;
using QuackQuack.Presentation.UI;
using QuackQuack.Systems.Run;
using QuackQuack.Systems.Pooling;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.PlayMode
{
    [Category("SpecialEnemyBarsRuntime")]
    public sealed class SpecialEnemyBarsTests
    {
        private readonly List<GameObject> owned = new List<GameObject>();
        private Camera camera;
        private RectTransform area;
        private SpecialEnemyBarsView view;
        private SpecialEnemyRegistry registry;
        private RunDirector director;
        private bool loaded;
        private string directory;
        private Func<string> previousPath;
        private GameObject Item(string name, params Type[] components)
        { var item = new GameObject(name, components); owned.Add(item); return item; }
        private SpecialEnemySnapshot Snapshot(int id = 1, long lease = 1, float health = 100, Vector3? point = null,
            string name = "Libélula Alfa", SpecialEnemyCategory kind = SpecialEnemyCategory.MiniBoss) =>
            new SpecialEnemySnapshot(id, lease, "encounter", name, kind, point ?? Vector3.zero, health, 100);
        private void CanvasFixture(int capacity = 3)
        {
            camera = Item("Special camera", typeof(Camera)).GetComponent<Camera>(); camera.enabled = false;
            camera.orthographic = true; camera.orthographicSize = 5; camera.transform.position = new Vector3(0, 0, -10);
            var canvas = Item("Special canvas", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            area = Item("Safe overlay", typeof(RectTransform)).GetComponent<RectTransform>(); area.SetParent(canvas.transform, false);
            area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one; area.offsetMin = area.offsetMax = Vector2.zero;
            registry = new SpecialEnemyRegistry(capacity);
            view = Item("Special bars", typeof(RectTransform), typeof(SpecialEnemyBarsView)).GetComponent<SpecialEnemyBarsView>();
            view.Bind(registry, camera, area); Canvas.ForceUpdateCanvases();
        }
        private RectTransform Bar(int id, long lease)
        { Assert.That(view.TryGetBar(id, lease, out var bar), Is.True); return bar; }
        private Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            return new Rect(corners[0], corners[2] - corners[0]);
        }

        [Test] public void EnemyProjectionUsesScaledMaximumAndRejectsDeadOrStaleLeases()
        {
            var state = new EnemyState(); Assert.That(state.TryActivate(1, "mini", 100, 1, 0, Vector2.zero, 10), Is.True);
            Assert.That(state.TryScaleEncounter(1, 2), Is.True); state.TryDamage(1, 50, out _, out _);
            Assert.That(state.TryGetSpecialSnapshot(1, 20, "encounter", "Mini", Vector3.up, out var snapshot), Is.True);
            Assert.That(snapshot.Health01, Is.EqualTo(0.75f)); Assert.That(snapshot.Category, Is.EqualTo(SpecialEnemyCategory.MiniBoss));
            Assert.That(state.TryGetSpecialSnapshot(2, 20, "encounter", "Mini", Vector3.up, out _), Is.False);
            state.TryDamage(1, 1000, out _, out _);
            Assert.That(state.TryGetSpecialSnapshot(1, 20, "encounter", "Mini", Vector3.up, out _), Is.False);
            state.Reset(1); state.TryActivate(2, "mini", 100, 1, 0, Vector2.zero, 10);
            Assert.That(snapshot.Health01, Is.EqualTo(0.75f), "A value snapshot cannot change on reuse.");
            Assert.That(state.TryGetSpecialSnapshot(1, 20, "encounter", "Mini", Vector3.up, out _), Is.False);
        }
        [Test] public void RegistryRejectsDefaultDuplicateInstanceAndOverflowThenClearsReferences()
        {
            var source = new SpecialEnemyRegistry(2); Assert.That(source.TryAdd(default), Is.False);
            Assert.That(source.TryAdd(Snapshot()), Is.True); Assert.That(source.TryAdd(Snapshot(lease: 2)), Is.False);
            Assert.That(source.TryAdd(Snapshot(2)), Is.True); Assert.That(source.TryAdd(Snapshot(3)), Is.False);
            source.Clear(); Assert.That(source.Count, Is.Zero); Assert.Throws<ArgumentOutOfRangeException>(() => { var unused = source[0]; });
            Assert.That(source.TryAdd(Snapshot(lease: 2)), Is.True);
        }
        [Test] public void IndividualHealthAndNameResetWhenSameActorGetsANewLease()
        {
            CanvasFixture(); registry.TryAdd(Snapshot(health: 25)); view.Refresh();
            var first = Bar(1, 1); Assert.That(first.Find("Health track/Health fill").GetComponent<RectTransform>().anchorMax.x, Is.EqualTo(0.25f));
            registry.Clear(); registry.TryAdd(Snapshot(lease: 2, name: "Sapo-Rei", kind: SpecialEnemyCategory.Boss)); view.Refresh();
            Assert.That(view.TryGetBar(1, 1, out _), Is.False);
            var next = Bar(1, 2); Assert.That(next, Is.SameAs(first));
            Assert.That(next.GetComponentInChildren<Text>().text, Is.EqualTo("CHEFE · Sapo-Rei"));
            Assert.That(next.Find("Health track/Health fill").GetComponent<RectTransform>().anchorMax.x, Is.EqualTo(1));
            foreach (var graphic in next.GetComponentsInChildren<Graphic>()) Assert.That(graphic.raycastTarget, Is.False);
            registry.Clear(); view.Refresh(); Assert.That(first.gameObject.activeSelf, Is.False); Assert.That(view.VisibleCount, Is.Zero);
        }
        [Test] public void CoincidentSpecialsGetIndividualNonOverlappingLanesAndOwnTethers()
        {
            CanvasFixture(); for (int i = 1; i <= 3; i++) registry.TryAdd(Snapshot(i)); view.Refresh();
            Assert.That(view.VisibleCount, Is.EqualTo(3));
            for (int i = 1; i <= 3; i++)
            {
                var bar = Bar(i, 1);
                for (int j = 1; j < i; j++) Assert.That(Bounds(bar).Overlaps(Bounds(Bar(j, 1))), Is.False);
                Assert.That(Bounds(area).Contains(Bounds(bar).min), Is.True); Assert.That(Bounds(area).Contains(Bounds(bar).max), Is.True);
            }
            int lines = 0;
            foreach (var graphic in area.GetComponentsInChildren<Image>()) if (graphic.name == "Anchor tether")
            { lines++; Assert.That(graphic.rectTransform.rect.height, Is.GreaterThan(0)); Assert.That(graphic.transform.GetSiblingIndex(), Is.LessThan(Bar(1, 1).GetSiblingIndex())); }
            Assert.That(lines, Is.EqualTo(3), "Each bar retains a tether behind all labels.");
        }
        [Test] public void OffscreenAndBehindCameraNeverClampToAnotherTargetsPosition()
        {
            CanvasFixture(); registry.TryAdd(Snapshot()); view.Refresh(); var bar = Bar(1, 1); var previous = bar.anchoredPosition;
            camera.transform.position += Vector3.right; view.Refresh(); Assert.That(bar.anchoredPosition.x, Is.LessThan(previous.x));
            registry.Clear(); registry.TryAdd(Snapshot(point: new Vector3(10000, 0, 0))); view.Refresh(); Assert.That(view.VisibleCount, Is.Zero);
            registry.Clear(); registry.TryAdd(Snapshot(point: new Vector3(0, 0, -20))); view.Refresh(); Assert.That(view.VisibleCount, Is.Zero);
            Assert.That(bar.gameObject.activeSelf, Is.False);
        }
        [Test] public void InsufficientSafeSpaceHidesInsteadOfOverlappingOrLeavingContainer()
        {
            CanvasFixture(); area.anchorMin = area.anchorMax = new Vector2(0.5f, 0.5f); area.sizeDelta = new Vector2(400, 120);
            registry.TryAdd(Snapshot()); registry.TryAdd(Snapshot(2)); view.Refresh();
            Assert.That(view.VisibleCount, Is.EqualTo(1)); Assert.That(view.HiddenForSpace, Is.EqualTo(1));
        }
        [Test] public void StableRefreshReusesPrewarmedObjectsAndDisableUnbindHideThem()
        {
            CanvasFixture(); registry.TryAdd(Snapshot()); view.Refresh(); var bar = Bar(1, 1);
            int count = area.GetComponentsInChildren<RectTransform>(true).Length;
            for (int i = 0; i < 100; i++) view.Refresh();
            Assert.That(area.GetComponentsInChildren<RectTransform>(true).Length, Is.EqualTo(count)); Assert.That(Bar(1, 1), Is.SameAs(bar));
            view.enabled = false; Assert.That(bar.gameObject.activeSelf, Is.False);
            view.enabled = true; view.Refresh(); Assert.That(view.VisibleCount, Is.EqualTo(1));
            view.Unbind(); Assert.That(bar.gameObject.activeSelf, Is.False);
        }
        [Test] public void RegistryProjectionDoesNotAllocateAfterWarmup()
        {
            var source = new SpecialEnemyRegistry(3); var snapshot = Snapshot();
            for (int i = 0; i < 10; i++) { source.Clear(); source.TryAdd(snapshot); }
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) { source.Clear(); source.TryAdd(snapshot); }
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
        [Test] public void BossPoolReuseHasANewLeaseAndNoSnapshotBeforeEncounterInitialization()
        {
            using (var pools = new PoolService())
            {
                pools.Register("boss", PoolFamily.Enemy, () => Item("Pooled boss", typeof(FrogKingController)).GetComponent<FrogKingController>(), 1);
                var boss = pools.Acquire<FrogKingController>("boss"); long first = boss.LeaseId;
                Assert.That(first, Is.GreaterThan(0)); Assert.That(boss.TryGetSpecialSnapshot("boss", out _), Is.False);
                pools.Release(boss); Assert.That(boss.LeaseId, Is.Zero);
                var reused = pools.Acquire<FrogKingController>("boss"); Assert.That(reused, Is.SameAs(boss)); Assert.That(reused.LeaseId, Is.GreaterThan(first));
                pools.Release(reused);
            }
        }
        [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(0f)]
        public void InvalidHealthCannotEnterAValidSnapshot(float health)
        {
            Assert.Throws<ArgumentException>(() => Snapshot(health: health));
            Assert.Throws<ArgumentException>(() => new SpecialEnemySnapshot(1, 1, "encounter", "Mini", SpecialEnemyCategory.MiniBoss, Vector3.zero, 10, health));
        }
        [UnityTest] public IEnumerator CaptureIndividualOverlappingSpecialsForVisualReview()
        {
            CanvasFixture();
            var canvas = area.GetComponentInParent<Canvas>();
            var target = new RenderTexture(1920, 1080, 24); target.Create();
            camera.targetTexture = target; camera.aspect = 1920f / 1080;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.12f, 0.2f, 0.28f);
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            var pixels = new Texture2D(1920, 1080, TextureFormat.RGB24, false); var previous = RenderTexture.active;
            try
            {
                yield return null; Canvas.ForceUpdateCanvases();
                registry.TryAdd(Snapshot(1, health: 25)); registry.TryAdd(Snapshot(2, health: 60, name: "Sapo-Cururu"));
                registry.TryAdd(Snapshot(3, name: "Sapo-Rei", kind: SpecialEnemyCategory.Boss));
                view.Refresh(); Assert.That(view.VisibleCount, Is.EqualTo(3)); Canvas.ForceUpdateCanvases();
                camera.Render(); RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); pixels.Apply();
                string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../Logs/Polimento002/T037"));
                Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, "overlapping-specials.png"), pixels.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; camera.targetTexture = null; target.Release(); Object.Destroy(target); Object.Destroy(pixels); }
        }

        private IEnumerator LoadRun()
        {
            loaded = true; previousPath = RunDirector.SavePathProvider;
            directory = Path.Combine(Application.temporaryCachePath, "QuackQuack-T037-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory); RunDirector.SavePathProvider = () => Path.Combine(directory, "save.json");
            yield return SceneManager.LoadSceneAsync("RunBootstrap");
            for (int i = 0; i < 300; i++) { director = Object.FindAnyObjectByType<RunDirector>(); if (director != null && director.Ready) break; yield return null; }
            Assert.That(director != null && director.Ready, Is.True); director.AutomaticTick = false; director.Scheduler.enabled = false;
            foreach (var runner in Object.FindObjectsByType<WeaponRunner>(FindObjectsSortMode.None)) runner.enabled = false;
        }
        [UnityTest] public IEnumerator RunRegistryTracksOnlyMinibossesAndRemovesDisabledDeadReusedAndTerminalActors()
        {
            yield return LoadRun();
            var spawns = director.Spawns;
            Assert.That(spawns.TryEnqueue(new SpawnRequest(100000, "mini", "libelula-alfa", 1, new Rect(-15, -8, 30, 16), 1, EventPriority.Essential, 200000)), Is.True);
            for (int tick = 100000; tick < 100010; tick++) spawns.Process(tick, new Rect(-4, -3, 8, 6));
            PooledEnemy mini = null;
            for (int i = 0; i < spawns.Capacity; i++) if (spawns.TryGetActive(i, out var actor, out _) && actor.Definition.Id == "libelula-alfa") mini = actor;
            Assert.That(mini, Is.Not.Null); Physics2D.SyncTransforms(); director.RefreshSpecialEnemies();
            Assert.That(director.SpecialEnemies.Count, Is.EqualTo(1)); var before = director.SpecialEnemies[0];
            Assert.That(before.InstanceId, Is.EqualTo(mini.GetInstanceID())); Assert.That(before.LeaseId, Is.EqualTo(mini.LeaseId));
            mini.TakeDamage(mini.LeaseId, mini.State.Health / 2, 1, 0); director.RefreshSpecialEnemies();
            Assert.That(director.SpecialEnemies[0].Health01, Is.EqualTo(0.5f));
            GameBootstrap.Instance.Run.TryTransition(RunPhase.Paused); mini.enabled = false; yield return null;
            Assert.That(director.SpecialEnemies.Count, Is.Zero);
            mini.enabled = true; director.RefreshSpecialEnemies(); Assert.That(director.SpecialEnemies.Count, Is.EqualTo(1));
            GameBootstrap.Instance.Run.TryResume(); mini.TakeDamage(mini.LeaseId, float.MaxValue, 1, 1); director.RefreshSpecialEnemies();
            Assert.That(director.SpecialEnemies.Count, Is.Zero);
            Assert.That(spawns.TryEnqueue(new SpawnRequest(100001, "mini", "libelula-alfa", 1, new Rect(-15, -8, 30, 16), 1, EventPriority.Essential, 200000)), Is.True);
            for (int tick = 100010; tick < 100020; tick++) spawns.Process(tick, new Rect(-4, -3, 8, 6));
            director.RefreshSpecialEnemies(); Assert.That(director.SpecialEnemies.Count, Is.EqualTo(1));
            Assert.That(director.SpecialEnemies[0].LeaseId, Is.Not.EqualTo(before.LeaseId));
            director.TryAbandon(); Assert.That(director.SpecialEnemies.Count, Is.Zero);
        }
        [UnityTest] public IEnumerator BossRegistryUsesLiveLeaseAndScaledHealthAndClearsAtVictory()
        {
            yield return LoadRun(); var run = GameBootstrap.Instance.Run;
            var player = Object.FindAnyObjectByType<PlayerController>();
            run.Advance(900); director.Advance(0, new Rect(-4, -3, 8, 6));
            Assert.That(director.Boss, Is.Not.Null); director.RefreshSpecialEnemies();
            Assert.That(director.SpecialEnemies.Count, Is.EqualTo(1)); var snapshot = director.SpecialEnemies[0];
            Assert.That(snapshot.Category, Is.EqualTo(SpecialEnemyCategory.Boss)); Assert.That(snapshot.LeaseId, Is.GreaterThan(0));
            Assert.That(snapshot.InstanceId, Is.EqualTo(director.Boss.GetInstanceID()));
            director.Boss.TakeDamage(director.Boss.MaxHealth / 4, player.GetInstanceID(), 0); director.RefreshSpecialEnemies();
            Assert.That(director.SpecialEnemies[0].Health01, Is.EqualTo(0.75f));
            director.Boss.enabled = false; director.RefreshSpecialEnemies(); Assert.That(director.SpecialEnemies.Count, Is.Zero);
            director.Boss.enabled = true; director.RefreshSpecialEnemies(); Assert.That(director.SpecialEnemies.Count, Is.EqualTo(1));
            director.Boss.TakeDamage(float.MaxValue, player.GetInstanceID(), 1); Assert.That(run.Outcome, Is.EqualTo(RunPhase.Won));
            Assert.That(director.SpecialEnemies.Count, Is.Zero);
        }
        [UnityTest] public IEnumerator BothAuthoredMinibossesHaveSeparateEncounterLabelsAndHealth()
        {
            yield return LoadRun();
            var names = new[] { "libelula-alfa", "sapo-cururu" };
            for (int i = 0; i < names.Length; i++)
                Assert.That(director.Spawns.TryEnqueue(new SpawnRequest(100000 + i, "mini", names[i], 1, new Rect(-15, -8, 30, 16), 1, EventPriority.Essential, 200000)), Is.True);
            for (int tick = 100000; tick < 100010; tick++) director.Spawns.Process(tick, new Rect(-4, -3, 8, 6));
            director.RefreshSpecialEnemies(); Assert.That(director.SpecialEnemies.Count, Is.EqualTo(2));
            var first = director.SpecialEnemies[0]; var second = director.SpecialEnemies[1];
            Assert.That(first.InstanceId, Is.Not.EqualTo(second.InstanceId)); Assert.That(first.EncounterId, Is.Not.EqualTo(second.EncounterId));
            Assert.That(first.DisplayName, Is.Not.EqualTo(second.DisplayName));
            Assert.That(first.Health01, Is.EqualTo(1)); Assert.That(second.Health01, Is.EqualTo(1));
            Assert.That(first.Category, Is.EqualTo(SpecialEnemyCategory.MiniBoss)); Assert.That(second.Category, Is.EqualTo(SpecialEnemyCategory.MiniBoss));
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            foreach (var item in owned) if (item != null) Object.Destroy(item); owned.Clear();
            if (loaded)
            {
                if (director != null) director.TryAbandon();
                if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
            }
            yield return null;
            if (!loaded) yield break;
            var scene = SceneManager.GetActiveScene(); SceneManager.SetActiveScene(SceneManager.CreateScene("After special bars"));
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            RunDirector.SavePathProvider = previousPath;
            string resolved = Path.GetFullPath(directory), prefix = Path.GetFullPath(Application.temporaryCachePath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Assert.That(resolved.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && Path.GetFileName(resolved).StartsWith("QuackQuack-T037-"), Is.True);
            if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
            loaded = false;
        }
    }
}
