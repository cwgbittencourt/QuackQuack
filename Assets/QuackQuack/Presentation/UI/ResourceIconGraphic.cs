using UnityEngine;
using UnityEngine.UI;

namespace QuackQuack.Presentation.UI
{
    public enum ResourceIconKind { Health, Breath }

    // Small uGUI silhouettes, independent of fonts and distinguishable without color.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ResourceIconGraphic : MaskableGraphic
    {
        [SerializeField] private ResourceIconKind kind;
        public ResourceIconKind Kind => kind;
        private static readonly Vector2[] Heart = {
            new Vector2(-0.5f, 0.15f), new Vector2(-0.4f, 0.42f), new Vector2(-0.18f, 0.5f),
            new Vector2(0, 0.28f), new Vector2(0.18f, 0.5f), new Vector2(0.4f, 0.42f),
            new Vector2(0.5f, 0.15f), new Vector2(0.4f, -0.08f), new Vector2(0, -0.5f), new Vector2(-0.4f, -0.08f)
        };
        public void Configure(ResourceIconKind value, Color tint)
        { kind = value; color = tint; raycastTarget = false; SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear(); var rect = GetPixelAdjustedRect();
            Vector2 center = rect.center; float size = Mathf.Min(rect.width, rect.height);
            if (kind == ResourceIconKind.Health)
            {
                vertices.AddVert(center, color, Vector2.zero);
                foreach (var point in Heart) vertices.AddVert(center + point * size, color, Vector2.zero);
                for (int i = 0; i < Heart.Length; i++) vertices.AddTriangle(0, i + 1, (i + 1) % Heart.Length + 1);
                return;
            }
            for (int row = -1; row <= 1; row++)
                for (int segment = 0; segment < 4; segment++)
                {
                    float x = -0.5f + segment * 0.25f;
                    var a = new Vector2(x, row * 0.27f + Mathf.Sin(segment * Mathf.PI / 2) * 0.08f);
                    var b = new Vector2(x + 0.25f, row * 0.27f + Mathf.Sin((segment + 1) * Mathf.PI / 2) * 0.08f);
                    var direction = (b - a).normalized; var normal = new Vector2(-direction.y, direction.x) * 0.035f;
                    int start = vertices.currentVertCount;
                    vertices.AddVert(center + (a - normal) * size, color, Vector2.zero);
                    vertices.AddVert(center + (a + normal) * size, color, Vector2.zero);
                    vertices.AddVert(center + (b + normal) * size, color, Vector2.zero);
                    vertices.AddVert(center + (b - normal) * size, color, Vector2.zero);
                    vertices.AddTriangle(start, start + 1, start + 2); vertices.AddTriangle(start, start + 2, start + 3);
                }
        }
    }
}
