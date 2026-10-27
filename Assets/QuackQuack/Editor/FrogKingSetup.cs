using QuackQuack.Content.Enemies;
using QuackQuack.Content.Characters;
using QuackQuack.Gameplay.Player;
using UnityEditor.SceneManagement;
using QuackQuack.Gameplay.Bosses;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuackQuack.Editor
{
    public static class FrogKingSetup
    {
        public const string SettingsPath = "Assets/QuackQuack/Content/Enemies/Prototype/FrogKingSettings.asset";
        public const string PrefabPath = "Assets/QuackQuack/Prefabs/Prototype/Enemies/sapo-rei.prefab";
        [MenuItem("QuackQuack/Build T057 Frog King")]
        public static void Build()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/QuackQuack/Art/Prototype/EnemyLine.mat");
            var hazard = new GameObject("Frog King hazard", typeof(FrogKingHazard));
            hazard.GetComponent<LineRenderer>().sharedMaterial = material;
            string hazardPath = "Assets/QuackQuack/Prefabs/Prototype/Enemies/FrogKingHazard.prefab";
            PrefabUtility.SaveAsPrefabAsset(hazard, hazardPath); Object.DestroyImmediate(hazard);
            var settings = AssetDatabase.LoadAssetAtPath<FrogKingSettings>(SettingsPath);
            if (settings == null) { settings = ScriptableObject.CreateInstance<FrogKingSettings>(); AssetDatabase.CreateAsset(settings, SettingsPath); }
            settings.HazardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(hazardPath);
            settings.MarkerMaterial = material;
            settings.WarningAudio = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/QuackQuack/Art/Prototype/CururuArea.wav");
            EditorUtility.SetDirty(settings);
            if (!settings.IsValid(out string reason)) throw new System.InvalidOperationException(reason);
            var root = new GameObject("Sapo-Rei", typeof(FrogKingController), typeof(AudioSource));
            root.GetComponent<AudioSource>().playOnAwake = false;
            var body = new GameObject("Body", typeof(SpriteRenderer)); body.transform.SetParent(root.transform, false);
            body.transform.localScale = Vector3.one * 2;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/QuackQuack/Art/Prototype/Circle.png");
            var renderer = body.GetComponent<SpriteRenderer>(); renderer.sprite = sprite;
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/QuackQuack/Art/Prototype/CircleUnlit.mat");
            renderer.color = new Color(0.3f, 0.7f, 0.15f); renderer.sortingOrder = 10;
            var warning = new GameObject("Danger marker", typeof(LineRenderer)); warning.transform.SetParent(root.transform, false);
            warning.GetComponent<LineRenderer>().sharedMaterial = material; warning.GetComponent<LineRenderer>().enabled = false;
            warning.GetComponent<LineRenderer>().sortingOrder = 20;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            BuildPractice(settings);
        }
        private static void BuildPractice(FrogKingSettings settings)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Camera", typeof(Camera), typeof(AudioListener));
            var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 9;
            camera.transform.position = new Vector3(0, 0, -10); camera.backgroundColor = new Color(0.08f, 0.14f, 0.2f);
            var root = new GameObject("Frog King practice", typeof(FrogKingPracticeArena));
            var practice = root.GetComponent<FrogKingPracticeArena>(); practice.Settings = settings;
            practice.Character = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/QuackQuack/Content/Characters/Pato.asset");
            practice.BossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var target = new GameObject("Pato", typeof(PlayerController)); target.transform.position = new Vector2(-4, -3);
            practice.Player = target.GetComponent<PlayerController>();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/QuackQuack/Art/Prototype/Circle.png");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/QuackQuack/Art/Prototype/CircleUnlit.mat");
            var visual = target.AddComponent<SpriteRenderer>(); visual.sprite = sprite; visual.sharedMaterial = material; visual.color = Color.yellow; visual.sortingOrder = 10;
            practice.Refuges = new FrogKingSafeArea[3];
            for (int i = 0; i < 3; i++)
            {
                var area = new GameObject("Refuge " + i, typeof(CircleCollider2D), typeof(FrogKingSafeArea), typeof(SpriteRenderer));
                area.transform.position = new Vector2(-5 + i * 5, -2); area.transform.localScale = Vector3.one * 2;
                area.GetComponent<CircleCollider2D>().isTrigger = true;
                var renderer = area.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sharedMaterial = material; renderer.color = new Color(0.2f, 0.45f, 0.3f);
                practice.Refuges[i] = area.GetComponent<FrogKingSafeArea>();
            }
            EditorSceneManager.SaveScene(scene, "Assets/QuackQuack/Scenes/FrogKingPractice.unity");
        }    }
}
