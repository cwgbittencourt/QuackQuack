using System.Collections;
using QuackQuack.Content.Relics;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Player;
using QuackQuack.Presentation.UI;
using QuackQuack.Systems.Run;
using UnityEngine;

namespace QuackQuack.Gameplay.Upgrades
{
    public sealed class LakeRelics : MonoBehaviour
    {
        [SerializeField] private PrototypeRelicCatalog catalog;
        [SerializeField] private RelicLoadoutView view;
        private RunState run;
        private LakeBuildProgression progression;
        private PlayerController player;
        private RelicDefinition[] definitions;
        private bool ownsPause;
        public RelicLoadout Loadout { get; private set; }
        public bool Ready { get; private set; }
        private IEnumerator Start()
        {
            progression = Object.FindAnyObjectByType<LakeBuildProgression>();
            player = Object.FindAnyObjectByType<PlayerController>();
            while (progression != null && !progression.Ready) yield return null;
            if (progression == null || player == null || GameBootstrap.Instance == null) yield break;
            Loadout = catalog.CreateLoadout(); run = GameBootstrap.Instance.Run;
            definitions = new RelicDefinition[catalog.Relics.Count];
            var ids = new string[10]; var names = new string[10]; var descriptions = new string[10]; var requirements = new string[10];
            for (int i = 0; i < 10; i++)
            {
                var d = catalog.Relics[i]; definitions[i] = d; ids[i] = d.Id; names[i] = d.DisplayName; descriptions[i] = d.Description;
                requirements[i] = d.Unlock == RelicUnlock.SurviveSeconds ? "Sobreviva " + d.Threshold + " s" : "Colete " + d.Threshold + " XP";
            }
            player.Relics = Loadout; progression.Runner.Relics = Loadout;
            UnlockProgress(); view.Bind(Loadout, ids, names, descriptions, requirements, Toggle, Show);
            run.Changed += PhaseChanged; Ready = true; PhaseChanged(run.Phase);
        }
        private void Update() { if (Ready && run.IsSimulating) UnlockProgress(); }
        public void RefreshUnlocks() { if (Ready) UnlockProgress(); }
        private void UnlockProgress()
        {
            foreach (var d in definitions)
                if (!Loadout.IsUnlocked(d.Id) && (d.Unlock == RelicUnlock.Available ||
                    (d.Unlock == RelicUnlock.SurviveSeconds && run.ElapsedSeconds >= d.Threshold) ||
                    (d.Unlock == RelicUnlock.CollectExperience && progression.Upgrades.Experience >= d.Threshold))) Loadout.Unlock(d.Id);
        }
        public bool Toggle(string id)
        {
            if (!Ready || !view.IsVisible || run.Phase != RunPhase.Paused) return false;
            return Loadout.IsEquipped(id) ? Loadout.Unequip(id) : Loadout.Equip(id);
        }
        public void Show(bool visible)
        {
            if (!Ready) return;
            if (visible)
            {
                if (view.IsVisible) return;
                if (!run.IsSimulating && run.Phase != RunPhase.Paused) return;
                ownsPause = run.IsSimulating;
                if (ownsPause) run.TryTransition(RunPhase.Paused);
                view.SetVisible(true);
            }
            else
            {
                view.SetVisible(false);
                if (ownsPause && run.Phase == RunPhase.Paused) run.TryResume();
                ownsPause = false;
            }
        }
        private void PhaseChanged(RunPhase phase)
        {
            view.SetAvailable(run.IsSimulating || phase == RunPhase.Paused);
            if (phase != RunPhase.Paused) { view.SetVisible(false); ownsPause = false; }
        }
        private void OnDestroy()
        {
            if (run != null) run.Changed -= PhaseChanged;
            if (view != null) view.Unbind();
            if (player != null) player.Relics = null;
            if (progression != null && progression.Runner != null) progression.Runner.Relics = null;
        }
    }
}
