using System.IO;
using QuackQuack.Core.Shared;
using QuackQuack.Gameplay.Player;
using QuackQuack.Gameplay.Upgrades;
using QuackQuack.Systems.Run;
using QuackQuack.Presentation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace QuackQuack.Editor
{
    public static class GameplayHudSetup
    {
        private static readonly Color Ink = new Color(0.04f, 0.09f, 0.12f, 0.94f);
        private static readonly Color Cream = new Color(0.98f, 0.94f, 0.8f);

        [MenuItem("QuackQuack/Build T031 gameplay HUD")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene("Assets/QuackQuack/Scenes/Lake.unity");
            Attach();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("T031_HUD_SAVED");
        }

        public static void Attach()
        {
            var existing = GameObject.Find("Gameplay HUD T031");
            if (existing != null) { Configure(existing.GetComponent<GameplayHud>()); return; }
            var root = new GameObject("Gameplay HUD T031", typeof(RectTransform), typeof(Canvas));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var hud = root.AddComponent<GameplayHud>();
            var panel = Panel("Player vitals", root.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -16), new Vector2(348, 166));
            var healthBar = Bar("Health bar", panel, new Vector2(14, -42), new Color(0.91f, 0.37f, 0.4f));
            var breathBar = Bar("Breath bar", panel, new Vector2(14, -94), new Color(0.98f, 0.84f, 0.32f));
            var ready = Label("Quack state", panel, new Vector2(14, -138), new Vector2(320, 22), 16, "PRONTO • APRIMORADO");
            var clock = Panel("Clock", root.transform, Vector2.one, Vector2.one, new Vector2(-16, -16), new Vector2(112, 52));
            var time = Label("Time", clock, new Vector2(12, -10), new Vector2(88, 32), 24, "00:00");
            time.alignment = TextAnchor.MiddleCenter;
            var footer = Panel("Messages", root.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(850, 76));
            var status = Label("Status", footer, new Vector2(16, -8), new Vector2(818, 30), 18, "");
            status.alignment = TextAnchor.MiddleCenter;
            var data = new SerializedObject(hud);
            Assign(data, "healthFill", healthBar); Assign(data, "breathFill", breathBar);
            Assign(data, "quackLabel", ready);
            Assign(data, "timeLabel", time); Assign(data, "statusLabel", status);
            data.ApplyModifiedPropertiesWithoutUndo();
            Configure(hud);
            var arena = Object.FindAnyObjectByType<LakeArenaController>();
            var arenaData = new SerializedObject(arena);
            Assign(arenaData, "hud", hud); arenaData.ApplyModifiedPropertiesWithoutUndo();
            // These generated world signs would sit underneath the fixed HUD panels.
            var oldHint = GameObject.Find("WASD / SETAS: MOVER     ESPACO: QUACK     ESC: PAUSA");
            if (oldHint != null) Object.DestroyImmediate(oldHint);
            var oldTerrain = GameObject.Find("TERRA");
            if (oldTerrain != null) Object.DestroyImmediate(oldTerrain);
        }

        // In-place migration: keep HUD/panel/fill identities and all external scene references.
        public static void Configure(GameplayHud hud)
        {
            ResponsiveHudSetup.Configure(hud);
            var safe = hud.transform.Find("HUD Safe Area");
            var vitals = (RectTransform)safe.Find("Player vitals"); vitals.sizeDelta = new Vector2(348, 122);
            var data = new SerializedObject(hud);
            foreach (string field in new[] { "healthLabel", "breathLabel", "terrainLabel" }) Assign(data, field, null);
            foreach (string name in new[] { "Health", "Breath", "Terrain" })
            { var legacy = vitals.Find(name); if (legacy != null) Object.DestroyImmediate(legacy.gameObject); }
            var footer = (RectTransform)safe.Find("Messages"); footer.sizeDelta = new Vector2(850, 44);
            var controls = footer.Find("Controls"); if (controls != null) Object.DestroyImmediate(controls.gameObject);
            Assign(data, "healthIcon", Icon("Health icon", vitals, new Vector2(14, -14), ResourceIconKind.Health, new Color(.91f,.37f,.4f)));
            Assign(data, "breathIcon", Icon("Breath icon", vitals, new Vector2(14, -54), ResourceIconKind.Breath, new Color(.98f,.84f,.32f)));
            ConfigureResourceBar((Image)data.FindProperty("healthFill").objectReferenceValue, new Vector2(50, -20));
            ConfigureResourceBar((Image)data.FindProperty("breathFill").objectReferenceValue, new Vector2(50, -60));
            var quack = (Text)data.FindProperty("quackLabel").objectReferenceValue;
            quack.rectTransform.anchoredPosition = new Vector2(14, -92); quack.text = "PRONTO • APRIMORADO";
            ((Text)data.FindProperty("statusLabel").objectReferenceValue).text = "";
            var progress = Panel("Experience progress", safe, new Vector2(.5f,0), new Vector2(.5f,0), new Vector2(0,108), new Vector2(420,56));
            var fill = Bar("Experience track", progress, new Vector2(16,-36), new Color(.98f,.84f,.32f));
            fill.rectTransform.sizeDelta = new Vector2(388,10); ((RectTransform)fill.transform.parent).sizeDelta = new Vector2(388,10); fill.fillAmount = 0;
            var level = Label("Level", progress, new Vector2(16,-4), new Vector2(388,24), 19, "NÍVEL 1"); level.alignment = TextAnchor.MiddleCenter;
            Assign(data, "experienceFill", fill); Assign(data, "levelLabel", level); data.ApplyModifiedPropertiesWithoutUndo();

            var hintsRoot = Rect("Contextual hints", safe, new Vector2(.5f,0), new Vector2(.5f,0), new Vector2(0,184), new Vector2(700,60));
            var hints = hintsRoot.GetComponent<ContextualHintsView>() ?? hintsRoot.gameObject.AddComponent<ContextualHintsView>();
            var hintLabel = Label("Instructions", hintsRoot, Vector2.zero, new Vector2(700,60), 20, ""); hintLabel.alignment = TextAnchor.MiddleCenter;
            var hintData = new SerializedObject(hints); Assign(hintData, "label", hintLabel); hintData.ApplyModifiedPropertiesWithoutUndo();
            var overlay = Rect("Special enemies", safe, Vector2.zero, Vector2.one*.5f, Vector2.zero, Vector2.zero);
            overlay.anchorMax = Vector2.one; overlay.offsetMin = overlay.offsetMax = Vector2.zero; overlay.SetAsFirstSibling();
            var specials = overlay.GetComponent<SpecialEnemyBarsView>() ?? overlay.gameObject.AddComponent<SpecialEnemyBarsView>();
            var composition = hud.GetComponent<GameplayHudComposition>() ?? hud.gameObject.AddComponent<GameplayHudComposition>();
            var binding = new SerializedObject(composition);
            Assign(binding, "hud", hud); Assign(binding, "hints", hints); Assign(binding, "specials", specials); Assign(binding, "specialArea", overlay);
            Assign(binding, "player", Object.FindAnyObjectByType<PlayerController>()); Assign(binding, "director", Object.FindAnyObjectByType<RunDirector>());
            Assign(binding, "worldCamera", Camera.main); binding.ApplyModifiedPropertiesWithoutUndo();
        }
        private static ResourceIconGraphic Icon(string name, Transform parent, Vector2 position, ResourceIconKind kind, Color color)
        {
            var rect = Rect(name,parent,new Vector2(0,1),new Vector2(0,1),position,new Vector2(24,24));
            var icon = rect.GetComponent<ResourceIconGraphic>() ?? rect.gameObject.AddComponent<ResourceIconGraphic>(); icon.Configure(kind,color); return icon;
        }
        private static void ConfigureResourceBar(Image fill, Vector2 position)
        {
            var track = (RectTransform)fill.transform.parent; track.anchoredPosition = position; track.sizeDelta = new Vector2(284,12);
            fill.rectTransform.sizeDelta = track.sizeDelta;
        }

        private static RectTransform Panel(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rect = Rect(name, parent, anchor, pivot, position, size);
            var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>(); image.color = Ink; image.raycastTarget = false;
            return rect;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var found = parent.Find(name);
            var obj = found != null ? found.gameObject : new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }

        private static Text Label(string name, Transform parent, Vector2 position, Vector2 size, int fontSize, string text)
        {
            var rect = Rect(name, parent, new Vector2(0, 1), new Vector2(0, 1), position, size);
            var label = rect.GetComponent<Text>() ?? rect.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = fontSize;
            label.color = Cream; label.text = text; label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false; return label;
        }

        private static Image Bar(string name, Transform parent, Vector2 position, Color color)
        {
            var background = Rect(name, parent, new Vector2(0, 1), new Vector2(0, 1), position, new Vector2(320, 12));
            var track = background.GetComponent<Image>() ?? background.gameObject.AddComponent<Image>(); track.color = new Color(0.15f, 0.22f, 0.25f); track.raycastTarget = false;
            var fill = Rect("Fill", background, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(320, 12));
            var image = fill.GetComponent<Image>() ?? fill.gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/QuackQuack/Art/Prototype/Square.png");
            image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal; image.fillOrigin = 0;
            image.color = color; image.fillAmount = 1; image.raycastTarget = false; return image;
        }

        private static void Assign(SerializedObject data, string name, Object value) => data.FindProperty(name).objectReferenceValue = value;

        public static void Preview()
        {
            LakeArenaSetup.Preview();
            var camera = Object.FindAnyObjectByType<Camera>();
            var hud = Object.FindAnyObjectByType<GameplayHud>();
            hud.Render(new HudSnapshot(100, 100, 100, 100, "", "", 0, true, "PRONTO • APRIMORADO"));
            var canvas = hud.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera; canvas.planeDistance = 1;
            var target = new RenderTexture(1280, 720, 24); target.Create();
            var previousTarget = camera.targetTexture; camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            var previous = RenderTexture.active; RenderTexture.active = target;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new UnityEngine.Rect(0, 0, 1280, 720), 0, 0); image.Apply();
            Directory.CreateDirectory("Logs"); File.WriteAllBytes("Logs/t031-preview.png", image.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = previousTarget;
            target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image);
            Debug.Log("T031_PREVIEW_SAVED");
        }

        public static void BuildAndPreview() { Build(); Preview(); }
    }
}
