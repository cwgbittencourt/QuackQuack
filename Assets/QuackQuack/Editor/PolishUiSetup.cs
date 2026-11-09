using QuackQuack.Gameplay.Upgrades;
using QuackQuack.Presentation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace QuackQuack.Editor
{
    public static partial class PolishUiSetup
    {
        public const string ProfilePath = "Assets/QuackQuack/Prefabs/Prototype/ControlSettings.prefab";
        [MenuItem("QuackQuack/Build 002 gameplay UI composition")]
        public static void BuildGameplayComposition()
        {
            UpgradeSelectionSetup.UpdateAreaPreview();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ProfilePath) == null) BuildProfile();
            IntegratedUiSetup.Build();
            Debug.Log("T038_GAMEPLAY_COMPOSITION_SAVED");
        }
        [MenuItem("QuackQuack/Build 002 control menus")]
        public static void Build()
        {
            RelicSetup.BuildView(); UpgradeSelectionSetup.BuildView(); BuildProfile();
            foreach (string path in new[] { "Assets/QuackQuack/Scenes/Lake.unity", "Assets/QuackQuack/Scenes/LakeRun.unity" }) {
                var scene = EditorSceneManager.OpenScene(path);
                if (Object.FindAnyObjectByType<ControlSettingsMenu>() == null)
                    PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ProfilePath));
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            IntegratedUiSetup.Build();
            AssetDatabase.SaveAssets(); Debug.Log("T013_CONTROL_MENUS_SAVED");
        }
        private static void BuildProfile()
        {
            var root = new GameObject("Control settings", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; root.GetComponent<Canvas>().sortingOrder = 275;
            var host = root.AddComponent<ControlSettingsMenu>(); var h = new SerializedObject(host);
            var open = Button("Open settings", root.transform, new Vector2(0, 0), "CONTROLES");
            var resume = Button("Resume fallback", root.transform, new Vector2(0, 0), "PAUSAR / CONTINUAR");
            var panel = Rect("Profile panel", root.transform, Vector2.zero, new Vector2(1920,1080));
            panel.gameObject.AddComponent<Image>().color = new Color(.03f,.07f,.09f,.98f);
            var bindings = panel.gameObject.AddComponent<ControlBindingsView>(); var b = new SerializedObject(bindings);
            Label("Title", panel, new Vector2(0,440), new Vector2(1500,60), 36, "CONTROLES");
            Label("Instructions", panel, new Vector2(0,365), new Vector2(1600,65), 24, "Confirme um atalho para alternar entre botões. Atalhos já usados são ignorados.");
            var fields = new[] { "quack", "pause", "relics", "family", "reset" };
            for (int i = 0; i < fields.Length; i++) b.FindProperty(fields[i]).objectReferenceValue = Button(fields[i], panel, new Vector2(0,260-i*100), fields[i] == "reset" ? "RESTAURAR PADRÕES" : "");
            b.FindProperty("status").objectReferenceValue = Label("Status", panel, new Vector2(0,-230), new Vector2(1600,60), 24, "");
            b.FindProperty("navigationHints").objectReferenceValue = Label("Navigation hints", panel, new Vector2(0,-435), new Vector2(1720,75), 23, "");
            var close = Button("Close settings", panel, new Vector2(0,-330), "VOLTAR"); b.ApplyModifiedPropertiesWithoutUndo();
            h.FindProperty("panel").objectReferenceValue = panel.gameObject; h.FindProperty("bindings").objectReferenceValue = bindings;
            h.FindProperty("open").objectReferenceValue = open; h.FindProperty("close").objectReferenceValue = close; h.FindProperty("resume").objectReferenceValue = resume;
            h.ApplyModifiedPropertiesWithoutUndo(); panel.gameObject.SetActive(false);
            IntegratedUiSetup.Configure(root);
            Place(open.GetComponent<RectTransform>(), new Vector2(-16,-232));
            Place(resume.GetComponent<RectTransform>(), new Vector2(-16,-100));
            open.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, ProfilePath); Object.DestroyImmediate(root);
        }
        private static void Place(RectTransform rect, Vector2 position) { rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one; rect.anchoredPosition = position; }
        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent,false);
            rect.anchorMin = rect.anchorMax = Vector2.one*.5f; rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        private static Text Label(string name, Transform parent, Vector2 position, Vector2 size, int fontSize, string value)
        {
            var text = Rect(name,parent,position,size).gameObject.AddComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize; text.color = Color.white; text.text = value; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false; return text;
        }
        private static Button Button(string name, Transform parent, Vector2 position, string label)
        {
            var rect = Rect(name,parent,position,new Vector2(580,72)); var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.15f,.36f,.38f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; MenuNavigation.Prepare(button);
            Label("Label",rect,Vector2.zero,rect.sizeDelta,24,label); return button;
        }
    }
}
