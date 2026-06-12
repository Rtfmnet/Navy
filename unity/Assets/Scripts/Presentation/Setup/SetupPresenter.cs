// Navy.Presentation.Setup

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Navy.Core.Contracts;
using Navy.Core.Engine;
using Navy.Core.Models;
using Navy.Infrastructure;
using Navy.Presentation.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;

namespace Navy.Presentation.Setup
{
    /// <summary>
    /// Manages ship placement phase (FR-SP).
    /// Handles drag-and-drop, auto-place, clear, rotate, and ready confirmation.
    /// </summary>
    public sealed class SetupPresenter : MonoBehaviour
    {
        [SerializeField] private SetupView _view;

        private ISessionService    _session;
        private PanelRouter        _router;
        private Board              _board;
        private MapConfig          _config;
        private Ship               _selectedShip;
        private List<Ship>         _placedShips = new();
        private bool               _locked;

        private void OnEnable()
        {
            _session = ServiceLocator.Session;
            _router  = ServiceLocator.Router;
            _session.OnGameStateChanged += HandleStateChanged;

            _view.AutoPlaceButton.onClick.AddListener(OnAutoPlace);
            _view.ClearButton.onClick.AddListener(OnClear);
            _view.RotateButton.onClick.AddListener(OnRotate);
            _view.ReadyButton.onClick.AddListener(OnReady);

            _view.InvalidPlacementIndicator.SetActive(false);
            _view.ReadyButton.interactable = false;
            _locked = false;

            // Auto-initialize using the current resolved MapType
            var state = _session.CurrentState;
            if (state != null)
            {
                var config = GameRules.GetConfig(state.MapType);
                Initialize(config);
            }
        }

        private void OnDisable()
        {
            _session.OnGameStateChanged -= HandleStateChanged;
        }

        public void Initialize(MapConfig config)
        {
            _config = config;
            _board  = new Board(config.BoardSize);
            BuildBoardGrid();
            BuildShipTray();
        }

        // ─── Board grid ───────────────────────────────────────────────────────────

        private void BuildBoardGrid()
        {
            // Instantiate cell prefabs in BoardContainer (PC #2 sets up GridLayoutGroup)
            foreach (Transform child in _view.BoardContainer)
                Destroy(child.gameObject);

            for (int y = 0; y < _config.BoardSize; y++)
            {
                for (int x = 0; x < _config.BoardSize; x++)
                {
                    var cell = Instantiate(_view.CellPrefab, _view.BoardContainer);
                    cell.name = $"Cell_{x}_{y}";
                    int cx = x, cy = y;
                    var btn = cell.GetComponent<UnityEngine.UI.Button>();
                    btn?.onClick.AddListener(() => OnCellClicked(cx, cy));
                }
            }
        }

        private void BuildShipTray()
        {
            // Ship icon instantiation happens in Inspector/prefab setup on PC #2
            // ShipDragHandler components are pre-placed in ShipTray by the scene setup
        }

        // ─── Drag & drop ──────────────────────────────────────────────────────────

        public void OnShipDropped(ShipDragHandler dragHandler, PointerEventData eventData)
        {
            if (_locked) { dragHandler.ResetPosition(); return; }

            // Convert screen position to board cell
            var cell = ScreenPositionToCell(eventData.position);
            if (cell == null) { dragHandler.ResetPosition(); return; }

            var orientation = _selectedShip?.Orientation ?? ShipOrientation.Horizontal;
            var ship = new Ship(dragHandler.Decks, orientation, cell);

            if (BoardValidator.IsValidPlacement(_board, ship))
            {
                _view.InvalidPlacementIndicator.SetActive(false);
                PlaceShip(ship);
            }
            else
            {
                _view.InvalidPlacementIndicator.SetActive(true);
                dragHandler.ResetPosition();
            }
        }

        private Cell ScreenPositionToCell(Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _view.BoardContainer, screenPos, null, out var local);

            var rect   = _view.BoardContainer.rect;
            float cellW = rect.width  / _config.BoardSize;
            float cellH = rect.height / _config.BoardSize;

            int x = Mathf.FloorToInt((local.x - rect.xMin) / cellW);
            int y = Mathf.FloorToInt((local.y - rect.yMin) / cellH);

            if (x < 0 || x >= _config.BoardSize || y < 0 || y >= _config.BoardSize)
                return null;

            return new Cell(x, y);
        }

        private void PlaceShip(Ship ship)
        {
            _board.AddShip(ship);
            foreach (var c in ship.GetCells())
                _board.SetCell(c, CellState.Ship);

            RefreshBoardDisplay();
            _view.ReadyButton.interactable = IsAllShipsPlaced();
        }

        private void OnCellClicked(int x, int y)
        {
            // Select/deselect for rotation — MVP: rotate is via Rotate button on selected ship
        }

        // ─── Buttons ──────────────────────────────────────────────────────────────

        private void OnAutoPlace()
        {
            if (_locked) return;
            OnClear();
            if (AutoPlacer.TryPlace(_config, out var newBoard))
            {
                _board = newBoard;
                RefreshBoardDisplay();
                _view.ReadyButton.interactable = true;
            }
        }

        private void OnClear()
        {
            if (_locked) return;
            _board = new Board(_config.BoardSize);
            RefreshBoardDisplay();
            _view.ReadyButton.interactable = false;
        }

        private void OnRotate()
        {
            if (_locked || _selectedShip == null) return;

            _board.RemoveAllShips();
            _selectedShip.Orientation = _selectedShip.Orientation == ShipOrientation.Horizontal
                ? ShipOrientation.Vertical : ShipOrientation.Horizontal;

            if (!BoardValidator.IsValidPlacement(_board, _selectedShip))
            {
                // Revert
                _selectedShip.Orientation = _selectedShip.Orientation == ShipOrientation.Horizontal
                    ? ShipOrientation.Vertical : ShipOrientation.Horizontal;
                _view.InvalidPlacementIndicator.SetActive(true);
            }

            _board.AddShip(_selectedShip);
            RefreshBoardDisplay();
        }

        private void OnReady()
        {
            if (_locked) return;
            // FR-SP-05: confirmation dialog before locking (implemented via UI confirm panel)
            _locked = true;
            _view.AutoPlaceButton.interactable = false;
            _view.ClearButton.interactable     = false;
            _view.RotateButton.interactable    = false;
            _view.ReadyButton.interactable     = false;
            _view.StatusText.text = LocalizationSettings.StringDatabase
                .GetLocalizedString("Game", "setup.waiting_opponent");

            _session.CommitBoardAsync().Forget();
        }

        // ─── State listener ───────────────────────────────────────────────────────

        private void HandleStateChanged(GameState state)
        {
            // Persist board reference into session state for GamePresenter to consume.
            var me = state.GetPlayer(_session.LocalUid);
            if (me != null && _board != null && me.Board == null)
                me.Board = _board;

            // Host: when both players have committed, write first turn UID and advance to Playing.
            if (state.Phase == GamePhase.Setup
                && _session.IsHost
                && state.Host?.BoardCommitted == true
                && state.Guest?.BoardCommitted == true)
            {
                string firstTurnUid = GameRules.PickFirstTurnUid(state.Host.Uid, state.Guest.Uid);
                _session.StartGameAsync(firstTurnUid).Forget();
            }

            if (state.Phase == GamePhase.Playing)
                _router.Show(AppScreen.Game);
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private bool IsAllShipsPlaced()
        {
            if (_config == null) return false;
            int placed = _board.Ships.Count;
            return placed == _config.TotalShips;
        }

        private void RefreshBoardDisplay()
        {
            // Update cell visuals in BoardContainer based on _board state
            for (int x = 0; x < _config.BoardSize; x++)
            {
                for (int y = 0; y < _config.BoardSize; y++)
                {
                    var cellGO = _view.BoardContainer.Find($"Cell_{x}_{y}");
                    if (cellGO == null) continue;
                    var img = cellGO.GetComponent<UnityEngine.UI.Image>();
                    if (img == null) continue;
                    img.color = _board.GetCell(x, y) == CellState.Ship
                        ? new Color(0.2f, 0.6f, 1f)
                        : Color.white;
                }
            }
        }
    }
}
