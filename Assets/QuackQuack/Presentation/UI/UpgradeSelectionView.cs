using System;
using QuackQuack.Core.Shared;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QuackQuack.Presentation.UI
{
    [Serializable]
    public sealed class UpgradeCardWidgets
    {
        public GameObject root;
        public Text title, kind, current, next, synergy, requirement;
        public Button choose, banish;
    }

    public sealed class UpgradeSelectionView : MonoBehaviour, ICancelHandler
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text heading, resources;
        [SerializeField] private Button reroll, skip;
        [SerializeField] private UpgradeCardWidgets[] cards;
        private Func<UpgradeSelectionSnapshot> read;
        private Func<string, string, bool> confirmCommand, banishCommand;
        private Func<string, bool> rerollCommand, skipCommand;
        private readonly UnityAction[] chooseHandlers = new UnityAction[3], banishHandlers = new UnityAction[3];
        private bool wired;
        private Selectable previousFocus;
        private readonly Selectable[] navigation = new Selectable[8];
        private int navigationCount;
        private int commandFrame = -1;
        public UpgradeSelectionSnapshot Displayed { get; private set; }
        public bool IsVisible => panel != null && panel.activeSelf;

        public void Bind(Func<UpgradeSelectionSnapshot> source, Func<string, string, bool> confirm,
            Func<string, string, bool> banish, Func<string, bool> reroll, Func<string, bool> skip)
        {
            read = source ?? throw new ArgumentNullException(nameof(source));
            confirmCommand = confirm; banishCommand = banish; rerollCommand = reroll; skipCommand = skip;
            Wire(); Refresh();
        }
        private void Wire()
        {
            if (wired) return;
            if (cards == null || cards.Length != 3) throw new InvalidOperationException("Selection requires three configured card slots.");
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                chooseHandlers[i] = () => Command(index, false);
                banishHandlers[i] = () => Command(index, true);
                cards[i].choose.onClick.AddListener(chooseHandlers[i]);
                cards[i].banish.onClick.AddListener(banishHandlers[i]);
            }
            reroll.onClick.AddListener(Reroll); skip.onClick.AddListener(Skip); wired = true;
        }
        private void LateUpdate() { Refresh(); if (IsVisible) MenuNavigation.Repair(navigation, navigationCount); }
        public void Refresh() { if (read != null) Render(read()); }
        public void Render(UpgradeSelectionSnapshot value)
        {
            if (ReferenceEquals(value, Displayed)) return;
            bool wasVisible = IsVisible;
            string previousId = Displayed == null ? null : Displayed.OfferId;
            Displayed = value;
            bool visible = value != null && value.Visible;
            if (visible && !wasVisible) previousFocus = MenuNavigation.Selected;
            panel.SetActive(visible);
            if (!visible)
            {
                if (wasVisible) MenuNavigation.Restore(previousFocus);
                return;
            }
            heading.text = "ESCOLHA UMA MELHORIA  •  NÍVEL " + value.Level;
            resources.text = "RERROLAGENS: " + value.Rerolls + "    •    BANIMENTOS: " + value.Banishes + "    •    Combate pausado";
            for (int i = 0; i < cards.Length; i++)
            {
                bool present = i < value.Cards.Count;
                cards[i].root.SetActive(present);
                if (!present) continue;
                var card = value.Cards[i]; var ui = cards[i];
                ui.title.text = card.Title; ui.kind.text = card.Kind;
                ui.current.text = "ATUAL\n" + card.Current; ui.next.text = "PRÓXIMO\n" + card.Next;
                ui.synergy.text = "SINERGIA\n" + card.Synergy; ui.requirement.text = "EVOLUÇÃO\n" + card.Requirement;
                ui.choose.interactable = true; ui.banish.interactable = value.Banishes > 0;
            }
            reroll.interactable = value.Rerolls > 0; skip.interactable = true;
            RebuildNavigation();
            if ((!wasVisible || previousId != value.OfferId) && value.Cards.Count > 0 && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(cards[0].choose.gameObject);
        }
        private void RebuildNavigation()
        {
            navigationCount = 0;
            foreach (var card in cards) {
                MenuNavigation.Prepare(card.choose); MenuNavigation.Prepare(card.banish);
                card.choose.navigation = card.banish.navigation = new Navigation { mode = Navigation.Mode.None };
                if (MenuNavigation.Usable(card.choose)) navigation[navigationCount++] = card.choose;
                if (MenuNavigation.Usable(card.banish)) navigation[navigationCount++] = card.banish;
            }
            reroll.navigation = new Navigation { mode = Navigation.Mode.None };
            if (MenuNavigation.Usable(reroll)) navigation[navigationCount++] = reroll;
            navigation[navigationCount++] = skip; MenuNavigation.Graph(navigation, navigationCount);
            MenuNavigation.Repair(navigation, navigationCount);
        }
        public void OnCancel(BaseEventData data)
        {
            if (!IsVisible) return;
            var selected = MenuNavigation.Selected;
            foreach (var card in cards) if (selected == card.banish && MenuNavigation.Usable(card.choose)) { card.choose.Select(); data.Use(); return; }
            MenuNavigation.Repair(navigation, navigationCount); data.Use(); // Mandatory choice is never resolved by Cancel.
        }
        private bool BeginCommand()
        {
            if (!IsVisible || Displayed == null || commandFrame == Time.frameCount) return false;
            commandFrame = Time.frameCount; return true;
        }
        private void Command(int index, bool banish)
        {
            if (!BeginCommand() || index >= Displayed.Cards.Count) return;
            (banish ? banishCommand : confirmCommand)?.Invoke(Displayed.OfferId, Displayed.Cards[index].Id);
            Refresh();
        }
        private void Reroll() { if (!BeginCommand()) return; rerollCommand?.Invoke(Displayed.OfferId); Refresh(); }
        private void Skip() { if (!BeginCommand()) return; skipCommand?.Invoke(Displayed.OfferId); Refresh(); }
        public void Unbind()
        {
            read = null; confirmCommand = banishCommand = null; rerollCommand = skipCommand = null;
            Displayed = null; if (panel != null) panel.SetActive(false);
        }
        private void OnDisable() { if (panel != null) panel.SetActive(false); Displayed = null; }
        private void OnDestroy()
        {
            Unbind();
            if (!wired) return;
            for (int i = 0; i < 3; i++)
            { cards[i].choose.onClick.RemoveListener(chooseHandlers[i]); cards[i].banish.onClick.RemoveListener(banishHandlers[i]); }
            reroll.onClick.RemoveListener(Reroll); skip.onClick.RemoveListener(Skip);
        }
    }
}
