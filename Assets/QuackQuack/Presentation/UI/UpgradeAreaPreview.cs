using System.Globalization;
using QuackQuack.Core.Shared;
using QuackQuack.Presentation.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace QuackQuack.Presentation.UI
{
    public sealed class UpgradeAreaPreview : MonoBehaviour
    {
        [SerializeField] private Image current, next;
        [SerializeField] private Text caption;
        public void Render(UpgradeCardSnapshot card)
        {
            bool visible = card != null && card.HasAreaPreview;
            gameObject.SetActive(visible);
            if (!visible) return;
            float maxRadius = Mathf.Max(card.CurrentRadius, card.NextRadius);
            current.rectTransform.sizeDelta = Vector2.one * WeaponVisuals.PreviewDiameter(card.CurrentRadius, maxRadius, 72);
            next.rectTransform.sizeDelta = Vector2.one * WeaponVisuals.PreviewDiameter(card.NextRadius, maxRadius, 72);
            caption.text = "RAIO: " + card.CurrentRadius.ToString("0.###", CultureInfo.InvariantCulture) +
                " → " + card.NextRadius.ToString("0.###", CultureInfo.InvariantCulture) + " u (atual → próximo)";
        }
    }
}
