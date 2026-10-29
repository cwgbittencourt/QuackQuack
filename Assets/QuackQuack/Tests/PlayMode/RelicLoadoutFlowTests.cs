using System.Collections;
using NUnit.Framework;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Core.Shared;
using QuackQuack.Gameplay.Player;
using QuackQuack.Gameplay.Upgrades;
using QuackQuack.Presentation.UI;
using QuackQuack.Systems.Run;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace QuackQuack.Tests.PlayMode
{
    public class RelicLoadoutFlowTests
    {
        private LakeRelics relics;
        private RelicLoadoutView view;
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            for (int i=0;i<240;i++) { relics=Object.FindAnyObjectByType<LakeRelics>(); if(relics!=null && relics.Ready) break; yield return null; }
            Assert.That(relics, Is.Not.Null); Assert.That(relics.Ready, Is.True);
            view=Object.FindAnyObjectByType<RelicLoadoutView>();
        }
        [UnityTest]
        public IEnumerator RealButtonsPauseEquipRemoveAndResumeWithoutDuplicating()
        {
            var run=GameBootstrap.Instance.Run;
            view.transform.Find("UI Safe Area/Open relics").GetComponent<Button>().onClick.Invoke();
            Assert.That(run.Phase,Is.EqualTo(RunPhase.Paused)); Assert.That(view.IsVisible,Is.True);
            relics.Show(true); // Reopening an already open panel must retain pause ownership.
            var toggle=view.transform.Find("UI Safe Area/Panel/Reference content/Relic 2/Toggle").GetComponent<Button>();
            toggle.onClick.Invoke(); Assert.That(relics.Loadout.EquippedIds.Count,Is.EqualTo(1));
            Assert.That(relics.Loadout.ApplyEffects(100),Is.EqualTo(105).Within(0.001));
            toggle.onClick.Invoke(); Assert.That(relics.Loadout.EquippedIds,Is.Empty);
            Assert.That(relics.Loadout.ApplyEffects(100),Is.EqualTo(100));
            view.transform.Find("UI Safe Area/Panel/Reference content/Close").GetComponent<Button>().onClick.Invoke();
            Assert.That(run.Phase,Is.EqualTo(RunPhase.Playing)); Assert.That(view.IsVisible,Is.False);
            Assert.That(relics.Toggle("pena-de-combate"),Is.False);
            yield return null;
        }
        [UnityTest]
        public IEnumerator UnlockProgressAndThreeSlotLimitAreVisibleAndTerminalClosesPanel()
        {
            GameBootstrap.Instance.Run.Advance(60); yield return null;
            Assert.That(relics.Loadout.IsUnlocked("apito-veloz"),Is.True);
            relics.Show(true);
            Assert.That(relics.Toggle("botas-do-lago"),Is.True); Assert.That(relics.Toggle("garras-de-junco"),Is.True);
            Assert.That(relics.Toggle("pena-de-combate"),Is.True); Assert.That(relics.Toggle("apito-veloz"),Is.False);
            Assert.That(relics.Loadout.EquippedIds.Count,Is.EqualTo(3));
            GameBootstrap.Instance.Run.TryTransition(RunPhase.Abandoned);
            Assert.That(view.IsVisible,Is.False); Assert.That(relics.Toggle("pena-de-combate"),Is.False);
        }
        [UnityTest]
        public IEnumerator MovementEffectUsesRealPlayerAndReturnsToBaselineAfterRemoval()
        {
            var player=Object.FindAnyObjectByType<PlayerController>();
            var command=new PlayerCommand(Vector2.right,false,false,InputSource.Keyboard);
            player.ApplyCommand(command); player.SendMessage("FixedUpdate");
            float baseline=player.GetComponent<Rigidbody2D>().linearVelocity.magnitude;
            Assert.That(baseline,Is.GreaterThan(0));
            relics.Show(true); relics.Toggle("garras-de-junco"); relics.Show(false);
            player.ApplyCommand(command); player.SendMessage("FixedUpdate");
            Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.magnitude,Is.EqualTo(baseline*1.05f).Within(0.001));
            relics.Show(true); relics.Toggle("garras-de-junco"); relics.Show(false);
            player.ApplyCommand(command); player.SendMessage("FixedUpdate");
            Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.magnitude,Is.EqualTo(baseline).Within(0.001));
            yield return null;
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if(GameBootstrap.Instance!=null) Object.Destroy(GameBootstrap.Instance.gameObject);
            yield return null;
            var loaded=SceneManager.GetActiveScene();
            var empty=SceneManager.CreateScene("After relics "+System.Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            if(loaded.IsValid() && loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
        }
    }
}
