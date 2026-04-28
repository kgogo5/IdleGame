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
            MonsterManager.Instance.OnMonsterKilled();
            Destroy(gameObject);
        }

        private void OnMouseDown()
        {
            if (Time.timeScale == 0f) return;
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            var stats = Managers.PlayerStats.Instance;
            if (stats == null || !stats.TryConsumeClick()) return;

            TakeDamage(stats.ClickDamage);
            AudioManager.Instance?.PlayHit();
            AudioManager.TryVibrate();
        }
    }
}
