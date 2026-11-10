using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using QuackQuack.Content.Weapons;
using System.Collections.Generic;

namespace QuackQuack.Editor
{
    // Review-only exhibit: numbered production sprites without names/functions as visual hints.
    public static class ArtReviewSetup
    {
        public static void AuditAndCaptureCombat()
        {
            ContentValidationWindow.ValidateForCI();
            var rows=new List<string>{"id\tlevel\trange\tareaRadius\thitRadius\tconeAngle\tsprite\tmaterial"};
            var weapons=AssetDatabase.FindAssets("t:WeaponDefinition",new[]{"Assets/QuackQuack/Content"})
                .Select(g=>AssetDatabase.LoadAssetAtPath<WeaponDefinition>(AssetDatabase.GUIDToAssetPath(g))).OrderBy(w=>w.Id).ToArray();
            if(weapons.Length!=9)throw new InvalidOperationException("Expected six weapons and three evolved results.");
            foreach(var weapon in weapons)
            {
                if(weapon.ProjectileSprite==null||weapon.ProjectileMaterial==null||weapon.ProjectileMaterial.mainTexture!=weapon.ProjectileSprite.texture)
                    throw new InvalidOperationException("Missing/mismatched weapon art: "+weapon.Id);
                for(int level=1;level<=5;level++)
                {
                    var stats=weapon.GetLevel(level,QuackQuack.Core.Run.TerrainType.Land);
                    rows.Add(string.Join("\t",weapon.Id,level,stats.Range,stats.AreaRadius,weapon.HitRadius,weapon.ConeAngle,
                        AssetDatabase.GetAssetPath(weapon.ProjectileSprite),AssetDatabase.GetAssetPath(weapon.ProjectileMaterial)));
                }
            }
            string folder=Environment.GetEnvironmentVariable("QUACKQUACK_REVIEW_OUTPUT")??"Logs/Polimento002/T047";Directory.CreateDirectory(folder);
            File.WriteAllLines(Path.Combine(folder,"weapon-geometry.tsv"),rows);
            EditorSceneManager.OpenScene(PolishArtSetup.Root+"/Sample/LakeCartoonCombat.unity");
            var camera=Object.FindAnyObjectByType<Camera>();
            var target=new RenderTexture(1920,1080,24);target.Create();var old=RenderTexture.active;
            var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);camera.targetTexture=target;camera.aspect=1920f/1080;
            try{camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,"combat-sheet.png"),pixels.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=old;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(pixels);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
            Debug.Log("T047_ART_AUDIT_OK weapons=9 levels=45 textureBindings=valid");
        }
        [MenuItem("QuackQuack/Art/Capture T047 identification sheet")]
        public static void CaptureIdentification()
        {
            string root=PolishArtSetup.Root;
            Sprite Find(string path,string name)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single(s=>s.name==name);
            var ids=new[]{"pato","formiga","aranha","vespa","besouro","caramujo-aquatico","libelula-alfa","sapo-cururu","sapo-rei","heal","xp","rock-wide","reeds","grass"};
            var sprites=ids.Select((id,i)=>Find(root+(i==8&&File.Exists(root+"/Characters/sapo-rei-royal.png")?"/Characters/sapo-rei-royal.png":(i==6||i==7)&&File.Exists(root+"/Characters/minibosses-elite.png")?"/Characters/minibosses-elite.png":i<2?"/Sources/sample-atlas.png":i<9||i==11?"/Characters/lake-threats-atlas.png":i<11?"/Combat/pickups-atlas.png":"/Environment/lake-plants.png"),id)).ToArray();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Review camera",typeof(Camera)).GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=4.5f;
            camera.transform.position=new Vector3(0,0,-10);camera.backgroundColor=new Color(.93f,.88f,.72f);camera.clearFlags=CameraClearFlags.SolidColor;
            var materials=new Material[sprites.Length];
            try
            {
                for(int i=0;i<sprites.Length;i++)
                {
                    var obj=new GameObject("Item "+(i+1),typeof(SpriteRenderer));obj.transform.position=new Vector3(-8.1f+i%7*2.7f,2.1f-i/7*4,0);
                    var image=obj.GetComponent<SpriteRenderer>();image.sprite=sprites[i];
                    materials[i]=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));materials[i].mainTexture=sprites[i].texture;image.sharedMaterial=materials[i];
                    obj.transform.localScale=Vector3.one*(2.15f/Mathf.Max(sprites[i].bounds.size.x,sprites[i].bounds.size.y));
                    var label=new GameObject("Number",typeof(TextMesh)).GetComponent<TextMesh>();label.text=(i+1).ToString();
                    label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=48;label.characterSize=.12f;label.anchor=TextAnchor.MiddleCenter;label.color=new Color(.22f,.12f,.065f);
                    label.transform.position=obj.transform.position+Vector3.down*1.5f;label.GetComponent<MeshRenderer>().sharedMaterial=label.font.material;
                }
                string folder=Environment.GetEnvironmentVariable("QUACKQUACK_REVIEW_OUTPUT")??"Logs/Polimento002/T047";Directory.CreateDirectory(folder);
                var target=new RenderTexture(1920,900,24);target.Create();var old=RenderTexture.active;camera.targetTexture=target;camera.aspect=1920f/900;
                var pixels=new Texture2D(1920,900,TextureFormat.RGB24,false);
                try{camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1920,900),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,"identification.png"),pixels.EncodeToPNG());}
                finally{camera.targetTexture=null;RenderTexture.active=old;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(pixels);}
                File.WriteAllLines(Path.Combine(folder,"identification-key.txt"),ids.Select((id,i)=>(i+1)+"="+id));
                Debug.Log("T047_IDENTIFICATION_OK count="+ids.Length);
            }
            finally{foreach(var material in materials)if(material!=null)Object.DestroyImmediate(material);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
        }
    }
}
