// Navy.Presentation.Result

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Navy.Presentation.Result
{
    /// <summary>
    /// Result screen UI bindings.
    /// Wire in Inspector on PC #2.
    /// </summary>
    public sealed class ResultView : Navy.Presentation.Common.UIPanelBase
    {
        [Header("Outcome")]
        [SerializeField] public TMP_Text OutcomeText;   // "Victory" / "Defeat" / "Draw"

        [Header("Statistics")]
        [SerializeField] public TMP_Text StatsText;     // multi-line session stats

        [Header("Buttons")]
        [SerializeField] public Button RematchButton;
        [SerializeField] public Button MainMenuButton;
    }
}
