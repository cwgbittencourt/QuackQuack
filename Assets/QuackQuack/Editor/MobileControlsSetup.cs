using System.IO;
using QuackQuack.Platform.Input;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace QuackQuack.Editor
{
    public static class MobileControlsSetup
    {
        public const string PrefabPath = "Assets/QuackQuack/Prefabs/Prototype/MobileControls.prefab";
        [MenuItem("QuackQuack/Build T066 mobile controls")]
        public static void Build()
        {
            var root = new GameObject("Mobile controls T066", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MobileInputAdapter));
            try
            {
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                root.GetComponent<Canvas>().sortingOrder = 200;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
                var input = root.GetComponent<MobileInputAdapter>();
                var stick = Rect("Joystick", root.transform, Vector2.zero, new Vector2(180, 180), new Vector2(260, 260));
                var baseImage = stick.gameObject.AddComponent<Image>(); baseImage.color = new Color(0.06f, 0.14f, 0.18f, 0.8f);
                var handle = Rect("Handle", stick, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(88, 88));
                var handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = new Color(0.38f, 0.8f, 0.84f); handleImage.raycastTarget = false;
                stick.gameObject.AddComponent<MobileJoystick>().Configure(input, handle);
                Label("MOVER", stick, new Vector2(0, 170), new Vector2(260, 40));
                ActionButton("QUACK", root.transform, new Vector2(1, 0), new Vector2(-180, 150), new Vector2(240, 180), input, MobileAction.Quack);
                ActionButton("PAUSAR", root.transform, Vector2.one, new Vector2(-180, -80), new Vector2(240, 90), input, MobileAction.Pause);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
            }
            finally { Object.DestroyImmediate(root); }
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = anchor; rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        private static void ActionButton(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, MobileInputAdapter input, MobileAction action)
        {
            var rect = Rect(name, parent, anchor, position, size);
            rect.gameObject.AddComponent<Image>().color = new Color(0.96f, 0.79f, 0.3f, 0.95f);
            rect.gameObject.AddComponent<MobileActionButton>().Configure(input, action);
            Label(name, rect, Vector2.zero, size);
        }
        private static void Label(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = Rect(name + " label", parent, new Vector2(0.5f, 0.5f), position, size);
            var text = rect.gameObject.AddComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = name; text.fontSize = 30; text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.75f, 0.94f, 0.96f); text.raycastTarget = false;
            if (name != "MOVER") text.color = new Color(0.04f, 0.09f, 0.12f);
        }
    }
}
