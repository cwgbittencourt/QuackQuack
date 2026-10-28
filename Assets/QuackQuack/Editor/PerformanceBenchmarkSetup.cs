using QuackQuack.Systems.Diagnostics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace QuackQuack.Editor
{
    public static class PerformanceBenchmarkSetup
    {
        public const string ScenePath = "Assets/QuackQuack/Scenes/PerformanceBenchmark.unity";
        [MenuItem("QuackQuack/Build T071 performance benchmark scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraRoot = new GameObject("Benchmark camera", typeof(Camera), typeof(AudioListener));
            var camera = cameraRoot.GetComponent<Camera>(); cameraRoot.tag = "MainCamera";
            camera.orthographic = true; camera.orthographicSize = 8; camera.transform.position = new Vector3(0, 0, -10);
            camera.backgroundColor = new Color(0.04f, 0.1f, 0.14f); camera.clearFlags = CameraClearFlags.SolidColor;
            var root = new GameObject("Deterministic benchmark T071");
            var benchmark = root.AddComponent<PerformanceBenchmark>();
            root.AddComponent<PerformanceTelemetry>();
            var canvasRoot = new GameObject("Benchmark status", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasRoot.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvasRoot.GetComponent<Canvas>().sortingOrder = 1000;
            var scaler = canvasRoot.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 1;
            var background = new GameObject("Status backdrop", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(canvasRoot.transform, false);
            var backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = backgroundRect.anchorMax = backgroundRect.pivot = new Vector2(0, 1);
            backgroundRect.anchoredPosition = new Vector2(24, -24); backgroundRect.sizeDelta = new Vector2(1100, 160);
            background.GetComponent<Image>().color = new Color(0, 0, 0, 0.9f);
            background.GetComponent<Image>().raycastTarget = false;
            var textRoot = new GameObject("Scenario description", typeof(RectTransform), typeof(Text));
            textRoot.transform.SetParent(canvasRoot.transform, false);
            var rect = textRoot.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(40, -32); rect.sizeDelta = new Vector2(1068, 144);
            var text = textRoot.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24; text.color = Color.white; text.raycastTarget = false;
            text.text = "BENCHMARK T071 / seed 71\n200 / 400 enemies\n512 projectiles / 512 pickups / 128 VFX\nWorkload only: no FPS acceptance.";
            var data = new SerializedObject(benchmark);
            Bind(data, "character", "Assets/QuackQuack/Content/Characters/Pato.asset");
            Bind(data, "enemies", "Assets/QuackQuack/Content/Catalogs/LakeEncounterCatalog.asset");
            var catalog = AssetDatabase.LoadAssetAtPath<QuackQuack.Content.Catalogs.PrototypeBuildCatalog>("Assets/QuackQuack/Content/Catalogs/PrototypeBuildCatalog.asset");
            data.FindProperty("projectile").objectReferenceValue = catalog.Weapons[0];
            Bind(data, "pickupPrefab", "Assets/QuackQuack/Prefabs/Prototype/ExperiencePickup.prefab", typeof(QuackQuack.Gameplay.Pickups.ExperiencePickup));
            Bind(data, "feedbackPrefab", "Assets/QuackQuack/Prefabs/Prototype/QuackFeedback.prefab", typeof(QuackQuack.Gameplay.Player.PooledQuackFeedback));
            data.FindProperty("status").objectReferenceValue = text;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("T071_BENCHMARK_SCENE_SAVED");
        }

        private static void Bind(SerializedObject data, string field, string path, System.Type component = null)
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (component != null) asset = ((GameObject)asset).GetComponent(component);
            if (asset == null) throw new System.InvalidOperationException("Missing benchmark reference: " + path);
            data.FindProperty(field).objectReferenceValue = asset;
        }
    }
}
