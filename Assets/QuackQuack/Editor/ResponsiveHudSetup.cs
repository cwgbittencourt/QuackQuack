using QuackQuack.Presentation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace QuackQuack.Editor
{
    public static class ResponsiveHudSetup
    {
        [MenuItem("QuackQuack/Build T069 responsive HUD")]
        public static void Build()
        {
            foreach (var name in new[] { "Lake", "LakeRun" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/QuackQuack/Scenes/" + name + ".unity");
                var hud = Object.FindAnyObjectByType<GameplayHud>();
                if (hud == null) throw new System.InvalidOperationException(name + " has no gameplay HUD.");
                Configure(hud);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("T069_RESPONSIVE_HUD_SAVED");
        }

        public static void Configure(GameplayHud hud)
        {
            var canvas = hud.GetComponent<Canvas>();
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1;

            var safe = canvas.transform.Find("HUD Safe Area") as RectTransform;
            if (safe == null)
            {
                safe = new GameObject("HUD Safe Area", typeof(RectTransform)).GetComponent<RectTransform>();
                safe.SetParent(canvas.transform, false);
            }
            safe.anchorMin = Vector2.zero;
            safe.anchorMax = Vector2.one;
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            foreach (var name in new[] { "Player vitals", "Clock", "Messages" })
            {
                var panel = canvas.transform.Find(name) ?? safe.Find(name);
                if (panel == null) throw new System.InvalidOperationException("Missing HUD panel: " + name);
                panel.SetParent(safe, false);
            }
            if (safe.GetComponent<SafeAreaController>() == null) safe.gameObject.AddComponent<SafeAreaController>();
        }
    }
}
