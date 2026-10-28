#if UNITY_EDITOR
using NUnit.Framework;
using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem;

namespace QuackQuack.Tests.PlayMode
{
    public class UnifiedInputContractTests
    {
        [Test]
        public void AuthoredAssetHasCommonActionsAndThreeBindingGroups()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/QuackQuack/Platform/Input/QuackQuackInput.inputactions");
            Assert.That(asset, Is.Not.Null, "T067 pending: shared input actions asset is missing.");
            var map = asset.FindActionMap("Gameplay", true);
            Assert.That(map.actions.Count, Is.EqualTo(3));
            Assert.That(map.FindAction("Move", true).type, Is.EqualTo(InputActionType.Value));
            Assert.That(map.FindAction("Quack", true).type, Is.EqualTo(InputActionType.Button));
            Assert.That(map.FindAction("Pause", true).type, Is.EqualTo(InputActionType.Button));
            foreach (var action in map.actions)
                foreach (var group in new[] { "Keyboard", "Gamepad", "Touch" })
                {
                    bool found = false;
                    foreach (var binding in action.bindings) if (binding.groups.Split(';').Contains(group)) found = true;
                    Assert.That(found, Is.True, action.name + " needs " + group + " bindings.");
                }
        }
    }
}
#endif
