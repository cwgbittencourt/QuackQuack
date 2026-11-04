using System;
using QuackQuack.Core.Shared;

namespace QuackQuack.Platform.Input
{
    public sealed class GamepadInputAdapter : IPlayerInput, IDisposable
    {
        private readonly ActionPlayerInput input;
        public GamepadInputAdapter(ControlProfileStore profiles = null) => input = new ActionPlayerInput(InputSource.Gamepad, profiles: profiles);
        internal long Activity => input.Activity;
        internal void SetContext(bool gameplay, bool pause) => input.SetContext(gameplay, pause);
        public PlayerCommand Read() => input.Read();
        public void Dispose() => input.Dispose();
    }
}
