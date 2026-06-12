// Navy.Core.Models
// Pure C# - NO UnityEngine dependency

using System.Collections.Generic;

namespace Navy.Core.Models
{
    /// <summary>
    /// Represents a single player's board (their ships + shot results on that board).
    /// </summary>
    public sealed class Board
    {
        public int Size { get; }
        private readonly List<Ship> _ships = new List<Ship>();
        private readonly CellState[,] _grid;

        public Board(int size)
        {
            Size = size;
            _grid = new CellState[size, size];
        }

        public IReadOnlyList<Ship> Ships => _ships;

        public void AddShip(Ship ship) => _ships.Add(ship);

        public void RemoveAllShips() => _ships.Clear();

        public CellState GetCell(int x, int y) => _grid[x, y];
        public CellState GetCell(Cell c) => _grid[c.X, c.Y];

        public void SetCell(int x, int y, CellState state) => _grid[x, y] = state;
        public void SetCell(Cell c, CellState state) => _grid[c.X, c.Y] = state;

        public bool IsInBounds(Cell c) => c.X >= 0 && c.X < Size && c.Y >= 0 && c.Y < Size;
        public bool IsInBounds(int x, int y) => x >= 0 && x < Size && y >= 0 && y < Size;

        /// <summary>Returns the ship occupying the given cell, or null.</summary>
        public Ship? GetShipAt(Cell cell)
        {
            foreach (var ship in _ships)
                foreach (var c in ship.GetCells())
                    if (c.Equals(cell)) return ship;
            return null;
        }

        public int AliveShipsCount()
        {
            int count = 0;
            foreach (var s in _ships)
                if (!s.IsSunk()) count++;
            return count;
        }

        public int SunkShipsCount()
        {
            int count = 0;
            foreach (var s in _ships)
                if (s.IsSunk()) count++;
            return count;
        }
    }

    public enum CellState
    {
        Empty,
        Ship,       // placed ship cell (local board only)
        Hit,        // shot that hit a ship
        Miss,       // shot that missed
        Adjacent    // auto-marked adjacent after sunk
    }
}
