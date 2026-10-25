using System.IO;
using QuackQuack.Systems.Run;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace QuackQuack.Editor
{
    public static class PrototypeSceneSetup
    {
        public static void CreateFoundationScenes()
        {
            const string folder = "Assets/QuackQuack/Scenes/";
            Directory.CreateDirectory(folder);
            if (!File.Exists(folder + "Lake.unity"))
            {
                var lake = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var camera = new GameObject("Lake Camera").AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.orthographic = true;
                camera.orthographicSize = 9;
                camera.transform.position = new Vector3(0, 0, -10);
                camera.backgroundColor = new Color(0.07f, 0.17f, 0.22f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                EditorSceneManager.SaveScene(lake, folder + "Lake.unity");
            }
            if (!File.Exists(folder + "Bootstrap.unity"))
            {
                var bootstrap = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
                EditorSceneManager.SaveScene(bootstrap, folder + "Bootstrap.unity");
            }
            EditorBuildSettings.scenes = new[] {
                new EditorBuildSettingsScene(folder + "Bootstrap.unity", true),
                new EditorBuildSettingsScene(folder + "Lake.unity", true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("QUACK_FOUNDATION_SCENES_OK");
        }
    }
}
