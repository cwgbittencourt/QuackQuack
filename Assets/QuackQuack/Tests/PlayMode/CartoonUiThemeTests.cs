using NUnit.Framework;
using QuackQuack.Presentation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace QuackQuack.Tests.PlayMode
{
    public class CartoonUiThemeTests
    {
        private GameObject root;
        private CartoonUiTheme theme;
        private Texture2D texture;
        private Sprite sprite;
        [SetUp] public void Setup()
        {
            root=new GameObject("Theme tests",typeof(RectTransform),typeof(Canvas));
            texture=new Texture2D(64,64);sprite=Sprite.Create(texture,new Rect(0,0,64,64),Vector2.one*.5f,100);
            theme=ScriptableObject.CreateInstance<CartoonUiTheme>();theme.Panel=theme.Button=theme.Fill=theme.Round=sprite;
        }
        [TearDown] public void Cleanup(){Object.DestroyImmediate(root);Object.DestroyImmediate(theme);Object.DestroyImmediate(sprite);Object.DestroyImmediate(texture);}
        private GameObject Child(string name){var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(root.transform,false);return obj;}
        [Test] public void QuackBindingUsesCapsuleWhileTouchActionKeepsRoundArt()
        {
            theme.Heart=theme.Feather=theme.Relics=theme.Quack=sprite;
            var binding=Child("QUACK");var bindingImage=binding.AddComponent<Image>();binding.AddComponent<Button>();
            var action=Child("QUACK");var actionImage=action.AddComponent<Image>();
            action.AddComponent<QuackQuack.Platform.Input.MobileActionButton>();
            theme.Apply(root);
            Assert.That(bindingImage.type,Is.EqualTo(Image.Type.Sliced));
            Assert.That(actionImage.type,Is.EqualTo(Image.Type.Simple));
        }
        [Test] public void SkinPreservesFillValueLayoutAndRaycastContract()
        {
            var obj=Child("Fill");var image=obj.AddComponent<Image>();image.type=Image.Type.Filled;image.fillAmount=.37f;image.raycastTarget=false;
            var rect=(RectTransform)obj.transform;rect.anchoredPosition=new Vector2(13,17);rect.sizeDelta=new Vector2(284,12);
            theme.Apply(root);theme.Apply(root);
            Assert.That(image.type,Is.EqualTo(Image.Type.Filled));Assert.That(image.fillAmount,Is.EqualTo(.37f));Assert.That(image.raycastTarget,Is.False);
            Assert.That(rect.anchoredPosition,Is.EqualTo(new Vector2(13,17)));Assert.That(rect.sizeDelta,Is.EqualTo(new Vector2(284,12)));
            Assert.That(image.sprite,Is.SameAs(sprite));
        }
        [Test] public void SkinDoesNotReplaceNavigationOrClickBindingsAndKeepsFocusOutline()
        {
            var obj=Child("Action");obj.AddComponent<Image>();var button=obj.AddComponent<Button>();
            var other=Child("Back").AddComponent<Button>();button.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnDown=other};
            int clicks=0;button.onClick.AddListener(()=>clicks++);MenuNavigation.Prepare(button);theme.Apply(root);button.onClick.Invoke();
            Assert.That(clicks,Is.EqualTo(1));Assert.That(button.navigation.selectOnDown,Is.SameAs(other));
            Assert.That(button.GetComponent<Outline>(),Is.Not.Null);Assert.That(button.GetComponent<Outline>().effectDistance.sqrMagnitude,Is.GreaterThan(0));
            Assert.That(button.colors.disabledColor,Is.Not.EqualTo(button.colors.normalColor));
        }
        [Test] public void SkinLeavesInvisibleModalMasksInvisibleAndUsesDarkText()
        {
            var mask=Child("Mask").AddComponent<Image>();mask.color=Color.clear;mask.raycastTarget=true;
            var label=Child("Label").AddComponent<Text>();label.text="RELÍQUIAS";
            var skin=root.AddComponent<CartoonUiSkin>();skin.Configure(theme);
            Assert.That(mask.color.a,Is.Zero);Assert.That(mask.raycastTarget,Is.True);
            Assert.That(label.text,Is.EqualTo("RELÍQUIAS"));Assert.That(label.color,Is.EqualTo(CartoonUiTheme.Ink));
        }
        [Test] public void TouchFeedbackTracksOnePointerAndClearsOnDisconnectOrDisable()
        {
            var obj=Child("QUACK");var image=obj.AddComponent<Image>();var feedback=obj.AddComponent<CartoonTouchFeedback>();
            var first=new UnityEngine.EventSystems.PointerEventData(null){pointerId=1};
            var other=new UnityEngine.EventSystems.PointerEventData(null){pointerId=2};
            feedback.OnPointerDown(first);Assert.That(image.color,Is.Not.EqualTo(Color.white));
            feedback.OnPointerUp(other);Assert.That(image.color,Is.Not.EqualTo(Color.white));
            obj.SetActive(false);Assert.That(image.color,Is.EqualTo(Color.white));
        }
        [Test] public void RadiusPreviewKeepsCircularSpritesAndDistinctCurrentAndNext()
        {
            var preview=Child("Area preview");preview.AddComponent<UpgradeAreaPreview>();
            var current=Child("Current radius").AddComponent<Image>();current.transform.SetParent(preview.transform,false);
            var next=Child("Next radius").AddComponent<Image>();next.transform.SetParent(preview.transform,false);
            preview.SetActive(false);
            theme.Apply(root);Assert.That(current.sprite,Is.SameAs(theme.Round));Assert.That(next.sprite,Is.SameAs(theme.Round));
            Assert.That(current.type,Is.EqualTo(Image.Type.Simple));Assert.That(current.color,Is.Not.EqualTo(next.color));
        }
    }
}
