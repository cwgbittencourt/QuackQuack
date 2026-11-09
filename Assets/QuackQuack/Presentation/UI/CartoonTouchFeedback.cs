using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QuackQuack.Presentation.UI
{
    [RequireComponent(typeof(Image))]
    public sealed class CartoonTouchFeedback : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,ICancelHandler
    {
        private Image image;
        private int? pointer;
        private void Awake()=>image=GetComponent<Image>();
        public void OnPointerDown(PointerEventData data)
        {if(!isActiveAndEnabled||pointer.HasValue||data.button!=PointerEventData.InputButton.Left)return;pointer=data.pointerId;image.color=new Color(1,.78f,.35f);}
        public void OnPointerUp(PointerEventData data){if(pointer==data.pointerId)ResetTint();}
        public void OnCancel(BaseEventData data)=>ResetTint();
        private void ResetTint(){pointer=null;if(image!=null)image.color=Color.white;}
        private void OnDisable()=>ResetTint();
        private void OnApplicationFocus(bool focused){if(!focused)ResetTint();}
    }
}
