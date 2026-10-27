#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NUnit.Framework;
using QuackQuack.Gameplay.Bosses;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
namespace QuackQuack.Tests.PlayMode
{
    public class FrogKingPracticeTests
    {
        [UnityTest] public IEnumerator PracticeSceneStartsPlayableEncounterAndCleansUp()
        {
            const string path = "Assets/QuackQuack/Scenes/FrogKingPractice.unity";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            var scene = SceneManager.GetSceneByPath(path);
            FrogKingPracticeArena practice = null; Camera camera = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.TryGetComponent<FrogKingPracticeArena>(out var arena)) practice = arena;
                if (root.TryGetComponent<Camera>(out var view)) camera = view;
            }
            try
            {
                yield return null; yield return new WaitForFixedUpdate();
                Assert.That(practice, Is.Not.Null); Assert.That(practice.Player.State, Is.Not.Null);
                var boss = practice.GetComponentInChildren<FrogKingController>(); Assert.That(boss, Is.Not.Null);
                Assert.That(boss.Health, Is.EqualTo(practice.Settings.Health));
                Assert.That(boss.GetComponentInChildren<LineRenderer>().enabled, Is.True);
                var target = new RenderTexture(1280, 720, 24); var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                var previous = RenderTexture.active;
                try
                {
                    camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                    string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../Logs/t057-practice.png"));
                    Directory.CreateDirectory(Path.GetDirectoryName(output)); File.WriteAllBytes(output, pixels.EncodeToPNG());
                }
                finally { camera.targetTexture = null; RenderTexture.active = previous; Object.DestroyImmediate(pixels); Object.DestroyImmediate(target); }
            }
            finally { if (scene.isLoaded) SceneManager.UnloadSceneAsync(scene); }
            yield return null;
        }
    }
}
#endif
