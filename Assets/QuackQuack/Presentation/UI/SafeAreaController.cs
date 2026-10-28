using System;
using UnityEngine;

namespace QuackQuack.Presentation.UI
{
    // Attach to a stretched child of a full-screen Canvas, with safe content below it.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaController : MonoBehaviour
    {
        private RectTransform root;
        private Func<Vector2> viewportProvider;
        private Func<Rect> safeAreaProvider;
        private Vector2 previousViewport;
        private Rect previousSafeArea;
        private bool applied;

        // Null providers restore the corresponding Screen source.
        public void SetViewportProvider(Func<Vector2> viewport, Func<Rect> safeArea)
        {
            viewportProvider = viewport;
            safeAreaProvider = safeArea;
            applied = false;
            Refresh();
        }

        private void OnEnable()
        {
            root = GetComponent<RectTransform>();
            applied = false;
            Refresh();
        }

        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            if (root == null) root = GetComponent<RectTransform>();
            var viewport = viewportProvider != null ? viewportProvider() : new Vector2(Screen.width, Screen.height);
            var safe = safeAreaProvider != null ? safeAreaProvider() : Screen.safeArea;
            // Screen geometry may be transiently unavailable during startup/resizing.
            // Keep the last valid layout until both sources become usable again.
            if (!Finite(viewport.x) || !Finite(viewport.y) || viewport.x <= 0 || viewport.y <= 0 ||
                !Finite(safe.x) || !Finite(safe.y) || !Finite(safe.width) || !Finite(safe.height) ||
                !Finite(safe.xMax) || !Finite(safe.yMax) || safe.width <= 0 || safe.height <= 0)
                return;

            var min = new Vector2(Mathf.Clamp01(safe.xMin / viewport.x), Mathf.Clamp01(safe.yMin / viewport.y));
            var max = new Vector2(Mathf.Clamp01(safe.xMax / viewport.x), Mathf.Clamp01(safe.yMax / viewport.y));
            if (max.x <= min.x || max.y <= min.y) return;
            if (applied && viewport.Equals(previousViewport) && safe.Equals(previousSafeArea)) return;

            root.anchorMin = min;
            root.anchorMax = max;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            previousViewport = viewport;
            previousSafeArea = safe;
            applied = true;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
