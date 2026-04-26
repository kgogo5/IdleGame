using UnityEngine;
using UnityEngine.UI;

namespace IdleGame.Core
{
    [RequireComponent(typeof(Image))]
    public class BackgroundLoader : MonoBehaviour
    {
        [SerializeField] private string _spritePath = "Backgrounds/stage1_bg";

        private void Start()
        {
            Sprite bg = Resources.Load<Sprite>(_spritePath);
            if (bg == null)
            {
                Debug.LogWarning($"[BackgroundLoader] 스프라이트를 찾을 수 없습니다: Resources/{_spritePath}");
                return;
            }
            Image img = GetComponent<Image>();
            img.sprite = bg;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
        }
    }
}
