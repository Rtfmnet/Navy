// Navy.Presentation.Game

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Navy.Presentation.Game
{
    /// <summary>
    /// Game screen UI bindings.
    /// Wire in Inspector on PC #2.
    /// FR-UI-02: large opponent board + mini own-board on top.
    /// FR-UI-03: double-tap to shoot.
    /// FR-UI-07: turn indicator.
    /// </summary>
    public sealed class GameView : Navy.Presentation.Common.UIPanelBase
    {
        [Header("Boards")]
        [SerializeField] public BoardRenderer  OpponentBoard;  // large, shooting target
        [SerializeField] public BoardRenderer  OwnBoardMini;   // mini-map, own ships

        [Header("HUD")]
        [SerializeField] public TMP_Text       TurnIndicatorText;
        [SerializeField] public TurnTimerView  TimerView;

        [Header("History")]
        [SerializeField] public HistoryPanelView HistoryPanel;

        [Header("Buttons")]
        [SerializeField] public Button SurrenderButton;

        [Header("Reconnect Overlay")]
        [SerializeField] public GameObject ReconnectOverlay;
        [SerializeField] public TMP_Text   ReconnectCountdownText;

        [Header("Surrender Confirmation")]
        [SerializeField] public GameObject SurrenderConfirmPanel;
        [SerializeField] public Button     SurrenderConfirmYes;
        [SerializeField] public Button     SurrenderConfirmNo;
    }
}
