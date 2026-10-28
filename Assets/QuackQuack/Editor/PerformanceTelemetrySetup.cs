using QuackQuack.Systems.Diagnostics;
using QuackQuack.Systems.Run;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace QuackQuack.Editor
{
    public static class PerformanceTelemetrySetup
    {
        [MenuItem("QuackQuack/Build T072 performance telemetry")]
        public static void Build()
        {
            foreach (var path in new[] { PerformanceBenchmarkSetup.ScenePath, "Assets/QuackQuack/Scenes/LakeRun.unity" })
            {
                var scene = EditorSceneManager.OpenScene(path);
                var benchmark = Object.FindAnyObjectByType<PerformanceBenchmark>();
                var director = Object.FindAnyObjectByType<RunDirector>();
                var root = benchmark != null ? benchmark.gameObject : director != null ? director.gameObject : null;
                if (root == null) throw new System.InvalidOperationException("No telemetry source in " + path);
                if (root.GetComponent<PerformanceTelemetry>() == null) root.AddComponent<PerformanceTelemetry>();
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("T072_TELEMETRY_SCENES_SAVED");
        }
    }
}
