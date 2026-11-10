using System.IO;
using QuackQuack.Systems.Diagnostics;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace QuackQuack.Editor
{
    public static class PolishPerformanceBuild
    {
        public const string AndroidPackage = "com.cemggames.quackquack.profile002";
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/T049Windows/QuackQuackProfile002.exe");
        public static void BuildAndroid() => Build(BuildTarget.Android, "Builds/T049Android/QuackQuackProfile002.apk");
        private static void Build(BuildTarget target, string path)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (Directory.Exists(Path.Combine(root, ".git")) || File.Exists(Path.Combine(root, ".git")))
                throw new BuildFailedException("Build profiling only in an isolated copy without .git.");
            if (EditorUserBuildSettings.activeBuildTarget != target) throw new BuildFailedException("Select profiling target first.");
            PrototypeBuildPipeline.ConfigureTarget(target);
            PlayerSettings.productName = "QuackQuack Profile 002 T049";
            PlayerSettings.enableFrameTimingStats = true;
            if (target == BuildTarget.Android)
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidPackage);
                EditorUserBuildSettings.buildAppBundle = false;
            }
            // Diagnostic capacity only: existing benchmark uses 80 of each common.
            // Production wave quantities, cadence, rewards and probabilities stay authored.
            var catalog = AssetDatabase.LoadAssetAtPath<QuackQuack.Content.Catalogs.LakeEncounterCatalog>("Assets/QuackQuack/Content/Catalogs/LakeEncounterCatalog.asset");
            var data = new SerializedObject(catalog); data.FindProperty("activeLimit").intValue = 400; data.ApplyModifiedPropertiesWithoutUndo();
            foreach (var enemy in catalog.CommonEnemies)
            {
                data = new SerializedObject(enemy); var size = data.FindProperty("poolMaxSize"); size.intValue = Mathf.Max(80, size.intValue); data.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssets();
            var benchmark = EditorSceneManager.OpenScene(PerformanceBenchmarkSetup.ScenePath);
            Object.FindAnyObjectByType<PerformanceBenchmark>().gameObject.AddComponent<PerformanceProfileCapture>();
            const string benchmarkPath = "Assets/QuackQuack/Scenes/T049Benchmark.unity";
            EditorSceneManager.SaveScene(benchmark, benchmarkPath);
            var entry = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Opt-in T049 profile entry").AddComponent<PolishPerformanceCapture>();
            const string entryPath = "Assets/QuackQuack/Scenes/T049Profile.unity";
            EditorSceneManager.SaveScene(entry, entryPath);
            var options = PrototypeBuildPipeline.CreateOptions(target, path);
            options.scenes = new[] { entryPath, benchmarkPath, "Assets/QuackQuack/Scenes/RunBootstrap.unity", "Assets/QuackQuack/Scenes/LakeRun.unity" };
            options.options = BuildOptions.Development;
            Directory.CreateDirectory(Path.GetDirectoryName(options.locationPathName));
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("T049 profiling build failed: " + report.summary.result);
            Debug.Log("T049_PROFILE_BUILD_OK " + target);
        }
    }
}
