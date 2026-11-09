using NUnit.Framework;
using QuackQuack.Presentation.UI;
using UnityEditor;
using UnityEngine;

namespace QuackQuack.Tests.EditMode
{
    public class CartoonUiAssetsTests
    {
        [Test] public void ThemeFramesHaveScalableBordersAndReadOnlyTextures()
        {
            var theme=AssetDatabase.LoadAssetAtPath<CartoonUiTheme>("Assets/QuackQuack/Art/LakeCartoon/UI/LakeCartoonTheme.asset");
            Assert.That(theme,Is.Not.Null);
            Assert.That(theme.IsValid,Is.True);
            foreach(var frame in new[]{theme.Panel,theme.Button})
            {Assert.That(frame,Is.Not.Null);Assert.That(frame.border.x,Is.GreaterThan(0));Assert.That(frame.border.y,Is.GreaterThan(0));Assert.That(frame.texture.isReadable,Is.False);}
            Assert.That(theme.Fill,Is.Not.Null);Assert.That(theme.Round,Is.Not.Null);
            Assert.That(theme.Illustrated,Is.True);
            foreach(var icon in new[]{theme.Quack,theme.Joystick,theme.Knob,theme.Heart,theme.Feather,theme.Clock})
                Assert.That(icon,Is.Not.Null);
            Assert.That(theme.DisplayFont,Is.Not.Null);
            Assert.That(theme.DisplayFontLicense,Is.Not.Null);
            StringAssert.Contains("SIL OPEN FONT LICENSE",theme.DisplayFontLicense.text);
        }
        [Test] public void MissingThemeFrameIsRejectedByContentValidation()
        {
            var theme=ScriptableObject.CreateInstance<CartoonUiTheme>();
            try{Assert.That(QuackQuack.Editor.ContentValidationWindow.Validate(new[]{theme}).Errors,Is.GreaterThan(0));}
            finally{Object.DestroyImmediate(theme);}
        }
        [TestCase("MobileControls")] [TestCase("RelicLoadout")] [TestCase("UpgradeSelection")]
        [TestCase("ResultsScreen")] [TestCase("ControlSettings")]
        public void AuthoredUiPrefabKeepsSafeAreaAndThemeBinding(string name)
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/QuackQuack/Prefabs/Prototype/"+name+".prefab");
            Assert.That(root,Is.Not.Null);Assert.That(root.GetComponent<CartoonUiSkin>(),Is.Not.Null);
            Assert.That(root.GetComponent<CartoonUiSkin>().Theme,Is.Not.Null);
            Assert.That(root.GetComponentInChildren<SafeAreaController>(true),Is.Not.Null);
        }
    }
}
