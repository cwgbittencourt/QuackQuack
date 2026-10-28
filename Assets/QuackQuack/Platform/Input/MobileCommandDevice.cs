using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace QuackQuack.Platform.Input
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MobileCommandState : IInputStateTypeInfo
    {
        public FourCC format => new FourCC('Q', 'M', 'O', 'B');
        [InputControl(layout = "Vector2")] public Vector2 move;
        [InputControl(layout = "Button", format = "FLT")] public float quack;
        [InputControl(layout = "Button", format = "FLT")] public float pause;
    }
    [InputControlLayout(stateType = typeof(MobileCommandState))]
    public sealed class MobileCommandDevice : InputDevice
    {
        public Vector2Control Move { get; private set; }
        public ButtonControl Quack { get; private set; }
        public ButtonControl Pause { get; private set; }
        protected override void FinishSetup()
        {
            base.FinishSetup(); Move = GetChildControl<Vector2Control>("move");
            Quack = GetChildControl<ButtonControl>("quack"); Pause = GetChildControl<ButtonControl>("pause");
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void RegisterLayout()
        {
            if (!InputSystem.ListLayouts().Contains("MobileCommand")) InputSystem.RegisterLayout<MobileCommandDevice>("MobileCommand");
        }
        public static MobileCommandDevice Create()
        {
            RegisterLayout();
            return (MobileCommandDevice)InputSystem.AddDevice("MobileCommand");
        }
    }
}
