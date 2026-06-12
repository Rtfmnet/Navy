// Navy.Presentation.Lobby

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Navy.Presentation.Lobby
{
    /// <summary>
    /// Lobby screen UI bindings.
    /// Wire in Inspector on PC #2.
    /// </summary>
    public sealed class LobbyView : Navy.Presentation.Common.UIPanelBase
    {
        [Header("Host mode")]
        [SerializeField] public GameObject HostPanel;
        [SerializeField] public TMP_Text   SessionCodeText;
        [SerializeField] public Button     CopyCodeButton;

        [Header("Guest mode")]
        [SerializeField] public GameObject JoinPanel;
        [SerializeField] public TMP_InputField CodeInput;
        [SerializeField] public Button         JoinButton;
        [SerializeField] public TMP_Text       JoinError;

        [Header("Status")]
        [SerializeField] public TMP_Text StatusText;

        [Header("Navigation")]
        [SerializeField] public Button BackButton;
    }
}
