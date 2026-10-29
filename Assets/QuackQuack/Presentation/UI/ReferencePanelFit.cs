using UnityEngine;

namespace QuackQuack.Presentation.UI
{
    // Fits authored modal content while its parent remains a full safe-area input blocker.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ReferencePanelFit : MonoBehaviour
    {
        private RectTransform rect;
        private void OnEnable() { rect = GetComponent<RectTransform>(); Refresh(); }
        private void LateUpdate() => Refresh();
        private void Refresh()
        {
            if (rect == null || !(rect.parent is RectTransform parent)) return;
            Vector2 available = parent.rect.size, authored = rect.rect.size;
            if (available.x <= 0 || available.y <= 0 || authored.x <= 0 || authored.y <= 0) return;
            float scale = Mathf.Min(1f, Mathf.Min(available.x / authored.x, available.y / authored.y));
            Vector3 target = new Vector3(scale, scale, 1);
            if (rect.localScale != target) rect.localScale = target;
        }
    }
}
