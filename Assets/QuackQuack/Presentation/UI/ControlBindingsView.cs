using QuackQuack.Platform.Input;
using UnityEngine;
using UnityEngine.UI;

namespace QuackQuack.Presentation.UI
{
    // Wired by PolishUiSetup in T013. No generated hierarchy or ownership of run pause here.
    public sealed class ControlBindingsView : MonoBehaviour
    {
        [SerializeField] private Button quack, pause, relics, family, reset;
        [SerializeField] private Text status, navigationHints;
        private UnityEngine.InputSystem.Gamepad displayedPad;
        [SerializeField] private Selectable pauseAlternative, relicsAlternative;
        private ControlProfileStore store;
        private bool bound;
        public void SetAlternatives(Selectable pauseAccess, Selectable relicsAccess) { pauseAlternative = pauseAccess; relicsAlternative = relicsAccess; }
        public bool HasPauseAlternative => Usable(pauseAlternative);
        public bool HasRelicsAlternative => Usable(relicsAlternative);
        private static bool Usable(Selectable target) => target != null && target.IsActive() && target.IsInteractable();
        public void Bind(ControlProfileStore profiles = null)
        {
            Unbind(); store = profiles ?? ControlProfileStore.Shared;
            if (quack != null) quack.onClick.AddListener(CycleQuack);
            if (pause != null) pause.onClick.AddListener(CyclePause);
            if (relics != null) relics.onClick.AddListener(CycleRelics);
            if (family != null) family.onClick.AddListener(CycleFamily);
            if (reset != null) reset.onClick.AddListener(ResetProfile);
            store.Changed += Refresh; bound = true; Refresh();
        }
        public bool Assign(ProfileAction action, string path)
        {
            if (store == null) Bind();
            bool alternative = action == ProfileAction.Pause ? HasPauseAlternative : HasRelicsAlternative;
            if (!store.Current.TryAssign(action, path, alternative, out var next)) { Message("Atalho indisponível ou já usado. Mantenha acesso à Pausa e Relíquias."); return false; }
            return Save(next);
        }
        private bool Save(ControlProfile profile)
        {
            bool success = store.TrySave(profile, HasPauseAlternative, HasRelicsAlternative, out string error);
            Message(success ? "Perfil salvo." : error); return success;
        }
        private void CycleQuack() => Cycle(ProfileAction.Quack);
        private void CyclePause() => Cycle(ProfileAction.Pause);
        private void CycleRelics() => Cycle(ProfileAction.Relics);
        private void Cycle(ProfileAction action)
        {
            var current = store.Current;
            int start = -1;
            for (int i = 0; i < ControlProfile.ButtonCount; i++) if (ControlProfile.ButtonAt(i) == current.Binding(action)) start = i;
            for (int offset = 1; offset <= ControlProfile.ButtonCount; offset++) {
                string path = ControlProfile.ButtonAt((start + offset) % ControlProfile.ButtonCount);
                if (current.TryAssign(action, path, false, out var next)) { Save(next); return; }
            }
        }
        private void CycleFamily() => Save(store.Current.WithFamily((ControllerFamily)(((int)store.Current.Family + 1) % 4)));
        private void ResetProfile() => Save(new ControlProfile());
        public void Refresh()
        {
            if (store == null) return;
            var profile = store.Current;
            displayedPad = UnityEngine.InputSystem.Gamepad.current;
            if (navigationHints != null) navigationHints.text = "MOVER: analógico esquerdo | NAVEGAR: direcional / analógico | CONFIRMAR: " + profile.LabelFor("<Gamepad>/buttonSouth", displayedPad) + " | VOLTAR: " + profile.LabelFor("<Gamepad>/buttonEast", displayedPad);
            SetLabel(quack, "QUACK: " + profile.Label(ProfileAction.Quack, UnityEngine.InputSystem.Gamepad.current));
            SetLabel(pause, "PAUSA: " + profile.Label(ProfileAction.Pause, UnityEngine.InputSystem.Gamepad.current));
            SetLabel(relics, "RELÍQUIAS: " + profile.Label(ProfileAction.Relics, UnityEngine.InputSystem.Gamepad.current));
            SetLabel(family, "CONTROLE: " + (profile.Family == ControllerFamily.Automatic ? "AUTOMÁTICO" : profile.Family == ControllerFamily.Generic ? "GENÉRICO" : profile.Family.ToString().ToUpperInvariant()));
        }
        private static void SetLabel(Button button, string label) { if (button != null) { var text = button.GetComponentInChildren<Text>(); if (text != null) text.text = label; } }
        private void Message(string text) { if (status != null) status.text = text; }
        private void LateUpdate() { if (bound && displayedPad != UnityEngine.InputSystem.Gamepad.current) Refresh(); }
        private void OnEnable() => Bind(store);
        private void OnDisable() => Unbind();
        private void Unbind()
        {
            if (!bound) return;
            if (quack != null) quack.onClick.RemoveListener(CycleQuack);
            if (pause != null) pause.onClick.RemoveListener(CyclePause);
            if (relics != null) relics.onClick.RemoveListener(CycleRelics);
            if (family != null) family.onClick.RemoveListener(CycleFamily);
            if (reset != null) reset.onClick.RemoveListener(ResetProfile);
            store.Changed -= Refresh; bound = false;
        }
        private void OnDestroy() => Unbind();
    }
}
