using UnityEngine;

namespace IdleGame.UI.Panels
{
    public class AchievementPanelUI : MonoBehaviour
    {
        private void Start()
        {
            UIHelper.MakeText(transform, "업적", 42, TextAnchor.UpperLeft,
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(20, -100), offsetMax: new Vector2(0, -20));

            UIHelper.MakeText(transform, "업적 시스템\n준비중...", 22, TextAnchor.MiddleCenter,
                anchorMin: new Vector2(0.1f, 0.3f), anchorMax: new Vector2(0.9f, 0.7f),
                offsetMin: Vector2.zero, offsetMax: Vector2.zero,
                color: new Color(0.5f, 0.5f, 0.55f));
        }
    }
}
