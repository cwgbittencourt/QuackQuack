using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace QuackQuack.Platform.Input
{
    public static class UpgradeUiEventSystem
    {
        public static void Ensure(Transform parent)
        {
            if (EventSystem.current != null) return;
            var root = new GameObject("Upgrade UI input", typeof(EventSystem), typeof(InputSystemUIInputModule));
            root.transform.SetParent(parent, false);
        }
    }
}
