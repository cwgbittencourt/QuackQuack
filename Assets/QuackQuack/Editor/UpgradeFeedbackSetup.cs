using System.IO;
using System.Text;
using QuackQuack.Gameplay.Pickups;
using QuackQuack.Gameplay.Upgrades;
using QuackQuack.Presentation.VFX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace QuackQuack.Editor
{
    public static class UpgradeFeedbackSetup
    {
        public const string PrefabPath = "Assets/QuackQuack/Prefabs/Prototype/UpgradeFeedback.prefab";
        private const string RarePath = "Assets/QuackQuack/Prefabs/Prototype/RareExperiencePickup.prefab";
        private const string Art = "Assets/QuackQuack/Art/Prototype";
        [MenuItem("QuackQuack/Build T045 upgrade feedback")]
        public static void Build()
        {
            Tone("Evolution", 660, true); Tone("RareReward", 440, false);
            var root = new GameObject("Upgrade feedback");
            var ring = root.AddComponent<QuackFeedbackController>();
            var material = AssetDatabase.LoadAssetAtPath<Material>(Art + "/QuackRing.mat");
            root.GetComponent<LineRenderer>().sharedMaterial = material;
            var accentRoot = new GameObject("Accent"); accentRoot.transform.SetParent(root.transform, false);
            var accent = accentRoot.AddComponent<LineRenderer>(); accent.sharedMaterial = material;
            accent.useWorldSpace = false; accent.loop = true; accent.startWidth = accent.endWidth = 0.06f;
            accent.positionCount = 0; accent.sortingOrder = 22;
            var labelRoot = new GameObject("Label"); labelRoot.transform.SetParent(root.transform, false);
            var label = labelRoot.AddComponent<TextMesh>(); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 64; label.characterSize = 0.12f; label.anchor = TextAnchor.MiddleCenter;
            label.GetComponent<MeshRenderer>().sharedMaterial = label.font.material; label.GetComponent<MeshRenderer>().sortingOrder = 24;
            label.text = "";
            var view = root.AddComponent<UpgradeFeedbackController>();
            var data = new SerializedObject(view);
            data.FindProperty("ring").objectReferenceValue = ring; data.FindProperty("accent").objectReferenceValue = accent;
            data.FindProperty("label").objectReferenceValue = label; data.ApplyModifiedPropertiesWithoutUndo();
            root.AddComponent<PooledUpgradeFeedback>();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); Object.DestroyImmediate(root);
            var rare = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/QuackQuack/Prefabs/Prototype/ExperiencePickup.prefab"));
            rare.name = "Rare experience pickup"; rare.transform.localScale = Vector3.one * 0.6f;
            rare.GetComponent<SpriteRenderer>().color = new Color(0.75f, 0.45f, 1);
            PrefabUtility.SaveAsPrefabAsset(rare, RarePath); Object.DestroyImmediate(rare);
            var scene = EditorSceneManager.OpenScene("Assets/QuackQuack/Scenes/Lake.unity");
            Attach(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        public static void Attach()
        {
            var old = GameObject.Find("Upgrade feedback T045"); if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject("Upgrade feedback T045"); var bridge = root.AddComponent<UpgradeFeedbackBridge>();
            var data = new SerializedObject(bridge);
            data.FindProperty("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<PooledUpgradeFeedback>();
            data.FindProperty("evolutionClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(Art + "/Evolution.wav");
            data.FindProperty("rareClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(Art + "/RareReward.wav");
            data.ApplyModifiedPropertiesWithoutUndo();
            var progression = Object.FindAnyObjectByType<LakeBuildProgression>();
            var marker = new GameObject("Rare reward spawn T045"); marker.transform.SetParent(root.transform); marker.transform.position = new Vector3(-9, 2);
            var progressionData = new SerializedObject(progression);
            progressionData.FindProperty("rareRewardPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(RarePath).GetComponent<ExperiencePickup>();
            progressionData.FindProperty("rareRewardSpawn").objectReferenceValue = marker.transform;
            progressionData.FindProperty("rareRewardExperience").intValue = 40;
            var thresholds = progressionData.FindProperty("experienceThresholds"); thresholds.arraySize = 7;
            for (int i = 0; i < 7; i++) thresholds.GetArrayElementAtIndex(i).intValue = (i + 1) * 10;
            progressionData.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Tone(string name, float frequency, bool rising)
        {
            string path = Art + "/" + name + ".wav";
            if (!File.Exists(path))
            {
                const int rate = 22050, count = 11025;
                using (var writer = new BinaryWriter(File.Create(path)))
                {
                    writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + count * 2); writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                    writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2);
                    writer.Write((short)2); writer.Write((short)16); writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(count * 2);
                    for (int i = 0; i < count; i++)
                    {
                        float t = i / (float)count;
                        float phase = 2 * Mathf.PI * frequency * (i / (float)rate) * (1 + (rising ? 0.5f : -0.2f) * t);
                        writer.Write((short)(Mathf.Sin(phase) * Mathf.Sin(Mathf.PI * t) * (1 - t) * 12000));
                    }
                }
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }
        public static void Preview()
        {
            EditorSceneManager.OpenScene("Assets/QuackQuack/Scenes/Lake.unity");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            for (int i = 0; i < 2; i++)
            {
                var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                var view = root.GetComponent<UpgradeFeedbackController>(); view.Initialize();
                view.Play(i == 0, new Vector2(i == 0 ? -4.5f : 4.5f, -1), i == 0 ? 2 : 1.2f, 1.2f, null, 0);
                view.Advance(0.35f);
            }
            var camera = Object.FindAnyObjectByType<Camera>(); var target = new RenderTexture(1280, 720, 24); target.Create(); camera.targetTexture = target;
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            var previous = RenderTexture.active; RenderTexture.active = target;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
            Directory.CreateDirectory("Logs"); File.WriteAllBytes("Logs/t045-preview.png", image.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image);
        }
    }
}
