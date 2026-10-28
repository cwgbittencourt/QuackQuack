using System.IO;
using QuackQuack.Platform.Input;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QuackQuack.Editor
{
    public static class UnifiedInputSetup
    {
        private const string ActionsPath = "Assets/QuackQuack/Platform/Input/QuackQuackInput.inputactions";
        private const string ConfigurationPath = "Assets/QuackQuack/Resources/PlayerInput.asset";
        [InitializeOnLoadMethod]
        private static void RegisterMobileLayout() => MobileCommandDevice.RegisterLayout();
        [MenuItem("QuackQuack/Build T067 unified input")]
        public static void Build()
        {
            RegisterMobileLayout();
            if (!File.Exists(ActionsPath))
            {
                var asset = ScriptableObject.CreateInstance<InputActionAsset>();
                try
                {
                    asset.name = "QuackQuackInput";
                    var map = asset.AddActionMap("Gameplay");
                    var move = map.AddAction("Move", InputActionType.Value); move.expectedControlType = "Vector2";
                    move.AddCompositeBinding("2DVector")
                        .With("Up", "<Keyboard>/w", "Keyboard").With("Up", "<Keyboard>/upArrow", "Keyboard")
                        .With("Down", "<Keyboard>/s", "Keyboard").With("Down", "<Keyboard>/downArrow", "Keyboard")
                        .With("Left", "<Keyboard>/a", "Keyboard").With("Left", "<Keyboard>/leftArrow", "Keyboard")
                        .With("Right", "<Keyboard>/d", "Keyboard").With("Right", "<Keyboard>/rightArrow", "Keyboard");
                    move.ChangeBinding(0).WithGroup("Keyboard");
                    move.AddBinding("<Gamepad>/leftStick", groups: "Gamepad"); move.AddBinding("<MobileCommand>/move", groups: "Touch");
                    var quack = map.AddAction("Quack", InputActionType.Button); quack.expectedControlType = "Button";
                    quack.AddBinding("<Keyboard>/space", groups: "Keyboard"); quack.AddBinding("<Gamepad>/buttonSouth", groups: "Gamepad"); quack.AddBinding("<MobileCommand>/quack", groups: "Touch");
                    var pause = map.AddAction("Pause", InputActionType.Button); pause.expectedControlType = "Button";
                    pause.AddBinding("<Keyboard>/escape", groups: "Keyboard"); pause.AddBinding("<Gamepad>/start", groups: "Gamepad"); pause.AddBinding("<MobileCommand>/pause", groups: "Touch");
                    asset.AddControlScheme("Keyboard").WithRequiredDevice("<Keyboard>");
                    asset.AddControlScheme("Gamepad").WithRequiredDevice("<Gamepad>");
                    asset.AddControlScheme("Touch").WithRequiredDevice("<MobileCommand>");
                    File.WriteAllText(ActionsPath, asset.ToJson());
                }
                finally { Object.DestroyImmediate(asset); }
                AssetDatabase.ImportAsset(ActionsPath, ImportAssetOptions.ForceSynchronousImport);
            }
            if (!File.Exists(MobileControlsSetup.PrefabPath)) MobileControlsSetup.Build();
            if (!AssetDatabase.IsValidFolder("Assets/QuackQuack/Resources")) AssetDatabase.CreateFolder("Assets/QuackQuack", "Resources");
            var configuration = AssetDatabase.LoadAssetAtPath<PlayerInputConfiguration>(ConfigurationPath);
            if (configuration == null) { configuration = ScriptableObject.CreateInstance<PlayerInputConfiguration>(); AssetDatabase.CreateAsset(configuration, ConfigurationPath); }
            var data = new SerializedObject(configuration);
            data.FindProperty("actions").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            data.FindProperty("mobileControlsPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(MobileControlsSetup.PrefabPath);
            data.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets();
        }
    }
}
