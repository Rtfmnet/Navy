// Navy.Infrastructure

using Navy.Core.Contracts;
using Navy.Presentation.Common;

namespace Navy.Infrastructure
{
    /// <summary>
    /// Simple static service registry. Registered in AppBootstrap.Awake().
    /// For MVP this is sufficient; migrate to Zenject/VContainer if needed.
    /// </summary>
    public static class ServiceLocator
    {
        public static ISessionService    Session   { get; set; }
        public static ISettingsRepository Settings  { get; set; }
        public static SoundManager       Sound     { get; set; }
        public static MusicManager       Music     { get; set; }
        public static VibrationManager   Vibration { get; set; }
        public static PanelRouter        Router    { get; set; }
    }
}
