using UnityEngine;
using UnityEngine.UI;

namespace QuackQuack.Presentation.UI
{
    [CreateAssetMenu(menuName="QuackQuack/Cartoon UI theme")]
    public sealed class CartoonUiTheme : ScriptableObject
    {
        public Sprite Panel, Button, Fill, Round;
        public Sprite Relics, Capsule, Quack, Joystick, Knob, Heart, Feather, Clock;
        public Font DisplayFont;
        public TextAsset DisplayFontLicense;
        public bool Illustrated=>Heart!=null&&Feather!=null&&Relics!=null;
        public bool IsValid=>Panel!=null&&Button!=null&&Fill!=null&&Round!=null&&Panel.border.x>0&&Button.border.x>0&&(DisplayFont==null||DisplayFontLicense!=null);
        public static readonly Color Ink=new Color(.22f,.12f,.065f);
        public static readonly Color Muted=new Color(.38f,.30f,.22f);
        public static readonly Color Teal=new Color(.04f,.43f,.48f);
        public static readonly Color Green=new Color(.12f,.38f,.22f);
        public static readonly Color Warning=new Color(.63f,.19f,.08f);

        public void Apply(GameObject root)
        {
            foreach(var image in root.GetComponentsInChildren<Image>(true))
            {
                if(image.color.a<.01f)continue;
                string name=image.name.ToLowerInvariant();
                bool quackButton=name=="quack"&&image.GetComponent<QuackQuack.Platform.Input.MobileActionButton>()!=null;
                if(name.StartsWith("illustrated "))continue;
                if(image.GetComponentInParent<UpgradeAreaPreview>(true)!=null)
                {
                    image.sprite=Round;image.type=Image.Type.Simple;
                    image.color=name.Contains("current")?new Color(.38f,.8f,.95f):new Color(1,.85f,.35f);
                    continue;
                }
                bool fill=image.type==Image.Type.Filled||name=="health fill";
                if(fill)
                {
                    image.sprite=Fill;
                    if(name=="health fill"||image.transform.parent.name.ToLowerInvariant().Contains("health"))image.color=new Color(.91f,.28f,.24f);
                    else if(image.transform.parent.name.ToLowerInvariant().Contains("experience"))image.color=new Color(.08f,.75f,.78f);
                    continue;
                }
                if(name.Contains("tether")){image.color=new Color(.22f,.12f,.065f,.6f);continue;}
                if(name.Contains("track")){image.sprite=Fill;image.color=Ink;continue;}
                if(Illustrated)
                {
                    Sprite art=name=="open relics"?Relics:quackButton?Quack:name=="joystick"?Joystick:
                        name=="handle"&&image.transform.parent.name=="Joystick"?Knob:
                        name.StartsWith("resource row")||name=="clock"||name=="experience progress"||name=="special health"?Capsule:null;
                    if(art!=null){image.sprite=art;image.type=quackButton||name=="joystick"||name=="handle"?Image.Type.Simple:Image.Type.Sliced;image.color=Color.white;continue;}
                }
                bool round=name.Contains("joystick")||name=="handle"||quackButton;
                image.sprite=round?Round:image.GetComponent<Selectable>()!=null?Button:Panel;
                image.type=round?Image.Type.Simple:Image.Type.Sliced;
                image.pixelsPerUnitMultiplier=image.sprite==Panel?2:1;
                image.color=Color.white;
            }
            foreach(var label in root.GetComponentsInChildren<Text>(true))
            {
                bool overWorld=label.name=="MOVER label"||label.GetComponentInParent<ContextualHintsView>(true)!=null;
                label.color=overWorld?new Color(1,.95f,.79f):Ink;
                if(DisplayFont!=null&&(label.GetComponentInParent<Selectable>(true)!=null||label.name=="Title"||label.name=="Heading"||
                    label.name=="Level"||label.name=="Time"||label.name.StartsWith("Illustrated ")||label.name=="QUACK label"))
                {label.font=DisplayFont;label.fontStyle=FontStyle.Normal;}
                if(overWorld)
                {
                    var outline=label.GetComponent<Outline>()??label.gameObject.AddComponent<Outline>();
                    outline.effectColor=Ink;outline.effectDistance=new Vector2(1,-1);
                }
            }
            foreach(var control in root.GetComponentsInChildren<Selectable>(true))
            {
                var colors=control.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,.94f,.72f);
                colors.selectedColor=new Color(1,.84f,.38f);colors.pressedColor=new Color(.85f,.65f,.35f);
                colors.disabledColor=new Color(.65f,.62f,.57f,.7f);control.colors=colors;
            }
        }
    }
}
