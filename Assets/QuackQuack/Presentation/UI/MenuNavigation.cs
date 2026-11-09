using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QuackQuack.Presentation.UI
{
    public static class MenuNavigation
    {
        public static bool Usable(Selectable value) => value != null && value.isActiveAndEnabled && value.IsInteractable();
        public static void Prepare(Selectable value)
        {
            if (value.GetComponent<MenuCancelRelay>() == null) value.gameObject.AddComponent<MenuCancelRelay>();
            var outline = value.GetComponent<Outline>(); if (outline == null) outline = value.gameObject.AddComponent<Outline>();
            outline.effectColor = CartoonUiTheme.Teal; outline.effectDistance = new Vector2(4, -4); outline.enabled = Selected == value;
            var colors = value.colors; colors.selectedColor = new Color(1, .82f, .32f); colors.highlightedColor = new Color(.75f, .9f, 1);
            value.colors = colors; value.transition = Selectable.Transition.ColorTint;
        }
        // An explicit cyclic list, rebuilt only when the available actions change.
        public static void Graph(Selectable[] actions, int count)
        {
            for (int i = 0; i < count; i++) {
                var current = actions[i]; Prepare(current);
                var previous = actions[(i + count - 1) % count]; var next = actions[(i + 1) % count];
                current.navigation = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnUp = previous, selectOnLeft = previous, selectOnDown = next, selectOnRight = next };
            }
        }
        public static bool Contains(Selectable[] actions, int count, GameObject selected)
        { for (int i = 0; i < count; i++) if (actions[i].gameObject == selected && Usable(actions[i])) return true; return false; }
        public static void Repair(Selectable[] actions, int count)
        {
            var events = EventSystem.current;
            if (events != null && !Contains(actions, count, events.currentSelectedGameObject))
                for (int i = 0; i < count; i++) if (Usable(actions[i])) { actions[i].Select(); break; }
        }
        public static Selectable Selected => EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null ? null : EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();
        public static void Restore(Selectable previous, Selectable fallback = null)
        { if (Usable(previous)) previous.Select(); else if (Usable(fallback)) fallback.Select(); }
    }
}
