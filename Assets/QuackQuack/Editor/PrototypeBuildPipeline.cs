using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace QuackQuack.Editor
{
    public static class PrototypeBuildPipeline
    {
        public static BuildPlayerOptions CreateOptions(BuildTarget target, string outputPath)
        {
            var group = TargetGroup(target);
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("A build output path is required.", nameof(outputPath));
            string extension = target == BuildTarget.Android ? ".apk" : ".exe";
            if (!string.Equals(Path.GetExtension(outputPath), extension, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Expected a " + extension + " output path.", nameof(outputPath));
            return new BuildPlayerOptions
            {
                scenes = new[] { "Assets/QuackQuack/Scenes/RunBootstrap.unity", "Assets/QuackQuack/Scenes/LakeRun.unity" },
                target = target,
                targetGroup = group,
                locationPathName = Path.GetFullPath(outputPath),
                options = BuildOptions.None
            };
        }

        public static void ConfigureTarget(BuildTarget target)
        {
            var group = TargetGroup(target);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.FromBuildTargetGroup(group), ScriptingImplementation.IL2CPP);
            if (target == BuildTarget.Android)
            {
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                ProjectSetup.ConfigureLandscape();
            }
        }

        // Select the platform in the Editor first, or pass -buildTarget in batch mode.
        // These entry points deliberately do not switch targets across domain reloads.
        [MenuItem("QuackQuack/Build prototype/Windows x64")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/QuackQuack.exe");

        [MenuItem("QuackQuack/Build prototype/Android ARM64 APK")]
        public static void BuildAndroid() => Build(BuildTarget.Android, "Builds/Android/QuackQuack.apk");

        public static BuildReport Build(BuildTarget target, string outputPath)
        {
            var options = CreateOptions(target, outputPath);
            if (EditorUserBuildSettings.activeBuildTarget != target)
                throw new InvalidOperationException("Select " + target + " before building, or use -buildTarget on the Unity command line.");
            if (!BuildPipeline.IsBuildTargetSupported(options.targetGroup, target))
                throw new InvalidOperationException("The Unity build support module for " + target + " is not installed.");
            foreach (var scene in options.scenes)
                if (!File.Exists(scene)) throw new FileNotFoundException("Missing production scene.", scene);
            ConfigureTarget(target);
            if (target == BuildTarget.Android) EditorUserBuildSettings.buildAppBundle = false;
            Directory.CreateDirectory(Path.GetDirectoryName(options.locationPathName));
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Prototype " + target + " build: " + report.summary.result + " (" + report.summary.totalErrors + " errors).");
            Debug.Log("PROTOTYPE_BUILD_OK target=" + target + " path=" + options.locationPathName);
            return report;
        }

        private static BuildTargetGroup TargetGroup(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows64: return BuildTargetGroup.Standalone;
                case BuildTarget.Android: return BuildTargetGroup.Android;
                default: throw new ArgumentOutOfRangeException(nameof(target), target, "Only Windows x64 and Android ARM64 are supported.");
            }
        }
    }
}
