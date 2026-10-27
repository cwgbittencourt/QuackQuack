using System;
using System.Collections;
using System.Collections.Generic;
using QuackQuack.Gameplay.Bosses;
using NUnit.Framework;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Run;
using QuackQuack.Systems.Combat;
using QuackQuack.Systems.Run;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.PlayMode
{
    public class FrogKingTelegraphTests
    {
        private const float LeadSeconds = 0.5f;
        private FrogKingController boss;
        private GameObject root;
        private LineRenderer marker;
        private AudioSource audio;
        private AudioClip clip;
        private Material material;
        private RunState run;
        private GameplayEventBus events;
        private readonly List<PresentationEvent> warnings = new List<PresentationEvent>();
        private readonly List<string> attacks = new List<string>();
        private readonly List<long> attackCorrelations = new List<long>();
        private readonly List<CombatEvent> combat = new List<CombatEvent>();

        [SetUp]
        public void CreateEncounter()
        {
            warnings.Clear(); attacks.Clear(); attackCorrelations.Clear(); combat.Clear();
            root = new GameObject("T049 isolated encounter");
            boss = root.AddComponent<FrogKingController>();
            boss.AutomaticTick = false;
            var visual = new GameObject("T049 danger marker");
            visual.transform.SetParent(root.transform);
            marker = visual.AddComponent<LineRenderer>();
            Shader shader = Shader.Find("Sprites/Default");
            Assert.That(shader, Is.Not.Null);
            material = new Material(shader);
            marker.sharedMaterial = material;
            marker.enabled = false;
            audio = root.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0;
            clip = AudioClip.Create("T049 warning fixture", 44100, 1, 44100, false);
            var samples = new float[44100];
            for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Sin(i * 2f * Mathf.PI * 440f / 44100f) * 0.1f;
            Assert.That(clip.SetData(samples, 0), Is.True);
            run = new RunState(49);
            Assert.That(run.TryTransition(RunPhase.Playing), Is.True);
            Assert.That(run.TryTransition(RunPhase.Boss), Is.True);
            events = new GameplayEventBus();
            events.Presentation += value => { if (value.Type == PresentationEventType.DangerTelegraphed) warnings.Add(value); };
            events.Combat += value => combat.Add(value);
            boss.AttackCommitted += (id, correlation) =>
            {
                // Observer sees a warning already published, never a same-frame unannounced attack.
                Assert.That(warnings.Count, Is.GreaterThan(0));
                attacks.Add(id); attackCorrelations.Add(correlation);
            };
            boss.Initialize(run, events, marker, audio, clip, LeadSeconds);
        }

        [TearDown]
        public void Cleanup()
        {
            events?.Clear();
            if (root != null) Object.DestroyImmediate(root);
            if (material != null) Object.DestroyImmediate(material);
            if (clip != null) Object.DestroyImmediate(clip);
        }

        [UnityTest] public IEnumerator JumpHasVisualAndAudioBeforeExecution() => CheckAttack("jump");
        [UnityTest] public IEnumerator WaveHasVisualAndAudioBeforeExecution() => CheckAttack("wave");
        [UnityTest] public IEnumerator TadpolesHaveVisualAndAudioBeforeExecution() => CheckAttack("tadpoles");
        [UnityTest] public IEnumerator SafeAreaDestructionHasVisualAndAudioBeforeExecution() => CheckAttack("destroy-safe-area");

        private IEnumerator CheckAttack(string id)
        {
            Vector2 target = new Vector2(3, -2);
            Assert.That(Begin(id, target), Is.True);
            Assert.That(warnings.Count, Is.EqualTo(1));
            PresentationEvent warning = warnings[0];
            Assert.That(warning.CorrelationId, Is.GreaterThan(0));
            Assert.That(warning.Priority, Is.EqualTo(EventPriority.Danger));
            Assert.That(warning.MaxDuration, Is.GreaterThanOrEqualTo(LeadSeconds));
            Assert.That(warning.Position, Is.EqualTo(target));
            AssertVisibleAndAudible();
            Assert.That(attacks, Is.Empty);
            Assert.That(combat, Is.Empty, "No attack effects during the warning window.");
            yield return null; // Allow real Unity presentation components one frame before committing.
            Assert.That(attacks, Is.Empty, "AutomaticTick=false must preserve manual control.");
            Advance(LeadSeconds - 0.01f);
            AssertVisibleAndAudible();
            Assert.That(attacks, Is.Empty);
            Assert.That(combat, Is.Empty);
            Advance(0.02f);
            CollectionAssert.AreEqual(new[] { id }, attacks);
            CollectionAssert.AreEqual(new[] { warning.CorrelationId }, attackCorrelations);
            Advance(0);
            Assert.That(attacks.Count, Is.EqualTo(1));
            Assert.That(warnings.Count, Is.EqualTo(1));
        }

        [Test]
        public void PendingAttackCannotBeReplacedOrDuplicateItsWarning()
        {
            Assert.That(Begin("jump", Vector2.zero), Is.True);
            Assert.That(Begin("wave", Vector2.one), Is.False);
            Assert.That(warnings.Count, Is.EqualTo(1));
            Advance(LeadSeconds + 0.01f);
            CollectionAssert.AreEqual(new[] { "jump" }, attacks);
        }

        [TestCase(RunPhase.Won)]
        [TestCase(RunPhase.Lost)]
        [TestCase(RunPhase.Abandoned)]
        public void TerminalEncounterCancelsWarningAndNeverCommitsPendingAttack(RunPhase outcome)
        {
            Assert.That(Begin("jump", Vector2.zero), Is.True);
            Assert.That(run.TryTransition(outcome), Is.True);
            Advance(LeadSeconds * 2);
            Assert.That(attacks, Is.Empty);
            Assert.That(combat, Is.Empty);
            Assert.That(marker.enabled, Is.False);
            Assert.That(audio.isPlaying, Is.False);
            Assert.That(Begin("wave", Vector2.zero), Is.False);
            Assert.That(warnings.Count, Is.EqualTo(1));
        }

        [Test]
        public void DisablingControllerCancelsPendingAttackWithoutStaleResume()
        {
            Assert.That(Begin("jump", Vector2.zero), Is.True);
            ((Behaviour)boss).enabled = false;
            Assert.That(marker.enabled, Is.False);
            Assert.That(audio.isPlaying, Is.False);
            ((Behaviour)boss).enabled = true;
            Advance(LeadSeconds * 2);
            Assert.That(attacks, Is.Empty);
            Assert.That(combat, Is.Empty);
        }

        private void AssertVisibleAndAudible()
        {
            Assert.That(marker.gameObject.activeInHierarchy && marker.enabled, Is.True);
            Assert.That(marker.positionCount, Is.GreaterThanOrEqualTo(3));
            Assert.That(marker.startWidth, Is.GreaterThan(0));
            Assert.That(marker.startColor.a, Is.GreaterThan(0));
            bool nonzeroExtent = false;
            for (int i = 1; i < marker.positionCount; i++)
                nonzeroExtent |= Vector3.Distance(marker.GetPosition(0), marker.GetPosition(i)) > 0.01f;
            Assert.That(nonzeroExtent, Is.True, "Marker must have nonzero geometry.");
            Assert.That(audio.clip, Is.SameAs(clip));
            Assert.That(audio.enabled && audio.gameObject.activeInHierarchy, Is.True);
            Assert.That(audio.mute, Is.False);
            Assert.That(audio.volume, Is.GreaterThan(0));
            Assert.That(audio.isPlaying, Is.True);
        }

        private bool Begin(string id, Vector2 target) => boss.TryBeginAttack(id, target);
        private void Advance(float seconds) => boss.Advance(seconds);
    }
}
