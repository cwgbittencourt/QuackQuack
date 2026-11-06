using System;
using QuackQuack.Core.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace QuackQuack.Presentation.UI
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SpecialEnemyBarsView : MonoBehaviour
    {
        [SerializeField] private Vector2 barSize = new Vector2(190, 44);
        [SerializeField] private float gap = 8;
        private sealed class Bar
        {
            public RectTransform Root, Fill, Tether;
            public Text Label;
            public int Instance;
            public long Lease;
            public string Name;
            public SpecialEnemyCategory Category;
            public Rect Bounds;
        }
        private Bar[] bars = Array.Empty<Bar>();
        private SpecialEnemyRegistry registry;
        private Camera worldCamera;
        private RectTransform area;
        private Canvas canvas;
        public int VisibleCount { get; private set; }
        public int HiddenForSpace { get; private set; }

        // The container is the safe-area overlay authored by T038. Prewarm outside combat.
        public void Bind(SpecialEnemyRegistry source, Camera camera, RectTransform container)
        {
            if (source == null || camera == null || container == null || container.GetComponentInParent<Canvas>() == null)
                throw new ArgumentException("Registry, world camera and safe canvas container required.");
            if (barSize.x <= 0 || barSize.y <= 0 || float.IsNaN(barSize.x) || float.IsNaN(barSize.y) ||
                float.IsInfinity(barSize.x) || float.IsInfinity(barSize.y) || gap < 0 || float.IsNaN(gap) || float.IsInfinity(gap))
                throw new InvalidOperationException("Invalid special bar layout.");
            Unbind(); registry = source; worldCamera = camera; area = container; canvas = container.GetComponentInParent<Canvas>();
            if (bars.Length < source.Capacity)
            {
                int previous = bars.Length; Array.Resize(ref bars, source.Capacity);
                for (int i = previous; i < bars.Length; i++) bars[i] = CreateBar();
            }
            foreach (var bar in bars)
            { bar.Root.SetParent(area, false); bar.Tether.SetParent(area, false); bar.Tether.SetAsFirstSibling(); }
            Refresh();
        }
        public void Unbind() { registry = null; worldCamera = null; Hide(); }
        private void LateUpdate() => Refresh();
        private void OnDisable() => Hide();
        private void OnDestroy()
        {
            // Bars may live under a separate safe container, so ownership stays explicit.
            foreach (var bar in bars)
            { if (bar.Root != null) Destroy(bar.Root.gameObject); if (bar.Tether != null) Destroy(bar.Tether.gameObject); }
        }
        private void Hide()
        {
            VisibleCount = HiddenForSpace = 0;
            foreach (var bar in bars)
            { if (bar.Root != null) bar.Root.gameObject.SetActive(false); if (bar.Tether != null) bar.Tether.gameObject.SetActive(false); }
        }
        public void Refresh()
        {
            if (!isActiveAndEnabled || registry == null || worldCamera == null || area == null || canvas == null) { Hide(); return; }
            VisibleCount = HiddenForSpace = 0;
            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Rect limits = area.rect;
            for (int i = 0; i < registry.Count; i++)
            {
                var snapshot = registry[i];
                var projected = worldCamera.WorldToViewportPoint(snapshot.Anchor);
                if (projected.z <= 0 || projected.x < 0 || projected.x > 1 || projected.y < 0 || projected.y > 1) continue;
                var screen = worldCamera.WorldToScreenPoint(snapshot.Anchor);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, uiCamera, out var anchor) || !limits.Contains(anchor)) continue;
                var position = anchor + Vector2.up * (gap + barSize.y / 2);
                Rect bounds = new Rect(position - barSize / 2, barSize);
                // Deterministic upward lanes; every displaced bar retains a tether to its own anchor.
                for (int attempt = 0; attempt < VisibleCount; attempt++)
                {
                    bool moved = false;
                    for (int j = 0; j < VisibleCount; j++)
                        if (bounds.Overlaps(bars[j].Bounds))
                        { bounds.y = bars[j].Bounds.yMax + gap; moved = true; }
                    if (!moved) break;
                }
                if (bounds.xMin < limits.xMin || bounds.xMax > limits.xMax || bounds.yMax > limits.yMax)
                { HiddenForSpace++; continue; }
                var bar = bars[VisibleCount++]; bar.Bounds = bounds;
                bar.Root.anchoredPosition = bounds.center - limits.center;
                if (bar.Instance != snapshot.InstanceId || bar.Lease != snapshot.LeaseId || bar.Name != snapshot.DisplayName || bar.Category != snapshot.Category)
                {
                    bar.Instance = snapshot.InstanceId; bar.Lease = snapshot.LeaseId; bar.Name = snapshot.DisplayName; bar.Category = snapshot.Category;
                    bar.Label.text = (snapshot.Category == SpecialEnemyCategory.Boss ? "CHEFE · " : "MINIBOSS · ") + snapshot.DisplayName;
                }
                bar.Fill.anchorMax = new Vector2(snapshot.Health01, 1);
                float distance = bounds.yMin - anchor.y;
                bar.Tether.anchoredPosition = new Vector2(anchor.x, bounds.yMin) - limits.center;
                bar.Tether.sizeDelta = new Vector2(2, distance);
                bar.Root.gameObject.SetActive(true); bar.Tether.gameObject.SetActive(true);
            }
            for (int i = VisibleCount; i < bars.Length; i++)
            { bars[i].Root.gameObject.SetActive(false); bars[i].Tether.gameObject.SetActive(false); }
        }
        public bool TryGetBar(int instanceId, long leaseId, out RectTransform root)
        {
            for (int i = 0; i < VisibleCount; i++) if (bars[i].Instance == instanceId && bars[i].Lease == leaseId)
            { root = bars[i].Root; return true; }
            root = null; return false;
        }
        private Bar CreateBar()
        {
            var root = Rect("Special health", area, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, barSize);
            Graphic(root, new Color(0.04f, 0.09f, 0.12f, 0.95f));
            var textRect = Rect("Name", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(barSize.x - 8, 30));
            var text = textRect.gameObject.AddComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 15; text.resizeTextForBestFit = true; text.resizeTextMinSize = 11; text.resizeTextMaxSize = 15;
            text.alignment = TextAnchor.MiddleCenter; text.color = Color.white; text.raycastTarget = false;
            var track = Rect("Health track", root, Vector2.zero, Vector2.zero, new Vector2(4, 4), new Vector2(barSize.x - 8, 8));
            Graphic(track, new Color(0.2f, 0.25f, 0.28f));
            var fill = Rect("Health fill", track, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            fill.anchorMax = Vector2.one; fill.offsetMin = fill.offsetMax = Vector2.zero;
            Graphic(fill, new Color(0.96f, 0.46f, 0.26f));
            var tether = Rect("Anchor tether", area, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1), Vector2.zero, new Vector2(2, gap));
            Graphic(tether, new Color(1, 1, 1, 0.5f));
            tether.SetAsFirstSibling(); root.gameObject.SetActive(false); tether.gameObject.SetActive(false);
            return new Bar { Root = root, Fill = fill, Label = text, Tether = tether };
        }
        private static void Graphic(RectTransform rect, Color color)
        { var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; }
        private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.gameObject.layer = parent.gameObject.layer;
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
    }
}
