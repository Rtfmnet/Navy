// Navy.Core.Models
// Pure C# - NO UnityEngine dependency

using System.Collections.Generic;

namespace Navy.Core.Models
{
    /// <summary>
    /// Ship placement on the board with its current hit state.
    /// </summary>
    public sealed class Ship
    {
        public int Decks { get; }
        public ShipOrientation Orientation { get; set; }
        public Cell Origin { get; set; }   // top-left cell

        private readonly bool[] _hitMap;

        public Ship(int decks, ShipOrientation orientation, Cell origin)
        {
            Decks = decks;
            Orientation = orientation;
            Origin = origin;
            _hitMap = new bool[decks];
        }

        /// <summary>Returns all cells occupied by this ship.</summary>
        public IReadOnlyList<Cell> GetCells()
        {
            var cells = new Cell[Decks];
            for (int i = 0; i < Decks; i++)
            {
                int x = Orientation == ShipOrientation.Horizontal ? Origin.X + i : Origin.X;
                int y = Orientation == ShipOrientation.Vertical   ? Origin.Y + i : Origin.Y;
                cells[i] = new Cell(x, y);
            }
            return cells;
        }

        /// <summary>Registers a hit on the cell. Returns true if this cell belongs to the ship.</summary>
        public bool TryHit(Cell cell)
        {
            var cells = GetCells();
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].Equals(cell))
                {
                    _hitMap[i] = true;
                    return true;
                }
            }
            return false;
        }

        public bool IsSunk()
        {
            foreach (var hit in _hitMap)
                if (!hit) return false;
            return true;
        }

        public bool IsHitAt(Cell cell)
        {
            var cells = GetCells();
            for (int i = 0; i < cells.Count; i++)
                if (cells[i].Equals(cell)) return _hitMap[i];
            return false;
        }
    }
}
