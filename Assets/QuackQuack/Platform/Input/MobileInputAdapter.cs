using QuackQuack.Core.Shared;
using UnityEngine;

namespace QuackQuack.Platform.Input
{
    public sealed class MobileInputAdapter : MonoBehaviour, IPlayerInput
    {
        private Vector2 move;
        private bool quackHeld, pauseHeld, quackEdge, pauseEdge;

        private void Start() => UpgradeUiEventSystem.Ensure(transform);
        public void SetMove(Vector2 value)
        {
            if (isActiveAndEnabled) move = new PlayerCommand(value).Move;
        }
        public void SetQuack(bool pressed)
        {
            if (!isActiveAndEnabled) return;
            quackEdge |= pressed && !quackHeld;
            quackHeld = pressed;
        }
        public void SetPause(bool pressed)
        {
            if (!isActiveAndEnabled) return;
            pauseEdge |= pressed && !pauseHeld;
            pauseHeld = pressed;
        }
        public void CancelQuack() { quackHeld = quackEdge = false; }
        public void CancelPause() { pauseHeld = pauseEdge = false; }
        public PlayerCommand Read()
        {
            if (!isActiveAndEnabled) ResetInput();
            var command = new PlayerCommand(move, quackEdge, pauseEdge, InputSource.Touch);
            quackEdge = pauseEdge = false;
            return command;
        }
        private void ResetInput() { move = Vector2.zero; CancelQuack(); CancelPause(); }
        private void OnEnable() => ResetInput();
        private void OnDisable() => ResetInput();
        private void OnApplicationFocus(bool focused) { if (!focused) ResetInput(); }
        private void OnApplicationPause(bool paused) { if (paused) ResetInput(); }
    }
}
