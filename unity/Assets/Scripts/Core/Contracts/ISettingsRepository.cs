// Navy.Core.Contracts
// Pure C# - NO UnityEngine dependency

namespace Navy.Core.Contracts
{
    /// <summary>
    /// Abstraction for persistent local settings (PlayerPrefs).
    /// </summary>
    public interface ISettingsRepository
    {
        string Nickname { get; set; }
        float SfxVolume { get; set; }       // 0–1
        float MusicVolume { get; set; }     // 0–1
        bool VibrationEnabled { get; set; }
        string Language { get; set; }       // "uk" / "en"

        void Save();
    }
}
