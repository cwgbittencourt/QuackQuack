using System;
using QuackQuack.Core.Shared;

namespace QuackQuack.Platform.Input
{
    public sealed class KeyboardInputAdapter : IPlayerInput, IDisposable
    {
        private readonly ActionPlayerInput input = new ActionPlayerInput(InputSource.Keyboard);
        internal long Activity => input.Activity;
        internal void SetContext(bool gameplay, bool pause) => input.SetContext(gameplay, pause);
        public PlayerCommand Read() => input.Read();
        public void Dispose() => input.Dispose();
    }
}
