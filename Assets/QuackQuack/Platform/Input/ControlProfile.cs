using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QuackQuack.Platform.Input
{
    public enum ControllerFamily { Automatic, Xbox, PlayStation, Generic }
    public enum ProfileAction { Quack, Pause, Relics }

    // Portable control paths only: never persist a device ID, serial, or layout instance.
    [Serializable]
    public sealed class ControlProfile
    {
        public const int CurrentVersion = 1;
        [SerializeField] private int version = CurrentVersion;
        [SerializeField] private ControllerFamily family;
        [SerializeField] private string quack = "<Gamepad>/buttonSouth";
        [SerializeField] private string pause = "<Gamepad>/start";
        [SerializeField] private string relics = "<Gamepad>/select";
        private static readonly string[] Buttons = {
            "<Gamepad>/buttonSouth", "<Gamepad>/buttonNorth", "<Gamepad>/buttonWest",
            "<Gamepad>/leftShoulder", "<Gamepad>/rightShoulder", "<Gamepad>/leftTrigger",
            "<Gamepad>/rightTrigger", "<Gamepad>/leftStickPress", "<Gamepad>/rightStickPress",
            "<Gamepad>/start", "<Gamepad>/select"
        };
        public int Version => version;
        public ControllerFamily Family => family;
        public string Binding(ProfileAction action) => action == ProfileAction.Quack ? quack : action == ProfileAction.Pause ? pause : action == ProfileAction.Relics ? relics : throw new ArgumentOutOfRangeException(nameof(action));
        public static int ButtonCount => Buttons.Length;
        public static string ButtonAt(int index) => Buttons[index];
        internal static ControlProfile FromStored(int version, ControllerFamily family, string quack, string pause, string relics) =>
            new ControlProfile { version = version, family = family, quack = quack, pause = pause, relics = relics };
        public ControlProfile Copy() => new ControlProfile { version = version, family = family, quack = quack, pause = pause, relics = relics };
        public bool IsValid()
        {
            if (version != CurrentVersion || !Enum.IsDefined(typeof(ControllerFamily), family)) return false;
            if (!Allowed(quack, false) || !Allowed(pause, true) || !Allowed(relics, true)) return false;
            return (pause.Length == 0 || pause != quack) && (relics.Length == 0 || relics != quack) &&
                (pause.Length == 0 || relics.Length == 0 || pause != relics);
        }
        private static bool Allowed(string path, bool allowEmpty)
        { return path != null && ((allowEmpty && path.Length == 0) || Array.IndexOf(Buttons, path) >= 0); }
        public bool TryAssign(ProfileAction action, string path, bool hasFocusableAlternative, out ControlProfile result)
        {
            result = Copy();
            if (!Enum.IsDefined(typeof(ProfileAction), action) || !Allowed(path, action != ProfileAction.Quack) ||
                (path.Length == 0 && !hasFocusableAlternative)) return false;
            if (action == ProfileAction.Quack) result.quack = path;
            else if (action == ProfileAction.Pause) result.pause = path;
            else result.relics = path;
            return result.IsValid();
        }
        public ControlProfile WithFamily(ControllerFamily value)
        { var copy = Copy(); copy.family = value; if (!copy.IsValid()) throw new ArgumentException("Invalid controller family."); return copy; }
        public bool CanActivate(bool pauseAlternative, bool relicsAlternative) => IsValid() &&
            (pause.Length != 0 || pauseAlternative) && (relics.Length != 0 || relicsAlternative);
        public void ApplyTo(InputActionAsset runtimeClone)
        {
            if (!IsValid()) throw new InvalidOperationException("Invalid control profile.");
            foreach (ProfileAction action in Enum.GetValues(typeof(ProfileAction)))
            {
                var target = runtimeClone.FindAction("Gameplay/" + action, true);
                for (int i = 0; i < target.bindings.Count; i++)
                    if (target.bindings[i].groups == "Gamepad") target.ApplyBindingOverride(i, Binding(action));
            }
        }
        public ControllerFamily ResolveFamily(Gamepad pad)
        {
            if (family != ControllerFamily.Automatic) return family;
            if (pad == null) return ControllerFamily.Generic;
            if (InputSystem.IsFirstLayoutBasedOnSecond(pad.layout, "XInputController")) return ControllerFamily.Xbox;
            if (InputSystem.IsFirstLayoutBasedOnSecond(pad.layout, "DualShockGamepad")) return ControllerFamily.PlayStation;
            return ControllerFamily.Generic;
        }
        public string Label(ProfileAction action, Gamepad pad) => LabelFor(Binding(action), pad);
        public string LabelFor(string path, Gamepad pad)
        {
            if (string.IsNullOrEmpty(path)) return "Menu";
            string controlName = path.Substring("<Gamepad>/".Length);
            var resolved = ResolveFamily(pad);
            if (resolved == ControllerFamily.Xbox)
            {
                switch (controlName) {
                    case "buttonSouth": return "A"; case "buttonEast": return "B";
                    case "buttonWest": return "X"; case "buttonNorth": return "Y";
                    case "start": return "Menu / Start"; case "select": return "View / Back";
                    case "leftShoulder": return "LB"; case "rightShoulder": return "RB";
                    case "leftTrigger": return "LT"; case "rightTrigger": return "RT";
                }
            }
            if (resolved == ControllerFamily.PlayStation)
            {
                switch (controlName) {
                    case "buttonSouth": return "Cross"; case "buttonEast": return "Circle";
                    case "buttonWest": return "Square"; case "buttonNorth": return "Triangle";
                    case "start": return "Options";
                    case "select": return pad != null && pad.layout.IndexOf("DualSense", StringComparison.OrdinalIgnoreCase) >= 0 ? "Create" : "Share / Select";
                    case "leftShoulder": return "L1"; case "rightShoulder": return "R1";
                    case "leftTrigger": return "L2"; case "rightTrigger": return "R2";
                }
            }
            var control = pad?.TryGetChildControl(controlName);
            return control != null ? control.displayName : controlName;
        }
    }
}
