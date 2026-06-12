// Navy.Infrastructure

using Cysharp.Threading.Tasks;
using Navy.Data.Firebase;
using Navy.Data.Settings;
using Navy.Presentation.Common;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Navy.Infrastructure
{
    /// <summary>
    /// Application entry point. Runs on the AppBootstrap GameObject in Main.unity.
    /// Order of operations:
    ///   1. Firebase SDK init
    ///   2. Anonymous sign-in
    ///   3. Load PlayerPrefs (nick, settings)
    ///   4. Apply settings (audio, vibration, locale)
    ///   5. Register services in ServiceLocator
    ///   6. Show Menu panel
    /// </summary>
    public sealed class AppBootstrap : MonoBehaviour
    {
        [SerializeField] private PanelRouter       _router;
        [SerializeField] private SoundManager      _sound;
        [SerializeField] private MusicManager      _music;
        [SerializeField] private VibrationManager  _vibration;

        private void Awake()
        {
            // Register managers immediately so they are available
            ServiceLocator.Sound     = _sound;
            ServiceLocator.Music     = _music;
            ServiceLocator.Vibration = _vibration;
            ServiceLocator.Router    = _router;

            // Handle minimise = quit (FR-CN-07)
            Application.focusChanged += OnApplicationFocus;
        }

        private async void Start()
        {
            await BootstrapAsync();
        }

        private async UniTask BootstrapAsync()
        {
            // 1. Firebase
            await FirebaseBootstrap.InitializeAsync(this.GetCancellationTokenOnDestroy());

            // 2. Auth
            var authService  = new FirebaseAuthService();
            var sessionService = new FirebaseSessionService(authService);
            await sessionService.SignInAnonymouslyAsync(this.GetCancellationTokenOnDestroy());

            // 3 & 4. Settings + apply
            var settings = new PlayerPrefsSettingsRepository();
            ServiceLocator.Settings = settings;
            ServiceLocator.Session  = sessionService;

            _sound?.SetVolume(settings.SfxVolume);
            _music?.SetVolume(settings.MusicVolume);
            _vibration?.SetEnabled(settings.VibrationEnabled);

            // Apply locale
            var locale = LocalizationSettings.AvailableLocales.GetLocale(settings.Language);
            if (locale != null) LocalizationSettings.SelectedLocale = locale;

            // 5. Show menu
            _router.Show(Common.AppScreen.Menu);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                // FR-CN-07: minimise = leave session immediately
                ServiceLocator.Session?.LeaveSessionAsync().Forget();
            }
        }

        private void OnDestroy()
        {
            Application.focusChanged -= OnApplicationFocus;
        }
    }
}
