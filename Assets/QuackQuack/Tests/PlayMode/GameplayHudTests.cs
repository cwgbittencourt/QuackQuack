using System.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Core.Run;
using QuackQuack.Core.Shared;
using QuackQuack.Content.Characters;
using QuackQuack.Content.Shared;
using QuackQuack.Gameplay.Player;
using QuackQuack.Platform.Input;
using QuackQuack.Presentation.UI;
using QuackQuack.Systems.Run;
using QuackQuack.Systems.Combat;
using QuackQuack.Systems.Upgrades;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.PlayMode
{
    public class GameplayHudTests
    {
        private readonly List<Object> owned = new List<Object>();
        private bool loadedArena;
        [SetUp] public void BeginCase() { loadedArena = false; }

        [UnityTest]
        public IEnumerator HudShowsTwoResourceBarsWithoutNumbersOrTerrainAndPreservesRules()
        {
            yield return LoadArena();
            var hud = Object.FindAnyObjectByType<GameplayHud>();
            Assert.That(hud, Is.Not.Null, "Lake needs a bound uGUI HUD.");
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(VisibleText(hud, "healthLabel"), Does.Not.Match("[0-9]"), "FR-004: no numeric resource labels.");
            Assert.That(VisibleText(hud, "breathLabel"), Does.Not.Match("[0-9]"));
            Assert.That(VisibleText(hud, "terrainLabel"), Is.Empty, "FR-004: terrain instructions leave the permanent HUD.");
            Assert.That(player.QuackReady, Is.True);
            Assert.That(Field<Image>(hud, "healthFill"), Is.Not.SameAs(Field<Image>(hud, "breathFill")));
            Assert.That(Field<Image>(hud, "breathFill").fillAmount, Is.EqualTo(1));
            player.State.TakeDamage(25, GameBootstrap.Instance.Run.ElapsedSeconds, 0);
            yield return null; yield return null;
            Assert.That(Field<Image>(hud, "healthFill").fillAmount, Is.EqualTo(0.75f).Within(0.001f));
            Assert.That(player.State.Health, Is.EqualTo(75));
        }

        [TestCase(0, 0, 100, 100, 0, 0)]
        [TestCase(75, 25, 100, 100, 0.75f, 0.25f)]
        [TestCase(150, 70, 200, 140, 0.75f, 0.5f)]
        [TestCase(100, 140, 100, 140, 1, 1)]
        [TestCase(150, 200, 100, 140, 1, 1)]
        [TestCase(-1, -1, 100, 100, 0, 0)]
        [TestCase(0, 0, 0, 0, 0, 0)]
        public void ResourceSnapshotProvidesNormalizedHealthAndActualBreathCapacity(float health, float breath, float maximum, float capacity, float hp01, float breath01)
        {
            object snapshot = new HudSnapshot(health, maximum, breath, capacity, "", "", 0);
            Assert.That(Convert.ToSingle(Read(snapshot, "Health01")), Is.EqualTo(hp01).Within(0.000001));
            Assert.That(Convert.ToSingle(Read(snapshot, "Breath01")), Is.EqualTo(breath01).Within(0.000001));
        }

        [TestCase(0, 0, 100, 100, 0, 0)]
        [TestCase(75, 25, 100, 100, 0.75f, 0.25f)]
        [TestCase(150, 70, 200, 140, 0.75f, 0.5f)]
        [TestCase(100, 140, 100, 140, 1, 1)]
        [TestCase(150, 200, 100, 140, 1, 1)]
        [TestCase(-1, -1, 100, 100, 0, 0)]
        [TestCase(0, 0, 0, 0, 0, 0)]
        public void ResourceBarsRenderClampedSnapshotWithoutMutatingItsValues(float health, float breath, float maximum, float capacity, float hp01, float breath01)
        {
            var hud = ResourceHud();
            var snapshot = new HudSnapshot(health, maximum, breath, capacity, "", "", 0);
            hud.Render(snapshot);
            Assert.That(Field<Image>(hud, "healthFill").fillAmount, Is.EqualTo(hp01).Within(0.000001));
            Assert.That(Field<Image>(hud, "breathFill").fillAmount, Is.EqualTo(breath01).Within(0.000001));
            Assert.That(snapshot.Health, Is.EqualTo(health)); Assert.That(snapshot.Breath, Is.EqualTo(breath));
            Assert.That(snapshot.MaxHealth, Is.EqualTo(maximum)); Assert.That(snapshot.Capacity, Is.EqualTo(capacity));
        }

        private GameplayHud ResourceHud()
        {
            var root = Own(new GameObject("HUD resource fixture", typeof(RectTransform), typeof(Canvas)));
            var hud = root.AddComponent<GameplayHud>();
            foreach (string name in new[] { "healthLabel", "breathLabel", "statusLabel", "timeLabel", "terrainLabel", "quackLabel" })
            {
                var field = typeof(GameplayHud).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null) field.SetValue(hud, Label(root.transform));
            }
            foreach (string name in new[] { "healthFill", "breathFill" })
            {
                var bar = new GameObject(name, typeof(RectTransform), typeof(Image)); bar.transform.SetParent(root.transform, false);
                typeof(GameplayHud).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(hud, bar.GetComponent<Image>());
            }
            return hud;
        }

        private sealed class NoInput : IPlayerInput { public PlayerCommand Read() => default; }

        [UnityTest]
        public IEnumerator ResourceShapesAndQuackWordsDistinguishStatesWithoutColor()
        {
            yield return LoadArena();
            var hud = Object.FindAnyObjectByType<GameplayHud>(); var player = Object.FindAnyObjectByType<PlayerController>();
            var health = Field<ResourceIconGraphic>(hud, "healthIcon"); var breath = Field<ResourceIconGraphic>(hud, "breathIcon");
            Assert.That(health.Kind, Is.EqualTo(ResourceIconKind.Health)); Assert.That(breath.Kind, Is.EqualTo(ResourceIconKind.Breath));
            Assert.That(health.canvasRenderer, Is.Not.Null); Assert.That(breath.canvasRenderer, Is.Not.Null);
            health.color = breath.color = Color.gray;
            Assert.That(IconVertexCount(health), Is.Not.EqualTo(IconVertexCount(breath)), "The silhouettes must differ even with the same tint.");
            Assert.That(health.raycastTarget || breath.raycastTarget, Is.False);
            hud.Unbind(); hud.Render(player.Snapshot());
            Assert.That(Field<Text>(hud, "quackLabel").text, Does.Contain("APRIMORADO"));
            player.State.Breath.TryConsume(1, out _); hud.Render(player.Snapshot());
            Assert.That(Field<Text>(hud, "quackLabel").text, Does.Contain("PRONTO").And.Not.Contain("APRIMORADO"));
            Assert.That(player.ActivateQuack(), Is.True); hud.Render(player.Snapshot());
            Assert.That(Field<Text>(hud, "quackLabel").text, Does.Contain("RECARREGANDO"));
        }

        [UnityTest]
        public IEnumerator ProducerAndExistingFooterDoNotPublishPermanentInstructionsOrTerrain()
        {
            yield return LoadArena();
            var hud = Object.FindAnyObjectByType<GameplayHud>(); var player = Object.FindAnyObjectByType<PlayerController>();
            var snapshot = player.Snapshot(); Assert.That(snapshot.Terrain, Is.Empty); Assert.That(snapshot.Status, Is.Empty);
            var controls = Field<Text>(hud, "statusLabel").transform.parent.Find("Controls");
            if (controls != null) Assert.That(controls.GetComponent<Text>().text, Is.Empty);
            Assert.That(Field<Image>(hud, "experienceFill").fillAmount, Is.EqualTo(snapshot.Progress.Fraction));
        }

        private static readonly RunPhase[] OtherOutcomes = { RunPhase.Won, RunPhase.Abandoned };
        [UnityTest]
        public IEnumerator TerminalMessagesStaySeparateFromProgress([ValueSource(nameof(OtherOutcomes))] RunPhase outcome)
        {
            yield return LoadArena();
            var hud = Object.FindAnyObjectByType<GameplayHud>(); var player = Object.FindAnyObjectByType<PlayerController>();
            var run = GameBootstrap.Instance.Run;
            if (outcome == RunPhase.Won) Assert.That(run.TryTransition(RunPhase.Boss), Is.True);
            Assert.That(run.TryTransition(outcome), Is.True); yield return null;
            Assert.That(Field<Text>(hud, "statusLabel").text, Does.Contain(outcome == RunPhase.Won ? "VITÓRIA" : "ABANDONADA"));
            Assert.That(Field<Text>(hud, "levelLabel"), Is.Not.SameAs(Field<Text>(hud, "statusLabel")));
        }

        [Test]
        public void CompatibilityCompositionIsCreatedOnceAndAnchorsProgressAtBottomCenter()
        {
            var hud = ResourceHud(); var snapshot = new HudSnapshot(100, 100, 100, 100, "ignored terrain", "warning", 0);
            hud.Render(snapshot); var fill = Field<Image>(hud, "experienceFill"); var level = Field<Text>(hud, "levelLabel");
            int count = hud.GetComponentsInChildren<RectTransform>(true).Length;
            for (int i = 0; i < 100; i++) hud.Render(snapshot);
            Assert.That(hud.GetComponentsInChildren<RectTransform>(true).Length, Is.EqualTo(count));
            Assert.That(Field<Image>(hud, "experienceFill"), Is.SameAs(fill)); Assert.That(Field<Text>(hud, "levelLabel"), Is.SameAs(level));
            var panel = level.transform.parent as RectTransform;
            Assert.That(panel.anchorMin, Is.EqualTo(new Vector2(0.5f, 0))); Assert.That(panel.anchorMax, Is.EqualTo(panel.anchorMin));
            Assert.That(fill.raycastTarget || level.raycastTarget, Is.False);
            Assert.That(Field<Text>(hud, "statusLabel").text, Is.EqualTo("warning"));
            Assert.That(VisibleText(hud, "terrainLabel"), Is.Empty);
        }

        private int IconVertexCount(ResourceIconGraphic icon)
        {
            using (var vertices = new VertexHelper())
            {
                typeof(ResourceIconGraphic).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                    null, new[] { typeof(VertexHelper) }, null).Invoke(icon, new object[] { vertices });
                var mesh = Own(new Mesh()); vertices.FillMesh(mesh); return mesh.vertexCount;
            }
        }

        [Test]
        public void PlayerHudSnapshotCarriesSameProgressAsUpgradeRulesDuringMultipleLevelChoice()
        {
            var run = new RunState(33); run.TryTransition(RunPhase.Playing);
            var events = new GameplayEventBus();
            var quack = Own(ScriptableObject.CreateInstance<QuackDefinition>());
            var character = Own(ScriptableObject.CreateInstance<CharacterDefinition>());
            typeof(ContentDefinition).GetField("id", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(quack, "quack");
            typeof(ContentDefinition).GetField("id", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(character, "pato");
            typeof(CharacterDefinition).GetField("quack", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(character, quack);
            var root = Own(new GameObject("HUD player projection", typeof(PlayerController)));
            var player = root.GetComponent<PlayerController>(); player.Initialize(character, new NoInput(), run, events);
            using (var upgrades = new UpgradeSystem(33))
            {
                upgrades.InitializeProgression(run, new[] { 10, 30, 60 }, new[] { "choice" }); player.Upgrades = upgrades;
                upgrades.TryCollectExperience(35);
                object actual = Read(player.Snapshot(), "Progress");
                Assert.That(actual, Is.EqualTo(Read(upgrades, "Progress")));
                Assert.That(Convert.ToInt32(Read(actual, "Level")), Is.EqualTo(3));
                Assert.That(Convert.ToInt32(Read(actual, "Experience")), Is.EqualTo(35));
                Assert.That(Convert.ToDouble(Read(actual, "Fraction")), Is.EqualTo(1d / 6).Within(0.000001));
                var hud = ResourceHud();
                var bar = new GameObject("Experience bar", typeof(RectTransform), typeof(Image)); bar.transform.SetParent(hud.transform, false);
                var levelLabel = Label(hud.transform);
                var fillField = typeof(GameplayHud).GetField("experienceFill", BindingFlags.Instance | BindingFlags.NonPublic);
                var levelField = typeof(GameplayHud).GetField("levelLabel", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(fillField, Is.Not.Null, "T035 missing XP bar binding.");
                Assert.That(levelField, Is.Not.Null, "T035 missing current level label binding.");
                fillField.SetValue(hud, bar.GetComponent<Image>()); levelField.SetValue(hud, levelLabel);
                hud.Render(player.Snapshot());
                Assert.That(bar.GetComponent<Image>().fillAmount, Is.EqualTo(1f / 6).Within(0.000001));
                Assert.That(levelLabel.text, Does.Contain("3"));
                for (int i = 0; i < 2; i++)
                    Assert.That(upgrades.TryConfirm(upgrades.CurrentOffer.OfferId, "choice"), Is.True);
                Assert.That(run.Phase, Is.EqualTo(RunPhase.Playing));
                upgrades.TryCollectExperience(25); hud.Render(player.Snapshot());
                Assert.That(upgrades.Experience, Is.EqualTo(60)); Assert.That(upgrades.Level, Is.EqualTo(4));
                Assert.That(bar.GetComponent<Image>().fillAmount, Is.EqualTo(1), "Last threshold must render full, not restart at zero.");
                Assert.That(levelLabel.text, Does.Contain("4"));
            }
            events.Clear();
        }

        [UnityTest]
        public IEnumerator HintsBeginOnlyWithSimulationAndHideAtExactlyTenSeconds()
        {
            var run = new RunState(33); var hints = Hints(run, () => InputSource.Keyboard, () => new ControlProfile());
            run.Advance(100); yield return null; AssertHint(hints.view, hints.label, false);
            run.TryTransition(RunPhase.Playing); yield return null; AssertHint(hints.view, hints.label, true);
            run.Advance(9.999); yield return null; AssertHint(hints.view, hints.label, true);
            run.Advance(0.001); yield return null; Assert.That(run.ElapsedSeconds, Is.EqualTo(10));
            AssertHint(hints.view, hints.label, false);
        }

        [UnityTest]
        public IEnumerator PauseAndMandatoryChoiceSuspendHintWindowWithoutWallClockConsumption()
        {
            var run = new RunState(33); run.TryTransition(RunPhase.Playing);
            var hints = Hints(run, () => InputSource.Keyboard, () => new ControlProfile());
            run.Advance(9.5); run.TryTransition(RunPhase.Paused); run.Advance(100);
            yield return new WaitForSecondsRealtime(0.03f);
            Assert.That(run.ElapsedSeconds, Is.EqualTo(9.5));
            run.TryResume(); yield return null; AssertHint(hints.view, hints.label, true);
            run.TryTransition(RunPhase.ChoosingUpgrade); run.Advance(100); yield return null;
            Assert.That(run.ElapsedSeconds, Is.EqualTo(9.5));
            run.TryResume(); yield return null; AssertHint(hints.view, hints.label, true);
            run.Advance(0.5); yield return null; AssertHint(hints.view, hints.label, false);
        }

        [UnityTest]
        public IEnumerator InputFamilyAndRemappedBindingsChangeVisibleInstructionsWithoutRestartingWindow()
        {
            var run = new RunState(33); run.TryTransition(RunPhase.Playing);
            InputSource source = InputSource.Keyboard; var profile = new ControlProfile().WithFamily(ControllerFamily.Xbox);
            var hints = Hints(run, () => source, () => profile); yield return null;
            string keyboard = hints.label.text; Assert.That(keyboard, Is.Not.Empty);
            run.Advance(9.75); source = InputSource.Gamepad; yield return null;
            Assert.That(hints.label.text, Is.Not.EqualTo(keyboard));
            foreach (var family in new[] { ControllerFamily.Xbox, ControllerFamily.PlayStation, ControllerFamily.Generic })
            {
                profile = profile.WithFamily(family); yield return null;
                Assert.That(hints.label.text, Does.Contain(profile.Label(ProfileAction.Quack, null)));
                Assert.That(hints.label.text, Does.Contain(profile.Label(ProfileAction.Pause, null)));
                Assert.That(hints.label.text, Does.Contain(profile.Label(ProfileAction.Relics, null)));
                Assert.That(run.ElapsedSeconds, Is.EqualTo(9.75));
            }
            Assert.That(profile.TryAssign(ProfileAction.Relics, "<Gamepad>/buttonNorth", true, out var remapped), Is.True);
            profile = remapped; yield return null; Assert.That(hints.label.text, Does.Contain(profile.Label(ProfileAction.Relics, null)));
            string gamepad = hints.label.text; source = InputSource.Touch; yield return null;
            Assert.That(hints.label.text, Is.Not.Empty); Assert.That(hints.label.text, Is.Not.EqualTo(gamepad));
            run.Advance(0.25); yield return null; AssertHint(hints.view, hints.label, false);
            source = InputSource.Keyboard; run.Advance(30); yield return null; AssertHint(hints.view, hints.label, false);
        }

        [UnityTest]
        public IEnumerator ReenableOrRebindSameRunCannotReplayExpiredHintsButNewRunGetsItsOwnWindow()
        {
            var run = new RunState(33); run.TryTransition(RunPhase.Playing);
            var hints = Hints(run, () => InputSource.Touch, () => new ControlProfile());
            run.Advance(10); yield return null; AssertHint(hints.view, hints.label, false);
            hints.view.gameObject.SetActive(false); hints.view.gameObject.SetActive(true);
            BindHints(hints.view, run, () => InputSource.Touch, () => new ControlProfile());
            yield return null; AssertHint(hints.view, hints.label, false);
            var nextRun = new RunState(34); nextRun.TryTransition(RunPhase.Playing);
            BindHints(hints.view, nextRun, () => InputSource.Touch, () => new ControlProfile());
            yield return null; AssertHint(hints.view, hints.label, true);
            nextRun.Advance(10); yield return null; AssertHint(hints.view, hints.label, false);
        }

        [UnityTest]
        public IEnumerator EachTerminalOutcomeSuppressesHintsEvenBeforeWindowExpires()
        {
            foreach (var outcome in new[] { RunPhase.Lost, RunPhase.Abandoned, RunPhase.Won })
            {
                var run = new RunState(33); run.TryTransition(RunPhase.Playing);
                var hints = Hints(run, () => InputSource.Keyboard, () => new ControlProfile());
                run.Advance(1); yield return null; AssertHint(hints.view, hints.label, true);
                if (outcome == RunPhase.Won) Assert.That(run.TryTransition(RunPhase.Boss), Is.True);
                Assert.That(run.TryTransition(outcome), Is.True);
                yield return null; AssertHint(hints.view, hints.label, false);
                run.TryTransition(RunPhase.Results); yield return null; AssertHint(hints.view, hints.label, false);
            }
        }

        private T Own<T>(T value) where T : Object { owned.Add(value); return value; }
        private static Text Label(Transform parent)
        {
            var label = new GameObject("Label", typeof(RectTransform), typeof(Text)); label.transform.SetParent(parent, false);
            var text = label.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return text;
        }
        private (MonoBehaviour view, Text label) Hints(RunState run, Func<InputSource> source, Func<ControlProfile> profile)
        {
            var type = typeof(GameplayHud).Assembly.GetType("QuackQuack.Presentation.UI.ContextualHintsView");
            Assert.That(type, Is.Not.Null, "T036 missing ContextualHintsView; temporal hints must use the real run clock.");
            var root = Own(new GameObject("Hint fixture", typeof(RectTransform), typeof(Canvas)));
            var view = (MonoBehaviour)root.AddComponent(type); var label = Label(root.transform);
            var field = type.GetField("label", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "ContextualHintsView needs its serialized Text label binding."); field.SetValue(view, label);
            BindHints(view, run, source, profile); return (view, label);
        }
        private static void BindHints(MonoBehaviour view, RunState run, Func<InputSource> source, Func<ControlProfile> profile)
        {
            var method = view.GetType().GetMethod("Bind", new[] { typeof(RunState), typeof(Func<InputSource>), typeof(Func<ControlProfile>) });
            Assert.That(method, Is.Not.Null, "T036 missing run/source/profile binding."); method.Invoke(view, new object[] { run, source, profile });
        }
        private static void AssertHint(MonoBehaviour view, Text label, bool visible)
        {
            Assert.That(Convert.ToBoolean(Read(view, "IsVisible")), Is.EqualTo(visible));
            if (visible) { Assert.That(label.isActiveAndEnabled, Is.True); Assert.That(label.text, Is.Not.Empty); }
            else Assert.That(!label.isActiveAndEnabled || string.IsNullOrEmpty(label.text), Is.True, "Expired instructions must actually leave the UI.");
        }
        private static object Read(object target, string name)
        {
            var property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property != null) return property.GetValue(target);
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(field, Is.Not.Null, "T034–T036 missing projection: " + target.GetType().Name + "." + name);
            return field.GetValue(target);
        }

        [UnityTest]
        public IEnumerator RejectionsPauseAndDeathReachVisibleStatus()
        {
            yield return LoadArena();
            var hud = Object.FindAnyObjectByType<GameplayHud>();
            Assert.That(hud, Is.Not.Null);
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(player.ActivateQuack(), Is.True);
            Assert.That(player.ActivateQuack(), Is.False);
            yield return null; yield return null;
            Assert.That(Field<Text>(hud, "statusLabel").text, Does.Contain("recarregando"));
            yield return new WaitForSeconds(0.4f);
            player.State.Breath.TryConsume(player.State.Breath.Capacity, out _);
            Assert.That(player.ActivateQuack(), Is.False);
            yield return null; yield return null;
            Assert.That(Field<Text>(hud, "statusLabel").text, Does.Contain("Sem Fôlego"));
            var run = GameBootstrap.Instance.Run;
            run.TryTransition(RunPhase.Paused);
            yield return null; yield return null;
            Assert.That(Field<Text>(hud, "statusLabel").text, Does.Contain("PAUSADO"));
            run.TryTransition(RunPhase.Playing);
            run.TryTransition(RunPhase.Lost);
            yield return null; yield return null;
            Assert.That(Field<Text>(hud, "statusLabel").text, Does.Contain("DERROTA"));
        }

        private IEnumerator LoadArena()
        {
            loadedArena = true;
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            for (int i = 0; i < 120 && SceneManager.GetActiveScene().name != "Lake"; i++) yield return null;
            yield return null; yield return null;
        }

        private static T Field<T>(GameplayHud hud, string name) =>
            (T)typeof(GameplayHud).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hud);

        private static string VisibleText(GameplayHud hud, string name)
        {
            var field = typeof(GameplayHud).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            var label = field?.GetValue(hud) as Text;
            return label != null && label.isActiveAndEnabled ? label.text : "";
        }

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            foreach (var item in owned) if (item != null) Object.Destroy(item);
            owned.Clear();
            if (loadedArena && GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
            yield return null;
            if (!loadedArena) yield break; // Standalone fixtures must not unload the Test Runner's own scene.
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("After HUD test"));
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
