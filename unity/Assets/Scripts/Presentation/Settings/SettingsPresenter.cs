// Navy.Presentation.Settings

using Navy.Core.Contracts;
using Navy.Infrastructure;
using Navy.Presentation.Common;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Navy.Presentation.Settings
{
    /// <summary>
    /// Settings screen presenter (FR-ST).
    /// Applies changes immediately and persists via ISettingsRepository.
    /// </summary>
    public sealed class SettingsPresenter : MonoBehaviour
    {
        [SerializeField] private SettingsView _view;

        private ISettingsRepository _settings;
        private PanelRouter         _router;

        private void OnEnable()
        {
            _settings = ServiceLocator.Settings;
            _router   = ServiceLocator.Router;

            // Restore current values
            _view.SfxSlider.value         = _settings.SfxVolume;
            _view.MusicSlider.value        = _settings.MusicVolume;
            _view.VibrationToggle.isOn     = _settings.VibrationEnabled;
            _view.NicknameInput.text       = _settings.Nickname;
            _view.NicknameError.gameObject.SetActive(false);

            // Language dropdown: index 0 = uk, 1 = en
            _view.LanguageDropdown.value = _settings.Language == "uk" ? 0 : 1;

            // Listeners
            _view.SfxSlider.onValueChanged.AddListener(OnSfxChanged);
            _view.MusicSlider.onValueChanged.AddListener(OnMusicChanged);
            _view.VibrationToggle.onValueChanged.AddListener(OnVibrationChanged);
            _view.LanguageDropdown.onValueChanged.AddListener(OnLanguageChanged);
            _view.SaveNicknameButton.onClick.AddListener(OnSaveNickname);
            _view.BackButton.onClick.AddListener(OnBack);
        }

        private void OnDisable()
        {
            _view.SfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
            _view.MusicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            _view.VibrationToggle.onValueChanged.RemoveListener(OnVibrationChanged);
            _view.LanguageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);
        }

        private void OnSfxChanged(float v)
        {
            _settings.SfxVolume = v;
            ServiceLocator.Sound?.SetVolume(v);
        }

        private void OnMusicChanged(float v)
        {
            _settings.MusicVolume = v;
            ServiceLocator.Music?.SetVolume(v);
        }

        private void OnVibrationChanged(bool enabled)
        {
            _settings.VibrationEnabled = enabled;
            ServiceLocator.Vibration?.SetEnabled(enabled);
        }

        private void OnLanguageChanged(int index)
        {
            string lang = index == 0 ? "uk" : "en";
            _settings.Language = lang;
            var locale = LocalizationSettings.AvailableLocales.GetLocale(lang);
            if (locale != null) LocalizationSettings.SelectedLocale = locale;
        }

        private void OnSaveNickname()
        {
            string nick = _view.NicknameInput.text?.Trim();
            bool valid  = !string.IsNullOrWhiteSpace(nick) && nick.Length >= 3 && nick.Length <= 16;
            _view.NicknameError.gameObject.SetActive(!valid);
            if (!valid) return;
            _settings.Nickname = nick;
            _settings.Save();
        }

        private void OnBack()
        {
            _settings.Save();
            _router.Show(AppScreen.Menu);
        }
    }
}
