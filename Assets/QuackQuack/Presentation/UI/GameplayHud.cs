using System;
using QuackQuack.Core.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace QuackQuack.Presentation.UI
{
    public sealed class GameplayHud : MonoBehaviour
    {
        [SerializeField] private Text healthLabel, breathLabel, statusLabel, timeLabel, terrainLabel;
        [SerializeField] private Image healthFill, breathFill;
        private Func<HudSnapshot> read;
        public void Bind(Func<HudSnapshot> source) { read = source; }
        private void Update()
        {
            if (read == null) return;
            var value = read();
            healthLabel.text = $"VIDA  {value.Health:0} / {value.MaxHealth:0}";
            breathLabel.text = value.Breath >= value.Capacity ? "FÔLEGO CHEIO • QUACK APRIMORADO" : $"FÔLEGO  {value.Breath:0} / {value.Capacity:0}";
            healthFill.fillAmount = value.Health / value.MaxHealth;
            breathFill.fillAmount = value.Breath / value.Capacity;
            breathFill.color = value.Breath >= value.Capacity ? new Color(0.98f, 0.84f, 0.32f) : new Color(0.29f, 0.78f, 0.86f);
            statusLabel.text = value.Status;
            terrainLabel.text = value.Terrain;
            timeLabel.text = $"{(int)value.Time / 60:00}:{(int)value.Time % 60:00}";
        }
    }
}
