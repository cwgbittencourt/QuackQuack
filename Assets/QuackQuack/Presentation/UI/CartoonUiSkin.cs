using UnityEngine;
using UnityEngine.UI;

namespace QuackQuack.Presentation.UI
{
    // Style once at activation; refresh glyph meshes only when the canvas resolution changes.
    public sealed class CartoonUiSkin : MonoBehaviour
    {
        [SerializeField] private CartoonUiTheme theme;
        private Canvas canvas;
        private Text[] labels;
        private float previousScale = -1;
        private Vector2 previousSize;
        public CartoonUiTheme Theme=>theme;
        public void Configure(CartoonUiTheme value){theme=value;Apply();}
        public void Apply()
        {
            if(theme!=null)theme.Apply(gameObject);
            canvas=GetComponent<Canvas>();labels=GetComponentsInChildren<Text>(true);previousScale=-1;
        }
        private void OnEnable()=>Apply();
        private void LateUpdate()
        {
            if(canvas==null||labels==null)return;
            var size=canvas.pixelRect.size;float scale=canvas.scaleFactor;
            if(Mathf.Approximately(scale,previousScale)&&size==previousSize)return;
            previousScale=scale;previousSize=size;
            foreach(var label in labels)if(label!=null)
            {label.cachedTextGenerator.Invalidate();label.SetAllDirty();}
        }
    }
}
