using System.Collections.Generic;
using QuackQuack.Content.Catalogs;
using QuackQuack.Content.Encounters;
using QuackQuack.Content.Enemies;
using QuackQuack.Content.Relics;
using QuackQuack.Gameplay.Bosses;
using QuackQuack.Gameplay.Enemies;
using QuackQuack.Gameplay.Pickups;
using QuackQuack.Gameplay.Player;
using QuackQuack.Gameplay.Upgrades;
using QuackQuack.Systems.Run;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace QuackQuack.Editor
{
    public static class LakeRunSetup
    {
        public const string SettingsPath = "Assets/QuackQuack/Content/Encounters/LakeRunSettings.asset";
        [MenuItem("QuackQuack/Build T060 complete lake run")]
        public static void Build()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LakeEncounterCatalog>(LakeEncounterCatalogSetup.CatalogPath);
            foreach (var d in catalog.GetAllEnemies()) BindPrefab(AssetDatabase.GetAssetPath(d.Prefab), false);
            BindPrefab(FrogKingSetup.PrefabPath, true);
            var settings = AssetDatabase.LoadAssetAtPath<LakeRunSettings>(SettingsPath);
            if (settings == null) { settings = ScriptableObject.CreateInstance<LakeRunSettings>(); AssetDatabase.CreateAsset(settings, SettingsPath); }
            settings.Catalog = catalog; settings.BossSettings = AssetDatabase.LoadAssetAtPath<FrogKingSettings>(FrogKingSetup.SettingsPath);
            settings.BossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FrogKingSetup.PrefabPath);
            settings.ExperiencePrefab = AssetDatabase.LoadAssetAtPath<ExperiencePickup>("Assets/QuackQuack/Prefabs/Prototype/ExperiencePickup.prefab");
            PickupPolicySetup.Configure(settings);
            EditorUtility.SetDirty(settings); if (!settings.IsValid) throw new System.InvalidOperationException("Invalid run settings.");
            var scene = EditorSceneManager.OpenScene("Assets/QuackQuack/Scenes/Lake.unity");
            LakeArenaSetup.ConfigurePlayerPhysics(Object.FindAnyObjectByType<PlayerController>());
            Object.DestroyImmediate(Object.FindAnyObjectByType<LakeArenaController>());
            foreach (var label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None)) Object.DestroyImmediate(label.gameObject);
            foreach (string name in new[] { "Risk corridor north", "Risk corridor south", "Emitter" })
            { var item = GameObject.Find(name); if (item != null) Object.DestroyImmediate(item); }
            var camera = Object.FindAnyObjectByType<Camera>(); camera.orthographicSize = 4.5f; camera.transform.position = new Vector3(-3, -1, -10);
            var progression = Object.FindAnyObjectByType<LakeBuildProgression>();
            Set(progression, "trainingPickups", p => p.boolValue = false);
            Set(progression, "experienceThresholds", p => { p.arraySize = 64; int total = 0; for (int i = 0; i < 64; i++) { total += 10 + i * 5; p.GetArrayElementAtIndex(i).intValue = total; } });
            var root = new GameObject("Lake run T060", typeof(RunDirector)); var director = root.GetComponent<RunDirector>();
            Set(director, "settings", p => p.objectReferenceValue = settings);
            Set(director, "relicCatalog", p => p.objectReferenceValue = AssetDatabase.LoadAssetAtPath<PrototypeRelicCatalog>("Assets/QuackQuack/Content/Relics/PrototypeRelicCatalog.asset"));
            var areas = new FrogKingSafeArea[3];
            for (int i = 0; i < 3; i++)
            {
                var area = new GameObject("Safe refuge " + (i + 1), typeof(CircleCollider2D), typeof(FrogKingSafeArea), typeof(SpriteRenderer));
                area.transform.SetParent(root.transform); area.transform.position = new Vector2(-5 + 5 * i, 1); area.transform.localScale = Vector3.one * 2;
                area.GetComponent<CircleCollider2D>().isTrigger = true;
                var renderer = area.GetComponent<SpriteRenderer>(); renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/QuackQuack/Art/Prototype/Circle.png");
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/QuackQuack/Art/Prototype/CircleUnlit.mat");
                renderer.color = new Color(0.3f, 0.7f, 0.4f); renderer.sortingOrder = -5; areas[i] = area.GetComponent<FrogKingSafeArea>();
            }
            Set(director, "refuges", p => { p.arraySize = areas.Length; for (int i = 0; i < areas.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = areas[i]; });
            var controls = new GameObject("Run controls", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            controls.transform.SetParent(root.transform); controls.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; controls.GetComponent<Canvas>().sortingOrder = 110;
            var scaler = controls.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
            var pause = Button(controls.transform, "PAUSAR", 160); var abandon = Button(controls.transform, "ABANDONAR", 100);
            Set(director, "pauseButton", p => p.objectReferenceValue = pause); Set(director, "abandonButton", p => p.objectReferenceValue = abandon);
            IntegratedUiSetup.ConfigureRunControls(controls);
            if (AssetDatabase.LoadAssetAtPath<ResultsProgressionDefinition>(ResultsSetup.ProgressPath) != null) ResultsSetup.Attach();
            director.gameObject.AddComponent<QuackQuack.Systems.Diagnostics.PerformanceTelemetry>();
            EditorSceneManager.SaveScene(scene, "Assets/QuackQuack/Scenes/LakeRun.unity");
            var entry = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootstrap = new GameObject("RunBootstrap", typeof(GameBootstrap)).GetComponent<GameBootstrap>();
            Set(bootstrap, "lakeScene", p => p.stringValue = "LakeRun");
            EditorSceneManager.SaveScene(entry, "Assets/QuackQuack/Scenes/RunBootstrap.unity");
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene("Assets/QuackQuack/Scenes/RunBootstrap.unity", true) };
            foreach (var existing in EditorBuildSettings.scenes) if (!existing.path.EndsWith("RunBootstrap.unity") && !existing.path.EndsWith("LakeRun.unity")) scenes.Add(existing);
            scenes.Add(new EditorBuildSettingsScene("Assets/QuackQuack/Scenes/LakeRun.unity", true)); EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
        }
        [MenuItem("QuackQuack/Update arena physics (002 T006)")]
        public static void UpdateArenaPhysics()
        {
            // Update the existing run rather than rebuilding UI/prefabs from the training
            // scene. Preserve object IDs, terrain, refuges, camera and current input layout.
            var scene = EditorSceneManager.OpenScene("Assets/QuackQuack/Scenes/LakeRun.unity");
            LakeArenaSetup.ConfigurePlayerPhysics(Object.FindAnyObjectByType<PlayerController>());
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new System.InvalidOperationException("Could not save LakeRun.");
            Debug.Log("T006_ARENA_PHYSICS_SAVED");
        }
        private static void BindPrefab(string path, bool boss)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponent<CombatTargetBinding>() == null) root.AddComponent<CombatTargetBinding>();
                root.layer = 9; var body = root.GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0;
                if (boss) root.GetComponent<CircleCollider2D>().radius = 1;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static UnityEngine.UI.Button Button(Transform parent, string text, float y)
        {
            var root = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.Button)); root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.right;
            rect.anchoredPosition = new Vector2(-20, y); rect.sizeDelta = new Vector2(220, 50);
            root.GetComponent<Image>().color = new Color(0.06f, 0.14f, 0.18f, 0.95f);
            var label = new GameObject("Label", typeof(RectTransform), typeof(Text)); label.transform.SetParent(root.transform, false);
            var labelRect = label.GetComponent<RectTransform>(); labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var value = label.GetComponent<Text>(); value.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); value.fontSize = 21;
            value.text = text; value.color = Color.white; value.alignment = TextAnchor.MiddleCenter; value.raycastTarget = false;
            return root.GetComponent<UnityEngine.UI.Button>();
        }
        private static void Set(Object target, string property, System.Action<SerializedProperty> assign)
        { var data = new SerializedObject(target); assign(data.FindProperty(property)); data.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
