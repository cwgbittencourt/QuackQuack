using QuackQuack.Core.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace QuackQuack.Platform.Input
{
    public sealed class MobileInputAdapter : MonoBehaviour, IPlayerInput
    {
        private MobileCommandDevice device;
        private ActionPlayerInput actions;
        internal long Activity => actions?.Activity ?? 0;
        private void Awake() { device = MobileCommandDevice.Create(); actions = new ActionPlayerInput(InputSource.Touch, device); }

        private void Start() => UpgradeUiEventSystem.Ensure(transform);
        public void SetMove(Vector2 value)
        {
            if (isActiveAndEnabled && device != null && device.added) InputState.Change(device.Move, new PlayerCommand(value).Move);
        }
        public void SetQuack(bool pressed)
        {
            if (isActiveAndEnabled && device != null && device.added) InputState.Change(device.Quack, pressed ? 1f : 0f);
        }
        public void SetPause(bool pressed)
        {
            if (isActiveAndEnabled && device != null && device.added) InputState.Change(device.Pause, pressed ? 1f : 0f);
        }
        public void CancelQuack() { if (device != null && device.added) InputState.Change(device.Quack, 0f); actions?.CancelQuack(); }
        public void CancelPause() { if (device != null && device.added) InputState.Change(device.Pause, 0f); actions?.CancelPause(); }
        public PlayerCommand Read()
        {
            if (!isActiveAndEnabled || actions == null) return new PlayerCommand(Vector2.zero, source: InputSource.Touch);
            return actions.Read();
        }
        private void ResetInput() { if (device != null && device.added) InputState.Change(device.Move, Vector2.zero); CancelQuack(); CancelPause(); }
        internal void SetContext(bool gameplay, bool pause) => actions?.SetContext(gameplay, pause);
        private void OnEnable() { ResetInput(); actions?.SetContext(true, true); }
        private void OnDisable() { ResetInput(); actions?.SetContext(false, false); }
        private void OnApplicationFocus(bool focused) { if (!focused) ResetInput(); }
        private void OnApplicationPause(bool paused) { if (paused) ResetInput(); }
        private void OnDestroy() { actions?.Dispose(); if (device != null && device.added) InputSystem.RemoveDevice(device); }
    }
}
