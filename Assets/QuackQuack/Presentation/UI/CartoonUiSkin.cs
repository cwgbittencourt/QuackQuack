using UnityEngine;

namespace QuackQuack.Presentation.UI
{
    // Apply once at activation, including inactive modal descendants. No combat Update.
    public sealed class CartoonUiSkin : MonoBehaviour
    {
        [SerializeField] private CartoonUiTheme theme;
        public CartoonUiTheme Theme=>theme;
        public void Configure(CartoonUiTheme value){theme=value;Apply();}
        public void Apply(){if(theme!=null)theme.Apply(gameObject);}
        private void OnEnable()=>Apply();
    }
}
