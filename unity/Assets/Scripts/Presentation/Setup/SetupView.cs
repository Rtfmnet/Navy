// Navy.Presentation.Setup

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Navy.Presentation.Setup
{
    /// <summary>
    /// Setup screen UI bindings.
    /// Wire in Inspector on PC #2.
    /// </summary>
    public sealed class SetupView : Navy.Presentation.Common.UIPanelBase
    {
        [Header("Board")]
        [SerializeField] public RectTransform BoardContainer;  // Grid for ship placement

        [Header("Ship tray")]
        [SerializeField] public Transform ShipTray;            // Parent for unplaced ship icons

        [Header("Buttons")]
        [SerializeField] public Button AutoPlaceButton;
        [SerializeField] public Button ClearButton;
        [SerializeField] public Button RotateButton;
        [SerializeField] public Button ReadyButton;

        [Header("Status")]
        [SerializeField] public TMP_Text StatusText;
        [SerializeField] public GameObject InvalidPlacementIndicator;

        [Header("Cell Prefab")]
        [SerializeField] public GameObject CellPrefab;         // Prefab created on PC #2
    }
}
