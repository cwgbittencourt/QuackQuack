using System;
using System.IO;
using System.Linq;
using QuackQuack.Presentation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace QuackQuack.Editor
{
    public static partial class PolishUiSetup
    {
        public const string ThemeFolder="Assets/QuackQuack/Art/LakeCartoon/UI";
        public const string ThemePath=ThemeFolder+"/LakeCartoonTheme.asset";
        [MenuItem("QuackQuack/Art/Apply T045 cartoon UI skin")]
        public static void BuildTheme()
        {
            EnsureThemeFolder(ThemeFolder);
            var theme=AssetDatabase.LoadAssetAtPath<CartoonUiTheme>(ThemePath);
            if(theme==null){theme=ScriptableObject.CreateInstance<CartoonUiTheme>();AssetDatabase.CreateAsset(theme,ThemePath);}
            theme.Panel=CreateFrame("Panel",128,128,20,6,false,false);
            theme.Button=CreateFrame("Button",128,64,16,5,false,false);
            theme.Fill=CreateFrame("Fill",64,32,12,0,false,true);
            theme.Round=CreateFrame("Round",128,128,63,6,true,false);
            LoadIllustratedTheme(theme);
            EditorUtility.SetDirty(theme);
            foreach(string path in new[]{MobileControlsSetup.PrefabPath,RelicSetup.PrefabPath,UpgradeSelectionSetup.PrefabPath,ResultsSetup.PrefabPath,ProfilePath})
            {
                var root=PrefabUtility.LoadPrefabContents(path);
                try{ApplySkin(root,theme);PrefabUtility.SaveAsPrefabAsset(root,path);}
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            foreach(string name in new[]{"Lake","LakeRun"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/QuackQuack/Scenes/"+name+".unity");
                foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    if(canvas.rootCanvas==canvas)ApplySkin(canvas.gameObject,theme);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();ContentValidationWindow.ValidateForCI();
            Debug.Log("T045_UI_THEME_OK prefabs=5 scenes=2");
        }
        private static void ApplySkin(GameObject root,CartoonUiTheme theme)
        {
            ApplyIllustratedLayout(root,theme);
            var rects=root.GetComponentsInChildren<RectTransform>(true);
            string geometry=LayoutSignature(rects);
            var controls=root.GetComponentsInChildren<Selectable>(true);
            var navigation=controls.Select(s=>s.navigation).ToArray();
            var raycasts=root.GetComponentsInChildren<Graphic>(true).Select(g=>g.raycastTarget).ToArray();
            foreach(var action in root.GetComponentsInChildren<QuackQuack.Platform.Input.MobileActionButton>(true))
                if(action.GetComponent<CartoonTouchFeedback>()==null)action.gameObject.AddComponent<CartoonTouchFeedback>();
            foreach(var joystick in root.GetComponentsInChildren<QuackQuack.Platform.Input.MobileJoystick>(true))
                if(joystick.GetComponent<CartoonTouchFeedback>()==null)joystick.gameObject.AddComponent<CartoonTouchFeedback>();
            var skin=root.GetComponent<CartoonUiSkin>()??root.AddComponent<CartoonUiSkin>();skin.Configure(theme);
            if(geometry!=LayoutSignature(rects)||!navigation.SequenceEqual(controls.Select(s=>s.navigation))||
                !raycasts.SequenceEqual(root.GetComponentsInChildren<Graphic>(true).Select(g=>g.raycastTarget)))
                throw new InvalidOperationException("Skin changed layout, focus graph or raycast targets: "+root.name);
        }
        public static void RefreshExistingTheme(GameObject root)
        {
            var skin=root.GetComponent<CartoonUiSkin>();
            if(skin==null||skin.Theme==null)return;
            ApplyIllustratedLayout(root,skin.Theme);skin.Apply();
        }
        private static string LayoutSignature(RectTransform[] rects)=>string.Join("|",rects.Select(r=>
            r.GetInstanceID()+":"+r.anchorMin.ToString("R")+r.anchorMax.ToString("R")+r.pivot.ToString("R")+r.anchoredPosition.ToString("R")+r.sizeDelta.ToString("R")+r.localScale.ToString("R")));

        // Native uGUI artwork, kept in source as deterministic rounded-rectangle geometry.
        private static Sprite CreateFrame(string name,int width,int height,float radius,float stroke,bool circle,bool fill)
        {
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);var pixels=new Color[width*height];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                float px=x+.5f-width/2f,py=y+.5f-height/2f;
                float dx=Mathf.Max(Mathf.Abs(px)-(width/2f-radius),0),dy=Mathf.Max(Mathf.Abs(py)-(height/2f-radius),0);
                float distance=circle?new Vector2(px,py).magnitude-width/2f:Mathf.Sqrt(dx*dx+dy*dy)-radius;
                Color color=fill?Color.white:distance>-stroke?CartoonUiTheme.Ink:
                    Color.Lerp(new Color(.94f,.78f,.48f),new Color(1,.94f,.76f),(float)y/height);
                color.a=Mathf.Clamp01(-distance);pixels[y*width+x]=color;
            }
            texture.SetPixels(pixels);texture.Apply();string path=ThemeFolder+"/"+name+".png";
            File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100;
            importer.spriteBorder=circle||fill?Vector4.zero:new Vector4(24,24,24,24);
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static void EnsureThemeFolder(string path)
        {if(!AssetDatabase.IsValidFolder(path)){string parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureThemeFolder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));}}
    }
}
