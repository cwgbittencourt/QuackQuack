using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using QuackQuack.Editor;
using QuackQuack.Presentation.Terrain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace QuackQuack.Tests.EditMode
{
    public class LakeEnvironmentAssetsTests
    {
        [TearDown] public void CloseScene()=>EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        [TestCase("Lake")] [TestCase("LakeRun")]
        public void DecorationsAreColliderFreeAndBelowActorsAndDanger(string name)
        {
            EditorSceneManager.OpenScene("Assets/QuackQuack/Scenes/"+name+".unity");
            var environment=UnityEngine.Object.FindAnyObjectByType<LakeWaterAnimation>();Assert.That(environment,Is.Not.Null);
            Assert.That(environment.RippleCount,Is.InRange(25,40));
            Assert.That(environment.GetComponentsInChildren<Collider2D>(true),Is.Empty);
            foreach(var renderer in environment.GetComponentsInChildren<Renderer>(true))
                Assert.That(renderer.sortingOrder,Is.LessThan(0),renderer.name);
            foreach(var collider in UnityEngine.Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
                if(collider.name.EndsWith("bank",StringComparison.OrdinalIgnoreCase)||collider.name.Contains("rock"))
                {
                    Assert.That(collider.transform.Find("Cartoon stones"),Is.Not.Null);
                    Assert.That(collider.GetComponent<SpriteRenderer>().enabled,Is.True,"Complete collision footprint stays visible.");
                }
        }
        [Test] public void AtlasUsesAlphaAndNonReadableSharedTextures()
        {
            var path=PolishArtSetup.EnvironmentFolder+"/lake-plants.png";
            var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();Assert.That(sprites.Length,Is.EqualTo(6));
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            Assert.That(importer.alphaIsTransparency,Is.True);Assert.That(importer.isReadable,Is.False);Assert.That(importer.mipmapEnabled,Is.False);
            Assert.That(importer.textureCompression,Is.EqualTo(TextureImporterCompression.Uncompressed));
        }
        [Test] public void RegenerationPreservesPhysicsSceneGuidsAndSerializedBytes()
        {
            var paths=new[]{"Assets/QuackQuack/Scenes/Lake.unity","Assets/QuackQuack/Scenes/LakeRun.unity"};
            var before=paths.Select(p=>Convert.ToBase64String(File.ReadAllBytes(p))).ToArray();var guids=paths.Select(AssetDatabase.AssetPathToGUID).ToArray();
            PolishArtSetup.BuildEnvironment();
            for(int i=0;i<paths.Length;i++)
            {
                Assert.That(Convert.ToBase64String(File.ReadAllBytes(paths[i])),Is.EqualTo(before[i]),paths[i]);
                Assert.That(AssetDatabase.AssetPathToGUID(paths[i]),Is.EqualTo(guids[i]));
            }
        }
    }
}
