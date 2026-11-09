using System;
using System.Collections.Generic;
using System.Linq;
using QuackQuack.Gameplay.Terrain;
using QuackQuack.Gameplay.Bosses;
using QuackQuack.Presentation.Terrain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace QuackQuack.Editor
{
    public static partial class PolishArtSetup
    {
        public const string EnvironmentFolder = Root + "/Environment";
        private static readonly string[] EnvironmentNames = {"reeds","flowers","lilies","grass","ripple","moss"};
        [MenuItem("QuackQuack/Art/Capture T046 environment")]
        public static void CaptureEnvironment()
        {
            EditorSceneManager.OpenScene("Assets/QuackQuack/Scenes/LakeRun.unity");
            var camera=Object.FindAnyObjectByType<Camera>();camera.orthographicSize=7.8f;camera.transform.position=new Vector3(0,0,-10);
            Capture(camera,"environment-16x9",1920,1080,"Logs/Polimento002/T046");
            Capture(camera,"environment-18x9",2160,1080,"Logs/Polimento002/T046");
            Capture(camera,"environment-19_5x9",2340,1080,"Logs/Polimento002/T046");
            Capture(camera,"environment-4x3",1440,1080,"Logs/Polimento002/T046");
            Debug.Log("T046_ENVIRONMENT_CAPTURE_OK");
        }
        [MenuItem("QuackQuack/Art/Integrate T046 lake environment")]
        public static void BuildEnvironment()
        {
            Folder(EnvironmentFolder);
            string atlas = EnvironmentFolder + "/lake-plants.png";
            ImportTrimmed(atlas,EnvironmentNames,3,2);
            var art = AssetDatabase.LoadAllAssetsAtPath(atlas).OfType<Sprite>().ToDictionary(s=>s.name);
            var material = EnvironmentMaterial(atlas);
            var refugeSprite = RefugeSprite();
            foreach(string sceneName in new[]{"Lake","LakeRun"})
            {
                var scene = EditorSceneManager.OpenScene("Assets/QuackQuack/Scenes/"+sceneName+".unity");
                string physics = EnvironmentPhysics(scene);
                var arena = GameObject.Find("Lake Arena T030").transform;
                var group = EnvironmentChild("Cartoon environment",arena);
                var ground = arena.Find("Ground").GetComponent<SpriteRenderer>(); ground.color = new Color(.59f,.69f,.29f);
                var water = arena.Find("Water").GetComponent<SpriteRenderer>(); water.color = new Color(.15f,.53f,.56f);
                var island = arena.Find("Island").GetComponent<SpriteRenderer>(); island.color = new Color(.64f,.72f,.32f);
                arena.Find("Shore").GetComponent<SpriteRenderer>().enabled = false;
                foreach(var image in arena.GetComponentsInChildren<SpriteRenderer>(true)) if(image.name=="Water ripple")image.enabled=false;
                // Outer edges are organic; inner water/land boundaries retain the exact terrain footprint.
                Surface("Lake sand",group,Vector2.zero,new Vector2(7,10),.38f,-9,new Color(.96f,.78f,.43f));
                Surface("Island sand",group,new Vector2(0,2),new Vector2(2,2),.10f,-7,new Color(.96f,.78f,.43f));
                // Island's original land square draws above its sand surround; never move its zone.
                island.sortingOrder = -6;
                var ripples = new List<SpriteRenderer>();
                for(int row=0;row<8;row++)for(int column=0;column<5;column++)
                {
                    var position = new Vector2(-2.9f+column*1.4f+(row%2)*.12f,-4.4f+row*1.24f);
                    if(Mathf.Abs(position.x)<1.5f&&Mathf.Abs(position.y-2)<1.5f)continue;
                    var image = Decoration("Surface "+row+" "+column,group,art["ripple"],material,position,.75f+(row%3)*.08f,-7);
                    image.color = new Color(.73f,.95f,.94f,.25f);ripples.Add(image);
                }
                for(int i=0;i<44;i++)
                {
                    float x=-11.7f+(i%8)*3.28f+(i%3)*.17f,y=-5.8f+(i/8)*2.2f;
                    if(Mathf.Abs(x)<4.2f&&Mathf.Abs(y)<5.7f)continue;
                    var image=Decoration("Ground detail "+i,group,art[i%4==0?"flowers":i%3==0?"grass":"moss"],material,new Vector2(x,y),i%4==0?.52f:.65f,-9);
                    image.color=new Color(1,1,1,i%4==0?.8f:.42f);
                }
                for(int side=-1;side<=1;side+=2)for(int i=0;i<7;i++)
                    Decoration("Shore plant "+side+" "+i,group,art[i%3==0?"reeds":"grass"],material,new Vector2(side*(3.95f+(i%2)*.12f),-4.5f+i*1.45f),i%3==0?.8f:.4f,-5);
                for(int i=0;i<5;i++)
                    Decoration("Lily decoration "+i,group,art["lilies"],material,new Vector2(i%2==0?-2.45f:2.4f,-3.6f+i*1.65f),.65f,-6);
                var driver=group.GetComponent<LakeWaterAnimation>();if(driver==null)driver=group.gameObject.AddComponent<LakeWaterAnimation>();driver.Configure(ripples.ToArray());
                foreach(var refuge in Object.FindObjectsByType<FrogKingSafeArea>(FindObjectsSortMode.None))
                {
                    var image=refuge.GetComponent<SpriteRenderer>();
                    if(image!=null){image.sprite=refugeSprite;image.sharedMaterial=EnvironmentMaterial(EnvironmentFolder+"/RefugeRing.png");image.color=Color.white;}
                }
                foreach(var box in arena.GetComponentsInChildren<BoxCollider2D>(true))
                    if(box.name.EndsWith("bank",StringComparison.OrdinalIgnoreCase)||box.name.Contains("rock")||box.name.StartsWith("Risk corridor"))
                        EnvironmentStones(box);
                if(physics!=EnvironmentPhysics(scene))throw new InvalidOperationException("Environment changed physical/terrain geometry: "+sceneName);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();ContentValidationWindow.ValidateForCI();
            Debug.Log("T046_ENVIRONMENT_OK scenes=2 physics=unchanged");
        }
        private static Material EnvironmentMaterial(string atlas)
        {
            string path=EnvironmentFolder+"/"+(atlas.EndsWith("RefugeRing.png")?"RefugeRing":"LakePlants")+".mat";
            var result=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(result==null){result=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));AssetDatabase.CreateAsset(result,path);}
            result.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(atlas);EditorUtility.SetDirty(result);return result;
        }
        private static Sprite RefugeSprite()
        {
            const int side=128;var texture=new Texture2D(side,side,TextureFormat.RGBA32,false);var pixels=new Color[side*side];
            for(int y=0;y<side;y++)for(int x=0;x<side;x++)
            {
                float radius=new Vector2(x+.5f-side/2,y+.5f-side/2).magnitude;
                pixels[y*side+x]=radius<58?new Color(.75f,.95f,.66f,.09f):new Color(.77f,.94f,.57f,Mathf.Clamp01(64-radius)*.8f);
            }
            texture.SetPixels(pixels);texture.Apply();string path=EnvironmentFolder+"/RefugeRing.png";
            System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=128;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static Transform EnvironmentChild(string name,Transform parent)
        {
            var child=parent.Find(name);
            if(child==null){child=new GameObject(name).transform;child.SetParent(parent,false);}
            return child;
        }
        private static SpriteRenderer Decoration(string name,Transform parent,Sprite sprite,Material material,Vector2 position,float width,int order)
        {
            var child=EnvironmentChild(name,parent);child.localPosition=position;
            child.localScale=Vector3.one*(width/sprite.bounds.size.x);
            var image=child.GetComponent<SpriteRenderer>();if(image==null)image=child.gameObject.AddComponent<SpriteRenderer>();
            image.sprite=sprite;image.sharedMaterial=material;image.color=Color.white;image.sortingOrder=order;return image;
        }
        private static void Surface(string name,Transform parent,Vector2 center,Vector2 size,float fringe,int order,Color color)
        {
            string path=EnvironmentFolder+"/"+name.Replace(' ','-')+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null){mesh=new Mesh{name=name};AssetDatabase.CreateAsset(mesh,path);}
            const int samples=24;var vertices=new Vector3[1+samples*4];var triangles=new int[samples*4*3];
            var colors=new Color[vertices.Length];var uv=new Vector2[vertices.Length];colors[0]=color;uv[0]=Vector2.one*.5f;
            for(int i=0;i<samples*4;i++)
            {
                float t=(i%samples)/(float)samples;int side=i/samples;
                Vector2 p=side==0?new Vector2(Mathf.Lerp(-size.x/2,size.x/2,t),-size.y/2):
                    side==1?new Vector2(size.x/2,Mathf.Lerp(-size.y/2,size.y/2,t)):
                    side==2?new Vector2(Mathf.Lerp(size.x/2,-size.x/2,t),size.y/2):new Vector2(-size.x/2,Mathf.Lerp(size.y/2,-size.y/2,t));
                float offset=fringe*(1+.22f*Mathf.Sin(i*.28f)+.12f*Mathf.Cos(i*.61f));
                p+=new Vector2(Mathf.Sign(p.x),Mathf.Sign(p.y))*offset;
                vertices[i+1]=p;colors[i+1]=color;uv[i+1]=Vector2.one*.5f;
                triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=(i+1)%(samples*4)+1;
            }
            mesh.Clear();mesh.vertices=vertices;mesh.triangles=triangles;mesh.colors=colors;mesh.uv=uv;mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            var child=EnvironmentChild(name,parent);child.localPosition=center;
            var filter=child.GetComponent<MeshFilter>();if(filter==null)filter=child.gameObject.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
            var image=child.GetComponent<MeshRenderer>();if(image==null)image=child.gameObject.AddComponent<MeshRenderer>();
            image.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/QuackQuack/Art/Prototype/Unlit.mat");image.sortingOrder=order;
        }
        private static void EnvironmentStones(BoxCollider2D collider)
        {
            var legacy=collider.GetComponent<SpriteRenderer>();if(legacy!=null){legacy.color=new Color(.30f,.25f,.18f,.28f);legacy.sortingOrder=0;}
            var stretched=collider.transform.Find("Cartoon obstacle");if(stretched!=null)stretched.GetComponent<SpriteRenderer>().enabled=false;
            var group=EnvironmentChild("Cartoon stones",collider.transform);
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Objects/rock-wide.prefab").GetComponent<SpriteRenderer>();
            var scale=collider.transform.lossyScale;float width=collider.size.x*Mathf.Abs(scale.x),height=collider.size.y*Mathf.Abs(scale.y);
            bool horizontal=width>=height;int count=Mathf.CeilToInt(Mathf.Max(width,height)/.8f);
            for(int i=0;i<count;i++)
            {
                var child=EnvironmentChild("Stone "+i,group);float along=(i+.5f)/count-.5f;
                child.localPosition=collider.offset+(horizontal?new Vector2(along*collider.size.x,0):new Vector2(0,along*collider.size.y));
                var image=child.GetComponent<SpriteRenderer>();if(image==null)image=child.gameObject.AddComponent<SpriteRenderer>();
                image.sprite=source.sprite;image.sharedMaterial=source.sharedMaterial;image.color=Color.white;image.sortingOrder=1;
                float w=horizontal?width/count:width,h=horizontal?height:height/count;
                child.localScale=new Vector3(w/(source.sprite.bounds.size.x*Mathf.Abs(scale.x)),h/(source.sprite.bounds.size.y*Mathf.Abs(scale.y)),1);
            }
        }
        public static string EnvironmentPhysics(Scene scene)
        {
            var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true));
            return string.Join("|",all.Where(t=>t.GetComponent<Collider2D>()!=null||t.GetComponent<TerrainZone>()!=null).Select(t=>
                t.GetInstanceID()+":"+t.localToWorldMatrix.ToString()+string.Join(";",t.GetComponents<Collider2D>().Select(c=>EditorJsonUtility.ToJson(c)))+
                (t.GetComponent<TerrainZone>()!=null?EditorJsonUtility.ToJson(t.GetComponent<TerrainZone>()):"")));
        }
    }
}
