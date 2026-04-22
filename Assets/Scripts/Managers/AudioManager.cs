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

            if (bgmClip == null) bgmClip = PlaceholderAudio.MakeBgm();
            _hitClip  = PlaceholderAudio.MakeHit();
            _goldClip = PlaceholderAudio.MakeGoldPing();

            bgmSource.clip   = bgmClip;
            bgmSource.volume = 1f;
            bgmSource.mute   = false;
            bgmSource.Play();

            Debug.Log($"[AudioManager] BGM Play 호출 — isPlaying={bgmSource.isPlaying}, clip={bgmClip?.name}, mute={bgmSource.mute}");

            SettingsManager sm = SettingsManager.Instance;
            if (sm == null)
            {
                Debug.LogError("[AudioManager] SettingsManager.Instance가 null");
                return;
            }

            sm.OnBgmChanged   += ApplyBgm;
            sm.OnSoundChanged += ApplySound;
            ApplyBgm(sm.BgmEnabled);
            ApplySound(sm.SoundEnabled);

            Debug.Log($"[AudioManager] 초기화 완료 — BGM={sm.BgmEnabled}, Sound={sm.SoundEnabled}");
        }

        private void OnDestroy()
        {
            if (SettingsManager.Instance == null) return;
            SettingsManager.Instance.OnBgmChanged   -= ApplyBgm;
            SettingsManager.Instance.OnSoundChanged -= ApplySound;
        }

        private void ApplyBgm(bool on)   => bgmSource.mute = !on;
        private void ApplySound(bool on) => sfxSource.mute = !on;

        public void PlayHit()      => PlaySfx(_hitClip);
        public void PlayGoldPing() => PlaySfx(_goldClip);

        public void PlaySfx(AudioClip clip)
        {
            if (clip == null || sfxSource.mute) return;
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
