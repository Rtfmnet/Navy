// Navy.Presentation.Settings

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Navy.Presentation.Settings
{
    /// <summary>
    /// Settings screen UI bindings.
    /// Wire in Inspector on PC #2.
    /// </summary>
    public sealed class SettingsView : Navy.Presentation.Common.UIPanelBase
    {
        [Header("Sound")]
        [SerializeField] public Slider   SfxSlider;
        [SerializeField] public Slider   MusicSlider;

        [Header("Vibration")]
        [SerializeField] public Toggle   VibrationToggle;

        [Header("Language")]
        [SerializeField] public TMP_Dropdown LanguageDropdown;  // options: Ukrainian, English

        [Header("Nickname")]
        [SerializeField] public TMP_InputField NicknameInput;
        [SerializeField] public Button         SaveNicknameButton;
        [SerializeField] public TMP_Text       NicknameError;

        [Header("Navigation")]
        [SerializeField] public Button BackButton;
    }
}
