using System.IO;
using QuackQuack.Content.Catalogs;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Pickups;
using QuackQuack.Gameplay.Player;
using QuackQuack.Gameplay.Upgrades;
using QuackQuack.Presentation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace QuackQuack.Editor
{
    public static class UpgradeSelectionSetup
    {
        public const string PrefabPath = "Assets/QuackQuack/Prefabs/Prototype/UpgradeSelection.prefab";
        private const string XpPath = "Assets/QuackQuack/Prefabs/Prototype/ExperiencePickup.prefab";
        private static readonly Color Cream = new Color(0.94f, 0.94f, 0.87f);

        [MenuItem("QuackQuack/Build T044 upgrade selection")]
        public static void Build()
        {
            BuildView();
            var xpRoot = new GameObject("Experience pickup");
            var pickup = xpRoot.AddComponent<ExperiencePickup>();
            xpRoot.GetComponent<CircleCollider2D>().isTrigger = true;
            xpRoot.transform.localScale = Vector3.one * 0.4f;
            var sprite = xpRoot.AddComponent<SpriteRenderer>();
            sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/QuackQuack/Art/Prototype/Circle.png");
            sprite.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/QuackQuack/Art/Prototype/CircleUnlit.mat");
            sprite.color = new Color(0.3f, 0.95f, 0.95f); sprite.sortingOrder = 12;
            PrefabUtility.SaveAsPrefabAsset(xpRoot, XpPath); Object.DestroyImmediate(xpRoot);
            var scene = EditorSceneManager.OpenScene("Assets/QuackQuack/Scenes/Lake.unity");
            var old = GameObject.Find("Build progression T044"); if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject("Build progression T044");
            var viewRoot = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            viewRoot.transform.SetParent(root.transform, false);
            var progression = root.AddComponent<LakeBuildProgression>();
            var data = new SerializedObject(progression);
            data.FindProperty("catalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PrototypeBuildCatalog>(PrototypeBuildCatalogSetup.CatalogPath);
            data.FindProperty("view").objectReferenceValue = viewRoot.GetComponent<UpgradeSelectionView>();
            data.FindProperty("player").objectReferenceValue = Object.FindAnyObjectByType<PlayerController>();
            data.FindProperty("experiencePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(XpPath).GetComponent<ExperiencePickup>();
            data.FindProperty("experiencePerPickup").intValue = 10; data.FindProperty("projectileCapacity").intValue = 32;
            var thresholds = data.FindProperty("experienceThresholds"); thresholds.arraySize = 3;
            for (int i = 0; i < 3; i++) thresholds.GetArrayElementAtIndex(i).intValue = (i + 1) * 10;
            var spawns = data.FindProperty("experienceSpawns"); spawns.arraySize = 3;
            var positions = new[] { new Vector3(-6, -4), new Vector3(0, -2), new Vector3(5, -5) };
            for (int i = 0; i < 3; i++)
            {
                var marker = new GameObject("XP spawn " + (i + 1)); marker.transform.SetParent(root.transform);
                marker.transform.position = positions[i]; spawns.GetArrayElementAtIndex(i).objectReferenceValue = marker.transform;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            if (GameObject.Find("Upgrade feedback T045") != null) UpgradeFeedbackSetup.Attach();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }

        public static void BuildView()
        {
            var root = new GameObject("Upgrade selection", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 300;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            var view = root.AddComponent<UpgradeSelectionView>();
            var panel = Rect("Selection modal", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.gameObject.AddComponent<Image>().color = new Color(0.025f, 0.045f, 0.06f, 0.96f);
            var heading = Label("Heading", panel, new Vector2(0, 430), new Vector2(1600, 60), 40, "ESCOLHA UMA MELHORIA");
            heading.alignment = TextAnchor.MiddleCenter;
            var hint = Label("Hint", panel, new Vector2(0, 375), new Vector2(1600, 45), 24, "Compare o próximo efeito e combine armas com passivas."); hint.alignment = TextAnchor.MiddleCenter;
            var data = new SerializedObject(view);
            data.FindProperty("panel").objectReferenceValue = panel.gameObject;
            data.FindProperty("heading").objectReferenceValue = heading;
            var cards = data.FindProperty("cards"); cards.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                var card = Rect("Card " + i, panel, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2((i - 1) * 500, 0), new Vector2(460, 650));
                card.gameObject.AddComponent<Image>().color = new Color(0.09f, 0.16f, 0.19f);
                var entry = cards.GetArrayElementAtIndex(i); entry.FindPropertyRelative("root").objectReferenceValue = card.gameObject;
                CardText(entry, "kind", "Type", card, -24, 45, 22, new Color(1, 0.83f, 0.36f));
                CardText(entry, "title", "Title", card, -78, 70, 32, Cream);
                CardText(entry, "current", "Current", card, -170, 100, 21, new Color(0.7f, 0.79f, 0.8f));
                CardText(entry, "next", "Next", card, -280, 105, 22, Cream);
                CardText(entry, "synergy", "Synergy", card, -402, 85, 21, new Color(0.43f, 0.9f, 0.88f));
                CardText(entry, "requirement", "Requirement", card, -495, 84, 19, Cream);
                var choose = Button("Choose " + i, card, new Vector2(-75, -285), new Vector2(260, 55), "ESCOLHER");
                var banish = Button("Banish " + i, card, new Vector2(145, -285), new Vector2(150, 55), "BANIR");
                entry.FindPropertyRelative("choose").objectReferenceValue = choose;
                entry.FindPropertyRelative("banish").objectReferenceValue = banish;
            }
            var resources = Label("Resources", panel, new Vector2(0, -375), new Vector2(1600, 45), 24, ""); resources.alignment = TextAnchor.MiddleCenter;
            data.FindProperty("resources").objectReferenceValue = resources;
            data.FindProperty("reroll").objectReferenceValue = Button("Reroll", panel, new Vector2(-210, -445), new Vector2(350, 62), "RERROLAR");
            data.FindProperty("skip").objectReferenceValue = Button("Skip", panel, new Vector2(210, -445), new Vector2(350, 62), "PULAR");
            data.ApplyModifiedPropertiesWithoutUndo(); panel.gameObject.SetActive(false);
            IntegratedUiSetup.Configure(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); Object.DestroyImmediate(root);
        }
        private static void CardText(SerializedProperty entry, string field, string name, RectTransform parent, float y, float height, int size, Color color)
        {
            var text = Label(name, parent, new Vector2(20, y), new Vector2(420, height), size, "");
            var rect = text.rectTransform; rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            text.color = color; entry.FindPropertyRelative(field).objectReferenceValue = text;
        }
        private static Button Button(string name, Transform parent, Vector2 position, Vector2 size, string label)
        {
            var rect = Rect(name, parent, Vector2.one * 0.5f, Vector2.one * 0.5f, position, size);
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(0.19f, 0.36f, 0.4f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; MenuNavigation.Prepare(button);
            var text = Label("Label", rect, Vector2.zero, size, 24, label); text.alignment = TextAnchor.MiddleCenter;
            return button;
        }
        private static Text Label(string name, Transform parent, Vector2 position, Vector2 size, int fontSize, string value)
        {
            var rect = Rect(name, parent, Vector2.one * 0.5f, Vector2.one * 0.5f, position, size);
            var text = rect.gameObject.AddComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize; text.text = value; text.color = Cream; text.raycastTarget = false;
            text.alignment = TextAnchor.UpperLeft; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
        {
            var root = new GameObject(name, typeof(RectTransform)); root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max;
            rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }

        public static void Preview()
        {
            var scene = EditorSceneManager.OpenScene("Assets/QuackQuack/Scenes/Lake.unity");
            var view = Object.FindAnyObjectByType<UpgradeSelectionView>();
            var catalog = AssetDatabase.LoadAssetAtPath<PrototypeBuildCatalog>(PrototypeBuildCatalogSetup.CatalogPath);
            var build = new BuildState(); build.TryAddWeapon("bumerangue");
            using (var system = catalog.CreateUpgradeSystem(42, build))
            {
                var run = new RunState(42); run.TryTransition(RunPhase.Playing); system.BindRun(run);
                system.GenerateOffer("preview", new[] { "bumerangue", "pena-aerodinamica", "quack-frontal" });
                var bridge = new GameObject("Preview bridge").AddComponent<UpgradeSelectionBridge>();
                bridge.Initialize(catalog, system, run, view); view.Refresh();
                var camera = Object.FindAnyObjectByType<Camera>();
                var canvas = view.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera; canvas.planeDistance = 1;
                var target = new RenderTexture(1280, 720, 24); target.Create(); camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                var previous = RenderTexture.active; RenderTexture.active = target;
                var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                image.ReadPixels(new UnityEngine.Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                Directory.CreateDirectory("Logs"); File.WriteAllBytes("Logs/t044-preview.png", image.EncodeToPNG());
                RenderTexture.active = previous; camera.targetTexture = null;
                target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image); Object.DestroyImmediate(bridge.gameObject);
            }
        }
    }
}
