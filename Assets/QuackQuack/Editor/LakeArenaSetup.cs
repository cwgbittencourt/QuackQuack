using System.IO;
using QuackQuack.Content.Characters;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Player;
using QuackQuack.Gameplay.Terrain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace QuackQuack.Editor
{
    public static class LakeArenaSetup
    {
        private const string Art = "Assets/QuackQuack/Art/Prototype";
        private const string Prefabs = "Assets/QuackQuack/Prefabs/Prototype";
        private static Sprite circle, square;
        private static Material material, circleMaterial;
        private static Transform root;

        [MenuItem("QuackQuack/Build T030 lake arena")]
        public static void Build()
        {
            EnsureFolder(Art); EnsureFolder(Prefabs);
            circle = CreateSprite("Circle", true);
            square = CreateSprite("Square", false);
            material = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Unlit.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
                AssetDatabase.CreateAsset(material, Art + "/Unlit.mat");
            }
            material.mainTexture = square.texture; EditorUtility.SetDirty(material);
            circleMaterial = AssetDatabase.LoadAssetAtPath<Material>(Art + "/CircleUnlit.mat");
            if (circleMaterial == null)
            {
                circleMaterial = new Material(material);
                AssetDatabase.CreateAsset(circleMaterial, Art + "/CircleUnlit.mat");
            }
            circleMaterial.mainTexture = circle.texture; EditorUtility.SetDirty(circleMaterial);
            var quack = LoadOrCreate<QuackDefinition>("Assets/QuackQuack/Content/Characters/PatoQuack.asset");
            Set(quack, "id", "quack"); Set(quack, "displayName", "Quack");
            var character = LoadOrCreate<CharacterDefinition>("Assets/QuackQuack/Content/Characters/Pato.asset");
            Set(character, "id", "pato"); Set(character, "displayName", "Pato"); Set(character, "quack", quack);
            if (!character.IsValid(out var reason) || !quack.IsValid(out reason)) throw new System.InvalidOperationException(reason);
            var enemy = CreatePrefab("Target", new Color(0.85f, 0.28f, 0.32f), 0.8f, 9);
            var projectile = CreatePrefab("LightProjectile", new Color(0.77f, 0.58f, 1), 0.32f, 9);
            var reward = CreatePrefab("BreathReward", new Color(1, 0.78f, 0.16f), 0.6f, 10);

            // Open and update the existing scene, preserving its GUID and existing camera.
            var scene = EditorSceneManager.OpenScene("Assets/QuackQuack/Scenes/Lake.unity");
            enemy = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/Target.prefab").GetComponent<QuackTarget>();
            projectile = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/LightProjectile.prefab").GetComponent<QuackTarget>();
            reward = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/BreathReward.prefab").GetComponent<QuackTarget>();
            var existing = GameObject.Find("Lake Arena T030");
            if (existing != null) Object.DestroyImmediate(existing);
            root = new GameObject("Lake Arena T030").transform;
            var camera = Object.FindAnyObjectByType<Camera>();
            camera.orthographic = true; camera.orthographicSize = 8;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.backgroundColor = new Color(0.045f, 0.09f, 0.12f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            if (camera.GetComponent<AudioListener>() == null) camera.gameObject.AddComponent<AudioListener>();

            Rect("Ground", Vector2.zero, new Vector2(25, 14), new Color(0.24f, 0.38f, 0.25f), -10);
            Rect("Shore", Vector2.zero, new Vector2(7.7f, 10.7f), new Color(0.76f, 0.68f, 0.43f), -9);
            var lake = Rect("Water", Vector2.zero, new Vector2(7, 10), new Color(0.13f, 0.42f, 0.56f), -8);
            var waterZone = lake.AddComponent<TerrainZone>();
            // Geometry is local; visual scaling is already on the renderer object.
            waterZone.Configure(Vector2.one, 0.02f);
            var island = Rect("Island", new Vector2(0, 2), new Vector2(2, 2), new Color(0.42f, 0.52f, 0.29f), -7);
            var landZone = island.AddComponent<TerrainZone>();
            landZone.Configure(Vector2.one, 0.04f, TerrainType.Land, 1);
            for (int i = 0; i < 7; i++)
                Rect("Water ripple", new Vector2((i % 2 == 0 ? -1 : 1) * 1.5f, -4 + i * 1.3f), new Vector2(0.8f, 0.035f), new Color(0.35f, 0.65f, 0.72f), -6);

            Wall("North bank", new Vector2(0, 7), new Vector2(25.6f, 0.5f));
            Wall("South bank", new Vector2(0, -7), new Vector2(25.6f, 0.5f));
            Wall("West bank", new Vector2(-12.5f, 0), new Vector2(0.5f, 14));
            Wall("East bank", new Vector2(12.5f, 0), new Vector2(0.5f, 14));
            Wall("Risk corridor north", new Vector2(6.5f, 4), new Vector2(6, 0.7f));
            Wall("Risk corridor south", new Vector2(6.5f, 1), new Vector2(6, 0.7f));
            Wall("Training rock", new Vector2(-4.3f, 4), new Vector2(2, 0.7f));
            Wall("South rock", new Vector2(5.5f, -3), new Vector2(2.3f, 1.2f));

            var pato = new GameObject("Pato");
            pato.SetActive(false); pato.transform.SetParent(root); pato.transform.position = new Vector2(-6, -2);
            pato.AddComponent<Rigidbody2D>();
            pato.AddComponent<CircleCollider2D>().radius = 0.42f;
            var player = pato.AddComponent<PlayerController>();
            Shape("Body", pato.transform, Vector2.zero, new Vector2(1, 0.8f), new Color(1, 0.84f, 0.28f), 5);
            Shape("Head", pato.transform, new Vector2(0.23f, 0.25f), new Vector2(0.57f, 0.57f), new Color(1, 0.96f, 0.72f), 6);
            Shape("Bill", pato.transform, new Vector2(0.51f, 0.18f), new Vector2(0.4f, 0.19f), new Color(1, 0.48f, 0.12f), 7);
            Shape("Eye", pato.transform, new Vector2(0.32f, 0.32f), new Vector2(0.09f, 0.09f), Color.black, 8);
            Set(player, "definition", character);
            var serializedPlayer = new SerializedObject(player);
            var zones = serializedPlayer.FindProperty("water"); zones.arraySize = 2;
            zones.GetArrayElementAtIndex(0).objectReferenceValue = waterZone;
            zones.GetArrayElementAtIndex(1).objectReferenceValue = landZone;
            serializedPlayer.ApplyModifiedPropertiesWithoutUndo();
            pato.SetActive(true);

            var arena = root.gameObject.AddComponent<LakeArenaController>();
            Set(arena, "player", player); Set(arena, "enemyPrefab", enemy);
            Set(arena, "projectilePrefab", projectile); Set(arena, "rewardPrefab", reward);
            var first = Marker("Training target spawn", new Vector2(-5, 2.5f));
            var guard = Marker("Risk guard spawn", new Vector2(6, 2.5f));
            var data = new SerializedObject(arena);
            var spawns = data.FindProperty("enemySpawns"); spawns.arraySize = 2;
            spawns.GetArrayElementAtIndex(0).objectReferenceValue = first;
            spawns.GetArrayElementAtIndex(1).objectReferenceValue = guard;
            data.ApplyModifiedPropertiesWithoutUndo();
            Set(arena, "projectileSpawn", Marker("Projectile emitter", new Vector2(11, 2.5f)));
            Set(arena, "rewardSpawn", Marker("Risk reward spawn", new Vector2(8.5f, 2.5f)));
            Shape("Emitter", root, new Vector2(11, 2.5f), new Vector2(0.7f, 0.7f), new Color(0.57f, 0.38f, 0.79f), 3);
            Label("LAGO  /  ARENA DE TREINO", new Vector2(0, 6.2f), 0.14f);
            Label("TERRA", new Vector2(-7, 5), 0.1f);
            Label("AGUA", new Vector2(0, -0.5f), 0.1f);
            Label("ROTA DE RISCO  >", new Vector2(7, 4.9f), 0.1f);
            Label("FOLEGO +", new Vector2(8.5f, 1.8f), 0.07f);
            Label("WASD / SETAS: MOVER     ESPACO: QUACK     ESC: PAUSA", new Vector2(0, -6.2f), 0.08f);

            if (GameObject.Find("Gameplay HUD T031") != null) GameplayHudSetup.Attach();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("T030_LAKE_ARENA_SAVED");
        }

        private static GameObject Rect(string name, Vector2 position, Vector2 size, Color color, int order)
        {
            var obj = new GameObject(name); obj.transform.SetParent(root); obj.transform.position = position;
            obj.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = square; renderer.color = color;
            renderer.sharedMaterial = material; renderer.sortingOrder = order; return obj;
        }
        private static void Wall(string name, Vector2 position, Vector2 size)
        {
            var obj = Rect(name, position, size, new Color(0.28f, 0.31f, 0.29f), 1);
            obj.layer = 8; obj.AddComponent<BoxCollider2D>();
        }
        private static Transform Marker(string name, Vector2 position)
        {
            var obj = new GameObject(name); obj.transform.SetParent(root); obj.transform.position = position; return obj.transform;
        }
        private static void Shape(string name, Transform parent, Vector2 position, Vector2 size, Color color, int order)
        {
            var obj = new GameObject(name); obj.transform.SetParent(parent); obj.transform.localPosition = position;
            obj.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = circle; renderer.color = color;
            renderer.sharedMaterial = circleMaterial; renderer.sortingOrder = order;
        }
        private static void Label(string text, Vector2 position, float size)
        {
            var obj = new GameObject(text); obj.transform.SetParent(root); obj.transform.position = position;
            var label = obj.AddComponent<TextMesh>(); label.text = text; label.anchor = TextAnchor.MiddleCenter;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 64; label.characterSize = size; label.color = new Color(0.98f, 0.94f, 0.79f);
            obj.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
            obj.GetComponent<MeshRenderer>().sortingOrder = 10;
        }
        private static QuackTarget CreatePrefab(string name, Color color, float scale, int layer)
        {
            var obj = new GameObject(name); obj.layer = layer; obj.transform.localScale = Vector3.one * scale;
            var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = circle; renderer.color = color;
            renderer.sharedMaterial = circleMaterial; renderer.sortingOrder = 4;
            obj.AddComponent<CircleCollider2D>().isTrigger = true;
            obj.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            obj.AddComponent<QuackTarget>(); obj.SetActive(false);
            var prefab = PrefabUtility.SaveAsPrefabAsset(obj, Prefabs + "/" + name + ".prefab");
            Object.DestroyImmediate(obj); return prefab.GetComponent<QuackTarget>();
        }
        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value == null) { value = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value, path); }
            return value;
        }
        private static void Set(Object target, string field, Object value)
        {
            var data = new SerializedObject(target); data.FindProperty(field).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Set(Object target, string field, string value)
        {
            var data = new SerializedObject(target); data.FindProperty(field).stringValue = value; data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void EnsureFolder(string path)
        {
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        private static Sprite CreateSprite(string name, bool round)
        {
            string path = Art + "/" + name + ".png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                    texture.SetPixel(x, y, !round || Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) < 31 ? Color.white : Color.clear);
                texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 64;
            importer.spriteImportMode = SpriteImportMode.Single;
            var spriteSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(spriteSettings);
            spriteSettings.spriteAlignment = (int)SpriteAlignment.Center;
            spriteSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(spriteSettings);
            importer.alphaIsTransparency = true; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static void Preview()
        {
            EditorSceneManager.OpenScene("Assets/QuackQuack/Scenes/Lake.unity");
            AddPreview("Target", new Vector2(-5, 2.5f));
            AddPreview("Target", new Vector2(6, 2.5f));
            AddPreview("BreathReward", new Vector2(8.5f, 2.5f));
            AddPreview("LightProjectile", new Vector2(10, 2.5f));
            var camera = Object.FindAnyObjectByType<Camera>();
            camera.aspect = 16f / 9;
            var target = new RenderTexture(1280, 720, 24);
            target.Create();
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
            Directory.CreateDirectory("Logs");
            File.WriteAllBytes("Logs/t030-preview.png", image.EncodeToPNG());
            RenderTexture.active = previous;
            target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image);
            Debug.Log("T030_PREVIEW_SAVED");
        }

        public static void BuildAndPreview() { Build(); Preview(); }

        private static void AddPreview(string prefab, Vector2 position)
        {
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/" + prefab + ".prefab"));
            obj.transform.position = position; obj.SetActive(true);
        }
    }
}
