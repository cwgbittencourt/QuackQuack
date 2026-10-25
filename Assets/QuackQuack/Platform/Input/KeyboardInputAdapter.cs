using System;
using QuackQuack.Core.Shared;
using UnityEngine.InputSystem;

namespace QuackQuack.Platform.Input
{
    public sealed class KeyboardInputAdapter : IPlayerInput, IDisposable
    {
        private readonly InputAction move, quack, pause;
        private bool quackEdge, pauseEdge;
        public KeyboardInputAdapter()
        {
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            quack = new InputAction("Quack", InputActionType.Button, "<Keyboard>/space");
            pause = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            quack.performed += _ => quackEdge = true;
            pause.performed += _ => pauseEdge = true;
            move.Enable(); quack.Enable(); pause.Enable();
        }
        public PlayerCommand Read()
        {
            var command = new PlayerCommand(move.ReadValue<UnityEngine.Vector2>(), quackEdge, pauseEdge);
            quackEdge = pauseEdge = false;
            return command;
        }
        public void Dispose() { move.Dispose(); quack.Dispose(); pause.Dispose(); }
    }
}
