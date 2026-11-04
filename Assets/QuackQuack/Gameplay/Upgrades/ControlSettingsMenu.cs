using System.Collections;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Player;
using QuackQuack.Platform.Input;
using QuackQuack.Presentation.UI;
using QuackQuack.Systems.Run;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QuackQuack.Gameplay.Upgrades
{
    public sealed class ControlSettingsMenu : MonoBehaviour, ICancelHandler
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button open, close, resume;
        [SerializeField] private ControlBindingsView bindings;
        private RunState run;
        private PlayerController player;
        private RelicLoadoutView relics;
        private Button pause, abandon;
        private bool ownsPause;
        private Selectable previousFocus;
        private readonly Selectable[] pauseActions = new Selectable[4];
        private readonly Selectable[] profileActions = new Selectable[6];
        private int pauseCount, profileCount;
        private bool wasPaused;
        public bool IsVisible => panel != null && panel.activeSelf;
        private IEnumerator Start()
        {
            while (GameBootstrap.Instance == null || (player = Object.FindAnyObjectByType<PlayerController>()) == null || player.State == null) yield return null;
            run = GameBootstrap.Instance.Run; relics = Object.FindAnyObjectByType<RelicLoadoutView>();
            var controls = GameObject.Find("Run controls");
            if (controls != null) foreach (var button in controls.GetComponentsInChildren<Button>(true)) {
                if (button.name == "PAUSAR") pause = button;
                if (button.name == "ABANDONAR") abandon = button;
            }
            if (pause == null) { pause = resume; player.SetExternalPauseButton(true); resume.onClick.AddListener(TogglePause); }
            else resume.gameObject.SetActive(false);
            open.onClick.AddListener(Open); close.onClick.AddListener(Close);
            bindings.SetAlternatives(pause, relics != null ? relics.OpenButton : null);
            foreach (var button in panel.GetComponentsInChildren<Button>(true)) { if (profileCount < profileActions.Length) profileActions[profileCount++] = button; MenuNavigation.Prepare(button); button.GetComponent<MenuCancelRelay>().Target = gameObject; }
            run.Changed += PhaseChanged; PhaseChanged(run.Phase);
        }
        private void TogglePause() { if (run.IsSimulating) run.TryTransition(RunPhase.Paused); else if (run.Phase == RunPhase.Paused) run.TryResume(); }
        public void Open()
        {
            if (run == null || IsVisible || (!run.IsSimulating && run.Phase != RunPhase.Paused) || (relics != null && relics.IsVisible)) return;
            previousFocus = MenuNavigation.Selected; ownsPause = run.IsSimulating;
            if (ownsPause) run.TryTransition(RunPhase.Paused);
            panel.SetActive(true); player.SetMenuInputBlocked(true); open.gameObject.SetActive(false);
            MenuNavigation.Graph(profileActions, profileCount); MenuNavigation.Repair(profileActions, profileCount);
        }
        public void Close()
        {
            if (!IsVisible) return;
            panel.SetActive(false); player.SetMenuInputBlocked(false); open.gameObject.SetActive(run.Phase == RunPhase.Paused);
            bool resumeOwned = ownsPause; ownsPause = false;
            if (resumeOwned && run.Phase == RunPhase.Paused) run.TryResume();
            MenuNavigation.Restore(previousFocus, open);
        }
        public void OnCancel(BaseEventData data) { if (IsVisible) Close(); else if (run != null && run.Phase == RunPhase.Paused) run.TryResume(); data.Use(); }
        private void PhaseChanged(RunPhase phase)
        {
            if (phase != RunPhase.Paused && IsVisible) { panel.SetActive(false); ownsPause = false; player.SetMenuInputBlocked(false); }
            open.gameObject.SetActive(phase == RunPhase.Paused && !IsVisible);
            if (pause == resume) { resume.gameObject.SetActive(run.IsSimulating || phase == RunPhase.Paused); resume.GetComponentInChildren<Text>().text = phase == RunPhase.Paused ? "CONTINUAR" : "PAUSAR"; }
        }
        private void LateUpdate()
        {
            if (run == null) return;
            if (IsVisible) { MenuNavigation.Repair(profileActions, profileCount); return; }
            bool paused = run.Phase == RunPhase.Paused && (relics == null || !relics.IsVisible);
            open.gameObject.SetActive(paused);
            if (paused) {
                pauseCount = 0;
                // Follow the visible top-to-bottom order across the two canvases.
                if (relics != null && MenuNavigation.Usable(relics.OpenButton)) pauseActions[pauseCount++] = relics.OpenButton;
                if (MenuNavigation.Usable(pause)) pauseActions[pauseCount++] = pause;
                if (MenuNavigation.Usable(abandon)) pauseActions[pauseCount++] = abandon;
                pauseActions[pauseCount++] = open;
                // This short menu can change availability as another modal closes.
                MenuNavigation.Graph(pauseActions, pauseCount);
                for (int i = 0; i < pauseCount; i++) pauseActions[i].GetComponent<MenuCancelRelay>().Target = gameObject;
                if (!wasPaused || !MenuNavigation.Contains(pauseActions, pauseCount, EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null)) MenuNavigation.Repair(pauseActions, pauseCount);
            }
            wasPaused = paused;
        }
        private void OnDestroy()
        {
            if (run != null) run.Changed -= PhaseChanged;
            if (open != null) open.onClick.RemoveListener(Open); if (close != null) close.onClick.RemoveListener(Close);
            if (resume != null) resume.onClick.RemoveListener(TogglePause);
            if (player != null && IsVisible) player.SetMenuInputBlocked(false);
        }
    }
}
