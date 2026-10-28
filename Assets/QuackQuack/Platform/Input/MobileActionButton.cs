using UnityEngine;
using UnityEngine.EventSystems;

namespace QuackQuack.Platform.Input
{
    public enum MobileAction { Quack, Pause }
    public sealed class MobileActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, ICancelHandler
    {
        [SerializeField] private MobileInputAdapter input;
        [SerializeField] private MobileAction action;
        private int? pointer;
        public void Configure(MobileInputAdapter adapter, MobileAction value) { Cancel(); input = adapter; action = value; }
        public void OnPointerDown(PointerEventData data)
        {
            if (!isActiveAndEnabled || input == null || !input.isActiveAndEnabled || pointer.HasValue || data.button != PointerEventData.InputButton.Left) return;
            pointer = data.pointerId;
            SetPressed(true);
        }
        public void OnPointerUp(PointerEventData data)
        {
            if (pointer != data.pointerId) return;
            pointer = null; SetPressed(false);
        }
        private void SetPressed(bool pressed)
        {
            if (input == null) return;
            if (action == MobileAction.Quack) input.SetQuack(pressed); else input.SetPause(pressed);
        }
        private void Cancel()
        {
            pointer = null;
            if (input == null) return;
            if (action == MobileAction.Quack) input.CancelQuack(); else input.CancelPause();
        }
        public void OnCancel(BaseEventData data) => Cancel();
        private void OnDisable() => Cancel();
        private void OnApplicationFocus(bool focused) { if (!focused) Cancel(); }
        private void OnApplicationPause(bool paused) { if (paused) Cancel(); }
    }
}
