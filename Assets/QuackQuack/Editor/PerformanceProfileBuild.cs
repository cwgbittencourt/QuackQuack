using QuackQuack.Systems.Diagnostics;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace QuackQuack.Editor
{
    public static class PerformanceProfileBuild
    {
        public const string AndroidPackage = "com.cemggames.quackquack.profilet074";
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/T074Windows/QuackQuackProfile.exe");
        public static void BuildAndroid() => Build(BuildTarget.Android, "Builds/T074Android/QuackQuackProfile.apk");
        private static void Build(BuildTarget target, string path)
        {
            var git = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../.git"));
            if (System.IO.Directory.Exists(git) || System.IO.File.Exists(git))
                throw new BuildFailedException("Use an isolated Unity copy without .git; profiling changes build settings and package identity.");
            if (EditorUserBuildSettings.activeBuildTarget != target) throw new BuildFailedException("Select the profiling target first.");
            PrototypeBuildPipeline.ConfigureTarget(target);
            PlayerSettings.enableFrameTimingStats = true;
            if (target == BuildTarget.Android)
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidPackage);
                EditorUserBuildSettings.buildAppBundle = false;
            }
            var scene = EditorSceneManager.OpenScene(PerformanceBenchmarkSetup.ScenePath);
            var benchmark = Object.FindAnyObjectByType<PerformanceBenchmark>();
            benchmark.gameObject.AddComponent<PerformanceProfileCapture>();
            const string scenePath = "Assets/QuackQuack/Scenes/T074Profile.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            var options = PrototypeBuildPipeline.CreateOptions(target, path);
            options.scenes = new[] { scenePath }; options.options = BuildOptions.Development;
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(options.locationPathName));
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Profiling build failed: " + report.summary.result);
            Debug.Log("T074_PROFILE_BUILD_OK " + target);
        }
    }
}
