using System.IO;
using QuackQuack.Gameplay.Player;
using QuackQuack.Presentation.VFX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace QuackQuack.Editor
{
    public static class QuackFeedbackSetup
    {
        private const string Art = "Assets/QuackQuack/Art/Prototype";
        private const string PrefabPath = "Assets/QuackQuack/Prefabs/Prototype/QuackFeedback.prefab";
        [MenuItem("QuackQuack/Build T032 Quack feedback")]
        public static void Build()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(Art + "/QuackRing.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("QuackQuack/FeedbackRing"));
                AssetDatabase.CreateAsset(material, Art + "/QuackRing.mat");
            }
            var quack = Tone("Quack", 220, 0.2f);
            var impact = Tone("Impact", 520, 0.12f);
            var reflection = Tone("Reflection", 880, 0.16f);
            var obj = new GameObject("Pooled Quack feedback");
            obj.AddComponent<LineRenderer>().sharedMaterial = material;
            var source = obj.AddComponent<AudioSource>(); source.playOnAwake = false;
            obj.AddComponent<QuackFeedbackController>(); obj.AddComponent<PooledQuackFeedback>(); obj.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(obj, PrefabPath); Object.DestroyImmediate(obj);
            var scene = EditorSceneManager.OpenScene("Assets/QuackQuack/Scenes/Lake.unity");
            Attach();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets(); Debug.Log("T032_FEEDBACK_SAVED");
        }

        public static void Attach()
        {
            var existing = GameObject.Find("Quack feedback T032");
            if (existing != null) Object.DestroyImmediate(existing);
            var bridge = new GameObject("Quack feedback T032").AddComponent<QuackFeedbackBridge>();
            var data = new SerializedObject(bridge);
            data.FindProperty("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<PooledQuackFeedback>();
            data.FindProperty("quackClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(Art + "/Quack.wav");
            data.FindProperty("impactClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(Art + "/Impact.wav");
            data.FindProperty("reflectionClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(Art + "/Reflection.wav");
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AudioClip Tone(string name, float frequency, float seconds)
        {
            string path = Art + "/" + name + ".wav";
            if (!File.Exists(path))
            {
                const int rate = 22050;
                int samples = (int)(rate * seconds);
                using (var writer = new BinaryWriter(File.Create(path)))
                {
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples * 2);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                    writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2);
                    writer.Write((short)2); writer.Write((short)16);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples * 2);
                    for (int i = 0; i < samples; i++)
                    {
                        float t = (float)i / samples;
                        float envelope = Mathf.Sin(t * Mathf.PI) * (1 - t);
                        writer.Write((short)(Mathf.Sin(i * frequency * Mathf.PI * 2 / rate * (1 - 0.25f * t)) * envelope * 12000));
                    }
                }
                AssetDatabase.ImportAsset(path);
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        public static void Preview()
        {
            GameplayHudSetup.Preview();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var wave = ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<QuackFeedbackController>();
            wave.Initialize(wave.GetComponent<LineRenderer>().sharedMaterial);
            wave.Play(new Vector2(-6, -2), 4, new Color(1, 0.83f, 0.28f), 0.45f); wave.Advance(0.24f); wave.gameObject.SetActive(true);
            SpriteRenderer target = null;
            foreach (var candidate in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                if (candidate.gameObject.name == "Target") { target = candidate; break; }
            if (target != null)
            {
                var outline = ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<QuackFeedbackController>();
                outline.Initialize(outline.GetComponent<LineRenderer>().sharedMaterial);
                outline.PlayOutline(target, 0.3f); outline.gameObject.SetActive(true);
            }
            var camera = Object.FindAnyObjectByType<Camera>();
            var texture = new RenderTexture(1280, 720, 24); texture.Create(); camera.targetTexture = texture;
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = texture });
            var previous = RenderTexture.active; RenderTexture.active = texture;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
            File.WriteAllBytes("Logs/t032-preview.png", image.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null;
            texture.Release(); Object.DestroyImmediate(texture); Object.DestroyImmediate(image);
            Debug.Log("T032_PREVIEW_SAVED");
        }
        public static void BuildAndPreview() { Build(); Preview(); }
    }
}
