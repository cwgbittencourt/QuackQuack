using System;
using QuackQuack.Core.Run;
using QuackQuack.Core.Shared;
using QuackQuack.Platform.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace QuackQuack.Presentation.UI
{
    // The run owns elapsed simulation time. Binding/enabling this view never starts a new timer.
    public sealed class ContextualHintsView : MonoBehaviour
    {
        [SerializeField] private Text label;
        private RunState run;
        private Func<InputSource> source;
        private Func<ControlProfile> profile;
        private readonly ControlProfile defaults = new ControlProfile();
        private ControlProfileStore store;
        private ControlProfile storedProfile;
        private InputSource displayedSource;
        private ControllerFamily displayedFamily;
        private Gamepad displayedPad;
        private string displayedQuack, displayedPause, displayedRelics;
        private bool textReady;
        public bool IsVisible { get; private set; }

        // Production binding caches the store's defensive copy until a profile actually changes.
        public void Bind(RunState state, Func<InputSource> readSource)
        {
            Bind(state, readSource, ReadStoredProfile);
            store = ControlProfileStore.Shared;
            store.Changed += ProfileChanged;
            ProfileChanged();
        }

        public void Bind(RunState state, Func<InputSource> readSource, Func<ControlProfile> readProfile)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (readSource == null) throw new ArgumentNullException(nameof(readSource));
            if (readProfile == null) throw new ArgumentNullException(nameof(readProfile));
            Unbind(); run = state; source = readSource; profile = readProfile;
            Refresh();
        }

        private ControlProfile ReadStoredProfile() => storedProfile ?? defaults;
        private void ProfileChanged() { storedProfile = store.Current; textReady = false; Refresh(); }
        public void Unbind()
        {
            if (store != null) store.Changed -= ProfileChanged;
            store = null; storedProfile = null; run = null; source = null; profile = null;
            Hide();
        }

        private void LateUpdate() => Refresh();
        private void OnEnable() => Refresh();
        private void OnDisable() => Hide();
        private void OnDestroy() => Unbind();
        private void Hide()
        {
            IsVisible = false; textReady = false;
            if (label != null) label.text = string.Empty;
        }
        private void Refresh()
        {
            if (!isActiveAndEnabled || label == null || run == null || !run.IsSimulating ||
                run.Outcome.HasValue || run.ElapsedSeconds >= 10)
            { Hide(); return; }

            var currentSource = source();
            var currentProfile = currentSource == InputSource.Gamepad ? profile() ?? defaults : defaults;
            var pad = currentSource == InputSource.Gamepad ? Gamepad.current : null;
            if (!textReady || displayedSource != currentSource || displayedPad != pad ||
                displayedFamily != currentProfile.Family || displayedQuack != currentProfile.Binding(ProfileAction.Quack) ||
                displayedPause != currentProfile.Binding(ProfileAction.Pause) || displayedRelics != currentProfile.Binding(ProfileAction.Relics))
            {
                displayedSource = currentSource; displayedPad = pad; displayedFamily = currentProfile.Family;
                displayedQuack = currentProfile.Binding(ProfileAction.Quack);
                displayedPause = currentProfile.Binding(ProfileAction.Pause); displayedRelics = currentProfile.Binding(ProfileAction.Relics);
                label.raycastTarget = false;
                label.text = currentSource == InputSource.Touch
                    ? "MOVER: joystick  |  QUACK: toque no botão\nPAUSA: PAUSAR  |  RELÍQUIAS: toque no botão"
                    : currentSource == InputSource.Gamepad
                        ? "MOVER: analógico esquerdo  |  QUACK: " + currentProfile.Label(ProfileAction.Quack, pad) +
                          "\nPAUSA: " + currentProfile.Label(ProfileAction.Pause, pad) + "  |  RELÍQUIAS: " + currentProfile.Label(ProfileAction.Relics, pad)
                        : "MOVER: WASD / setas  |  QUACK: Espaço\nPAUSA: Esc  |  RELÍQUIAS: R";
                textReady = true;
            }
            IsVisible = label.isActiveAndEnabled;
        }
    }
}
