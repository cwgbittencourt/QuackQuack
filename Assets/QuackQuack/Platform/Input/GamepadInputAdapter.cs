using System;
using QuackQuack.Core.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QuackQuack.Platform.Input
{
    public sealed class GamepadInputAdapter : IPlayerInput, IDisposable
    {
        private readonly InputAction move, quack, pause;
        private bool quackEdge, pauseEdge, disposed;

        public GamepadInputAdapter()
        {
            // Stick processing (including deadzone) belongs to the Input System.
            move = new InputAction("Move", InputActionType.Value, "<Gamepad>/leftStick");
            quack = new InputAction("Quack", InputActionType.Button, "<Gamepad>/buttonSouth");
            pause = new InputAction("Pause", InputActionType.Button, "<Gamepad>/start");
            quack.performed += OnQuack;
            pause.performed += OnPause;
            InputSystem.onDeviceChange += OnDeviceChange;
            move.Enable();
            quack.Enable();
            pause.Enable();
        }

        public PlayerCommand Read()
        {
            if (disposed) throw new ObjectDisposedException(nameof(GamepadInputAdapter));
            var command = new PlayerCommand(move.ReadValue<Vector2>(), quackEdge, pauseEdge, InputSource.Gamepad);
            quackEdge = pauseEdge = false;
            return command;
        }

        private void OnQuack(InputAction.CallbackContext context) => quackEdge = true;
        private void OnPause(InputAction.CallbackContext context) => pauseEdge = true;

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (!(device is Gamepad)) return;
            if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected ||
                change == InputDeviceChange.Disabled || change == InputDeviceChange.SoftReset || change == InputDeviceChange.HardReset)
                quackEdge = pauseEdge = false;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            InputSystem.onDeviceChange -= OnDeviceChange;
            quack.performed -= OnQuack;
            pause.performed -= OnPause;
            move.Dispose();
            quack.Dispose();
            pause.Dispose();
            quackEdge = pauseEdge = false;
        }
    }
}
