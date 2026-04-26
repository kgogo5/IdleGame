using System;
using UnityEngine;

namespace IdleGame.Managers
{
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        public float BgmVolume          { get; private set; }
        public float SfxVolume          { get; private set; }
        public bool  VibrationEnabled   { get; private set; }
        public bool  NotificationEnabled{ get; private set; }

        public event Action<float> OnBgmVolumeChanged;
        public event Action<float> OnSfxVolumeChanged;
        public event Action<bool>  OnVibrationChanged;
        public event Action<bool>  OnNotificationChanged;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        private void Load()
        {
            BgmVolume          = PlayerPrefs.GetFloat("set_bgm_vol",  0.8f);
            SfxVolume          = PlayerPrefs.GetFloat("set_sfx_vol",  0.8f);
            VibrationEnabled   = PlayerPrefs.GetInt("set_vibration",  1) == 1;
            NotificationEnabled= PlayerPrefs.GetInt("set_notification",1) == 1;
        }

        private void Save()
        {
            PlayerPrefs.SetFloat("set_bgm_vol",      BgmVolume);
            PlayerPrefs.SetFloat("set_sfx_vol",      SfxVolume);
            PlayerPrefs.SetInt("set_vibration",       VibrationEnabled   ? 1 : 0);
            PlayerPrefs.SetInt("set_notification",    NotificationEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetBgmVolume(float value)
        {
            BgmVolume = Mathf.Clamp01(value);
            OnBgmVolumeChanged?.Invoke(BgmVolume);
            Save();
        }

        public void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            OnSfxVolumeChanged?.Invoke(SfxVolume);
            Save();
        }

        public void SetVibration(bool value)
        {
            VibrationEnabled = value;
            OnVibrationChanged?.Invoke(value);
            Save();
        }

        public void SetNotification(bool value)
        {
            NotificationEnabled = value;
            OnNotificationChanged?.Invoke(value);
            Save();
        }
    }
}
