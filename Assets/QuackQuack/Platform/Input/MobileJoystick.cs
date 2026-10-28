using UnityEngine;
using UnityEngine.EventSystems;

namespace QuackQuack.Platform.Input
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler,
        IInitializePotentialDragHandler, ICancelHandler
    {
        [SerializeField] private MobileInputAdapter input;
        [SerializeField] private RectTransform handle;
        private int? pointer;
        private Vector2 direction;
        public void Configure(MobileInputAdapter adapter, RectTransform knob)
        {
            ResetStick(); input = adapter; handle = knob;
        }
        public void OnInitializePotentialDrag(PointerEventData data) => data.useDragThreshold = false;
        public void OnPointerDown(PointerEventData data)
        {
            if (!isActiveAndEnabled || input == null || !input.isActiveAndEnabled || pointer.HasValue || data.button != PointerEventData.InputButton.Left) return;
            pointer = data.pointerId;
            Move(data);
        }
        public void OnDrag(PointerEventData data) { if (pointer == data.pointerId) Move(data); }
        public void OnPointerUp(PointerEventData data) { if (pointer == data.pointerId) ResetStick(); }
        private float Radius => Mathf.Max(1, Mathf.Min(((RectTransform)transform).rect.width, ((RectTransform)transform).rect.height) * 0.5f -
            (handle == null ? 0 : Mathf.Max(handle.rect.width, handle.rect.height) * 0.5f));
        private void Move(PointerEventData data)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, data.position, data.pressEventCamera, out var local)) return;
            direction = Vector2.ClampMagnitude(local / Radius, 1);
            input.SetMove(direction);
            if (handle != null) handle.anchoredPosition = direction * Radius;
        }
        private void ResetStick()
        {
            pointer = null; direction = Vector2.zero;
            if (input != null) input.SetMove(Vector2.zero);
            if (handle != null) handle.anchoredPosition = Vector2.zero;
        }
        public void OnCancel(BaseEventData data) => ResetStick();
        private void OnDisable() => ResetStick();
        private void OnApplicationFocus(bool focused) { if (!focused) ResetStick(); }
        private void OnApplicationPause(bool paused) { if (paused) ResetStick(); }
        private void OnRectTransformDimensionsChange() { if (handle != null) handle.anchoredPosition = direction * Radius; }
    }
}
