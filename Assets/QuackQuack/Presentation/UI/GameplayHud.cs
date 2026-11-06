using System;
using QuackQuack.Core.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace QuackQuack.Presentation.UI
{
    public sealed class GameplayHud : MonoBehaviour
    {
        [SerializeField] private Text healthLabel, breathLabel, statusLabel, timeLabel, terrainLabel, quackLabel;
        [SerializeField] private Image healthFill, breathFill;
        [SerializeField] private ResourceIconGraphic healthIcon, breathIcon;
        [SerializeField] private Image experienceFill;
        [SerializeField] private Text levelLabel;
        private Func<HudSnapshot> read;
        private int previousSecond = -1, previousLevel = -1;
        private bool visualsReady;
        private void Awake() => HideLegacyText();
        public void Bind(Func<HudSnapshot> source)
        { read = source; previousLevel = previousSecond = -1; InitializeVisuals(); }
        public void Unbind() { read = null; }
        private void LateUpdate()
        {
            if (read == null) return;
            Render(read());
        }

        public void Render(HudSnapshot value)
        {
            InitializeVisuals();
            healthFill.fillAmount = value.Health01;
            breathFill.fillAmount = value.Breath01;
            bool full = value.Capacity > 0 && value.Breath01 >= 1;
            breathFill.color = full ? new Color(0.98f, 0.84f, 0.32f) : new Color(0.29f, 0.78f, 0.86f);
            if (breathIcon != null) breathIcon.color = breathFill.color;
            if (quackLabel != null)
            {
                quackLabel.text = value.QuackState;
                quackLabel.color = value.QuackReady ? new Color(0.98f, 0.84f, 0.32f) : new Color(0.75f, 0.8f, 0.81f);
            }
            if (statusLabel != null) statusLabel.text = value.Status;
            if (experienceFill != null) experienceFill.fillAmount = value.Progress.Fraction;
            if (levelLabel != null && previousLevel != value.Progress.Level)
            { previousLevel = value.Progress.Level; levelLabel.text = $"NÍVEL {previousLevel}"; }
            int second = Math.Max(0, (int)value.Time);
            if (second != previousSecond)
            {
                timeLabel.text = $"{second / 60:00}:{second % 60:00}";
                previousSecond = second;
            }
        }
        private void HideLegacyText()
        {
            if (healthLabel != null) healthLabel.text = "";
            if (breathLabel != null) breathLabel.text = "";
            if (terrainLabel != null) terrainLabel.text = "";
            // Compatibility only for older scenes/fixtures; current scenes have no static footer.
            var controls = statusLabel != null ? statusLabel.transform.parent.Find("Controls") : null;
            if (controls != null && controls.TryGetComponent<Text>(out var text)) text.text = "";
        }
        private void InitializeVisuals()
        {
            if (visualsReady) return;
            HideLegacyText();
            healthIcon = EnsureIcon(healthIcon, healthLabel, ResourceIconKind.Health, new Color(0.91f, 0.37f, 0.4f));
            breathIcon = EnsureIcon(breathIcon, breathLabel, ResourceIconKind.Breath, new Color(0.29f, 0.78f, 0.86f));
            if (experienceFill == null || levelLabel == null) CreateProgressView();
            visualsReady = true;
        }
        private static ResourceIconGraphic EnsureIcon(ResourceIconGraphic icon, Text legacy, ResourceIconKind kind, Color color)
        {
            if (icon == null && legacy != null)
            {
                var rect = UiRect(kind + " icon", legacy.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(24, 24));
                icon = rect.gameObject.AddComponent<ResourceIconGraphic>();
            }
            if (icon != null) icon.Configure(kind, color);
            return icon;
        }
        // One-time compatibility composition for existing scenes; serialized bindings take precedence.
        // Current authored scenes bind these fields; standalone fixtures retain this fallback.
        private void CreateProgressView()
        {
            var safe = healthFill != null ? healthFill.GetComponentInParent<SafeAreaController>() : null;
            var parent = safe != null ? safe.transform : transform;
            var panel = UiRect("Experience progress", parent, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(420, 56));
            var background = panel.gameObject.AddComponent<Image>(); background.color = new Color(0.04f, 0.09f, 0.12f, 0.94f); background.raycastTarget = false;
            if (experienceFill == null)
            {
                var track = UiRect("Experience track", panel, Vector2.zero, Vector2.zero, new Vector2(16, 10), new Vector2(388, 10));
                var trackImage = track.gameObject.AddComponent<Image>(); trackImage.color = new Color(0.15f, 0.22f, 0.25f); trackImage.raycastTarget = false;
                var fill = UiRect("Experience fill", track, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(388, 10));
                experienceFill = fill.gameObject.AddComponent<Image>(); experienceFill.sprite = healthFill != null ? healthFill.sprite : null;
                experienceFill.type = Image.Type.Filled; experienceFill.fillMethod = Image.FillMethod.Horizontal;
                experienceFill.fillOrigin = 0; experienceFill.fillAmount = 0;
                experienceFill.color = new Color(0.98f, 0.84f, 0.32f); experienceFill.raycastTarget = false;
            }
            if (levelLabel == null)
            {
                var label = UiRect("Level", panel, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(388, 24));
                levelLabel = label.gameObject.AddComponent<Text>();
                levelLabel.font = statusLabel != null && statusLabel.font != null ? statusLabel.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                levelLabel.fontSize = 19; levelLabel.color = new Color(0.98f, 0.94f, 0.8f); levelLabel.alignment = TextAnchor.MiddleCenter; levelLabel.raycastTarget = false;
            }
        }
        private static RectTransform UiRect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = parent.gameObject.layer; rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }
        private void OnDestroy() { Unbind(); }
    }
}
