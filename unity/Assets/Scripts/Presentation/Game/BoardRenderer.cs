// Navy.Presentation.Game

using System.Collections.Generic;
using Navy.Core.Models;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Navy.Presentation.Game
{
    /// <summary>
    /// Renders a Board onto a grid of UI Image cells.
    /// Supports two modes: OpponentBoard (shots display only) and OwnBoard (mini-map).
    /// FR-GP-04: never reveal the shape of a sunken ship on opponent's board.
    /// </summary>
    public sealed class BoardRenderer : MonoBehaviour
    {
        [SerializeField] private RectTransform _container;
        [SerializeField] private GameObject    _cellPrefab;  // assigned on PC #2
        [SerializeField] private bool          _isOpponentBoard;

        [Header("Cell Colors")]
        [SerializeField] private Color _colorEmpty    = Color.white;
        [SerializeField] private Color _colorShip     = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color _colorHit      = new Color(1f, 0.3f, 0.1f);
        [SerializeField] private Color _colorMiss     = new Color(0.7f, 0.7f, 0.9f);
        [SerializeField] private Color _colorAdjacent = new Color(0.8f, 0.8f, 1f);
        [SerializeField] private Color _colorAim      = new Color(1f, 1f, 0f, 0.6f);

        private int _boardSize;
        private Image[,] _cells;
        private Cell _aimCell;

        // ─── Initialization ───────────────────────────────────────────────────────

        public void Initialize(int boardSize)
        {
            _boardSize = boardSize;
            _cells     = new Image[boardSize, boardSize];

            foreach (Transform child in _container)
                Destroy(child.gameObject);

            for (int y = 0; y < boardSize; y++)
            {
                for (int x = 0; x < boardSize; x++)
                {
                    var go  = Instantiate(_cellPrefab, _container);
                    go.name = $"Cell_{x}_{y}";
                    _cells[x, y] = go.GetComponent<Image>();
                }
            }
        }

        // ─── Rendering ────────────────────────────────────────────────────────────

        public void Render(Board board)
        {
            for (int x = 0; x < _boardSize; x++)
            {
                for (int y = 0; y < _boardSize; y++)
                {
                    var state = board.GetCell(x, y);
                    _cells[x, y].color = GetColor(state, x, y);
                }
            }
        }

        public void SetAimCell(Cell cell)
        {
            _aimCell = cell;
        }

        public void ClearAim()
        {
            _aimCell = null;
        }

        private Color GetColor(CellState state, int x, int y)
        {
            if (_aimCell != null && _aimCell.X == x && _aimCell.Y == y)
                return _colorAim;

            return state switch
            {
                CellState.Empty    => _colorEmpty,
                CellState.Ship     => _isOpponentBoard ? _colorEmpty : _colorShip,  // FR-GP-04
                CellState.Hit      => _colorHit,
                CellState.Miss     => _colorMiss,
                CellState.Adjacent => _colorAdjacent,
                _                  => _colorEmpty
            };
        }

        // ─── Click passthrough ────────────────────────────────────────────────────

        public Cell GetCellAt(Vector2 screenPos)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _container, screenPos, null, out var local)) return null;

            var rect  = _container.rect;
            float cw  = rect.width  / _boardSize;
            float ch  = rect.height / _boardSize;
            int x     = Mathf.FloorToInt((local.x - rect.xMin) / cw);
            int y     = Mathf.FloorToInt((local.y - rect.yMin) / ch);

            if (x < 0 || x >= _boardSize || y < 0 || y >= _boardSize) return null;
            return new Cell(x, y);
        }
    }
}
