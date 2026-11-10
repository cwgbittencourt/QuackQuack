#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace QuackQuack.Tests.PlayMode
{
    public sealed class InputMonitorOrderingRegressionTests
    {
        private sealed class Probe : IInputStateChangeMonitor
        {
            public int Calls;
            public void NotifyControlStateChanged(InputControl control, double time, InputEventPtr eventPtr, long monitorIndex) { Calls++; }
            public void NotifyTimerExpired(InputControl control, double time, long monitorIndex, int timerIndex) { }
        }
        [Test]
        public void InitialSignalMustNotSortAnotherDevicesAlreadySignalledMonitors()
        {
            var devices = new List<Gamepad>();
            var background = InputSystem.settings.backgroundBehavior;
            var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            Action<InputEventPtr, InputDevice> seed = null;
            try {
                // Keep B's device index beyond the UI runner's listener count.
                // The regression requires device indices and listener indices to differ.
                for (int i = 0; i < 32; i++) devices.Add(InputSystem.AddDevice<Gamepad>());
                var a = InputSystem.AddDevice<Gamepad>(); devices.Add(a);
                var b = InputSystem.AddDevice<Gamepad>(); devices.Add(b);
                var trigger = InputSystem.AddDevice<Gamepad>(); devices.Add(trigger);
                int bIndex = 0; while (InputSystem.devices[bIndex] != b) bIndex++;
                var first = new Probe();
                InputState.AddChangeMonitor(a.leftStick, first, 0);
                for (int i = 1; i <= bIndex; i++) InputState.AddChangeMonitor(a.leftStick, new Probe(), i);
                var high = new Probe(); var dead = new Probe(); var medium = new Probe();
                InputState.AddChangeMonitor(b.leftStick, high, 2L << 48);
                InputState.AddChangeMonitor(b.leftStick, dead, 1L << 48);
                bool seeded = false;
                seed = (evt, device) => {
                    if (device != trigger || seeded) return;
                    seeded = true;
                    // Removal during event processing leaves a hole until compaction.
                    InputState.RemoveChangeMonitor(b.leftStick, dead, 1L << 48);
                    InputState.AddChangeMonitor(b.leftStick, medium, 1L << 48);
                };
                InputSystem.onEvent += seed;
                InputSystem.QueueStateEvent(trigger, new GamepadState { leftStick = Vector2.right });
                InputSystem.Update(); Assert.That(seeded, Is.True);
                var manager = typeof(InputSystem).GetField("s_Manager", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                var type = manager.GetType();
                var signal = type.GetMethod("SignalStateChangeMonitor", BindingFlags.Public | BindingFlags.Instance);
                // Reproduce initial-state checks: B already has a pending bit when A signals.
                signal.Invoke(manager, new object[] { b.leftStick, medium });
                signal.Invoke(manager, new object[] { a.leftStick, first });
                var fire = type.GetMethod("FireStateChangeNotifications", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                Assert.DoesNotThrow(() => fire.Invoke(manager, null));
                Assert.That(medium.Calls, Is.EqualTo(1)); Assert.That(high.Calls, Is.Zero);
            } finally {
                if (seed != null) InputSystem.onEvent -= seed;
                for (int i = devices.Count - 1; i >= 0; i--) if (devices[i].added) InputSystem.RemoveDevice(devices[i]);
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
            }
        }
    }
}
#endif
