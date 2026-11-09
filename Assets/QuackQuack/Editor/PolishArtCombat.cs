using System;
using System.IO;
using System.Linq;
using QuackQuack.Content.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace QuackQuack.Editor
{
    public static partial class PolishArtSetup
    {
        private static readonly string[] CombatIds={"bumerangue","ovos","quack-frontal","aura-energia","foguetes","bolhas","tornado-penas","incubadora-combate","quack-supersonico"};

        [MenuItem("QuackQuack/Art/Integrate T044 combat and pickups")]
        public static void BuildCombat()
        {
            Folder(Root+"/Combat");
            string atlas=Root+"/Combat/weapons-atlas.png";
            if(!File.Exists(atlas))throw new InvalidOperationException("Missing T044 weapons atlas");
            ImportTrimmed(atlas,CombatIds,3,3);
            string circles=Root+"/Combat/circles-atlas.png";
            if(!File.Exists(circles))throw new InvalidOperationException("Missing clean aura/bubble atlas");
            ImportTrimmed(circles,new[]{"aura-energia","bolhas"},2,1);
            string bubble=Root+"/Combat/bubble-iridescent.png";
            if(!File.Exists(bubble))throw new InvalidOperationException("Missing selected iridescent bubble");
            ImportTrimmed(bubble,new[]{"bolhas"},1,1);
            var combat=AssetDatabase.LoadAllAssetsAtPath(atlas).OfType<Sprite>().Where(s=>s.name!="aura-energia"&&s.name!="bolhas")
                .Concat(AssetDatabase.LoadAllAssetsAtPath(circles).OfType<Sprite>().Where(s=>s.name=="aura-energia"))
                .Concat(AssetDatabase.LoadAllAssetsAtPath(bubble).OfType<Sprite>()).ToArray();
            var material=CombatMaterial(atlas);
            foreach(string id in CombatIds)
            {
                var definition=AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/QuackQuack/Content/Weapons/Prototype/"+id+".asset");
                var data=new SerializedObject(definition);
                var sprite=combat.Single(s=>s.name==id);
                data.FindProperty("projectileSprite").objectReferenceValue=sprite;
                data.FindProperty("projectileMaterial").objectReferenceValue=CombatMaterial(AssetDatabase.GetAssetPath(sprite.texture));
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            string pickups=Root+"/Combat/pickups-atlas.png";
            if(!File.Exists(pickups)){File.Copy(Root+"/Sources/pickups-c2-x3.png",pickups);AssetDatabase.ImportAsset(pickups);}
            ImportTrimmed(pickups,new[]{"heal","xp"},2,1);
            var rewards=AssetDatabase.LoadAllAssetsAtPath(pickups).OfType<Sprite>().ToArray();
            var rewardMaterial=CombatMaterial(pickups);
            foreach(string name in new[]{"HealingPickup","ExperiencePickup","RareExperiencePickup","BreathReward"})
            {
                string path="Assets/QuackQuack/Prefabs/Prototype/"+name+".prefab";
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    string physics=PhysicsSignature(root);
                    foreach(var old in root.GetComponentsInChildren<SpriteRenderer>(true))old.enabled=false;
                    bool healing=name=="HealingPickup";
                    var sprite=rewards.Single(s=>s.name==(healing?"heal":"xp"));
                    // BreathReward is a training fixture; keep its own distinct gold tint and silhouette.
                    if(name=="BreathReward")sprite=combat.Single(s=>s.name=="bumerangue");
                    var visual=ChildImage(root.transform,"Cartoon reward");
                    SetReward(visual,sprite,name=="BreathReward"?material:rewardMaterial,1);
                    if(name=="RareExperiencePickup")
                    {var satellite=ChildImage(root.transform,"Rare satellite");SetReward(satellite,sprite,rewardMaterial,.4f);satellite.transform.localPosition=new Vector3(.4f,.3f,0);}
                    if(physics!=PhysicsSignature(root))throw new InvalidOperationException("Reward physics changed: "+name);
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();
            CombatGallery(combat,rewards,material,rewardMaterial);
            ContentValidationWindow.ValidateForCI();
            Debug.Log("T044_COMBAT_OK weapons=9 pickups=4 geometry=unchanged");
        }

        private static SpriteRenderer ChildImage(Transform parent,string name)
        {
            var child=parent.Find(name);
            if(child==null){var obj=new GameObject(name,typeof(SpriteRenderer));obj.transform.SetParent(parent,false);child=obj.transform;}
            return child.GetComponent<SpriteRenderer>();
        }
        private static void SetReward(SpriteRenderer image,Sprite sprite,Material material,float diameter)
        {
            image.sprite=sprite;image.sharedMaterial=material;image.enabled=true;image.color=Color.white;image.sortingOrder=12;
            image.transform.localScale=Vector3.one*(diameter/Mathf.Max(sprite.bounds.size.x,sprite.bounds.size.y));
        }
        private static Material CombatMaterial(string atlas)
        {
            string path=Path.ChangeExtension(atlas,"mat");var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));AssetDatabase.CreateAsset(material,path);}
            material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(atlas);EditorUtility.SetDirty(material);return material;
        }
        private static void ImportTrimmed(string path,string[] names,int columns,int rows)
        {
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.LoadImage(File.ReadAllBytes(path));
            try
            {
                var pixels=texture.GetPixels32();int width=texture.width/columns,height=texture.height/rows;
#pragma warning disable 618
                var slices=new SpriteMetaData[names.Length];
                for(int i=0;i<names.Length;i++)
                {
                    int ox=i%columns*width,oy=(rows-1-i/columns)*height;
                    int minX=width,minY=height,maxX=-1,maxY=-1;
                    for(int y=0;y<height;y++)for(int x=0;x<width;x++)
                        if(pixels[(oy+y)*texture.width+ox+x].a>8){minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);}
                    if(maxX<0)throw new InvalidOperationException("Empty sprite: "+names[i]);
                    minX=Math.Max(0,minX-1);minY=Math.Max(0,minY-1);maxX=Math.Min(width-1,maxX+1);maxY=Math.Min(height-1,maxY+1);
                    var rect=new Rect(ox+minX,oy+minY,maxX-minX+1,maxY-minY+1);
                    if(names[i]=="bolhas"||names[i]=="aura-energia")
                    {float side=Mathf.Max(rect.width,rect.height);rect=new Rect(Mathf.Clamp(rect.center.x-side/2,ox,ox+width-side),Mathf.Clamp(rect.center.y-side/2,oy,oy+height-side),side,side);}
                    slices[i]=new SpriteMetaData{name=names[i],rect=rect,alignment=(int)SpriteAlignment.Center,pivot=new Vector2(.5f,.5f)};
                }
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit=128;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;
                importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
                importer.spritesheet=slices;importer.SaveAndReimport();
                foreach(var slice in slices)Debug.Log("T044_SLICE "+slice.name+" "+slice.rect);
#pragma warning restore 618
            }
            finally{UnityEngine.Object.DestroyImmediate(texture);}
        }
        private static void CombatGallery(Sprite[] combat,Sprite[] rewards,Material material,Material rewardMaterial)
        {
            string path=Root+"/Sample/LakeCartoonCombat.unity";
            var scene=File.Exists(path)?EditorSceneManager.OpenScene(path):EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            foreach(var root in scene.GetRootGameObjects())UnityEngine.Object.DestroyImmediate(root);
            var camera=new GameObject("Combat camera",typeof(Camera)).GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5.4f;
            camera.transform.position=new Vector3(0,0,-10);camera.backgroundColor=new Color(.90f,.84f,.65f);camera.clearFlags=CameraClearFlags.SolidColor;
            var all=combat.Concat(rewards).ToArray();
            for(int i=0;i<all.Length;i++)
            {
                Vector2 position=new Vector2(-6+i%4*4,3.3f-i/4*3.1f);
                var view=new GameObject(all[i].name,typeof(SpriteRenderer));view.transform.position=position;
                SetReward(view.GetComponent<SpriteRenderer>(),all[i],CombatMaterial(AssetDatabase.GetAssetPath(all[i].texture)),1.7f);
                var text=new GameObject("Label "+i,typeof(TextMesh));var label=text.GetComponent<TextMesh>();
                label.text=all[i].name;label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=36;label.characterSize=.065f;label.anchor=TextAnchor.MiddleCenter;label.color=Ink;
                text.transform.position=position+new Vector2(0,-1.15f);text.GetComponent<MeshRenderer>().sharedMaterial=label.font.material;
            }
            EditorSceneManager.SaveScene(scene,path);Capture(camera,"combat",1920,1080,"Logs/Polimento002/T044");
        }
    }
}
