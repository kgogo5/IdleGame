using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IdleGame.Data;

namespace IdleGame.Managers
{
    public class ParticleManager : MonoBehaviour
    {
        private static ParticleManager _instance;
        public static ParticleManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("ParticleManager");
                    _instance = go.AddComponent<ParticleManager>();
                }
                return _instance;
            }
        }

        private Dictionary<string, ParticleEffectData> _configs = new();

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadConfigs();
        }

        // ─────────────────────────────────────────
        // 로드
        // ─────────────────────────────────────────
        private void LoadConfigs()
        {
            var file = Resources.Load<TextAsset>("Particles/hit_particles");
            if (file == null)
            {
                Debug.LogWarning("[ParticleManager] hit_particles.json 파일을 찾을 수 없습니다.");
                return;
            }

            var config = JsonUtility.FromJson<ParticleEffectConfig>(file.text);
            if (config?.effects == null) return;

            foreach (var effect in config.effects)
                _configs[effect.id] = effect;

            Debug.Log($"[ParticleManager] {_configs.Count}개 이펙트 로드 완료");
        }

        // ─────────────────────────────────────────
        // 외부 호출 진입점
        // ─────────────────────────────────────────
        public void Spawn(string id, Vector3 position)
        {
            if (!_configs.TryGetValue(id, out var data))
            {
                Debug.LogWarning($"[ParticleManager] 알 수 없는 이펙트 id: {id}");
                return;
            }

            switch (data.effectType)
            {
                case "magic_circle": StartCoroutine(SpawnMagicCircle(position, data)); break;
                case "slash_line":   StartCoroutine(SpawnSlashLine(position, data));   break;
                case "stab_line":    StartCoroutine(SpawnStabLine(position, data));    break;
                default:             SpawnBurst(position, data);                       break;
            }
        }

        private void SpawnBurst(Vector3 position, ParticleEffectData data)
        {
            var go = new GameObject($"Particle_{data.id}");
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();

            ApplyMain(ps, data);
            ApplyEmission(ps, data);
            ApplyShape(ps, data);
            ApplyRenderer(ps, data);
            ApplySizeOverLifetime(ps, data);
            ApplyColorOverLifetime(ps, data);

            ps.Play();
        }

        // ─────────────────────────────────────────
        // 마법 원형 이펙트: 순차 스폰 → 낙하
        // ─────────────────────────────────────────
        private IEnumerator SpawnMagicCircle(Vector3 center, ParticleEffectData d)
        {
            var ps = CreateSequentialPS(center, d);

            // Phase 1: 파티클을 원 위에 하나씩 순차 스폰
            float angleStep = 360f / d.burstCount;
            for (int i = 0; i < d.burstCount; i++)
            {
                float rad = (angleStep * i) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(
                    Mathf.Cos(rad) * d.circleRadius,
                    Mathf.Sin(rad) * d.circleRadius,
                    0f
                );

                var ep = new ParticleSystem.EmitParams();
                ep.position     = center + offset;
                ep.velocity     = Vector3.zero;
                ep.startLifetime = d.startLifetime;
                ep.startSize    = Random.Range(d.startSizeMin, d.startSizeMax);
                ep.startColor   = d.color.ToColor();
                ps.Emit(ep, 1);

                yield return new WaitForSeconds(d.spawnInterval);
            }

            // Phase 2: 원 완성 → 각 파티클을 중심에서 바깥 방향으로 튕겨냄 + 중력 활성화
            var main2 = ps.main;
            main2.gravityModifier = d.gravityModifier; // 이 시점부터 중력 적용

            var particles = new ParticleSystem.Particle[ps.particleCount];
            int count = ps.GetParticles(particles);

            for (int i = 0; i < count; i++)
            {
                Vector3 outDir = (particles[i].position - center);
                if (outDir == Vector3.zero) outDir = Random.insideUnitSphere;
                outDir.z = 0f;
                outDir.Normalize();

                float speed = d.fallSpeed * Random.Range(0.8f, 1.3f);
                particles[i].velocity = outDir * speed;
            }
            ps.SetParticles(particles, count);
        }

        // ─────────────────────────────────────────
        // 베기: 대각선 순차 스폰 → 수직 퍼짐
        // ─────────────────────────────────────────
        private IEnumerator SpawnSlashLine(Vector3 center, ParticleEffectData d)
        {
            var ps = CreateSequentialPS(center, d);

            float rad = d.lineAngle * Mathf.Deg2Rad;
            Vector3 lineDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f); // 슬래시 방향
            Vector3 perpDir = new Vector3(-lineDir.y, lineDir.x, 0f);          // 수직 방향

            // Phase 1: 대각선 위에 순차 스폰
            for (int i = 0; i < d.burstCount; i++)
            {
                float t = (float)i / (d.burstCount - 1) - 0.5f; // -0.5 ~ 0.5
                Vector3 spawnPos = center + lineDir * (t * d.lineLength);

                var ep = new ParticleSystem.EmitParams();
                ep.position      = spawnPos;
                ep.velocity      = Vector3.zero;
                ep.startLifetime = d.startLifetime;
                ep.startSize     = Random.Range(d.startSizeMin, d.startSizeMax);
                ep.startColor    = d.color.ToColor();
                ps.Emit(ep, 1);

                yield return new WaitForSeconds(d.spawnInterval);
            }

            // Phase 2: 각 파티클을 슬래시 수직 방향 양쪽으로 퍼뜨림 + 중력
            var main = ps.main;
            main.gravityModifier = d.gravityModifier;

            var particles = new ParticleSystem.Particle[ps.particleCount];
            int count = ps.GetParticles(particles);

            for (int i = 0; i < count; i++)
            {
                float sign  = (i % 2 == 0) ? 1f : -1f; // 번갈아 양쪽으로
                float speed = d.fallSpeed * Random.Range(0.8f, 1.2f);
                particles[i].velocity = perpDir * sign * speed;
            }
            ps.SetParticles(particles, count);
        }

        // ─────────────────────────────────────────
        // 찌르기: 위로 순차 스폰 → 위쪽 집중 발사
        // ─────────────────────────────────────────
        private IEnumerator SpawnStabLine(Vector3 center, ParticleEffectData d)
        {
            var ps = CreateSequentialPS(center, d);

            // Phase 1: 아래→위로 선상에 순차 스폰
            for (int i = 0; i < d.burstCount; i++)
            {
                float t = (float)i / (d.burstCount - 1); // 0 ~ 1
                Vector3 spawnPos = center + Vector3.up * (t * d.stabLength);

                var ep = new ParticleSystem.EmitParams();
                ep.position      = spawnPos;
                ep.velocity      = Vector3.zero;
                ep.startLifetime = d.startLifetime;
                ep.startSize     = Random.Range(d.startSizeMin, d.startSizeMax);
                ep.startColor    = d.color.ToColor();
                ps.Emit(ep, 1);

                yield return new WaitForSeconds(d.spawnInterval);
            }

            // Phase 2: 위쪽으로 집중 발사 (약간의 X 랜덤) + 중력
            var main = ps.main;
            main.gravityModifier = d.gravityModifier;

            var particles = new ParticleSystem.Particle[ps.particleCount];
            int count = ps.GetParticles(particles);

            for (int i = 0; i < count; i++)
            {
                float spreadX = Random.Range(-0.3f, 0.3f);
                float speed   = d.fallSpeed * Random.Range(0.7f, 1.0f);
                particles[i].velocity = new Vector3(spreadX, speed, 0f);
            }
            ps.SetParticles(particles, count);
        }

        // 순차 스폰용 ParticleSystem 공통 생성
        private ParticleSystem CreateSequentialPS(Vector3 center, ParticleEffectData d)
        {
            var go = new GameObject($"Particle_{d.id}");
            go.transform.position = center;
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop            = false;
            main.playOnAwake     = false;
            main.startLifetime   = d.startLifetime;
            main.startSpeed      = 0f;
            main.startSize       = new ParticleSystem.MinMaxCurve(d.startSizeMin, d.startSizeMax);
            main.startColor      = d.color.ToColor();
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction      = ParticleSystemStopAction.Destroy;
            main.maxParticles    = d.burstCount + 5;

            var emission = ps.emission;
            emission.enabled      = false;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.enabled = false;

            ApplyRenderer(ps, d);
            ApplySizeOverLifetime(ps, d);
            ApplyColorOverLifetime(ps, d);

            ps.Play();
            return ps;
        }

        // ─────────────────────────────────────────
        // 모듈별 적용
        // ─────────────────────────────────────────
        private static void ApplyMain(ParticleSystem ps, ParticleEffectData d)
        {
            var main = ps.main;
            main.startLifetime  = d.startLifetime;
            main.startSpeed     = new ParticleSystem.MinMaxCurve(d.startSpeedMin, d.startSpeedMax);
            main.startSize      = new ParticleSystem.MinMaxCurve(d.startSizeMin, d.startSizeMax);
            main.startColor     = d.color.ToColor();
            main.gravityModifier = d.gravityModifier;
            main.loop           = false;
            main.stopAction     = ParticleSystemStopAction.Destroy;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
        }

        private static void ApplyEmission(ParticleSystem ps, ParticleEffectData d)
        {
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, d.burstCount) });
        }

        private static void ApplyShape(ParticleSystem ps, ParticleEffectData d)
        {
            var shape = ps.shape;
            shape.enabled = true;

            switch (d.shapeType)
            {
                case "sphere":
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius    = d.shapeRadius;
                    break;

                case "edge":
                    // 가로 선에서 좌우로 퍼지는 베기 효과
                    shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
                    shape.radius    = d.shapeRadius;
                    break;

                case "cone":
                    // 위쪽(앞쪽) 방향으로 집중되는 찌르기 효과
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.radius    = d.shapeRadius;
                    shape.angle     = d.shapeAngle;
                    // 위를 향하도록 회전
                    shape.rotation  = new Vector3(-90f, 0f, 0f);
                    break;

                default:
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius    = 0.1f;
                    break;
            }
        }

        private static void ApplyRenderer(ParticleSystem ps, ParticleEffectData d)
        {
            var renderer = ps.GetComponent<ParticleSystemRenderer>();

            switch (d.renderMode)
            {
                case "stretched":
                    renderer.renderMode    = ParticleSystemRenderMode.Stretch;
                    renderer.velocityScale = d.velocityScale;
                    renderer.lengthScale   = d.lengthScale;
                    break;

                default: // billboard
                    renderer.renderMode = ParticleSystemRenderMode.Billboard;
                    break;
            }

            renderer.sortingOrder = 10; // 몬스터 스프라이트(0) 앞에 렌더링
        }

        private static void ApplySizeOverLifetime(ParticleSystem ps, ParticleEffectData d)
        {
            var sol = ps.sizeOverLifetime;
            sol.enabled = d.sizeOverLifetime;

            if (!d.sizeOverLifetime) return;

            // 1.0 → 0.0 선형 감소
            sol.size = new ParticleSystem.MinMaxCurve(
                1f,
                AnimationCurve.Linear(0f, 1f, 1f, 0f)
            );
        }

        private static void ApplyColorOverLifetime(ParticleSystem ps, ParticleEffectData d)
        {
            var col = ps.colorOverLifetime;
            col.enabled = d.colorFadeOut;

            if (!d.colorFadeOut) return;

            var startColor = d.color.ToColor();
            var endColor   = d.colorEnd != null ? d.colorEnd.ToColor()
                                                : new Color(startColor.r, startColor.g, startColor.b, 0f);

            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(startColor, 0f), new GradientColorKey(endColor, 1f) },
                new[] { new GradientAlphaKey(startColor.a, 0f), new GradientAlphaKey(0f, 1f) }
            );

            col.color = new ParticleSystem.MinMaxGradient(gradient);
        }
    }
}
