using System.IO;
using QuackQuack.Content.Relics;
using QuackQuack.Core.Progression;
using QuackQuack.Gameplay.Upgrades;
using QuackQuack.Presentation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace QuackQuack.Editor
{
    public static class RelicSetup
    {
        public const string CatalogPath = "Assets/QuackQuack/Content/Relics/PrototypeRelicCatalog.asset";
        public const string PrefabPath = "Assets/QuackQuack/Prefabs/Prototype/RelicLoadout.prefab";
        [MenuItem("QuackQuack/Build T052 relic loadout")]
        public static void Build()
        {
            const string folder = "Assets/QuackQuack/Content/Relics/Prototype";
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            var names = new[] { "Botas do Lago", "Garras de Junco", "Pena de Combate", "Pedra da Margem", "Pérola do Lago",
                "Apito Veloz", "Lente de Orvalho", "Anel de Ondas", "Concha de Fôlego", "Pena Leve" };
            var ids = new[] { "botas-do-lago", "garras-de-junco", "pena-de-combate", "pedra-da-margem", "perola-do-lago",
                "apito-veloz", "lente-de-orvalho", "anel-de-ondas", "concha-de-folego", "pena-leve" };
            var attributes = new[] { RelicAttribute.MovementSpeed, RelicAttribute.MovementSpeed, RelicAttribute.Damage,
                RelicAttribute.Damage, RelicAttribute.Damage, RelicAttribute.AttackSpeed, RelicAttribute.Range,
                RelicAttribute.Area, RelicAttribute.BreathRecovery, RelicAttribute.MovementSpeed };
            var terrain = new[] { RelicTerrain.Water, RelicTerrain.Land, RelicTerrain.Any, RelicTerrain.Land, RelicTerrain.Water,
                RelicTerrain.Any, RelicTerrain.Any, RelicTerrain.Any, RelicTerrain.Any, RelicTerrain.Any };
            var effects = new[] { "+5% movimento na água", "+5% movimento em terra", "+5% dano das armas", "+5% dano das armas em terra",
                "+5% dano das armas na água", "+5% velocidade de ataque", "+5% alcance das armas", "+5% raio de área das armas",
                "+5% recuperação de Fôlego", "+5% movimento" };
            var relics = new RelicDefinition[10];
            for (int i = 0; i < 10; i++)
            {
                string path = folder + "/" + ids[i] + ".asset";
                relics[i] = AssetDatabase.LoadAssetAtPath<RelicDefinition>(path);
                if (relics[i] == null) { relics[i] = ScriptableObject.CreateInstance<RelicDefinition>(); AssetDatabase.CreateAsset(relics[i], path); }
                var s = new SerializedObject(relics[i]);
                s.FindProperty("id").stringValue = ids[i]; s.FindProperty("displayName").stringValue = names[i];
                s.FindProperty("description").stringValue = effects[i]; s.FindProperty("attribute").intValue = (int)attributes[i];
                s.FindProperty("terrain").intValue = (int)terrain[i]; s.FindProperty("bonus").floatValue = 0.05f;
                s.FindProperty("unlock").intValue = i < 3 ? 0 : i < 7 ? 1 : 2;
                s.FindProperty("threshold").intValue = i < 3 ? 0 : i < 7 ? (i - 2) * 15 : (i - 6) * 10;
                s.FindProperty("rarity").intValue = i < 3 ? 0 : i < 7 ? 1 : 2;
                s.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(relics[i]);
            }
            var catalog = AssetDatabase.LoadAssetAtPath<PrototypeRelicCatalog>(CatalogPath);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<PrototypeRelicCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            var data = new SerializedObject(catalog); var entries = data.FindProperty("relics"); entries.arraySize = 10;
            for (int i = 0; i < 10; i++) entries.GetArrayElementAtIndex(i).objectReferenceValue = relics[i];
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(catalog);
            if (!catalog.IsValid(out string reason)) throw new System.InvalidOperationException(reason);
            BuildView();
            var scene = EditorSceneManager.OpenScene("Assets/QuackQuack/Scenes/Lake.unity");
            var old = GameObject.Find("Relics T052"); if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject("Relics T052");
            var ui = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)); ui.transform.SetParent(root.transform);
            var bridge = root.AddComponent<LakeRelics>(); var serialized = new SerializedObject(bridge);
            serialized.FindProperty("catalog").objectReferenceValue = catalog;
            serialized.FindProperty("view").objectReferenceValue = ui.GetComponent<RelicLoadoutView>(); serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        private static void BuildView()
        {
            var root = new GameObject("Relic loadout", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; root.GetComponent<Canvas>().sortingOrder = 250;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
            var view = root.AddComponent<RelicLoadoutView>(); var s = new SerializedObject(view);
            var panel = Rect("Panel", root.transform, Vector2.zero, new Vector2(1920,1080)); panel.gameObject.AddComponent<Image>().color = new Color(0.03f,0.07f,0.09f,0.98f);
            s.FindProperty("panel").objectReferenceValue = panel.gameObject;
            s.FindProperty("summary").objectReferenceValue = Text("Title",panel,new Vector2(0,470),new Vector2(1600,60),32,"");
            Text("Hint",panel,new Vector2(0,415),new Vector2(1600,45),22,"Equipe até três. A partida fica pausada. Bônus são removidos ao desequipar.");
            var labels = s.FindProperty("labels"); labels.arraySize = 10; var toggles=s.FindProperty("toggles"); toggles.arraySize=10;
            for(int i=0;i<10;i++)
            {
                var card=Rect("Relic "+i,panel,new Vector2(i<5?-435:435,300-(i%5)*145),new Vector2(820,130));
                card.gameObject.AddComponent<Image>().color=new Color(0.09f,0.17f,0.20f);
                labels.GetArrayElementAtIndex(i).objectReferenceValue=Text("Description",card,new Vector2(-95,0),new Vector2(590,115),23,"");
                toggles.GetArrayElementAtIndex(i).objectReferenceValue=Button("Toggle",card,new Vector2(300,0),new Vector2(180,65),"EQUIPAR");
            }
            s.FindProperty("close").objectReferenceValue=Button("Close",panel,new Vector2(0,-465),new Vector2(360,65),"VOLTAR");
            s.FindProperty("open").objectReferenceValue=Button("Open relics",root.transform,new Vector2(0,485),new Vector2(230,55),"RELÍQUIAS");
            s.ApplyModifiedPropertiesWithoutUndo(); panel.gameObject.SetActive(false);
            IntegratedUiSetup.Configure(root);
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath); Object.DestroyImmediate(root);
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);var rect=obj.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=Vector2.one*0.5f;rect.anchoredPosition=position;rect.sizeDelta=size;return rect;}
        private static Text Text(string name,Transform parent,Vector2 pos,Vector2 size,int fontSize,string value)
        {var text=Rect(name,parent,pos,size).gameObject.AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=fontSize;text.text=value;text.color=Color.white;text.alignment=TextAnchor.MiddleLeft;text.raycastTarget=false;return text;}
        private static Button Button(string name,Transform parent,Vector2 pos,Vector2 size,string label)
        {var rect=Rect(name,parent,pos,size);var image=rect.gameObject.AddComponent<Image>();image.color=new Color(0.15f,0.36f,0.38f);var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;Text("Label",rect,Vector2.zero,size,23,label).alignment=TextAnchor.MiddleCenter;return button;}
        public static void Preview()
        {
            EditorSceneManager.OpenScene("Assets/QuackQuack/Scenes/Lake.unity");
            var view=Object.FindAnyObjectByType<RelicLoadoutView>();
            var catalog=AssetDatabase.LoadAssetAtPath<PrototypeRelicCatalog>(CatalogPath); var loadout=catalog.CreateLoadout();
            var ids=new string[10]; var names=new string[10]; var descriptions=new string[10]; var requirements=new string[10];
            for(int i=0;i<10;i++) {var d=catalog.Relics[i];ids[i]=d.Id;names[i]=d.DisplayName;descriptions[i]=d.Description;
                requirements[i]=d.Unlock==RelicUnlock.SurviveSeconds?"Sobreviva "+d.Threshold+" s":"Colete "+d.Threshold+" XP";if(i<3)loadout.Unlock(d.Id);}
            loadout.Equip(ids[0]);view.Bind(loadout,ids,names,descriptions,requirements,id=>false,visible=>{});view.SetVisible(true);
            var camera=Object.FindAnyObjectByType<Camera>();var canvas=view.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            var target=new RenderTexture(1280,720,24);target.Create();camera.targetTexture=target;Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            image.ReadPixels(new UnityEngine.Rect(0,0,1280,720),0,0);image.Apply();Directory.CreateDirectory("Logs");File.WriteAllBytes("Logs/t052-preview.png",image.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);
        }
    }
}
