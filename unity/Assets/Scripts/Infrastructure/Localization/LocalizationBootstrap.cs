// Navy.Infrastructure.Localization

using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Navy.Infrastructure.Localization
{
    /// <summary>
    /// Ensures Unity Localization is initialized before first use.
    /// Called from AppBootstrap. The localization system initializes lazily by default;
    /// this component triggers it explicitly to avoid first-frame flicker.
    /// </summary>
    public sealed class LocalizationBootstrap : MonoBehaviour
    {
        private async void Awake()
        {
            await LocalizationSettings.InitializationOperation.Task;
#if UNITY_EDITOR || DEBUG_BUILD
            Debug.Log("[LocalizationBootstrap] Localization ready. " +
                      $"Locale: {LocalizationSettings.SelectedLocale?.Identifier.Code}");
#endif
        }
    }
}
