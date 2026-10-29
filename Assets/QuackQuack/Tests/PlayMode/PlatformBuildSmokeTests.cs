using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Player;
using QuackQuack.Presentation.UI;
using QuackQuack.Systems.Run;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
#endif

namespace QuackQuack.Tests.PlayMode
{
    // This fixture is compiled into test players too. Editor success is not player evidence.
    [Category("PlatformRuntimeSmoke")]
    public class PlatformBuildSmokeTests
    {
        private string directory, temporaryRoot;
        private Func<string> previousPath;
        private bool loadedProductionScene;

        [SetUp]
        public void SetUp()
        {
            loadedProductionScene = false;
            previousPath = RunDirector.SavePathProvider;
            temporaryRoot = Path.GetFullPath(Application.temporaryCachePath);
            directory = Path.Combine(temporaryRoot, "QuackQuack-T064-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            RunDirector.SavePathProvider = () => Path.Combine(directory, "save.json");
        }

        [Test]
        public void ProductionBootstrapAndLakeRunAreAvailableInTheTestPlayer()
        {
            Assert.That(Application.unityVersion, Is.EqualTo("6000.4.3f1"));
            Assert.That(Application.CanStreamedLevelBeLoaded("RunBootstrap"), Is.True, "Build must package the production entry scene.");
            Assert.That(Application.CanStreamedLevelBeLoaded("LakeRun"), Is.True, "Build must package the production lake scene.");
#if !UNITY_EDITOR
            Assert.That(Application.platform, Is.EqualTo(RuntimePlatform.WindowsPlayer).Or.EqualTo(RuntimePlatform.Android));
            Assert.That(IntPtr.Size, Is.EqualTo(8), "Players must be 64-bit.");
#if !ENABLE_IL2CPP
            Assert.Fail("T070: Windows/Android smoke players must use IL2CPP.");
#endif
#endif
        }

        [UnityTest]
        public IEnumerator ProductionBootstrapInitializesLakeRunWithLiveServicesAndClock()
        {
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
            yield return null;
            Assert.That(Application.CanStreamedLevelBeLoaded("RunBootstrap"), Is.True);
            loadedProductionScene = true;
            yield return SceneManager.LoadSceneAsync("RunBootstrap");
            double deadline = Time.realtimeSinceStartupAsDouble + 30;
            RunDirector director = null;
            while (Time.realtimeSinceStartupAsDouble < deadline)
            {
                director = Object.FindAnyObjectByType<RunDirector>();
                if (director != null && director.Ready) break;
                yield return null;
            }
            Assert.That(director, Is.Not.Null, "Production director must be included and instantiated.");
            Assert.That(director.Ready, Is.True, "LakeRun must initialize within 30 seconds.");
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("LakeRun"));
            var bootstrap = GameBootstrap.Instance;
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(Application.targetFrameRate, Is.EqualTo(60), "Production bootstrap must request the nominal frame rate.");
            Assert.That(Object.FindObjectsByType<GameBootstrap>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(bootstrap.Events, Is.Not.Null);
            Assert.That(bootstrap.Pools, Is.Not.Null);
            Assert.That(bootstrap.Pools.IsDisposed, Is.False);
            Assert.That(bootstrap.ExternalClock, Is.True);
            Assert.That(bootstrap.Run.Phase, Is.EqualTo(RunPhase.Playing));
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(player, Is.Not.Null);
            Assert.That(player.State, Is.Not.Null);
            Assert.That(player.State.Health, Is.GreaterThan(0));
            Assert.That(Object.FindAnyObjectByType<GameplayHud>(), Is.Not.Null);
            var telemetry = director.GetComponent<QuackQuack.Systems.Diagnostics.PerformanceTelemetry>();
            Assert.That(telemetry, Is.Not.Null);
            Assert.That(telemetry.Capture(0), Is.True, "Production telemetry must bind in the player too.");
            Assert.That(telemetry.Latest.Enemies.Capacity, Is.GreaterThan(0));
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(director.BuildCatalog, Is.Not.Null);
            Assert.That(director.RunSettings, Is.Not.Null);
            Assert.That(director.Spawns, Is.Not.Null);
            Assert.That(director.Scheduler, Is.Not.Null);
            double before = bootstrap.Run.ElapsedSeconds;
            deadline = Time.realtimeSinceStartupAsDouble + 2;
            while (bootstrap.Run.ElapsedSeconds <= before && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.That(bootstrap.Run.ElapsedSeconds, Is.GreaterThan(before), "Initialization must leave a running simulation.");
            director.AutomaticTick = false;
            Debug.Log("T064_RUNTIME_SMOKE_OK platform=" + Application.platform + " unity=" + Application.unityVersion);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            try
            {
                if (!loadedProductionScene) yield break;
                // Keep the injected save path until the production scene has finished unloading.
                var director = Object.FindAnyObjectByType<RunDirector>();
                if (director != null && director.Ready) director.TryAbandon();
                if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
                yield return null;
                var scene = SceneManager.GetActiveScene();
                SceneManager.SetActiveScene(SceneManager.CreateScene("After platform smoke"));
                if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
                yield return null;
            }
            finally
            {
                RunDirector.SavePathProvider = previousPath;
                if (directory != null)
                {
                    string resolved = Path.GetFullPath(directory);
                    string prefix = temporaryRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                    Assert.That(resolved.StartsWith(prefix, StringComparison.OrdinalIgnoreCase), Is.True);
                    Assert.That(Path.GetFileName(resolved), Does.StartWith("QuackQuack-T064-"));
                    if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
                }
            }
        }
    }

#if UNITY_EDITOR
    // Calls the production pipeline without compiling players or switching targets.
    public class PlatformBuildConfigurationSmokeTests
    {
        [TestCase(BuildTarget.StandaloneWindows64)]
        [TestCase(BuildTarget.Android)]
        public void SharedBuildOptionsUseProductionScenesAndRequestedTarget(BuildTarget target)
        {
            var activeTarget = EditorUserBuildSettings.activeBuildTarget;
            var editorScenes = EditorBuildSettings.scenes.Select(s => s.path).ToArray();
            var create = RequireMethod("CreateOptions", typeof(BuildTarget), typeof(string));
            string output = Path.GetFullPath(Path.Combine(Application.temporaryCachePath,
                "QuackQuack-T064-Build-" + Guid.NewGuid().ToString("N"), target == BuildTarget.Android ? "Smoke.apk" : "Smoke.exe"));
            var options = (BuildPlayerOptions)create.Invoke(null, new object[] { target, output });
            Assert.That(options.target, Is.EqualTo(target));
            Assert.That(options.options, Is.EqualTo(BuildOptions.None), "Product builds must not launch a player or enable development/test options.");
            Assert.That(Path.GetFullPath(options.locationPathName), Is.EqualTo(output));
            Assert.That(options.scenes, Is.Not.Null.And.Not.Empty);
            Assert.That(options.scenes[0], Is.EqualTo("Assets/QuackQuack/Scenes/RunBootstrap.unity"));
            Assert.That(options.scenes, Does.Contain("Assets/QuackQuack/Scenes/LakeRun.unity"));
            Assert.That(options.scenes.Distinct().Count(), Is.EqualTo(options.scenes.Length));
            foreach (var scene in options.scenes)
            {
                Assert.That(File.Exists(scene), Is.True, "Missing build scene: " + scene);
                Assert.That(scene, Does.Not.Contain("Practice").And.Not.Contain("Test"));
            }
            var otherTarget = target == BuildTarget.Android ? BuildTarget.StandaloneWindows64 : BuildTarget.Android;
            string otherOutput = Path.ChangeExtension(output, otherTarget == BuildTarget.Android ? ".apk" : ".exe");
            var other = (BuildPlayerOptions)create.Invoke(null, new object[] { otherTarget, otherOutput });
            CollectionAssert.AreEqual(options.scenes, other.scenes, "Both platforms must package the same gameplay scenes.");
            Assert.That(options.scenes, Does.Not.Contain("Assets/QuackQuack/Scenes/Bootstrap.unity").And.Not.Contain("Assets/QuackQuack/Scenes/Lake.unity"));
            options.scenes[0] = "Mutated caller copy";
            Assert.That(other.scenes[0], Is.EqualTo("Assets/QuackQuack/Scenes/RunBootstrap.unity"));
            Assert.That(EditorUserBuildSettings.activeBuildTarget, Is.EqualTo(activeTarget));
            CollectionAssert.AreEqual(editorScenes, EditorBuildSettings.scenes.Select(s => s.path).ToArray());
        }

        [TestCase(null)]
        [TestCase(" ")]
        [TestCase("Invalid.zip")]
        public void InvalidOutputPathsAreRejectedBeforeBuilding(string path)
        {
            var create = RequireMethod("CreateOptions", typeof(BuildTarget), typeof(string));
            var exception = Assert.Throws<TargetInvocationException>(() => create.Invoke(null, new object[] { BuildTarget.StandaloneWindows64, path }));
            Assert.That(exception.InnerException, Is.InstanceOf<ArgumentException>());
        }

        [TestCase(BuildTarget.WebGL)]
        [TestCase(BuildTarget.StandaloneOSX)]
        public void UnsupportedTargetsAreRejectedWithoutSwitchingPlatform(BuildTarget target)
        {
            var activeTarget = EditorUserBuildSettings.activeBuildTarget;
            var create = RequireMethod("CreateOptions", typeof(BuildTarget), typeof(string));
            var configure = RequireMethod("ConfigureTarget", typeof(BuildTarget));
            var optionsError = Assert.Throws<TargetInvocationException>(() => create.Invoke(null, new object[] { target, "Invalid.exe" }));
            var configureError = Assert.Throws<TargetInvocationException>(() => configure.Invoke(null, new object[] { target }));
            Assert.That(optionsError.InnerException, Is.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(configureError.InnerException, Is.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(EditorUserBuildSettings.activeBuildTarget, Is.EqualTo(activeTarget));
        }

        [TestCase(BuildTarget.StandaloneWindows64)]
        [TestCase(BuildTarget.Android)]
        public void SharedTargetConfigurationUsesIl2CppAndAndroidArm64(BuildTarget target)
        {
            var configure = RequireMethod("ConfigureTarget", typeof(BuildTarget));
            var named = target == BuildTarget.Android ? NamedBuildTarget.Android : NamedBuildTarget.Standalone;
            var previousBackend = PlayerSettings.GetScriptingBackend(named);
            var previousArchitecture = PlayerSettings.Android.targetArchitectures;
            try
            {
                // An empty implementation must not pass just because this clone is already configured.
                PlayerSettings.SetScriptingBackend(named, ScriptingImplementation.Mono2x);
                if (target == BuildTarget.Android) PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
                configure.Invoke(null, new object[] { target });
                Assert.That(PlayerSettings.GetScriptingBackend(named), Is.EqualTo(ScriptingImplementation.IL2CPP));
                if (target == BuildTarget.Android)
                    Assert.That(PlayerSettings.Android.targetArchitectures, Is.EqualTo(AndroidArchitecture.ARM64));
            }
            finally
            {
                PlayerSettings.SetScriptingBackend(named, previousBackend);
                PlayerSettings.Android.targetArchitectures = previousArchitecture;
            }
        }

        private static MethodInfo RequireMethod(string name, params Type[] arguments)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("QuackQuack.Editor.PrototypeBuildPipeline"))
                .FirstOrDefault(t => t != null);
            Assert.That(type, Is.Not.Null, "T070 pending: production PrototypeBuildPipeline is missing (T064 red phase).");
            var method = type.GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, arguments, null);
            Assert.That(method, Is.Not.Null, "T070 contract: public static " + name + " is required.");
            return method;
        }
    }
#endif
}
