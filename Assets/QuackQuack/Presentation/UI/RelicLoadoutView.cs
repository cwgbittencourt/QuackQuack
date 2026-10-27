using System;
using QuackQuack.Core.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace QuackQuack.Presentation.UI
{
    public sealed class RelicLoadoutView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text summary;
        [SerializeField] private Text[] labels;
        [SerializeField] private Button[] toggles;
        [SerializeField] private Button open, close;
        private RelicLoadout loadout;
        private string[] ids, names, descriptions, requirements;
        private Func<string, bool> toggle;
        private Action<bool> show;
        private UnityEngine.Events.UnityAction[] handlers;
        private int version = -1;
        private bool available = true;
        public bool IsVisible => panel != null && panel.activeSelf;
        public void Bind(RelicLoadout state, string[] ids, string[] names, string[] descriptions, string[] requirements,
            Func<string, bool> command, Action<bool> showCommand)
        {
            Unbind(); loadout = state; this.ids = ids; this.names = names; this.descriptions = descriptions;
            this.requirements = requirements; toggle = command; show = showCommand;
            handlers = new UnityEngine.Events.UnityAction[10];
            for (int i = 0; i < 10; i++) { int index = i; handlers[i] = () => { toggle(this.ids[index]); Refresh(); }; toggles[i].onClick.AddListener(handlers[i]); }
            open.onClick.AddListener(Open); close.onClick.AddListener(Close); Refresh();
        }
        private void Open() => show?.Invoke(true);
        private void Close() => show?.Invoke(false);
        public void SetVisible(bool visible) { panel.SetActive(visible); open.gameObject.SetActive(available && !visible); Refresh(); }
        public void SetAvailable(bool value) { available = value; open.gameObject.SetActive(value && !IsVisible); if (!value) SetVisible(false); }
        private void LateUpdate() => Refresh();
        public void Refresh()
        {
            if (loadout == null || version == loadout.Version) return;
            version = loadout.Version;
            summary.text = "RELÍQUIAS  " + loadout.EquippedIds.Count + "/3  •  Relíquias desbloqueadas";
            for (int i = 0; i < 10; i++)
            {
                bool equipped = loadout.IsEquipped(ids[i]), unlocked = loadout.IsUnlocked(ids[i]);
                labels[i].text = names[i] + "\n" + descriptions[i] + "\n" + (unlocked ? (equipped ? "EQUIPADA" : "DESBLOQUEADA") : requirements[i]);
                toggles[i].interactable = unlocked && (equipped || loadout.EquippedIds.Count < 3);
                toggles[i].GetComponentInChildren<Text>().text = equipped ? "REMOVER" : unlocked ? "EQUIPAR" : "BLOQUEADA";
            }
        }
        public void Unbind()
        {
            if (handlers != null) for (int i = 0; i < handlers.Length; i++) toggles[i].onClick.RemoveListener(handlers[i]);
            if (open != null) open.onClick.RemoveListener(Open); if (close != null) close.onClick.RemoveListener(Close);
            handlers = null; loadout = null; toggle = null; show = null; version = -1;
        }
        private void OnDestroy() => Unbind();
    }
}
