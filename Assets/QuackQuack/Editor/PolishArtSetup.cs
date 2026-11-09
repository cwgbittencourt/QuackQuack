using System;
using System.IO;
using System.Linq;
using QuackQuack.Content.Characters;
using QuackQuack.Presentation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace QuackQuack.Editor
{
    // An isolated art-direction exhibit. Does not replace production prefabs or build scenes.
    public static class PolishArtSetup
    {
        public const string Root = "Assets/QuackQuack/Art/LakeCartoon";
        private static readonly Color Ink = new Color(.20f, .105f, .055f);
        private static readonly Color Cream = new Color(1, .90f, .66f);
        private static Sprite[] sprites;

        [MenuItem("QuackQuack/Art/Build T042 cartoon sample")]
        public static void BuildSample()
        {
            string atlas = Root + "/Sources/sample-atlas.png";
            if (!File.Exists(atlas)) throw new InvalidOperationException("Missing T042 source atlas.");
            ImportAtlas(atlas);
            sprites = AssetDatabase.LoadAllAssetsAtPath(atlas).OfType<Sprite>().ToArray();
            Folder(Root + "/Sample");
            string scenePath = Root + "/Sample/LakeCartoonSample.unity";
            var scene = File.Exists(scenePath) ? EditorSceneManager.OpenScene(scenePath) :
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (var old in scene.GetRootGameObjects()) UnityEngine.Object.DestroyImmediate(old);
            var camera = new GameObject("Art sample camera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -10); camera.orthographic = true;
            camera.orthographicSize = 5.4f; camera.backgroundColor = new Color(.17f, .48f, .52f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.gameObject.AddComponent<AudioListener>();

            // Context only: flat terrain swatches, not the environment production batch.
            var square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/QuackQuack/Art/Prototype/Square.png");
            Shape("Land context", square, new Vector2(-55.25f, 0), new Vector2(100, 12), new Color(.53f,.65f,.27f), -10);
            Shape("Shore context", square, new Vector2(-5.05f, 0), new Vector2(.4f,12), new Color(.89f,.74f,.41f), -9);
            for (int i = 0; i < 15; i++)
                Shape("Surface detail " + i, square, new Vector2(-3.7f + i % 5 * 2.4f, -3.5f + i / 5 * 2.8f),
                    new Vector2(.65f,.025f), new Color(.40f,.72f,.72f,.5f), -8);

            Place("pato", "pato", new Vector2(-2.4f,-.3f), 1.5f, 5);
            Place("formiga", "formiga", new Vector2(2.9f,-.15f), 1.25f, 10);
            Place("art.obstacle.rocks", "rock", new Vector2(-5.6f,1.2f), 2.1f, 1);
            Place("art.pickup.xp", "xp", new Vector2(1.5f,2.25f), .48f, 12);
            Place("art.pickup.heal", "heal", new Vector2(-.25f,-2.25f), .6f, 12);
            // Decorative wave remains inside the authored Quack range. The line is the exact range boundary.
            var quack = AssetDatabase.LoadAssetAtPath<QuackDefinition>("Assets/QuackQuack/Content/Characters/PatoQuack.asset");
            float range = quack.Range;
            Place("fx.quack-active", "quack", new Vector2(-.5f,-.3f), 2.4f, 14);
            var boundary = new GameObject("Quack authored range — static exhibit", typeof(LineRenderer));
            var line = boundary.GetComponent<LineRenderer>(); line.useWorldSpace = true;
            line.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/QuackQuack/Art/Prototype/QuackRing.mat");
            line.startColor = line.endColor = new Color(.65f,1,1,.65f); line.startWidth = line.endWidth = .035f;
            line.sortingOrder = 13; line.positionCount = 64; line.loop = true;
            for (int i=0;i<64;i++) { float a=i*360f/64*Mathf.Deg2Rad;
                line.SetPosition(i,new Vector3(-2.4f+Mathf.Cos(a)*range,-.3f+Mathf.Sin(a)*range)); }

            Hud(camera);
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            Capture(camera, "16x9", 1920,1080);
            Capture(camera, "18x9", 2160,1080);
            Capture(camera, "19_5x9", 2340,1080);
            Capture(camera, "4x3", 1440,1080);
            Debug.Log("T042_ART_SAMPLE_OK sprites=" + sprites.Length + " range=" + range);
        }

        private static void ImportAtlas(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 128; importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false; importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 2048;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            int w=texture.width/3, h=texture.height/2;
            string[] names={"pato","formiga","rock","xp","heal","quack"};
            // Unity serializes slice IDs/metas; never manufacture GUIDs or scene YAML.
#pragma warning disable 618
            var slices = new SpriteMetaData[6];
            for(int i=0;i<6;i++) slices[i]=new SpriteMetaData {name=names[i],
                rect=new Rect(i%3*w,(1-i/3)*h,w,h), alignment=(int)SpriteAlignment.Center,pivot=new Vector2(.5f,.5f)};
            importer.spritesheet=slices;
#pragma warning restore 618
            importer.SaveAndReimport();
        }

        private static void Place(string id,string spriteName,Vector2 position,float height,int order)
        {
            var sprite=sprites.Single(s=>s.name==spriteName);
            Shape(id,sprite,position,new Vector2(height*sprite.bounds.size.x/sprite.bounds.size.y,height),Color.white,order);
        }
        private static void Shape(string name,Sprite sprite,Vector2 pos,Vector2 size,Color tint,int order)
        {
            var obj=new GameObject(name,typeof(SpriteRenderer)); obj.transform.position=pos;
            obj.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);
            var renderer=obj.GetComponent<SpriteRenderer>(); renderer.sprite=sprite; renderer.color=tint; renderer.sortingOrder=order;
            renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/QuackQuack/Art/Prototype/Unlit.mat");
        }
        private static RectTransform Box(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,Color color)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(Image)); obj.transform.SetParent(parent,false);
            var rect=(RectTransform)obj.transform; rect.anchorMin=rect.anchorMax=anchor; rect.pivot=anchor;
            rect.anchoredPosition=pos; rect.sizeDelta=size;
            var image=obj.GetComponent<Image>(); image.color=color; image.raycastTarget=false;
            image.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); image.type=Image.Type.Sliced;
            return rect;
        }
        private static void Label(Transform parent,string value,Vector2 anchor,Vector2 pos,Vector2 size,int fontSize,Color color)
        {
            var obj=new GameObject(value,typeof(RectTransform),typeof(Text)); obj.transform.SetParent(parent,false);
            var rect=(RectTransform)obj.transform; rect.anchorMin=rect.anchorMax=anchor;rect.pivot=anchor;
            rect.anchoredPosition=pos;rect.sizeDelta=size;
            var text=obj.GetComponent<Text>(); text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text=value;text.fontSize=fontSize;text.fontStyle=FontStyle.Bold;text.color=color;
            text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
        }
        private static void Hud(Camera camera)
        {
            var root=new GameObject("Static art exhibit HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var safe=new GameObject("Safe Area",typeof(RectTransform),typeof(SafeAreaController));safe.transform.SetParent(root.transform,false);
            var safeRect=(RectTransform)safe.transform;safeRect.anchorMin=Vector2.zero;safeRect.anchorMax=Vector2.one;safeRect.offsetMin=safeRect.offsetMax=Vector2.zero;
            for(int i=0;i<2;i++)
            {
                var panel=Box(i==0?"Health":"Breath",safe.transform,new Vector2(0,1),new Vector2(24,-24-i*66),new Vector2(350,58),Ink);
                Box("Frame",panel,new Vector2(0,1),new Vector2(4,-4),new Vector2(342,50),Cream);
                var icon=new GameObject("Resource icon",typeof(RectTransform),typeof(CanvasRenderer),typeof(ResourceIconGraphic));icon.transform.SetParent(panel,false);
                var rect=(RectTransform)icon.transform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(14,-12);rect.sizeDelta=new Vector2(34,34);
                icon.GetComponent<ResourceIconGraphic>().Configure(i==0?ResourceIconKind.Health:ResourceIconKind.Breath,i==0?new Color(.9f,.22f,.22f):new Color(.07f,.65f,.75f));
                Box("Track",panel,new Vector2(0,1),new Vector2(62,-15),new Vector2(266,28),Ink);
                Box("Fill",panel,new Vector2(0,1),new Vector2(66,-19),new Vector2(i==0?205:163,20),i==0?new Color(.94f,.32f,.26f):new Color(.13f,.85f,.85f));
            }
            var relics=Box("Relics",safe.transform,new Vector2(.5f,1),new Vector2(0,-24),new Vector2(230,64),Ink);
            Box("Frame",relics,new Vector2(.5f,1),new Vector2(0,-4),new Vector2(222,56),Cream);
            Label(relics,"RELÍQUIAS",new Vector2(.5f,1),new Vector2(0,-8),new Vector2(220,48),25,Ink);
            var pause=Box("Pause",safe.transform,Vector2.one,new Vector2(-24,-24),new Vector2(230,64),Ink);
            Box("Frame",pause,Vector2.one,new Vector2(-4,-4),new Vector2(222,56),Cream);
            Label(pause,"PAUSAR",Vector2.one,new Vector2(-8,-8),new Vector2(214,48),25,Ink);
            Label(safe.transform,"04:32",Vector2.one,new Vector2(-270,-30),new Vector2(130,48),30,Cream);
            var xp=Box("XP and level",safe.transform,new Vector2(.5f,0),new Vector2(0,12),new Vector2(640,54),Ink);
            Box("Frame",xp,new Vector2(.5f,0),new Vector2(0,4),new Vector2(632,46),Cream);
            Label(xp,"NÍVEL 7",Vector2.zero,new Vector2(12,8),new Vector2(116,36),22,Ink);
            Box("Track",xp,Vector2.zero,new Vector2(140,14),new Vector2(478,26),Ink);
            Box("Fill",xp,Vector2.zero,new Vector2(144,18),new Vector2(290,18),new Color(.12f,.85f,.85f));
            Label(safe.transform,"AMOSTRA ARTÍSTICA • NÃO É PARTIDA",new Vector2(.5f,0),new Vector2(0,82),new Vector2(650,34),19,Cream);
        }
        private static void Capture(Camera camera,string name,int width,int height)
        {
            string output="Logs/Polimento002/T042";Directory.CreateDirectory(output);
            var target=new RenderTexture(width,height,24);target.Create(); camera.targetTexture=target;
            camera.aspect=(float)width/height; Canvas.ForceUpdateCanvases(); camera.Render();
            var old=RenderTexture.active;RenderTexture.active=target;
            var image=new Texture2D(width,height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
            File.WriteAllBytes(output+"/sample-"+name+".png",image.EncodeToPNG());
            RenderTexture.active=old;camera.targetTexture=null;target.Release();
            UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
        }
        private static void Folder(string path)
        {if(!AssetDatabase.IsValidFolder(path)){Folder(Path.GetDirectoryName(path).Replace('\\','/'));AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'),Path.GetFileName(path));}}
    }
}
