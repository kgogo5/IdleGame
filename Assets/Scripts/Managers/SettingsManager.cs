using System;
using UnityEngine;

namespace IdleGame.Managers
{
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        public bool SoundEnabled        { get; private set; }
        public bool BgmEnabled          { get; private set; }
        public bool VibrationEnabled    { get; private set; }
        public bool NotificationEnabled { get; private set; }

        public event Action<bool> OnSoundChanged;
        public event Action<bool> OnBgmChanged;
        public event Action<bool> OnVibrationChanged;
        public event Action<bool> OnNotificationChanged;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        private void Load()
        {
            SoundEnabled        = PlayerPrefs.GetInt("set_sound",        1) == 1;
            BgmEnabled          = PlayerPrefs.GetInt("set_bgm",          1) == 1;
            VibrationEnabled    = PlayerPrefs.GetInt("set_vibration",    1) == 1;
            NotificationEnabled = PlayerPrefs.GetInt("set_notification", 1) == 1;
        }

        private void Save()
        {
            PlayerPrefs.SetInt("set_sound",        SoundEnabled        ? 1 : 0);
            PlayerPrefs.SetInt("set_bgm",          BgmEnabled          ? 1 : 0);
            PlayerPrefs.SetInt("set_vibration",    VibrationEnabled    ? 1 : 0);
            PlayerPrefs.SetInt("set_notification", NotificationEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetSound(bool value)
        {
            SoundEnabled = value;
            OnSoundChanged?.Invoke(value);
            // TODO: AudioManager 연동
            Save();
        }

        public void SetBgm(bool value)
        {
            BgmEnabled = value;
            OnBgmChanged?.Invoke(value);
            // TODO: AudioManager BGM 연동
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
