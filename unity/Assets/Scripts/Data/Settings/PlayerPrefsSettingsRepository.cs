// Navy.Data.Settings

using Navy.Core.Contracts;
using UnityEngine;

namespace Navy.Data.Settings
{
    /// <summary>
    /// Persists user settings in PlayerPrefs.
    /// Keys are prefixed to avoid collisions.
    /// </summary>
    public sealed class PlayerPrefsSettingsRepository : ISettingsRepository
    {
        private const string KeyNickname   = "navy.nickname";
        private const string KeySfxVolume  = "navy.sfx_volume";
        private const string KeyMusicVolume = "navy.music_volume";
        private const string KeyVibration  = "navy.vibration";
        private const string KeyLanguage   = "navy.language";

        public string Nickname
        {
            get => PlayerPrefs.GetString(KeyNickname, string.Empty);
            set => PlayerPrefs.SetString(KeyNickname, value);
        }

        public float SfxVolume
        {
            get => PlayerPrefs.GetFloat(KeySfxVolume, 1f);
            set => PlayerPrefs.SetFloat(KeySfxVolume, Mathf.Clamp01(value));
        }

        public float MusicVolume
        {
            get => PlayerPrefs.GetFloat(KeyMusicVolume, 0.5f);
            set => PlayerPrefs.SetFloat(KeyMusicVolume, Mathf.Clamp01(value));
        }

        public bool VibrationEnabled
        {
            get => PlayerPrefs.GetInt(KeyVibration, 1) == 1;
            set => PlayerPrefs.SetInt(KeyVibration, value ? 1 : 0);
        }

        public string Language
        {
            get => PlayerPrefs.GetString(KeyLanguage, "uk");
            set => PlayerPrefs.SetString(KeyLanguage, value);
        }

        public void Save() => PlayerPrefs.Save();
    }
}
