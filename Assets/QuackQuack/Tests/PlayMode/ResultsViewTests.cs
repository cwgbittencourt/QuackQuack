#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using QuackQuack.Content.Catalogs;
using QuackQuack.Content.Encounters;
using QuackQuack.Content.Relics;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Presentation.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace QuackQuack.Tests.PlayMode
{
    public class ResultsViewTests
    {
        private GameObject root;
        private ResultsView view;
        private ResultsProgressionDefinition definition;
        private readonly Dictionary<string, string> names = new Dictionary<string, string>();
        [SetUp] public void SetUp()
        {
            names.Clear(); root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/QuackQuack/Prefabs/Prototype/ResultsScreen.prefab")); view = root.GetComponent<ResultsView>(); view.Bind(null, null);
            definition = AssetDatabase.LoadAssetAtPath<ResultsProgressionDefinition>("Assets/QuackQuack/Content/Encounters/ResultsProgression.asset");
            names["sapo-rei"] = "Sapo-Rei"; names["formiga"] = "Formiga"; names["quack"] = "Quack";
            names["survival-seconds"] = "Tempo sobrevivido"; names["enemies-defeated"] = "Inimigos derrotados"; names["longest-streak"] = "Maior sequência"; names["total-damage"] = "Dano total";
            foreach (var d in definition.Challenges) names[d.Id] = d.Title;
        }
        [TearDown] public void Cleanup() => Object.DestroyImmediate(root);
        private string Name(string id) => names.TryGetValue(id, out string value) ? value : id;
        private RunResult Sample(RunPhase outcome)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PrototypeBuildCatalog>("Assets/QuackQuack/Content/Catalogs/PrototypeBuildCatalog.asset");
            var relicCatalog = AssetDatabase.LoadAssetAtPath<PrototypeRelicCatalog>("Assets/QuackQuack/Content/Relics/PrototypeRelicCatalog.asset");
            var build = new BuildState();
            foreach (var d in catalog.GetAllWeapons()) names[d.Id] = d.DisplayName;
            foreach (var d in catalog.Weapons) { build.TryAddWeapon(d.Id); for (int i = 1; i < 5; i++) build.TryUpgradeWeapon(d.Id); }
            foreach (var d in catalog.Passives) { names[d.Id] = d.DisplayName; build.TryAddPassive(d.Id); }
            foreach (var d in catalog.Evolutions) { names[d.Id] = d.BaseWeapon.DisplayName + " + " + d.RequiredPassive.DisplayName; build.TryEvolveWeapon(d.BaseWeapon.Id, d.RequiredPassive.Id, d.ResultWeapon.Id); }
            var relics = relicCatalog.CreateLoadout(); foreach (var d in relicCatalog.Relics) { names[d.Id] = d.DisplayName; relics.Unlock(d.Id); }
            for (int i = 0; i < 3; i++) relics.Equip(relicCatalog.Relics[i].Id);
            var run = new RunState(61); using var session = new RunResultSession(run, 101, 50, 0.5, build, relics);
            run.TryTransition(RunPhase.Playing); run.Advance(920.25); session.RecordBossReached("sapo-rei");
            for (int i = 0; i < 100; i++) { if (i == 30) session.BreakStreak(); session.RecordDefeat("formiga"); }
            foreach (var d in catalog.Weapons) session.RecordWeaponDamage(d.Id, 125); session.RecordWeaponDamage("quack", 40);
            foreach (var d in catalog.Evolutions) session.RecordSynergy(d.Id);
            session.RecordItem(relicCatalog.Relics[7].Id, 1); session.RecordItem(relicCatalog.Relics[8].Id, 1);
            run.TryTransition(RunPhase.Boss); run.TryTransition(outcome); return session.FinalizeResult();
        }
        [TestCase(RunPhase.Won, "VITÓRIA")] [TestCase(RunPhase.Lost, "DERROTA")] [TestCase(RunPhase.Abandoned, "PARTIDA ABANDONADA")]
        public void CompleteScreenUsesAuthoredNamesAndAllRequiredFields(RunPhase outcome, string title)
        {
            var result = Sample(outcome); var receipt = RunProgression.Apply(new RunProgressProfile(), "sample", result, definition.CreateRules());
            view.Show(result, receipt, Name, true);
            Assert.That(view.IsVisible, Is.True); Assert.That(view.TitleText, Is.EqualTo(title)); Assert.That(view.SummaryText, Does.Contain("15:20").And.Contain("Sapo-Rei").And.Contain("61"));
            Assert.That(view.CombatText, Does.Contain("100").And.Contain("70").And.Contain("Quack").And.Contain("Bumerangue"));
            Assert.That(view.BuildText, Does.Contain("N5").And.Contain("Passivas").And.Contain("Evoluções").And.Contain("Sinergias").And.Contain("Relíquias equipadas").And.Contain("Histórico"));
            Assert.That(view.RewardsText, Does.Contain("Moedas ganhas  101").And.Contain("ITENS DESBLOQUEADOS").And.Contain("RECORDES").And.Contain("DESAFIOS").And.Contain("NOVO"));
            if (outcome == RunPhase.Abandoned) Assert.That(view.RewardsText, Does.Contain("Desconto por abandono").And.Contain("Bônus de vitória  0"));
            Assert.That(view.SaveText, Is.EqualTo("Progresso salvo")); Assert.That(view.CanRetry, Is.False); Assert.That(view.CanRestart, Is.True);
            Capture("t061-" + outcome.ToString().ToLowerInvariant() + ".png");
        }
        [Test] public void TerminalNotificationAfterPanelTeardownDoesNotReopenDestroyedUi()
        {
            var result=Sample(RunPhase.Abandoned);
            var receipt=RunProgression.Apply(new RunProgressProfile(),"teardown",result,definition.CreateRules());
            var panel=(GameObject)typeof(ResultsView).GetField("panel",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(view);
            Object.DestroyImmediate(panel);
            Assert.DoesNotThrow(()=>view.Show(result,receipt,Name,true));Assert.That(view.IsVisible,Is.False);
        }
        [Test] public void LongBuildAndProgressRemainScrollableInsteadOfTruncatingDetails()
        {
            var result = Sample(RunPhase.Won); view.Show(result, RunProgression.Apply(new RunProgressProfile(), "sample", result, definition.CreateRules()), Name, true);
            var scrolls = root.GetComponentsInChildren<ScrollRect>(); Assert.That(scrolls.Length, Is.EqualTo(3));
            Assert.That(scrolls[1].content.rect.height, Is.GreaterThan(scrolls[1].viewport.rect.height));
            scrolls[1].verticalNormalizedPosition = 0; scrolls[2].verticalNormalizedPosition = 0; Canvas.ForceUpdateCanvases();
            Assert.That(scrolls[1].content.anchoredPosition.y, Is.GreaterThan(0)); Capture("t061-details.png");
        }
        [Test] public void RebindAndRepeatedRenderingDoNotAccumulateCallbacksOrProgress()
        {
            int retries = 0; view.Bind(() => retries++, null); view.Bind(() => retries++, null);
            var result = Sample(RunPhase.Abandoned); var receipt = RunProgression.Apply(new RunProgressProfile(), "sample", result, definition.CreateRules());
            view.Show(result, receipt, Name, false); string first = view.RewardsText; view.Show(result, receipt, Name, false);
            Assert.That(view.RewardsText, Is.EqualTo(first)); Assert.That(view.SaveText, Does.Contain("não salvo")); Assert.That(view.CanRetry, Is.True);
            foreach (var button in root.GetComponentsInChildren<Button>()) if (button.name == "TENTAR SALVAR") button.onClick.Invoke();
            Assert.That(retries, Is.EqualTo(1)); view.Show(result, receipt, Name, true);
            foreach (var button in root.GetComponentsInChildren<Button>()) if (button.name == "TENTAR SALVAR") button.onClick.Invoke();
            Assert.That(retries, Is.EqualTo(1));
        }
        private void Capture(string filename)
        {
            var cameraRoot = new GameObject("Results capture camera", typeof(Camera)); var camera = cameraRoot.GetComponent<Camera>(); camera.orthographic = true; camera.transform.position = new Vector3(0, 0, -10); camera.clearFlags = CameraClearFlags.SolidColor;
            var canvas = root.GetComponent<Canvas>(); var target = new RenderTexture(1280, 720, 24); var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false); var previous = RenderTexture.active;
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; camera.targetTexture = target; camera.aspect = 1280f / 720;
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../Logs", filename)); Directory.CreateDirectory(Path.GetDirectoryName(output)); File.WriteAllBytes(output, pixels.EncodeToPNG());
            }
            finally { canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; camera.targetTexture = null; RenderTexture.active = previous; Object.DestroyImmediate(target); Object.DestroyImmediate(pixels); Object.DestroyImmediate(cameraRoot); }
        }
    }
}
#endif
