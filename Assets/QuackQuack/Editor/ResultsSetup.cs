using QuackQuack.Content.Encounters;
using QuackQuack.Core.Run;
using QuackQuack.Presentation.UI;
using QuackQuack.Systems.Run;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace QuackQuack.Editor
{
    public static class ResultsSetup
    {
        private const string NavigationHint = "Esquerda/direita: colunas e ações  •  Cima/baixo: rolar coluna  •  Retornar: focar ações";
        public const string ProgressPath = "Assets/QuackQuack/Content/Encounters/ResultsProgression.asset";
        public const string PrefabPath = "Assets/QuackQuack/Prefabs/Prototype/ResultsScreen.prefab";
        [MenuItem("QuackQuack/Build T061 results screen")]
        public static void Build()
        {
            var definition = AssetDatabase.LoadAssetAtPath<ResultsProgressionDefinition>(ProgressPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<ResultsProgressionDefinition>();
                definition.Challenges = new[] {
                    new ResultsChallengeDefinition { Id = "lake-hunter", Title = "Derrotar 100 inimigos", Metric = ResultMetric.EnemiesDefeated, Target = 100, Cumulative = true },
                    new ResultsChallengeDefinition { Id = "lake-survivor", Title = "Sobreviver por 15 minutos", Metric = ResultMetric.SurvivalSeconds, Target = 900 },
                    new ResultsChallengeDefinition { Id = "frog-king-victory", Title = "Vencer o Sapo-Rei", Metric = ResultMetric.Victories, Target = 1, Cumulative = true }
                };
                AssetDatabase.CreateAsset(definition, ProgressPath);
            }
            definition.CreateRules();
            var root = new GameObject("Results screen T061", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(ResultsView));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; root.GetComponent<Canvas>().sortingOrder = 500;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            var panel = Rect("Results panel", root.transform, Vector2.zero, Vector2.one); panel.gameObject.AddComponent<Image>().color = new Color(0.025f, 0.07f, 0.09f, 0.995f);
            var title = Label("Outcome", panel, 72, -48, 1776, 70, 46, "RESULTADO");
            var summary = Label("Summary", panel, 72, -130, 1776, 54, 23, "TEMPO  00:00"); summary.color = new Color(0.67f, 0.78f, 0.8f);
            var divider = Box("Divider", panel, 72, -196, 1776, 2); divider.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.54f);
            var scrolls = new ScrollRect[3]; var texts = new Text[3];
            for (int i = 0; i < 3; i++) texts[i] = Column(panel, 72 + i * 600, out scrolls[i]);
            var status = Label("Save status", panel, 72, -934, 950, 54, 22, "Progresso salvo");
            var retry = Button(panel, "TENTAR SALVAR", 1080, -934, 320, new Color(0.12f, 0.28f, 0.32f), Color.white);
            var restart = Button(panel, "JOGAR NOVAMENTE", 1424, -934, 424, new Color(0.98f, 0.81f, 0.35f), new Color(0.06f, 0.1f, 0.12f));
            Label("Scroll hint", panel, 72, -1016, 1776, 30, 18, NavigationHint).color = new Color(0.56f, 0.67f, 0.7f);
            var view = root.GetComponent<ResultsView>();
            Set(view, "panel", p => p.objectReferenceValue = panel.gameObject); Set(view, "title", p => p.objectReferenceValue = title); Set(view, "summary", p => p.objectReferenceValue = summary);
            Set(view, "combat", p => p.objectReferenceValue = texts[0]); Set(view, "build", p => p.objectReferenceValue = texts[1]); Set(view, "rewards", p => p.objectReferenceValue = texts[2]);
            Set(view, "saveStatus", p => p.objectReferenceValue = status); Set(view, "retry", p => p.objectReferenceValue = retry); Set(view, "restart", p => p.objectReferenceValue = restart);
            Set(view, "scrolls", p => { p.arraySize = 3; for (int i = 0; i < 3; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = scrolls[i]; });
            panel.gameObject.SetActive(false); IntegratedUiSetup.Configure(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); Object.DestroyImmediate(root);
            var scene = EditorSceneManager.OpenScene("Assets/QuackQuack/Scenes/LakeRun.unity"); Attach(); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        public static void UpdateIllustratedLayout()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try {
                PolishUiSetup.RefreshExistingTheme(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            } finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets(); Debug.Log("T050_RESULTS_LAYOUT_SAVED");
        }
        public static void UpdateNavigation()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try {
                foreach (var button in root.GetComponentsInChildren<Button>(true)) { button.targetGraphic = button.GetComponent<Image>(); MenuNavigation.Prepare(button); }
                foreach (var bar in root.GetComponentsInChildren<Scrollbar>(true)) MenuNavigation.Prepare(bar);
                foreach (var text in root.GetComponentsInChildren<Text>(true)) if (text.name == "Scroll hint") text.text = NavigationHint;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            } finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets(); Debug.Log("T014_RESULTS_NAVIGATION_SAVED");
        }
        public static void Attach()
        {
            var existing = Object.FindAnyObjectByType<ResultsView>(); if (existing != null) Object.DestroyImmediate(existing.gameObject);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            var director = Object.FindAnyObjectByType<RunDirector>(); var bridge = director.GetComponent<ResultsBridge>(); if (bridge == null) bridge = director.gameObject.AddComponent<ResultsBridge>();
            Set(bridge, "view", p => p.objectReferenceValue = root.GetComponent<ResultsView>());
            Set(bridge, "progression", p => p.objectReferenceValue = AssetDatabase.LoadAssetAtPath<ResultsProgressionDefinition>(ProgressPath));
        }
        private static Text Column(Transform parent, float x, out ScrollRect scroll)
        {
            var rect = Box("Result column", parent, x, -224, 576, 680); rect.GetComponent<Image>().color = new Color(0.06f, 0.13f, 0.16f);
            scroll = rect.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 36;
            var viewport = Rect("Viewport", rect, Vector2.zero, Vector2.one); viewport.offsetMin = new Vector2(20, 20); viewport.offsetMax = new Vector2(-32, -20); viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", viewport, new Vector2(0, 1), Vector2.one); content.pivot = new Vector2(0.5f, 1); content.sizeDelta = Vector2.zero;
            var text = content.gameObject.AddComponent<Text>(); Style(text, 24); text.lineSpacing = 1.16f;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var bar = Rect("Scrollbar", rect, new Vector2(1, 0), Vector2.one); bar.pivot = new Vector2(1, 0.5f); bar.sizeDelta = new Vector2(10, -40); bar.anchoredPosition = new Vector2(-8, 0);
            bar.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.09f, 0.11f);
            var handle = Rect("Handle", bar, Vector2.zero, Vector2.one); var image = handle.gameObject.AddComponent<Image>(); image.color = new Color(0.25f, 0.48f, 0.52f);
            var scrollbar = bar.gameObject.AddComponent<Scrollbar>(); scrollbar.direction = Scrollbar.Direction.BottomToTop; scrollbar.handleRect = handle; scrollbar.targetGraphic = image; MenuNavigation.Prepare(scrollbar);
            scroll.viewport = viewport; scroll.content = content; scroll.verticalScrollbar = scrollbar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return text;
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 minimum, Vector2 maximum)
        {
            var root = new GameObject(name, typeof(RectTransform)); root.transform.SetParent(parent, false); var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = minimum; rect.anchorMax = maximum; rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
        }
        private static RectTransform Box(string name, Transform parent, float x, float y, float width, float height)
        { var rect = Rect(name, parent, Vector2.up, Vector2.up); rect.pivot = Vector2.up; rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height); rect.gameObject.AddComponent<Image>(); return rect; }
        private static Text Label(string name, Transform parent, float x, float y, float width, float height, int size, string value)
        { var rect = Rect(name, parent, Vector2.up, Vector2.up); rect.pivot = Vector2.up; rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height); var text = rect.gameObject.AddComponent<Text>(); Style(text, size); text.text = value; return text; }
        private static void Style(Text text, int size)
        { text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = size; text.color = new Color(0.93f, 0.95f, 0.9f); text.supportRichText = true; text.alignment = TextAnchor.UpperLeft; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; text.raycastTarget = false; }
        private static Button Button(Transform parent, string title, float x, float y, float width, Color background, Color foreground)
        {
            var rect = Box(title, parent, x, y, width, 64); rect.GetComponent<Image>().color = background; var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>(); MenuNavigation.Prepare(button);
            var text = Label("Label", rect, 8, -10, width - 16, 44, 24, title); text.color = foreground; text.alignment = TextAnchor.MiddleCenter; return button;
        }
        private static void Set(Object target, string field, System.Action<SerializedProperty> assign)
        { var data = new SerializedObject(target); assign(data.FindProperty(field)); data.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
