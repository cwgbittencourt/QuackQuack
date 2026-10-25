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
        private Func<HudSnapshot> read;
        private int previousHealth = -1, previousMaxHealth = -1, previousBreath = -1, previousCapacity = -1, previousSecond = -1;
        public void Bind(Func<HudSnapshot> source) { read = source; }
        public void Unbind() { read = null; }
        private void LateUpdate()
        {
            if (read == null) return;
            Render(read());
        }

        public void Render(HudSnapshot value)
        {
            int health = Mathf.RoundToInt(value.Health), maxHealth = Mathf.RoundToInt(value.MaxHealth);
            int breath = Mathf.RoundToInt(value.Breath), capacity = Mathf.RoundToInt(value.Capacity);
            if (health != previousHealth || maxHealth != previousMaxHealth)
            {
                healthLabel.text = $"VIDA  {health} / {maxHealth}";
                previousHealth = health; previousMaxHealth = maxHealth;
            }
            if (breath != previousBreath || capacity != previousCapacity)
            {
                breathLabel.text = $"FÔLEGO  {breath} / {capacity}";
                previousBreath = breath; previousCapacity = capacity;
            }
            healthFill.fillAmount = Ratio(value.Health, value.MaxHealth);
            breathFill.fillAmount = Ratio(value.Breath, value.Capacity);
            bool full = value.Capacity > 0 && value.Breath >= value.Capacity;
            breathFill.color = full ? new Color(0.98f, 0.84f, 0.32f) : new Color(0.29f, 0.78f, 0.86f);
            if (quackLabel != null)
            {
                quackLabel.text = value.QuackState;
                quackLabel.color = value.QuackReady ? new Color(0.98f, 0.84f, 0.32f) : new Color(0.75f, 0.8f, 0.81f);
            }
            statusLabel.text = value.Status;
            terrainLabel.text = value.Terrain;
            int second = Math.Max(0, (int)value.Time);
            if (second != previousSecond)
            {
                timeLabel.text = $"{second / 60:00}:{second % 60:00}";
                previousSecond = second;
            }
        }
        private static float Ratio(float current, float maximum) => maximum > 0 ? Mathf.Clamp01(current / maximum) : 0;
        private void OnDestroy() { Unbind(); }
    }
}
