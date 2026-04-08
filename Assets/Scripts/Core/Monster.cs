using System;
using System.Collections;
using UnityEngine;
using IdleGame.Data;

namespace IdleGame.Core
{
    public class Monster : MonoBehaviour
    {
        private MonsterData _data;
        private double _currentHealth;
        private Vector3 _originalPosition;

        [SerializeField] private float _shakeDuration = 0.1f;
        [SerializeField] private float _shakeAmount = 0.1f;

        public double CurrentHealth => _currentHealth;
        public double MaxHealth => _data != null ? _data.maxHealth : 0;
        public string MonsterName => _data != null ? _data.monsterName : "";

        public event Action<double, double> OnHealthChanged; // current, max

        public void Setup(MonsterData data)
        {
            _data = data;
            _currentHealth = data.maxHealth;
            _originalPosition = transform.position;

            // 스프라이트 설정
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = data.sprite;
            }

            OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
            Debug.Log($"Monster spawned: {MonsterName} (HP: {_currentHealth})");
        }

        public void TakeDamage(double damage)
        {
            _currentHealth -= damage;
            Debug.Log($"Monster damaged: -{damage} (HP: {_currentHealth}/{MaxHealth})");

            OnHealthChanged?.Invoke(_currentHealth, MaxHealth);

            // 흔들림 효과
            StartCoroutine(ShakeEffect());

            if (_currentHealth <= 0)
            {
                Die();
            }
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

            // 원래 위치로 복귀
            transform.position = _originalPosition;
        }

        private void Die()
        {
            // 골드 지급
            CurrencyManager.Instance.AddGold(_data.goldReward);

            // 다음 몬스터 소환
            MonsterManager.Instance.SpawnMonster();

            Destroy(gameObject);
        }

        // Unity 기본 클릭 감지 (간단한 방법)
        private void OnMouseDown()
        {
            Debug.Log("Monster clicked via OnMouseDown!");
            TakeDamage(10);
        }
    }
}
