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
        private float _regenPerSecond;
        private Vector3 _originalPosition;
        private bool _isDead;
        private SpriteRenderer _shadowRenderer;

        [SerializeField] private float _shakeDuration = 0.1f;
        [SerializeField] private float _shakeAmount = 0.1f;

        public double CurrentHealth => _currentHealth;
        public double MaxHealth => _maxHealth;
        public string MonsterName => _data != null ? _data.monsterName : "";
        public bool IsBoss => _data != null && _data.isBoss;

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
            _regenPerSecond = data.regenPerSecond;
            _originalPosition = transform.position;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = data.sprite;
                sr.color  = data.tintColor;
            }

            var s = data.spriteSize;
            if (s != Vector2.zero)
            {
                float m = Data.GameConfig.Get().monsterSizeMultiplier;
                transform.localScale = new Vector3(s.x * m, s.y * m, 1f);
            }

            SetupShadow(data);

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        private void Update()
        {
            if (_regenPerSecond <= 0f || _currentHealth <= 0) return;
            _currentHealth = System.Math.Min(_currentHealth + _regenPerSecond * Time.deltaTime, _maxHealth);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        public void TakeDamage(double damage)
        {
            if (_isDead) return;

            double actualDamage = System.Math.Min(damage, _currentHealth);
            CurrencyManager.Instance.AddGold(_goldReward * (actualDamage / _maxHealth) * Data.GameConfig.Get().goldOnHitRatio);

            _currentHealth = System.Math.Max(0, _currentHealth - damage);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            StartCoroutine(ShakeEffect());
            if (_data.hitSprite != null)
                StartCoroutine(HitSpriteEffect());
            if (_currentHealth <= 0) Die();
        }

        private IEnumerator HitSpriteEffect()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) yield break;

            sr.sprite = _data.hitSprite;
            if (_shadowRenderer != null) _shadowRenderer.sprite = _data.hitSprite;

            yield return new WaitForSeconds(Data.GameConfig.Get().hitSpriteDuration);

            if (!_isDead && sr != null)
            {
                sr.sprite = _data.sprite;
                if (_shadowRenderer != null) _shadowRenderer.sprite = _data.sprite;
            }
        }

        private void SetupShadow(MonsterData data)
        {
            var cfg = Data.GameConfig.Get();
            if (!cfg.shadowEnabled) return;

            // 기존 그림자 제거
            var old = transform.Find("Shadow");
            if (old != null) Destroy(old.gameObject);

            var mainSr = GetComponent<SpriteRenderer>();

            var shadowObj = new GameObject("Shadow");
            shadowObj.transform.SetParent(transform, false);

            // 스프라이트 하단 기준으로 위치 계산
            float spriteBottom = mainSr.localBounds.min.y;
            shadowObj.transform.localPosition = new Vector3(cfg.shadowOffset.x, spriteBottom + cfg.shadowOffset.y, 0.01f);
            shadowObj.transform.localScale    = new Vector3(cfg.shadowScale.x, cfg.shadowScale.y, 1f);
            _shadowRenderer = shadowObj.AddComponent<SpriteRenderer>();
            _shadowRenderer.sprite           = data.sprite;
            _shadowRenderer.color            = cfg.shadowColor;
            _shadowRenderer.sortingLayerName = mainSr.sortingLayerName;
            _shadowRenderer.sortingOrder     = mainSr.sortingOrder - 1;
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
            _isDead = true;
            CurrencyManager.Instance.AddGold(_goldReward);
            AudioManager.Instance?.PlayGoldPing();
            RollDrop();
            StartCoroutine(DieEffect());
        }

        private IEnumerator DieEffect()
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            float duration = 0.35f;
            float elapsed  = 0f;

            Vector3 startScale    = transform.localScale;
            float   startRotation = transform.eulerAngles.z;
            float   targetRotation = startRotation + UnityEngine.Random.Range(-30f, 30f);

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float smooth = 1f - (1f - t) * (1f - t);

                transform.rotation = Quaternion.Euler(0f, 0f,
                    Mathf.LerpAngle(startRotation, targetRotation, smooth));

                transform.position = _originalPosition + Vector3.down * (0.3f * smooth);

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

        private void RollDrop()
        {
            if (_data == null || Managers.InventoryManager.Instance == null) return;

            // 보스는 무조건 레어+ 아이템 1개 보장
            if (_data.isBoss)
            {
                Managers.InventoryManager.Instance.GiveBossGuaranteedDrop(
                    _data.rareWeight, _data.uniqueWeight, _data.legendaryWeight);
                return;
            }

            float effectiveDropChance = _data.dropChance * (float)(Managers.PlayerStats.Instance?.DropRateMultiplier ?? 1.0);
            if (UnityEngine.Random.value > effectiveDropChance) return;

            // 커스텀 + 등급 풀을 합산한 가중치 테이블 구성
            var pool = new System.Collections.Generic.List<(float weight, System.Action give)>();

            if (_data.customDrops != null)
                foreach (var entry in _data.customDrops)
                    if (entry.weight > 0 && entry.item != null)
                    {
                        var item = entry.item;
                        pool.Add((entry.weight, () => Managers.InventoryManager.Instance.GiveItem(item.name)));
                    }

            if (_data.normalWeight    > 0) pool.Add((_data.normalWeight,    () => Managers.InventoryManager.Instance.GiveRandomItem(Data.ItemRarity.Normal)));
            if (_data.rareWeight      > 0) pool.Add((_data.rareWeight,      () => Managers.InventoryManager.Instance.GiveRandomItem(Data.ItemRarity.Rare)));
            if (_data.uniqueWeight    > 0) pool.Add((_data.uniqueWeight,    () => Managers.InventoryManager.Instance.GiveRandomItem(Data.ItemRarity.Unique)));
            if (_data.legendaryWeight > 0) pool.Add((_data.legendaryWeight, () => Managers.InventoryManager.Instance.GiveRandomItem(Data.ItemRarity.Legendary)));

            if (pool.Count == 0) return;

            float total = 0f;
            foreach (var (w, _) in pool) total += w;
            float roll = UnityEngine.Random.value * total;
            float cumul = 0f;
            foreach (var (w, give) in pool)
            {
                cumul += w;
                if (roll <= cumul) { give(); return; }
            }
            pool[pool.Count - 1].give();
        }

        private void OnMouseDown()
        {
            if (Time.timeScale == 0f) return;
            if (IsPointerOverUI()) return;

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

        private static readonly System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult> _raycastResults = new();
        private static bool IsPointerOverUI()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null) return false;
            var pointerData = new UnityEngine.EventSystems.PointerEventData(es)
            {
                position = Input.mousePosition
            };
            _raycastResults.Clear();
            es.RaycastAll(pointerData, _raycastResults);
            return _raycastResults.Count > 0;
        }
    }
}
