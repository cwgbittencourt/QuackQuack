using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using QuackQuack.Editor;
using QuackQuack.Gameplay.Player;
using QuackQuack.Presentation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.EditMode
{
    public sealed class GameplayHudCompositionTests
    {
        private static readonly string[] Paths = {
            "Assets/QuackQuack/Scenes/Lake.unity", "Assets/QuackQuack/Scenes/LakeRun.unity",
            MobileControlsSetup.PrefabPath, RelicSetup.PrefabPath, UpgradeSelectionSetup.PrefabPath,
            ResultsSetup.PrefabPath, PolishUiSetup.ProfilePath
        };
        private static Object Reference(Object component, string name) => new SerializedObject(component).FindProperty(name).objectReferenceValue;
        [TearDown] public void CloseScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [TestCase("Lake")] [TestCase("LakeRun")]
        public void ScenesSerializeCompositionAndNoLegacyResourceOrStaticInstructionObjects(string name)
        {
            EditorSceneManager.OpenScene("Assets/QuackQuack/Scenes/" + name + ".unity");
            var hud = Object.FindAnyObjectByType<GameplayHud>(); Assert.That(hud, Is.Not.Null);
            var composition = hud.GetComponent<GameplayHudComposition>(); Assert.That(composition, Is.Not.Null);
            foreach (string field in new[] { "hud", "hints", "specials", "specialArea", "player", "worldCamera" })
                Assert.That(Reference(composition, field), Is.Not.Null, field);
            Assert.That(Reference(composition, "director") != null, Is.EqualTo(name == "LakeRun"));
            foreach (string field in new[] { "healthLabel", "breathLabel", "terrainLabel" }) Assert.That(Reference(hud,field), Is.Null);
            var safe = hud.GetComponentInChildren<SafeAreaController>().transform;
            foreach (string field in new[] { "healthIcon", "breathIcon", "healthFill", "breathFill", "experienceFill", "levelLabel", "timeLabel", "statusLabel", "quackLabel" })
            {
                var graphic = Reference(hud,field) as Graphic; Assert.That(graphic, Is.Not.Null,field);
                Assert.That(graphic.transform.IsChildOf(safe), Is.True,field); Assert.That(graphic.raycastTarget, Is.False,field);
            }
            var progress = (Image)Reference(hud,"experienceFill"); Assert.That(progress.type, Is.EqualTo(Image.Type.Filled)); Assert.That(progress.sprite, Is.Not.Null);
            Assert.That(progress.transform.parent.parent.GetComponent<RectTransform>().anchorMin, Is.EqualTo(new Vector2(.5f,0)));
            Assert.That(safe.Find("Player vitals/Health"), Is.Null); Assert.That(safe.Find("Player vitals/Breath"), Is.Null);
            Assert.That(safe.Find("Player vitals/Terrain"), Is.Null); Assert.That(safe.Find("Messages/Controls"), Is.Null);
            Assert.That(safe.GetComponentsInChildren<ContextualHintsView>(true).Length, Is.EqualTo(1));
            Assert.That(safe.GetComponentsInChildren<SpecialEnemyBarsView>(true).Length, Is.EqualTo(1));
            Assert.That(Reference((ContextualHintsView)Reference(composition,"hints"),"label"), Is.Not.Null);
        }
        [Test] public void PrefabsKeepPreviewBindingsProfileButtonsAndFullModalFitting()
        {
            var upgrades = AssetDatabase.LoadAssetAtPath<GameObject>(UpgradeSelectionSetup.PrefabPath).GetComponent<UpgradeSelectionView>();
            var cards = new SerializedObject(upgrades).FindProperty("cards"); Assert.That(cards.arraySize, Is.EqualTo(3));
            for (int i = 0; i < cards.arraySize; i++)
            {
                var card = cards.GetArrayElementAtIndex(i); var preview = card.FindPropertyRelative("areaPreview").objectReferenceValue;
                Assert.That(preview, Is.Not.Null); Assert.That(card.FindPropertyRelative("effects").objectReferenceValue, Is.Not.Null);
                foreach(string field in new[] {"current","next","caption"}) Assert.That(Reference(preview,field), Is.Not.Null);
            }
            var profile = AssetDatabase.LoadAssetAtPath<GameObject>(PolishUiSetup.ProfilePath);
            var bindings = profile.GetComponentInChildren<ControlBindingsView>(true);
            foreach(string field in new[] {"quack","pause","relics","family","reset","navigationHints"}) Assert.That(Reference(bindings,field), Is.Not.Null);
            foreach(string path in new[] {UpgradeSelectionSetup.PrefabPath, RelicSetup.PrefabPath, ResultsSetup.PrefabPath, PolishUiSetup.ProfilePath})
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentInChildren<ReferencePanelFit>(true), Is.Not.Null,path);
        }
        [Test] public void RepeatingCompositionGenerationPreservesSerializedBytesAndAssetGuids()
        {
            var before = Paths.ToDictionary(path => path, path => Convert.ToBase64String(File.ReadAllBytes(path)));
            var guids = Paths.ToDictionary(path => path, AssetDatabase.AssetPathToGUID);
            PolishUiSetup.BuildGameplayComposition();
            foreach(string path in Paths)
            {
                Assert.That(Convert.ToBase64String(File.ReadAllBytes(path)), Is.EqualTo(before[path]), "Generation must be idempotent: " + path);
                Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guids[path]),path);
            }
        }
    }
}
