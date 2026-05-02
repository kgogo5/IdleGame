using UnityEngine;

namespace IdleGame.Managers
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("BGM (비워두면 임시 사운드 자동 생성)")]
        [SerializeField] private AudioClip   bgmClip;
        [SerializeField] private AudioSource bgmSource;

        [Header("SFX (비워두면 임시 사운드 자동 생성)")]
        [SerializeField] private AudioSource sfxSource;

        private AudioClip _hitClip;
        private AudioClip _goldClip;
        private string _currentBgmPath;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (bgmSource == null) bgmSource = MakeSource("BGMSource", loop: true);
            if (sfxSource == null) sfxSource = MakeSource("SFXSource", loop: false);
        }

        private void Start()
        {
            Debug.Log("[AudioManager] Start() 시작");

            if (bgmClip == null)
                bgmClip = Resources.Load<AudioClip>("Audio/BGM/bgm_main");
            if (bgmClip == null)
                bgmClip = PlaceholderAudio.MakeBgm();
            _hitClip  = PlaceholderAudio.MakeHit();
            _goldClip = PlaceholderAudio.MakeGoldPing();

            bgmSource.clip   = bgmClip;
            bgmSource.mute   = false;
            bgmSource.Play();

            Debug.Log($"[AudioManager] BGM Play — isPlaying={bgmSource.isPlaying}");

            SettingsManager sm = SettingsManager.Instance;
            if (sm == null) { Debug.LogError("[AudioManager] SettingsManager null"); return; }

            sm.OnBgmVolumeChanged += v => bgmSource.volume = v;
            sm.OnSfxVolumeChanged += v => sfxSource.volume = v;
            bgmSource.volume = sm.BgmVolume;
            sfxSource.volume = sm.SfxVolume;

            Debug.Log($"[AudioManager] 초기화 완료 — BGM={sm.BgmVolume:F2}, SFX={sm.SfxVolume:F2}");
        }

        private void OnDestroy()
        {
            if (SettingsManager.Instance == null) return;
            SettingsManager.Instance.OnBgmVolumeChanged -= v => bgmSource.volume = v;
            SettingsManager.Instance.OnSfxVolumeChanged -= v => sfxSource.volume = v;
        }

        // BGM을 Resources 경로로 교체 (같은 경로면 무시)
        public void PlayBgmByPath(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath) || resourcePath == _currentBgmPath) return;
            var clip = Resources.Load<AudioClip>(resourcePath);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] BGM 로드 실패: Resources/{resourcePath}");
                return;
            }
            _currentBgmPath = resourcePath;
            bgmSource.clip  = clip;
            bgmSource.Play();
        }

        public void PlayHit()      => PlaySfx(_hitClip);
        public void PlayGoldPing() => PlaySfx(_goldClip);

        public void PlaySfx(AudioClip clip)
        {
            if (clip == null) return;
            sfxSource.PlayOneShot(clip);
        }

        public static void TryVibrate()
        {
            if (SettingsManager.Instance == null || !SettingsManager.Instance.VibrationEnabled) return;
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        private AudioSource MakeSource(string label, bool loop)
        {
            GameObject go = new GameObject(label);
            go.transform.SetParent(transform);
            AudioSource src = go.AddComponent<AudioSource>();
            src.loop        = loop;
            src.playOnAwake = false;
            src.volume      = 1f;
            return src;
        }
    }
}
