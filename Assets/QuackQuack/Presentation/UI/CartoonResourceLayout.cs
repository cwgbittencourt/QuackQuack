using UnityEngine;

namespace QuackQuack.Presentation.UI
{
    [DefaultExecutionOrder(50)]
    public sealed class CartoonResourceLayout : MonoBehaviour
    {
        public RectTransform Group;
        public RectTransform[] Rows,Tracks,Fills;
        private float previous=-1;
        private void OnEnable()=>previous=-1;
        private void LateUpdate()
        {
            if(Group==null||!(Group.parent is RectTransform safe))return;
            float width=Mathf.Min(560,safe.rect.width*.34f);
            if(width<=0||Mathf.Approximately(width,previous))return;
            previous=width;Group.sizeDelta=new Vector2(width,148);
            foreach(var row in Rows)row.sizeDelta=new Vector2(width,48);
            foreach(var track in Tracks)track.sizeDelta=new Vector2(Mathf.Max(40,width-160),16);
            foreach(var fill in Fills)fill.sizeDelta=new Vector2(Mathf.Max(40,width-160),16);
        }
    }
}
