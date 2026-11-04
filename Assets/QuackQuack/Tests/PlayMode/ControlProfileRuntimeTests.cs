using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using QuackQuack.Platform.Input;
using QuackQuack.Presentation.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.PlayMode
{
    public sealed class ControlProfileRuntimeTests
    {
        private string directory;
        private ControlProfileStore store;
        private Gamepad pad;
        private GamepadInputAdapter input;
        private GameObject root;
        private Gamepad previousPad;
        private InputSettings.BackgroundBehavior background;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode routing;
#endif
        [SetUp] public void SetUp() {
            background = InputSystem.settings.backgroundBehavior; InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            routing = InputSystem.settings.editorInputBehaviorInPlayMode; InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            previousPad = Gamepad.current; directory = Path.Combine(Application.temporaryCachePath, "ProfileTests", Guid.NewGuid().ToString("N")); store = new ControlProfileStore(directory); pad = InputSystem.AddDevice<Gamepad>(); }
        [UnityTearDown] public IEnumerator TearDown() { input?.Dispose(); if (root != null) Object.Destroy(root); InputSystem.RemoveDevice(pad);
            if (previousPad != null && previousPad.added) previousPad.MakeCurrent();
            InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = routing;
#endif
            yield return null; if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        private void State(params GamepadButton[] buttons) { var state = new GamepadState(); foreach (var button in buttons) { if (button == GamepadButton.RightTrigger) state.rightTrigger = 1; else if (button == GamepadButton.LeftTrigger) state.leftTrigger = 1; else state = state.WithButton(button); } InputSystem.QueueStateEvent(pad, state); InputSystem.Update(); }
        [Test] public void RuntimeRebindWorksWithoutSelectAndKeepsAuthoredAsset()
        {
            string authored = PlayerInputConfiguration.Load().Actions.ToJson();
            input = new GamepadInputAdapter(store); State(GamepadButton.Select);
            Assert.That(new ControlProfile().TryAssign(ProfileAction.Relics, "<Gamepad>/rightShoulder", false, out var next), Is.True);
            Assert.That(store.TrySave(next, false, false, out _), Is.True);
            Assert.That(input.Read().RelicsPressed, Is.False, "old edge cleared on rebind");
            State(); input.Read(); State(GamepadButton.Select); Assert.That(input.Read().RelicsPressed, Is.False);
            State(); input.Read(); State(GamepadButton.RightShoulder); Assert.That(input.Read().RelicsPressed, Is.True);
            Assert.That(input.Read().RelicsPressed, Is.False); Assert.That(PlayerInputConfiguration.Load().Actions.ToJson(), Is.EqualTo(authored));
        }
        [TestCase("Gamepad", ControllerFamily.Generic)] [TestCase("XInputController", ControllerFamily.Xbox)] [TestCase("DualShockGamepad", ControllerFamily.PlayStation)]
        public void LabelsUseLayoutOrExplicitFamily(string layout, ControllerFamily expected)
        {
            var device = (Gamepad)InputSystem.AddDevice(layout);
            try {
                var profile = new ControlProfile(); Assert.That(profile.ResolveFamily(device), Is.EqualTo(expected));
                Assert.That(profile.LabelFor("<Gamepad>/buttonSouth", device), Is.EqualTo(expected == ControllerFamily.Xbox ? "A" : expected == ControllerFamily.PlayStation ? "Cross" : device.buttonSouth.displayName));
                Assert.That(profile.WithFamily(ControllerFamily.Generic).LabelFor("<Gamepad>/buttonSouth", device), Is.EqualTo(device.buttonSouth.displayName));
                Assert.That(profile.WithFamily(ControllerFamily.PlayStation).LabelFor("<Gamepad>/buttonEast", device), Is.EqualTo("Circle"));
            } finally { InputSystem.RemoveDevice(device); }
        }
        [Test] public void ViewRejectsEmptyShortcutWithoutLiveAlternativeAndCanAssignShoulder()
        {
            root = new GameObject("profile view"); var view = root.AddComponent<ControlBindingsView>(); view.Bind(store);
            Assert.That(view.Assign(ProfileAction.Relics, ""), Is.False);
            Assert.That(view.Assign(ProfileAction.Relics, "<Gamepad>/rightShoulder"), Is.True);
            Assert.That(store.Current.Binding(ProfileAction.Relics), Is.EqualTo("<Gamepad>/rightShoulder"));
        }
        [Test] public void NativeStorageRoundTripAndBackupRecoverWithoutChangingProgress()
        {
            Directory.CreateDirectory(directory);
            string progress = Path.Combine(directory, "progress.json"); File.WriteAllText(progress, "preserved");
            Assert.That(store.TrySave(new ControlProfile(), false, false, out _), Is.True);
            Assert.That(new ControlProfile().TryAssign(ProfileAction.Relics, "<Gamepad>/rightShoulder", false, out var next), Is.True);
            Assert.That(store.TrySave(next, false, false, out _), Is.True);
            Assert.That(new ControlProfileStore(directory).Current.Binding(ProfileAction.Relics), Is.EqualTo("<Gamepad>/rightShoulder"));
            File.WriteAllText(Path.Combine(directory, ControlProfileStore.FileName), "corrupt");
            var recovered = new ControlProfileStore(directory); Assert.That(recovered.Recovered, Is.True);
            Assert.That(recovered.Current.Binding(ProfileAction.Relics), Is.EqualTo("<Gamepad>/select"));
            Assert.That(File.ReadAllText(progress), Is.EqualTo("preserved"));
        }
        [Test] public void ViewRetainsItsBoundStoreAcrossDisableAndEnable()
        {
            root = new GameObject("profile view"); var view = root.AddComponent<ControlBindingsView>(); view.Bind(store);
            root.SetActive(false); root.SetActive(true);
            Assert.That(view.Assign(ProfileAction.Relics, "<Gamepad>/rightShoulder"), Is.True);
            Assert.That(store.Current.Binding(ProfileAction.Relics), Is.EqualTo("<Gamepad>/rightShoulder"));
        }
        [Test] public void RemappedPauseAndQuackProduceOnlyTheirActions()
        {
            var profile = new ControlProfile(); Assert.That(profile.TryAssign(ProfileAction.Pause, "<Gamepad>/leftShoulder", false, out profile), Is.True);
            Assert.That(profile.TryAssign(ProfileAction.Quack, "<Gamepad>/rightTrigger", false, out profile), Is.True);
            Assert.That(store.TrySave(profile, false, false, out _), Is.True);
            input = new GamepadInputAdapter(store); State(); input.Read(); State(GamepadButton.LeftShoulder);
            var command = input.Read(); Assert.That(command.PausePressed, Is.True); Assert.That(command.QuackPressed || command.RelicsPressed, Is.False);
            State(); input.Read(); State(GamepadButton.RightTrigger); command = input.Read(); Assert.That(command.QuackPressed, Is.True); Assert.That(command.PausePressed || command.RelicsPressed, Is.False);
        }
    }
}
