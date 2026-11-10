using System;
using QuackQuack.Core.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace QuackQuack.Platform.Input
{
    public sealed class UnifiedPlayerInput : IPlayerInput, IDisposable
    {
        private readonly KeyboardInputAdapter keyboard = new KeyboardInputAdapter();
        private readonly GamepadInputAdapter gamepad = new GamepadInputAdapter();
        private GameObject mobileRoot;
        private GameObject ownedEvents;
        private EventSystem navigationEvents;
        private bool previousNavigation;
        private MobileInputAdapter mobile;
        private bool gameplay = true, allowPause = true, suspended, disposed;
        private bool unfocused, applicationPaused;
        private bool externalPauseButton;
        private bool availabilityContextDirty;
        private bool IsSuspended => suspended || applicationPaused || (unfocused && InputSystem.settings.backgroundBehavior != InputSettings.BackgroundBehavior.IgnoreFocus);
        private long activity;
        public InputSource Source { get; private set; } = InputSource.Keyboard;
        public bool ControllerAvailable { get; private set; }
        public MobileInputAdapter Mobile => mobile;
        public UnifiedPlayerInput()
        {
            ControllerAvailable = HasEnabledController();
            EnsureMobile();
            InputSystem.onDeviceChange += OnDeviceChange;
        }
        private static bool HasEnabledController()
        {
            foreach (var pad in Gamepad.all) if (pad.added && pad.enabled) return true;
            return false;
        }
        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (disposed || !(device is Gamepad)) return;
            if (change != InputDeviceChange.Added && change != InputDeviceChange.Removed &&
                change != InputDeviceChange.Disconnected && change != InputDeviceChange.Reconnected &&
                change != InputDeviceChange.Enabled && change != InputDeviceChange.Disabled) return;
            bool available = HasEnabledController();
            if (available == ControllerAvailable) return;
            ControllerAvailable = available;
            // Device dispatch can run while Input System traverses state monitors.
            // Activating touch here enables/disables actions and writes virtual state reentrantly.
            // Keep availability current, but apply presentation/actions on the consumer's next read.
            availabilityContextDirty = true;
        }
        private void EnsureMobile()
        {
            if (mobile != null || (!Application.isMobilePlatform && Touchscreen.current == null)) return;
            // Hiding gameplay controls for a modal must not disable the modal's EventSystem.
            if (EventSystem.current == null)
            {
                ownedEvents = new GameObject("Player UI events"); UpgradeUiEventSystem.Ensure(ownedEvents.transform);
            }
            mobileRoot = UnityEngine.Object.Instantiate(PlayerInputConfiguration.Load().MobileControlsPrefab);
            mobile = mobileRoot.GetComponent<MobileInputAdapter>(); ApplyPauseButton(); ApplyContext();
        }
        public void SetExternalPauseButton(bool available)
        { externalPauseButton = available; ApplyPauseButton(); }
        private void ApplyPauseButton()
        {
            if (mobileRoot == null) return;
            foreach (var button in mobileRoot.GetComponentsInChildren<MobileActionButton>(true))
                if (button.Action == MobileAction.Pause) button.gameObject.SetActive(!externalPauseButton);
        }
        public PlayerCommand Read()
        {
            if (disposed) throw new ObjectDisposedException(nameof(UnifiedPlayerInput));
            EnsureMobile();
            if (availabilityContextDirty) ApplyContext();
            ApplyNavigationContext();
            var k = keyboard.Read(); var g = gamepad.Read();
            var t = mobile != null ? mobile.Read() : new PlayerCommand(Vector2.zero, source: InputSource.Touch);
            if (keyboard.Activity > activity) { activity = keyboard.Activity; Source = InputSource.Keyboard; }
            if (gamepad.Activity > activity) { activity = gamepad.Activity; Source = InputSource.Gamepad; }
            if (mobile != null && mobile.Activity > activity) { activity = mobile.Activity; Source = InputSource.Touch; }
            var selected = Source == InputSource.Touch ? t : Source == InputSource.Gamepad ? g : k;
            return new PlayerCommand(gameplay && !IsSuspended ? selected.Move : Vector2.zero,
                gameplay && !IsSuspended && selected.QuackPressed, allowPause && !IsSuspended && selected.PausePressed, Source,
                allowPause && !IsSuspended && selected.RelicsPressed);
        }
        public void SetContext(bool gameplay, bool pause)
        { this.gameplay = gameplay; allowPause = gameplay || pause; ApplyContext(); }
        public void SetSuspended(bool value) { suspended = value; ApplyContext(); }
        public void SetFocused(bool value)
        {
            unfocused = !value;
            if (!value)
            {
                // Clear old input even when the configured policy permits new background input.
                bool previous = suspended; suspended = true; ApplyContext(); suspended = previous;
            }
            ApplyContext();
        }
        public void SetApplicationPaused(bool value) { applicationPaused = value; ApplyContext(); }
        private void ApplyContext()
        {
            // A connect+disconnect may be coalesced before Read. Cancel the prior
            // gesture even if the final availability leaves touch visible.
            if (availabilityContextDirty && mobileRoot != null) mobileRoot.SetActive(false);
            availabilityContextDirty = false;
            ApplyNavigationContext();
            bool play = gameplay && !IsSuspended, pause = allowPause && !IsSuspended;
            keyboard.SetContext(play, pause); gamepad.SetContext(play, pause);
            if (mobile != null)
            {
                // OnDisable/OnEnable resets the old touch gesture. The menu EventSystem is separate.
                bool touchPlay = play && !ControllerAvailable, touchPause = pause && !ControllerAvailable;
                mobileRoot.SetActive(touchPlay || touchPause);
                mobile.SetContext(touchPlay, touchPause);
            }
        }
        private void ApplyNavigationContext()
        {
            var current = EventSystem.current;
            if (navigationEvents != current)
            {
                if (navigationEvents != null) navigationEvents.sendNavigationEvents = previousNavigation;
                navigationEvents = current;
                if (current != null) previousNavigation = current.sendNavigationEvents;
            }
            // Pointer clicks remain available. Submit/Move/Cancel are only for menus.
            if (current != null) current.sendNavigationEvents = previousNavigation && !gameplay && !IsSuspended;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            InputSystem.onDeviceChange -= OnDeviceChange;
            keyboard.Dispose(); gamepad.Dispose();
            if (navigationEvents != null) navigationEvents.sendNavigationEvents = previousNavigation;
            if (mobileRoot != null) { mobileRoot.SetActive(false); UnityEngine.Object.Destroy(mobileRoot); }
            if (ownedEvents != null) UnityEngine.Object.Destroy(ownedEvents);
        }
    }
}
