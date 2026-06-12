// Navy.Presentation.Menu

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Navy.Presentation.Menu
{
    /// <summary>
    /// Main menu UI bindings.
    /// Wire all references in the Inspector on PC #2.
    /// </summary>
    public sealed class MenuView : Navy.Presentation.Common.UIPanelBase
    {
        [Header("Nickname")]
        [SerializeField] public TMP_InputField NicknameInput;
        [SerializeField] public Button         SaveNicknameButton;
        [SerializeField] public TMP_Text       NicknameError;

        [Header("Navigation")]
        [SerializeField] public Button HostButton;
        [SerializeField] public Button JoinButton;
        [SerializeField] public Button SettingsButton;
    }
}
