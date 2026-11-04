using UnityEngine;

namespace QuackQuack.Core.Shared
{
    public enum InputSource { Keyboard, Gamepad, Touch }
    public readonly struct PlayerCommand
    {
        public Vector2 Move { get; }
        public bool QuackPressed { get; }
        public bool PausePressed { get; }
        public bool RelicsPressed { get; }
        public InputSource Source { get; }
        public PlayerCommand(Vector2 move, bool quackPressed = false, bool pausePressed = false, InputSource source = InputSource.Keyboard, bool relicsPressed = false)
        {
            Move = float.IsNaN(move.x) || float.IsNaN(move.y) || float.IsInfinity(move.x) || float.IsInfinity(move.y)
                ? Vector2.zero : Vector2.ClampMagnitude(move, 1);
            QuackPressed = quackPressed;
            PausePressed = pausePressed;
            RelicsPressed = relicsPressed;
            Source = source;
        }
    }
}
