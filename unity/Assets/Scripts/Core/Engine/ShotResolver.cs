// Navy.Core.Engine
// Pure C# - NO UnityEngine dependency

using System;
using System.Collections.Generic;
using Navy.Core.Models;

namespace Navy.Core.Engine
{
    /// <summary>
    /// Resolves a shot against a target board and returns the result.
    /// Also handles auto-marking adjacent cells when a ship is sunk (FR-GP-03).
    /// </summary>
    public static class ShotResolver
    {
        /// <summary>
        /// Resolves a shot at <paramref name="coord"/> on <paramref name="targetBoard"/>.
        /// Caller must validate that the cell has not already been shot.
        /// </summary>
        /// <param name="adjacentCells">Out: cells to mark as adjacent-miss (only populated on Sunk).</param>
        public static ShotResult Resolve(Board targetBoard, Cell coord, out List<Cell> adjacentCells)
        {
            adjacentCells = new List<Cell>();

            var ship = targetBoard.GetShipAt(coord);
            if (ship == null)
            {
                targetBoard.SetCell(coord, CellState.Miss);
                return ShotResult.Miss;
            }

            ship.TryHit(coord);
            targetBoard.SetCell(coord, CellState.Hit);

            if (ship.IsSunk())
            {
                // Auto-mark adjacent cells as misses (FR-GP-03)
                adjacentCells = BoardValidator.GetAdjacentCells(targetBoard, ship);
                foreach (var adj in adjacentCells)
                {
                    var current = targetBoard.GetCell(adj);
                    if (current == CellState.Empty || current == CellState.Ship)
                        targetBoard.SetCell(adj, CellState.Adjacent);
                }
                return ShotResult.Sunk;
            }

            return ShotResult.Hit;
        }

        public static bool IsCellAlreadyShot(Board board, Cell coord)
        {
            var state = board.GetCell(coord);
            return state == CellState.Hit || state == CellState.Miss || state == CellState.Adjacent;
        }
    }
}
