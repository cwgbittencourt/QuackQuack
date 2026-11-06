using System;
using QuackQuack.Presentation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace QuackQuack.Editor
{
    public static class IntegratedUiSetup
    {
        [MenuItem("QuackQuack/Repair integrated gameplay UI")]
        public static void Build()
        {
            foreach (string path in new[] { MobileControlsSetup.PrefabPath, RelicSetup.PrefabPath, UpgradeSelectionSetup.PrefabPath, ResultsSetup.PrefabPath, PolishUiSetup.ProfilePath })
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try { Configure(root); PrefabUtility.SaveAsPrefabAsset(root,path); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            foreach (string name in new[] { "Lake", "LakeRun" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/QuackQuack/Scenes/" + name + ".unity");
                var hud = Object.FindAnyObjectByType<GameplayHud>();
                if (hud == null) throw new InvalidOperationException(name + " is missing its authored HUD.");
                GameplayHudSetup.Configure(hud);
                var controls = GameObject.Find("Run controls");
                if (controls != null) ConfigureRunControls(controls);
                else if (name == "LakeRun") throw new InvalidOperationException("LakeRun is missing its authored run controls.");
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("T038_INTEGRATED_UI_SAVED");
        }

        public static void Configure(GameObject root)
        {
            var safe = SafeCanvas(root);
            if (root.GetComponent<QuackQuack.Platform.Input.MobileInputAdapter>() != null)
            {
                Place(safe.Find("Joystick") as RectTransform, Vector2.zero, Vector2.one*0.5f, new Vector2(180,250));
                Place(safe.Find("QUACK") as RectTransform, Vector2.right, Vector2.one*0.5f, new Vector2(-180,210));
                Place(safe.Find("PAUSAR") as RectTransform, Vector2.right, Vector2.one*0.5f, new Vector2(-180,395));
            }
            if (root.GetComponent<RelicLoadoutView>() != null)
            {
                Place(safe.Find("Open relics") as RectTransform, new Vector2(0.5f,1), new Vector2(0.5f,1), new Vector2(0,-16));
                FitModal(safe.Find("Panel") as RectTransform);
            }
            if (root.GetComponent<UpgradeSelectionView>() != null) FitModal(safe.Find("Selection modal") as RectTransform);
            if (root.GetComponent<QuackQuack.Gameplay.Upgrades.ControlSettingsMenu>() != null) FitModal(safe.Find("Profile panel") as RectTransform);
            if (root.GetComponent<ResultsView>() != null) FitModal(safe.Find("Results panel") as RectTransform);
        }

        public static void ConfigureRunControls(GameObject root)
        {
            var safe = SafeCanvas(root);
            Place(safe.Find("PAUSAR") as RectTransform, Vector2.one, Vector2.one, new Vector2(-16,-100));
            Place(safe.Find("ABANDONAR") as RectTransform, Vector2.one, Vector2.one, new Vector2(-16,-166));
        }
        private static RectTransform SafeCanvas(GameObject root)
        {
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = 1;
            var safe = root.transform.Find("UI Safe Area") as RectTransform;
            if (safe == null)
            {
                safe = new GameObject("UI Safe Area",typeof(RectTransform),typeof(SafeAreaController)).GetComponent<RectTransform>();
                safe.SetParent(root.transform,false);
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    if (child != safe && child.parent == root.transform) child.SetParent(safe,false);
            }
            safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one; safe.offsetMin = safe.offsetMax = Vector2.zero;
            return safe;
        }
        private static void FitModal(RectTransform panel)
        {
            if (panel == null) throw new InvalidOperationException("Missing authored modal panel.");
            var content = panel.Find("Reference content") as RectTransform;
            if (content == null)
            {
                content = new GameObject("Reference content",typeof(RectTransform),typeof(ReferencePanelFit)).GetComponent<RectTransform>();
                content.SetParent(panel,false);
                foreach (Transform child in panel.GetComponentsInChildren<Transform>(true))
                    if (child != content && child.parent == panel) child.SetParent(content,false);
            }
            Place(content,Vector2.one*0.5f,Vector2.one*0.5f,Vector2.zero);
            content.sizeDelta = new Vector2(1920,1080); content.localScale = Vector3.one;
            panel.anchorMin = Vector2.zero; panel.anchorMax = Vector2.one; panel.offsetMin = panel.offsetMax = Vector2.zero;
        }
        private static void Place(RectTransform rect,Vector2 anchor,Vector2 pivot,Vector2 position)
        {
            if (rect == null) throw new InvalidOperationException("Missing authored UI control.");
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot; rect.anchoredPosition = position;
        }
    }
}
