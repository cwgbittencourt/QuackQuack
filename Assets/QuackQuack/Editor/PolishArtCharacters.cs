using System;
using System.Linq;
using QuackQuack.Gameplay.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace QuackQuack.Editor
{
    public static partial class PolishArtSetup
    {
        private static readonly string[] Threats={"aranha","vespa","besouro","caramujo-aquatico","libelula-alfa","sapo-cururu","sapo-rei","rock-wide","rock-tall"};

        [MenuItem("QuackQuack/Art/Integrate T043 characters and obstacles")]
        public static void BuildCharacters()
        {
            string atlas=Root+"/Characters/lake-threats-atlas.png";
            if(!System.IO.File.Exists(atlas))throw new InvalidOperationException("Missing T043 atlas.");
            ImportAtlas(atlas,Threats,3,3);
            string approved=Root+"/Sources/sample-atlas.png";
            ImportAtlas(approved,new[]{"pato","formiga","rock","xp","heal","quack-frontal"},3,2);
            sprites=AssetDatabase.LoadAllAssetsAtPath(atlas).OfType<Sprite>()
                .Concat(AssetDatabase.LoadAllAssetsAtPath(approved).OfType<Sprite>().Where(s=>s.name=="pato"||s.name=="formiga")).ToArray();
            LoadThreatRevisions();
            string[] ids={"formiga","aranha","vespa","besouro","caramujo-aquatico","libelula-alfa","sapo-cururu","sapo-rei"};
            foreach(string id in ids)
            {
                string path="Assets/QuackQuack/Prefabs/Prototype/Enemies/"+id+".prefab";
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    string physics=PhysicsSignature(root);
                    SetActorArt(root,id,id=="sapo-rei"?2.5f:id=="libelula-alfa"||id=="sapo-cururu"?1.7f:1.15f,10);
                    if(physics!=PhysicsSignature(root))throw new InvalidOperationException("Physics changed: "+id);
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            Folder(Root+"/Objects");
            foreach(string id in new[]{"rock-wide","rock-tall"})
            {
                var rock=new GameObject(id,typeof(SpriteRenderer));
                ConfigureImage(rock.GetComponent<SpriteRenderer>(),id,1,1);
                PrefabUtility.SaveAsPrefabAsset(rock,Root+"/Objects/"+id+".prefab");UnityEngine.Object.DestroyImmediate(rock);
            }
            foreach(string scenePath in new[]{"Assets/QuackQuack/Scenes/Lake.unity","Assets/QuackQuack/Scenes/LakeRun.unity","Assets/QuackQuack/Scenes/FrogKingPractice.unity"})
            {
                var scene=EditorSceneManager.OpenScene(scenePath);
                foreach(var actor in UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
                {
                    string physics=PhysicsSignature(actor.gameObject);SetActorArt(actor.gameObject,"pato",1.5f,5);
                    if(physics!=PhysicsSignature(actor.gameObject))throw new InvalidOperationException("Player physics changed");
                }
                foreach(var root in scene.GetRootGameObjects())
                foreach(var collider in root.GetComponentsInChildren<BoxCollider2D>(true))
                {
                    string name=collider.name;
                    if(!(name.EndsWith("bank",StringComparison.OrdinalIgnoreCase)||name.Contains("rock")||name.StartsWith("Risk corridor")))continue;
                    var old=collider.GetComponent<SpriteRenderer>();if(old==null)continue;
                    string physics=PhysicsSignature(collider.gameObject);
                    // Keep the full collision footprint visible beneath the irregular rock artwork.
                    old.enabled=true;old.color=new Color(.33f,.34f,.32f);
                    var visual=collider.transform.Find("Cartoon obstacle");
                    if(visual==null){var obj=new GameObject("Cartoon obstacle",typeof(SpriteRenderer));obj.transform.SetParent(collider.transform,false);visual=obj.transform;}
                    string id=name.Contains("rock")?"rock-wide":"rock-tall";
                    var sprite=sprites.Single(s=>s.name==id);var image=visual.GetComponent<SpriteRenderer>();
                    image.sprite=sprite;image.color=Color.white;image.sortingOrder=1;
                    image.sharedMaterial=CharacterMaterial(sprite);
                    visual.localScale=new Vector3(collider.size.x/sprite.bounds.size.x,collider.size.y/sprite.bounds.size.y,1);
                    visual.localPosition=collider.offset;
                    if(physics!=PhysicsSignature(collider.gameObject))throw new InvalidOperationException("Obstacle physics changed");
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                if(scenePath.EndsWith("LakeRun.unity"))Capture(UnityEngine.Object.FindAnyObjectByType<Camera>(),"lake-scene",1920,1080,"Logs/Polimento002/T043");
            }
            Gallery();AssetDatabase.SaveAssets();
            Debug.Log("T043_CHARACTERS_OK actors=9 threats=8 geometry=unchanged");
            ContentValidationWindow.ValidateForCI();
        }

        private static string PhysicsSignature(GameObject root)
        {
            return root.transform.localPosition.ToString("R")+root.transform.localRotation.ToString("R")+root.transform.localScale.ToString("R")+string.Join("|",root.GetComponentsInChildren<Collider2D>(true).Select(c=>EditorJsonUtility.ToJson(c)))+
                string.Join("|",root.GetComponentsInChildren<Rigidbody2D>(true).Select(b=>EditorJsonUtility.ToJson(b)));
        }
        private static void LoadThreatRevisions()
        {
            string path=Root+"/Characters/minibosses-elite.png";
            if(!System.IO.File.Exists(path))return;
            ImportTrimmed(path,new[]{"libelula-alfa","sapo-cururu"},2,1);
            sprites=(sprites??Array.Empty<Sprite>()).Where(s=>s.name!="libelula-alfa"&&s.name!="sapo-cururu")
                .Concat(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()).ToArray();
            string king=Root+"/Characters/sapo-rei-royal.png";
            if(System.IO.File.Exists(king))
            {
                ImportTrimmed(king,new[]{"sapo-rei"},1,1);
                sprites=sprites.Where(s=>s.name!="sapo-rei").Concat(AssetDatabase.LoadAllAssetsAtPath(king).OfType<Sprite>()).ToArray();
            }
        }
        [MenuItem("QuackQuack/Art/Apply T047 threat hierarchy revision")]
        public static void BuildThreatRevision()
        {
            if(!System.IO.File.Exists(Root+"/Characters/minibosses-elite.png"))throw new InvalidOperationException("Missing T047 miniboss revision atlas.");
            LoadThreatRevisions();
            var ids=System.IO.File.Exists(Root+"/Characters/sapo-rei-royal.png")?new[]{"libelula-alfa","sapo-cururu","sapo-rei"}:new[]{"libelula-alfa","sapo-cururu"};
            foreach(string id in ids)
            {
                string path="Assets/QuackQuack/Prefabs/Prototype/Enemies/"+id+".prefab";
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    string before=PhysicsSignature(root);SetActorArt(root,id,id=="sapo-rei"?2.5f:1.7f,10);
                    if(before!=PhysicsSignature(root))throw new InvalidOperationException("Threat physics changed: "+id);
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();Debug.Log("T047_THREATS_OK count="+ids.Length+" physics=unchanged");
        }
        private static void SetActorArt(GameObject root,string id,float height,int order)
        {
            foreach(var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))renderer.enabled=false;
            var visual=root.transform.Find("Cartoon actor");
            if(visual==null){var obj=new GameObject("Cartoon actor",typeof(SpriteRenderer));obj.transform.SetParent(root.transform,false);visual=obj.transform;}
            ConfigureImage(visual.GetComponent<SpriteRenderer>(),id,height,order);
            if(visual.GetComponent<CartoonActorVisual>()==null)visual.gameObject.AddComponent<CartoonActorVisual>();
        }
        private static void ConfigureImage(SpriteRenderer image,string id,float height,int order)
        {
            var sprite=sprites.Single(s=>s.name==id);image.enabled=true;image.sprite=sprite;image.color=Color.white;image.sortingOrder=order;image.flipX=id=="formiga";
            image.sharedMaterial=CharacterMaterial(sprite);
            image.transform.localScale=Vector3.one*(height/sprite.bounds.size.y);
        }
        private static void Gallery()
        {
            string path=Root+"/Sample/LakeCartoonCast.unity";
            var scene=System.IO.File.Exists(path)?EditorSceneManager.OpenScene(path):EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            foreach(var root in scene.GetRootGameObjects())UnityEngine.Object.DestroyImmediate(root);
            var camera=new GameObject("Cast camera",typeof(Camera)).GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5.4f;
            camera.transform.position=new Vector3(0,0,-10);camera.backgroundColor=new Color(.90f,.84f,.65f);camera.clearFlags=CameraClearFlags.SolidColor;
            string[] ids={"pato","formiga","aranha","vespa","besouro","caramujo-aquatico","libelula-alfa","sapo-cururu","sapo-rei","rock-wide","rock-tall"};
            var canvas=new GameObject("Names",typeof(RectTransform),typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceCamera;
            canvas.GetComponent<Canvas>().worldCamera=camera;canvas.GetComponent<Canvas>().planeDistance=1;
            for(int i=0;i<ids.Length;i++)
            {
                var position=new Vector2(-6+i%4*4,3.3f-i/4*3.1f);
                var view=new GameObject(ids[i],typeof(SpriteRenderer));view.transform.position=position;
                ConfigureImage(view.GetComponent<SpriteRenderer>(),ids[i],2,10);
                var text=new GameObject(ids[i],typeof(TextMesh));var label=text.GetComponent<TextMesh>();
                label.text=ids[i];label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=36;label.characterSize=.065f;label.anchor=TextAnchor.MiddleCenter;label.color=Ink;
                text.transform.position=position+new Vector2(0,-1.2f);text.GetComponent<MeshRenderer>().sharedMaterial=label.font.material;
            }
            EditorSceneManager.SaveScene(scene,path);Capture(camera,"cast",1920,1080,"Logs/Polimento002/T043");
        }
        private static Material CharacterMaterial(Sprite sprite)
        {
            string path=Root+"/Characters/"+sprite.texture.name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));AssetDatabase.CreateAsset(material,path);}
            material.mainTexture=sprite.texture;EditorUtility.SetDirty(material);return material;
        }
    }
}
