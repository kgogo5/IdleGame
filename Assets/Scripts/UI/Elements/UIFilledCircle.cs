using UnityEngine;
using UnityEngine.UI;

namespace IdleGame.UI
{
    // 꽉 찬 원형(디스크)을 그리는 커스텀 UI 그래픽
    [RequireComponent(typeof(RectTransform))]
    public class UIFilledCircle : MaskableGraphic
    {
        private const int SEGMENTS = 64;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            float r = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;

            vh.AddVert(Vector2.zero, color, Vector2.zero);

            for (int i = 0; i <= SEGMENTS; i++)
            {
                float angle = (float)i / SEGMENTS * Mathf.PI * 2f;
                vh.AddVert(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r, color, Vector2.zero);
            }

            for (int i = 0; i < SEGMENTS; i++)
                vh.AddTriangle(0, i + 1, i + 2);
        }
    }
}
