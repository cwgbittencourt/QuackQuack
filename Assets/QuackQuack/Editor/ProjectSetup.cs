using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace QuackQuack.Editor
{
    public static class ProjectSetup
    {
        [MenuItem("QuackQuack/Configure prototype")]
        public static void Configure()
        {
            const string folder = "Assets/QuackQuack/Settings";
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>(folder + "/LakeRenderer2D.asset");
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(renderer, folder + "/LakeRenderer2D.asset");
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(folder + "/LakeURP.asset");
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, folder + "/LakeURP.asset");
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            var quality = QualitySettings.GetQualityLevel();
            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(quality, false);
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            EditorSettings.serializationMode = SerializationMode.ForceText;
            PlayerSettings.companyName = "CEMG Games";
            PlayerSettings.productName = "QuackQuack";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            settings.FindProperty("activeInputHandler").intValue = 1;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log("QUACK_SETUP_OK: URP 2D, Input System, landscape, 1920x1080, IL2CPP, Android ARM64; Windows target is StandaloneWindows64.");
        }
    }
}
