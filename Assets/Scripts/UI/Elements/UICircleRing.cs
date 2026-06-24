using UnityEngine;
using UnityEngine.UI;

namespace IdleGame.UI
{
    // 속이 완전히 투명한 링(원형 테두리)을 그리는 커스텀 UI 그래픽
    [RequireComponent(typeof(RectTransform))]
    public class UICircleRing : MaskableGraphic
    {
        private float _thickness = 10f;
        private const int SEGMENTS = 64;

        public float Thickness
        {
            get => _thickness;
            set { _thickness = value; SetVerticesDirty(); }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            float outerR = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
            float innerR = Mathf.Max(0f, outerR - _thickness);

            for (int i = 0; i < SEGMENTS; i++)
            {
                float a1 = (float)i       / SEGMENTS * Mathf.PI * 2f;
                float a2 = (float)(i + 1) / SEGMENTS * Mathf.PI * 2f;

                Vector2 o1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * outerR;
                Vector2 o2 = new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * outerR;
                Vector2 i1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * innerR;
                Vector2 i2 = new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * innerR;

                int idx = vh.currentVertCount;
                vh.AddVert(i1, color, Vector2.zero);
                vh.AddVert(o1, color, Vector2.zero);
                vh.AddVert(o2, color, Vector2.zero);
                vh.AddVert(i2, color, Vector2.zero);

                vh.AddTriangle(idx,     idx + 1, idx + 2);
                vh.AddTriangle(idx,     idx + 2, idx + 3);
            }
        }
    }
}
