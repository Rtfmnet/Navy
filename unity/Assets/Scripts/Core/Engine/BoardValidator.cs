// Navy.Core.Engine
// Pure C# - NO UnityEngine dependency

using System.Collections.Generic;
using Navy.Core.Models;

namespace Navy.Core.Engine
{
    /// <summary>
    /// Validates ship placement on a board according to game rules:
    /// ships must not overlap, touch (including diagonals), or go out of bounds.
    /// </summary>
    public static class BoardValidator
    {
        public static bool IsValidPlacement(Board board, Ship newShip)
        {
            var cells = newShip.GetCells();

            // Check bounds
            foreach (var cell in cells)
                if (!board.IsInBounds(cell))
                    return false;

            // Check no overlap or adjacency with existing ships
            var occupied = GetAllOccupiedAndAdjacent(board);
            foreach (var cell in cells)
                if (occupied.Contains((cell.X, cell.Y)))
                    return false;

            return true;
        }

        public static bool IsFullyValid(Board board, MapConfig config)
        {
            // Check ship counts and sizes
            var required = new Dictionary<int, int>();
            foreach (var group in config.Ships)
                required[group.Decks] = group.Count;

            var actual = new Dictionary<int, int>();
            foreach (var ship in board.Ships)
            {
                if (!actual.ContainsKey(ship.Decks)) actual[ship.Decks] = 0;
                actual[ship.Decks]++;
            }

            foreach (var kv in required)
            {
                if (!actual.TryGetValue(kv.Key, out int count)) return false;
                if (count != kv.Value) return false;
            }

            // Check each ship is individually valid (bounds + no adjacency)
            var placed = new List<Ship>(board.Ships);
            var tempBoard = new Board(board.Size);
            foreach (var ship in placed)
            {
                if (!IsValidPlacement(tempBoard, ship)) return false;
                // Add this ship's cells to the temp board so next ship can check adjacency
                foreach (var cell in ship.GetCells())
                    tempBoard.SetCell(cell, CellState.Ship);
                tempBoard.AddShip(ship);
            }

            return true;
        }

        private static HashSet<(int, int)> GetAllOccupiedAndAdjacent(Board board)
        {
            var set = new HashSet<(int, int)>();
            foreach (var ship in board.Ships)
            {
                foreach (var cell in ship.GetCells())
                {
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                            set.Add((cell.X + dx, cell.Y + dy));
                }
            }
            return set;
        }

        /// <summary>
        /// Returns all cells adjacent (including diagonals) to a ship,
        /// that are within board bounds and NOT part of the ship itself.
        /// Used to mark adjacent misses after sinking.
        /// </summary>
        public static List<Cell> GetAdjacentCells(Board board, Ship ship)
        {
            var shipCells = new HashSet<(int, int)>();
            foreach (var c in ship.GetCells())
                shipCells.Add((c.X, c.Y));

            var result = new List<Cell>();
            foreach (var c in ship.GetCells())
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = c.X + dx, ny = c.Y + dy;
                        if (!board.IsInBounds(nx, ny)) continue;
                        if (shipCells.Contains((nx, ny))) continue;
                        var adj = new Cell(nx, ny);
                        bool alreadyAdded = false;
                        foreach (var r in result)
                            if (r.Equals(adj)) { alreadyAdded = true; break; }
                        if (!alreadyAdded) result.Add(adj);
                    }
                }
            }
            return result;
        }
    }
}
