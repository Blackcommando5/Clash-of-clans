using UnityEngine;
using UnityEngine.UI;

namespace Kingdoms.UI
{
    // Native UI geometry keeps borders and gradients crisp without extra texture assets.
    [AddComponentMenu("Kingdoms/UI/Welcome Panel")]
    public sealed class WelcomePanel : MaskableGraphic
    {
        [Min(0f)] public float radius = 12f;
        public Color topColor = Color.white;
        public Color bottomColor = Color.white;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            if (r.width <= 0f || r.height <= 0f) return;
            float round = Mathf.Min(radius, Mathf.Min(r.width, r.height) * 0.5f);
            Add(vh, r.center, r);
            const int segments = 8;
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 center = new Vector2(corner == 0 || corner == 3 ? r.xMax - round : r.xMin + round,
                    corner < 2 ? r.yMax - round : r.yMin + round);
                for (int step = 0; step <= segments; step++)
                {
                    float angle = (corner * 90f + step * 90f / segments) * Mathf.Deg2Rad;
                    Add(vh, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * round, r);
                }
            }
            int count = 4 * (segments + 1);
            for (int i = 1; i <= count; i++) vh.AddTriangle(0, i, i == count ? 1 : i + 1);
        }

        void Add(VertexHelper vh, Vector2 p, Rect r)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = p;
            vertex.color = Color.Lerp(bottomColor, topColor, Mathf.InverseLerp(r.yMin, r.yMax, p.y)) * color;
            vertex.uv0 = new Vector2(Mathf.InverseLerp(r.xMin, r.xMax, p.x), Mathf.InverseLerp(r.yMin, r.yMax, p.y));
            vh.AddVert(vertex);
        }
    }
}
