using UnityEngine;
using TMPro;
using IdleGame.Core;

namespace IdleGame.UI
{
    public class StageDisplay : MonoBehaviour
    {
        private TextMeshProUGUI _text;

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
        }

        private void Start()
        {
            if (MonsterManager.Instance != null)
            {
                MonsterManager.Instance.OnStageChanged += UpdateStage;
                UpdateStage(MonsterManager.Instance.Stage);
            }
        }

        private void UpdateStage(int stage)
        {
            if (_text != null)
                _text.text = $"Stage {stage}";
        }

        private void OnDestroy()
        {
            if (MonsterManager.Instance != null)
                MonsterManager.Instance.OnStageChanged -= UpdateStage;
        }
    }
}
