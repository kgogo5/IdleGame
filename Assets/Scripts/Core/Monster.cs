using System;
using System.Collections;
using UnityEngine;
using IdleGame.Data;
using IdleGame.Managers;

namespace IdleGame.Core
{
    public class Monster : MonoBehaviour
    {
        private MonsterData _data;
        private double _maxHealth;
        private double _goldReward;
        private double _currentHealth;
        private Vector3 _originalPosition;

        [SerializeField] private float _shakeDuration = 0.1f;
        [SerializeField] private float _shakeAmount = 0.1f;

        public double CurrentHealth => _currentHealth;
        public double MaxHealth => _maxHealth;
        public string MonsterName => _data != null ? _data.monsterName : "";

        public event Action<double, double> OnHealthChanged;

        public void Setup(MonsterData data)
        {
            Setup(data, data.maxHealth, data.goldReward);
        }

        public void Setup(MonsterData data, double maxHp, double goldReward)
        {
            _data = data;
            _maxHealth = maxHp;
            _goldReward = goldReward;
            _currentHealth = _maxHealth;
            _originalPosition = transform.position;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = data.sprite;

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        public void TakeDamage(double damage)
        {
            _currentHealth -= damage;
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            StartCoroutine(ShakeEffect());
            if (_currentHealth <= 0) Die();
        }

        private IEnumerator ShakeEffect()
        {
            float elapsed = 0f;
            while (elapsed < _shakeDuration)
            {
                float x = _originalPosition.x + UnityEngine.Random.Range(-_shakeAmount, _shakeAmount);
                float y = _originalPosition.y + UnityEngine.Random.Range(-_shakeAmount, _shakeAmount);
                transform.position = new Vector3(x, y, _originalPosition.z);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = _originalPosition;
        }

        private void Die()
        {
            CurrencyManager.Instance.AddGold(_goldReward);
            AudioManager.Instance?.PlayGoldPing();
            StartCoroutine(DieEffect());
        }

        private IEnumerator DieEffect()
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            float duration = 0.35f;
            float elapsed  = 0f;

            Vector3 startScale    = transform.localScale;
            float   startRotation = transform.eulerAngles.z;
            float   targetRotation = startRotation + UnityEngine.Random.Range(-30f, 30f); // 랜덤으로 좌우 기울기

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float smooth = 1f - (1f - t) * (1f - t); // ease-out

                // 기울기
                transform.rotation = Quaternion.Euler(0f, 0f,
                    Mathf.LerpAngle(startRotation, targetRotation, smooth));

                // 아래로 살짝 이동
                transform.position = _originalPosition + Vector3.down * (0.3f * smooth);

                // 페이드 아웃
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = 1f - smooth;
                    sr.color = c;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            MonsterManager.Instance.OnMonsterKilled();
            Destroy(gameObject);
        }

        private void OnMouseDown()
        {
            if (Time.timeScale == 0f) return;
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            var stats = Managers.PlayerStats.Instance;
            if (stats == null || !stats.TryConsumeClick()) return;

            Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            pos.z = 0f;
            string effectId = InventoryManager.Instance?.GetEquippedParticleEffectId() ?? "hit_punch";
            ParticleManager.Instance?.Spawn(effectId, pos);

            TakeDamage(stats.ClickDamage);
            AudioManager.Instance?.PlayHit();
            AudioManager.TryVibrate();
        }
    }
}
