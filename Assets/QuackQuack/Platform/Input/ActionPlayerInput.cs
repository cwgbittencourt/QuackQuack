using System;
using QuackQuack.Core.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QuackQuack.Platform.Input
{
    // One private runtime clone per consumer. Bindings live only in the authored asset.
    internal sealed class ActionPlayerInput : IPlayerInput, IDisposable
    {
        private static long activityClock;
        private readonly InputActionAsset asset;
        private readonly InputAction move, quack, pause, relics;
        private readonly InputSource source;
        private readonly InputDevice device;
        private bool quackEdge, pauseEdge, relicsEdge, disposed, waitForNeutral;
        public long Activity { get; private set; }
        public ActionPlayerInput(InputSource source, InputDevice device = null)
        {
            this.source = source; this.device = device;
            asset = UnityEngine.Object.Instantiate(PlayerInputConfiguration.Load().Actions);
            asset.bindingMask = InputBinding.MaskByGroup(source.ToString());
            if (device != null) asset.devices = new[] { device };
            var map = asset.FindActionMap("Gameplay", true);
            move = map.FindAction("Move", true); quack = map.FindAction("Quack", true); pause = map.FindAction("Pause", true); relics = map.FindAction("Relics", true);
            move.performed += OnMove; quack.performed += OnQuack; pause.performed += OnPause; relics.performed += OnRelics;
            InputSystem.onDeviceChange += OnDeviceChange;
            map.Enable();
        }
        private void OnMove(InputAction.CallbackContext context)
        { if (context.ReadValue<Vector2>().sqrMagnitude > 0) Activity = ++activityClock; }
        private void OnQuack(InputAction.CallbackContext context) { quackEdge = true; Activity = ++activityClock; }
        private void OnPause(InputAction.CallbackContext context) { pauseEdge = true; Activity = ++activityClock; }
        private void OnRelics(InputAction.CallbackContext context) { relicsEdge = true; Activity = ++activityClock; }
        public PlayerCommand Read()
        {
            if (disposed) throw new ObjectDisposedException(nameof(ActionPlayerInput));
            var value = move.enabled ? move.ReadValue<Vector2>() : Vector2.zero;
            if (value == Vector2.zero) waitForNeutral = false;
            var command = new PlayerCommand(waitForNeutral ? Vector2.zero : value, quackEdge, pauseEdge, source, relicsEdge);
            quackEdge = pauseEdge = relicsEdge = false; return command;
        }
        public void SetContext(bool gameplay, bool allowPause)
        {
            if (disposed) return;
            // Pending edges belong to the context in which they were produced.
            if (move.enabled != gameplay || pause.enabled != allowPause) quackEdge = pauseEdge = relicsEdge = false;
            if (move.enabled != gameplay)
            {
                waitForNeutral = true;
                if (gameplay) { move.Enable(); quack.Enable(); } else { move.Disable(); quack.Disable(); }
                quackEdge = false;
            }
            if (pause.enabled != allowPause) { if (allowPause) pause.Enable(); else pause.Disable(); pauseEdge = false; }
            if (relics.enabled != allowPause) SetRelicsContext(allowPause);
        }
        // Relics is available during play/pause, never during mandatory choice or terminal UI.
        private void SetRelicsContext(bool allowed) { if (allowed) relics.Enable(); else relics.Disable(); relicsEdge = false; }
        public void CancelQuack() => quackEdge = false;
        public void CancelPause() => pauseEdge = false;
        private void OnDeviceChange(InputDevice changed, InputDeviceChange change)
        {
            bool relevant = device != null ? changed == device : source == InputSource.Gamepad ? changed is Gamepad : changed is Keyboard;
            if (relevant && (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected ||
                change == InputDeviceChange.Disabled || change == InputDeviceChange.SoftReset || change == InputDeviceChange.HardReset))
            {
                quackEdge = pauseEdge = relicsEdge = false;
                // Another older held device must not silently resume after the active one disappears.
                waitForNeutral = true;
            }
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            InputSystem.onDeviceChange -= OnDeviceChange;
            move.performed -= OnMove; quack.performed -= OnQuack; pause.performed -= OnPause; relics.performed -= OnRelics;
            asset.Disable(); quackEdge = pauseEdge = relicsEdge = false;
            UnityEngine.Object.Destroy(asset);
        }
    }
}
