using System;
using System.IO;
using System.Linq;
using QuackQuack.Presentation.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace QuackQuack.Editor
{
    public static partial class PolishUiSetup
    {
        private static void LoadIllustratedTheme(CartoonUiTheme theme)
        {
            string path=ThemeFolder+"/IllustratedHud.png";
            if(!File.Exists(path))throw new InvalidOperationException("Missing illustrated T045 HUD atlas");
            string[] names={"ui-relics","ui-capsule","ui-quack","ui-joystick","ui-knob","ui-heart","ui-feather","ui-clock","ui-panel"};
            PolishArtSetup.ImportTrimmed(path,names,3,3,(name,rect)=>
            {
                if(name!="ui-relics"&&name!="ui-capsule"&&name!="ui-panel")return Vector4.zero;
                float x=name=="ui-relics"?Mathf.Min(100,rect.width*.3f):name=="ui-capsule"?Mathf.Min(72,rect.width*.25f):64;
                float y=name=="ui-capsule"?20:name=="ui-panel"?64:Mathf.Min(40,rect.height*.3f);return new Vector4(x,y,x,y);
            });
            var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            Sprite Get(string name)=>sprites.Single(s=>s.name==name);
            theme.Relics=Get("ui-relics");theme.Capsule=Get("ui-capsule");theme.Quack=Get("ui-quack");
            theme.Joystick=Get("ui-joystick");theme.Knob=Get("ui-knob");theme.Heart=Get("ui-heart");
            theme.Feather=Get("ui-feather");theme.Clock=Get("ui-clock");theme.Panel=Get("ui-panel");theme.Button=theme.Capsule;
            theme.DisplayFont=AssetDatabase.LoadAssetAtPath<Font>(ThemeFolder+"/Fonts/LilitaOne-Regular.ttf");
            theme.DisplayFontLicense=AssetDatabase.LoadAssetAtPath<TextAsset>(ThemeFolder+"/Fonts/OFL.txt");
            if(theme.DisplayFont==null)throw new InvalidOperationException("Missing licensed display font");
        }
        private static RectTransform VisualRect(string name,Transform parent,Vector2 position,Vector2 size)
        {
            var found=parent.Find(name);var obj=found!=null?found.gameObject:new GameObject(name,typeof(RectTransform));
            obj.transform.SetParent(parent,false);var rect=(RectTransform)obj.transform;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
        }
        private static Image IllustratedImage(string name,Transform parent,Sprite sprite,Vector2 position,Vector2 size)
        {
            var rect=VisualRect(name,parent,position,size);var image=rect.GetComponent<Image>()??rect.gameObject.AddComponent<Image>();
            image.sprite=sprite;image.color=Color.white;image.type=Image.Type.Simple;image.preserveAspect=true;image.raycastTarget=false;return image;
        }
        private static Text IllustratedText(string name,Transform parent,string value,Vector2 position,Vector2 size,CartoonUiTheme theme,int fontSize=24)
        {
            var rect=VisualRect(name,parent,position,size);var text=rect.GetComponent<Text>()??rect.gameObject.AddComponent<Text>();
            text.font=theme.DisplayFont;text.fontSize=fontSize;text.text=value;text.color=CartoonUiTheme.Ink;
            text.alignment=TextAnchor.MiddleLeft;text.raycastTarget=false;return text;
        }
        private static void ApplyIllustratedLayout(GameObject root,CartoonUiTheme theme)
        {
            if(!theme.Illustrated)return;
            if(root.GetComponent<ResultsView>()!=null)
                foreach(var scroll in root.GetComponentsInChildren<ScrollRect>(true))
                {
                    scroll.viewport.offsetMin=new Vector2(44,40);
                    scroll.viewport.offsetMax=new Vector2(-62,-40);
                    var bar=scroll.verticalScrollbar.GetComponent<RectTransform>();
                    bar.anchoredPosition=new Vector2(-36,0);bar.sizeDelta=new Vector2(14,-80);
                }
            if(root.GetComponent<UpgradeSelectionView>()!=null)
            foreach(var card in root.GetComponentsInChildren<RectTransform>(true))
            {
                if(!card.name.StartsWith("Card ")||card.GetComponent<Image>()==null)continue;
                string[] names={"Type","Title","Current","Next","Active effects","Synergy","Requirement"};
                float[] top={-34,-66,-120,-200,-382,-452,-510};
                float[] height={26,48,72,72,64,52,68};
                for(int i=0;i<names.Length;i++)
                {
                    var label=card.Find(names[i]) as RectTransform;if(label==null)continue;
                    label.anchoredPosition=new Vector2(44,top[i]);label.sizeDelta=new Vector2(372,height[i]);
                }
                var preview=card.Find("Area preview") as RectTransform;
                if(preview!=null)
                {
                    preview.anchoredPosition=new Vector2(44,-278);preview.sizeDelta=new Vector2(372,100);
                    ((RectTransform)preview.Find("Current radius")).anchoredPosition=new Vector2(93,-36);
                    ((RectTransform)preview.Find("Next radius")).anchoredPosition=new Vector2(279,-36);
                    ((RectTransform)preview.Find("Radius comparison")).sizeDelta=new Vector2(372,24);
                }
            }
            var hud=root.GetComponent<GameplayHud>();
            if(hud!=null)
            {
                var safe=root.transform.Find("HUD Safe Area");
                var group=(RectTransform)safe.Find("Player vitals");group.sizeDelta=new Vector2(560,148);
                group.GetComponent<Image>().enabled=false;
                var rows=new RectTransform[2];var tracks=new RectTransform[2];var fills=new RectTransform[2];
                var data=new SerializedObject(hud);
                for(int i=0;i<2;i++)
                {
                    var row=VisualRect("Resource row "+i,group,new Vector2(0,-56*i),new Vector2(560,48));row.SetAsFirstSibling();
                    var background=row.GetComponent<Image>()??row.gameObject.AddComponent<Image>();background.sprite=theme.Capsule;background.type=Image.Type.Sliced;background.raycastTarget=false;background.color=Color.white;
                    var legacy=(ResourceIconGraphic)data.FindProperty(i==0?"healthIcon":"breathIcon").objectReferenceValue;
                    legacy.enabled=false;legacy.color=Color.white;
                    var icon=(RectTransform)legacy.transform;icon.anchoredPosition=new Vector2(8,-56*i-4);icon.sizeDelta=new Vector2(40,40);
                    IllustratedImage(i==0?"Illustrated heart":"Illustrated feather",icon,i==0?theme.Heart:theme.Feather,Vector2.zero,new Vector2(40,40));
                    IllustratedText("Illustrated resource name "+i,group,i==0?"VIDA":"FÔLEGO",new Vector2(54,-56*i-8),new Vector2(88,32),theme,21);
                    var fill=(Image)data.FindProperty(i==0?"healthFill":"breathFill").objectReferenceValue;
                    var track=(RectTransform)fill.transform.parent;track.anchoredPosition=new Vector2(144,-56*i-16);track.sizeDelta=new Vector2(400,16);
                    fill.rectTransform.sizeDelta=track.sizeDelta;rows[i]=row;tracks[i]=track;fills[i]=fill.rectTransform;
                }
                var status=(Text)data.FindProperty("quackLabel").objectReferenceValue;status.rectTransform.anchoredPosition=new Vector2(12,-112);
                status.rectTransform.sizeDelta=new Vector2(536,26);
                var outline=status.GetComponent<Outline>()??status.gameObject.AddComponent<Outline>();outline.effectColor=CartoonUiTheme.Ink;outline.effectDistance=new Vector2(1,-1);
                var fit=group.GetComponent<CartoonResourceLayout>()??group.gameObject.AddComponent<CartoonResourceLayout>();fit.Group=group;fit.Rows=rows;fit.Tracks=tracks;fit.Fills=fills;
                var progress=(RectTransform)safe.Find("Experience progress");progress.sizeDelta=new Vector2(640,56);
                var level=(Text)data.FindProperty("levelLabel").objectReferenceValue;level.rectTransform.anchoredPosition=new Vector2(16,-8);level.rectTransform.sizeDelta=new Vector2(120,36);level.alignment=TextAnchor.MiddleLeft;
                var xp=(Image)data.FindProperty("experienceFill").objectReferenceValue;var xpTrack=(RectTransform)xp.transform.parent;
                xpTrack.anchoredPosition=new Vector2(148,-20);xpTrack.sizeDelta=new Vector2(472,16);xp.rectTransform.sizeDelta=xpTrack.sizeDelta;
                var clock=safe.Find("Clock") as RectTransform;
                if(clock!=null)
                {
                    clock.sizeDelta=new Vector2(170,64);IllustratedImage("Illustrated clock",clock,theme.Clock,new Vector2(8,-8),new Vector2(48,48));
                    var time=(Text)data.FindProperty("timeLabel").objectReferenceValue;time.rectTransform.anchoredPosition=new Vector2(60,-12);time.rectTransform.sizeDelta=new Vector2(100,40);
                }
            }
            foreach(var image in root.GetComponentsInChildren<Image>(true))
            {
                if(image.name=="Open relics")((RectTransform)image.transform).sizeDelta=new Vector2(270,76);
                if(image.name=="QUACK"&&image.GetComponent<QuackQuack.Platform.Input.MobileActionButton>()!=null)
                {
                    ((RectTransform)image.transform).sizeDelta=new Vector2(240,240);
                    var label=image.GetComponentInChildren<Text>();var rect=label.rectTransform;
                    rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,0);rect.anchoredPosition=new Vector2(0,38);rect.sizeDelta=new Vector2(220,52);label.fontSize=34;label.alignment=TextAnchor.MiddleCenter;
                }
                if(image.name=="Open settings")
                {
                    ((RectTransform)image.transform).sizeDelta=new Vector2(220,48);
                    var text=image.GetComponentInChildren<Text>();text.rectTransform.sizeDelta=new Vector2(220,48);text.fontSize=22;
                }
            }
            foreach(var text in root.GetComponentsInChildren<Text>(true))if(text.name=="MOVER label")text.text="";
        }
    }
}
