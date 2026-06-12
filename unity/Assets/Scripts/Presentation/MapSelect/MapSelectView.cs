// Navy.Presentation.MapSelect

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Navy.Presentation.MapSelect
{
    /// <summary>
    /// Map selection screen UI bindings.
    /// Wire in Inspector on PC #2.
    /// </summary>
    public sealed class MapSelectView : Navy.Presentation.Common.UIPanelBase
    {
        [SerializeField] public Button   SmallButton;
        [SerializeField] public Button   MediumButton;
        [SerializeField] public Button   LargeButton;
        [SerializeField] public TMP_Text StatusText;
        [SerializeField] public TMP_Text SelectedMapText;
    }
}
