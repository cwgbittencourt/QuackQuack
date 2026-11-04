using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QuackQuack.Presentation.UI
{
    // UI input modules dispatch Cancel to the selected object, not its ancestors.
    public sealed class MenuCancelRelay : MonoBehaviour, ICancelHandler, ISelectHandler, IDeselectHandler
    {
        public GameObject Target;
        private void SetCue(bool value) { var outline = GetComponent<Outline>(); if (outline != null) outline.enabled = value; }
        public void OnSelect(BaseEventData data) => SetCue(true);
        public void OnDeselect(BaseEventData data) => SetCue(false);
        private void OnDisable() => SetCue(false);
        private void OnEnable() => SetCue(EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject);
        public void OnCancel(BaseEventData data)
        {
            if (Target != null) ExecuteEvents.Execute(Target, data, ExecuteEvents.cancelHandler);
            else if (transform.parent != null) ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, data, ExecuteEvents.cancelHandler);
        }
    }
}
