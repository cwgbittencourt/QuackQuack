using System;
using System.IO;
using QuackQuack.Content.Catalogs;
using QuackQuack.Content.Enemies;
using QuackQuack.Content.Encounters;
using QuackQuack.Gameplay.Enemies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace QuackQuack.Editor
{
    public static class LakeEncounterCatalogSetup
    {
        public const string CatalogPath="Assets/QuackQuack/Content/Catalogs/LakeEncounterCatalog.asset";
        private const string EnemyRoot="Assets/QuackQuack/Content/Enemies/Prototype";
        private const string EncounterRoot="Assets/QuackQuack/Content/Encounters/Prototype";
        private const string PrefabRoot="Assets/QuackQuack/Prefabs/Prototype/Enemies";
        [MenuItem("QuackQuack/Build T056 lake encounter catalog")]
        public static void Build()
        {
            Folder(EnemyRoot);Folder(EncounterRoot);Folder(PrefabRoot);
            var material=Load<Material>("Assets/QuackQuack/Art/Prototype/EnemyLine.mat",()=>new Material(Shader.Find("QuackQuack/FeedbackRing")));
            material.shader=Shader.Find("QuackQuack/FeedbackRing");EditorUtility.SetDirty(material);
            string warningPath=PrefabRoot+"/EnemyWarning.prefab";
            var warning=new GameObject("Enemy warning",typeof(EnemyAreaEffect));warning.GetComponent<LineRenderer>().sharedMaterial=material;
            warning.GetComponent<AudioSource>().playOnAwake=false;PrefabUtility.SaveAsPrefabAsset(warning,warningPath);Object.DestroyImmediate(warning);
            var visual=AssetDatabase.LoadAssetAtPath<GameObject>(warningPath);
            var clips=new[]{Tone("EnemyWeb",420),Tone("EnemyCharge",740),Tone("EnemyTrail",280),Tone("AlphaDive",880),Tone("CururuArea",190)};
            string[] ids={"formiga","aranha","vespa","besouro","caramujo-aquatico","libelula-alfa","sapo-cururu"};
            string[] names={"Formiga","Aranha","Vespa","Besouro","Caramujo Aquático","Libélula Alfa","Sapo-Cururu"};
            var roles=new[]{EnemyRole.Swarm,EnemyRole.WebCaster,EnemyRole.Charger,EnemyRole.FrontalArmored,EnemyRole.SlowingTrail,EnemyRole.Charger,EnemyRole.WebCaster};
            var colors=new[]{new Color(0.8f,0.23f,0.16f),new Color(0.65f,0.3f,0.8f),new Color(1,0.8f,0.2f),new Color(0.25f,0.45f,0.8f),new Color(0.2f,0.75f,0.65f),new Color(0.35f,0.9f,1),new Color(0.35f,0.7f,0.23f)};
            float[] health={8,14,12,32,24,180,300},speed={1.8f,1.2f,2,1,0.8f,2.8f,1.1f};
            int[] capacities={128,64,48,32,32,1,1};var enemies=new EnemyDefinition[7];
            for(int i=0;i<7;i++)
            {
                var prefab=BuildEnemyPrefab(ids[i],names[i],i,colors[i],material);
                var d=Load<EnemyDefinition>(EnemyRoot+"/"+ids[i]+".asset");Identity(d,ids[i],names[i]);
                Set(d,"role",p=>p.intValue=(int)roles[i]);Set(d,"health",p=>p.floatValue=health[i]);Set(d,"moveSpeed",p=>p.floatValue=speed[i]);
                Set(d,"contactDamage",p=>p.floatValue=i<5?4:10);Set(d,"attackDamage",p=>p.floatValue=i<5?6:14);
                Set(d,"attackInterval",p=>p.floatValue=i==5?2.4f:i==6?3.5f:3);Set(d,"attackRange",p=>p.floatValue=i<5?4:6);
                Set(d,"telegraphSeconds",p=>p.floatValue=i<5?0.7f:1);Set(d,"effectDuration",p=>p.floatValue=i<5?2.5f:4);
                Set(d,"slowMultiplier",p=>p.floatValue=0.65f);Set(d,"chargeSpeed",p=>p.floatValue=i==5?8:6);Set(d,"chargeDuration",p=>p.floatValue=i==5?0.8f:0.65f);
                Set(d,"frontalDamageMultiplier",p=>p.floatValue=0.25f);Set(d,"frontalArcDegrees",p=>p.floatValue=120);
                Set(d,"knockbackResistance",p=>p.floatValue=i>=5?0.5f:i==3?0.35f:0);Set(d,"interruptionResistance",p=>p.floatValue=i>=5?0.35f:0);
                Set(d,"experienceReward",p=>p.intValue=i<5?i+2:50);Set(d,"coinReward",p=>p.intValue=i<5?1:15);
                Set(d,"poolInitialSize",p=>p.intValue=Math.Min(capacities[i],16));Set(d,"poolMaxSize",p=>p.intValue=capacities[i]);
                Set(d,"prefab",p=>p.objectReferenceValue=prefab);Set(d,"telegraphVisual",p=>p.objectReferenceValue=visual);
                int clip=i==1?0:i==2?1:i==4?2:i==5?3:i==6?4:1;Set(d,"telegraphAudio",p=>p.objectReferenceValue=clips[clip]);
                enemies[i]=d;
            }
            var settings=Load<EnemyBehaviorSettings>(EnemyRoot+"/LakeBehaviorSettings.asset");
            Set(settings,"steeringBudget",p=>p.intValue=64);Set(settings,"effectCapacityPerType",p=>p.intValue=8);
            var waves=new EncounterDefinition[6];
            for(int i=0;i<6;i++)
            {
                int count=Math.Min(5,i+1);var composition=new string[count];var quantities=new int[count];
                for(int j=0;j<count;j++){composition[j]=ids[j];quantities[j]=j==0?2+i:1;}
                waves[i]=Encounter("lake-wave-"+(i+1),"Onda "+(i+1),EncounterKind.Wave,i*150,(i+1)*150,composition,quantities,3-i*0.3f,32+i*24);
            }
            var minis=new[]{Encounter("encounter-libelula-alfa",names[5],EncounterKind.AlphaDragonfly,300,300,new[]{ids[5]},new[]{1},1,1),
                Encounter("encounter-sapo-cururu",names[6],EncounterKind.CururuToad,600,600,new[]{ids[6]},new[]{1},1,1)};
            var final=Encounter("encounter-sapo-rei","Sapo-Rei",EncounterKind.FrogKing,900,900,new[]{LakeEncounterCatalog.FinalBossId},new[]{1},1,1);
            var catalog=Load<LakeEncounterCatalog>(CatalogPath);
            Refs(catalog,"commonEnemies",new[]{enemies[0],enemies[1],enemies[2],enemies[3],enemies[4]});Refs(catalog,"miniBosses",new[]{enemies[5],enemies[6]});
            Refs(catalog,"waves",waves);Refs(catalog,"miniBossEncounters",minis);Set(catalog,"finalBossEncounter",p=>p.objectReferenceValue=final);
            Set(catalog,"behaviorSettings",p=>p.objectReferenceValue=settings);
            if(!catalog.IsValid(out string reason))throw new InvalidOperationException(reason);AssetDatabase.SaveAssets();
        }
        private static GameObject BuildEnemyPrefab(string id,string name,int index,Color color,Material lineMaterial)
        {
            var root=new GameObject(name,typeof(PooledEnemy));root.layer=9;root.GetComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Kinematic;
            root.GetComponent<CircleCollider2D>().radius=index>=5?0.65f:0.4f;
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/QuackQuack/Art/Prototype/Circle.png");
            var spriteMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/QuackQuack/Art/Prototype/CircleUnlit.mat");
            Part(root.transform,"Body",sprite,spriteMaterial,color,Vector2.zero,index>=5?1.3f:0.8f);
            if(index==0) {Part(root.transform,"Head",sprite,spriteMaterial,color,new Vector2(0.35f,0),0.45f);Part(root.transform,"Abdomen",sprite,spriteMaterial,color,new Vector2(-0.35f,0),0.55f);}
            if(index==1)for(int n=0;n<4;n++){Part(root.transform,"Leg "+n,sprite,spriteMaterial,color,new Vector2((n/2==0?-1:1)*0.35f,(n%2==0?-1:1)*0.35f),0.3f);}
            if(index==2||index==5){Part(root.transform,"Wing A",sprite,spriteMaterial,new Color(0.8f,0.95f,1),new Vector2(0,0.45f),index==5?0.7f:0.45f);Part(root.transform,"Wing B",sprite,spriteMaterial,new Color(0.8f,0.95f,1),new Vector2(0,-0.45f),index==5?0.7f:0.45f);}
            if(index==4)Part(root.transform,"Shell",sprite,spriteMaterial,new Color(0.85f,0.7f,0.4f),new Vector2(-0.1f,0),0.5f);
            if(index==6){Part(root.transform,"Eye A",sprite,spriteMaterial,Color.white,new Vector2(0.4f,0.4f),0.25f);Part(root.transform,"Eye B",sprite,spriteMaterial,Color.white,new Vector2(0.4f,-0.4f),0.25f);}
            var direction=new GameObject(index==3?"Frontal shield":"Facing");direction.transform.SetParent(root.transform,false);
            var line=direction.AddComponent<LineRenderer>();line.useWorldSpace=false;line.sharedMaterial=lineMaterial;line.startColor=line.endColor=Color.white;
            line.startWidth=line.endWidth=0.05f;line.sortingOrder=12;line.positionCount=index==3?13:3;
            for(int n=0;n<line.positionCount;n++)
            {
                float angle=(-60+n*10)*Mathf.Deg2Rad;
                line.SetPosition(n,index==3?new Vector3(Mathf.Cos(angle)*0.45f,Mathf.Sin(angle)*0.45f):new Vector3(n==1?0.55f:0.3f,n==0?0.15f:n==2?-0.15f:0));
            }
            string path=PrefabRoot+"/"+id+".prefab";PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        private static void Part(Transform parent,string name,Sprite sprite,Material material,Color color,Vector2 position,float size)
        {var root=new GameObject(name);root.transform.SetParent(parent,false);root.transform.localPosition=position;root.transform.localScale=Vector3.one*size;var renderer=root.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=material;renderer.color=color;renderer.sortingOrder=10;}
        private static EncounterDefinition Encounter(string id,string name,EncounterKind kind,double start,double end,string[] ids,int[] quantities,float interval,int limit)
        {
            var d=Load<EncounterDefinition>(EncounterRoot+"/"+id+".asset");Identity(d,id,name);Set(d,"kind",p=>p.intValue=(int)kind);
            Set(d,"startSeconds",p=>p.doubleValue=start);Set(d,"endSeconds",p=>p.doubleValue=end);Set(d,"arenaId",p=>p.stringValue="lake");
            Set(d,"spawnInterval",p=>p.floatValue=interval);Set(d,"maxActive",p=>p.intValue=limit);
            Set(d,"enemyIds",p=>{p.arraySize=ids.Length;for(int i=0;i<ids.Length;i++)p.GetArrayElementAtIndex(i).stringValue=ids[i];});
            Set(d,"quantities",p=>{p.arraySize=quantities.Length;for(int i=0;i<quantities.Length;i++)p.GetArrayElementAtIndex(i).intValue=quantities[i];});return d;
        }
        private static AudioClip Tone(string name,int frequency)
        {
            string path="Assets/QuackQuack/Art/Prototype/"+name+".wav";const int rate=22050,samples=11025;
            using(var writer=new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+samples*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(samples*2);
                for(int i=0;i<samples;i++)writer.Write((short)(Math.Sin(i*2*Math.PI*frequency/rate)*6000*(1-i/(double)samples)));
            }
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        private static T Load<T>(string path,Func<T> create=null)where T:Object
        {var value=AssetDatabase.LoadAssetAtPath<T>(path);if(value==null){value=create!=null?create():(T)(Object)ScriptableObject.CreateInstance(typeof(T));AssetDatabase.CreateAsset(value,path);}return value;}
        private static void Identity(Object obj,string id,string name){Set(obj,"id",p=>p.stringValue=id);Set(obj,"displayName",p=>p.stringValue=name);}
        private static void Refs<T>(Object obj,string field,T[] values)where T:Object
        {Set(obj,field,p=>{p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];});}
        private static void Set(Object obj,string field,Action<SerializedProperty> action)
        {var s=new SerializedObject(obj);action(s.FindProperty(field));s.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(obj);}
        private static void Folder(string path){if(!AssetDatabase.IsValidFolder(path)){Directory.CreateDirectory(path);AssetDatabase.Refresh();}}
        public static void Preview()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Catalog preview camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);
            camera.orthographic=true;camera.orthographicSize=4;camera.backgroundColor=new Color(0.03f,0.06f,0.08f);camera.clearFlags=CameraClearFlags.SolidColor;
            var catalog=AssetDatabase.LoadAssetAtPath<LakeEncounterCatalog>(CatalogPath);var enemies=catalog.GetAllEnemies();
            Label("LAGO • CATÁLOGO DE ENCONTROS",new Vector2(0,3.2f),0.15f);
            for(int i=0;i<enemies.Length;i++)
            {
                Vector2 position=i<4?new Vector2(-5.1f+i*3.4f,1.5f):new Vector2(-4.5f+(i-4)*4.5f,-1.3f);
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(enemies[i].Prefab);instance.transform.position=position;
                Label(enemies[i].DisplayName,position+Vector2.down*0.95f,0.12f);
                var roles=new[]{"Perseguição","Teias","Investida","Armadura frontal","Trilha lenta"};
                Label(i<5?roles[i]:i==5?"MINIBOSS • 05:00":"MINIBOSS • 10:00",position+Vector2.down*1.35f,0.085f);
            }
            Label("Ondas 00:00–15:00  •  Sapo-Rei solicitado em 15:00",new Vector2(0,-3.5f),0.1f);
            var target=new RenderTexture(1280,720,24);target.Create();camera.targetTexture=target;
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            image.ReadPixels(new UnityEngine.Rect(0,0,1280,720),0,0);image.Apply();Directory.CreateDirectory("Logs");File.WriteAllBytes("Logs/t056-preview.png",image.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);
        }
        private static void Label(string value,Vector2 position,float size)
        {
            var label=new GameObject(value).AddComponent<TextMesh>();label.text=value;label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize=48;label.characterSize=size;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=Color.white;
            label.GetComponent<MeshRenderer>().sharedMaterial=label.font.material;label.transform.position=position;
        }
    }
}
