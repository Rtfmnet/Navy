// Navy.Core.Engine
// Pure C# - NO UnityEngine dependency

using System;
using System.Collections.Generic;
using Navy.Core.Models;

namespace Navy.Core.Engine
{
    /// <summary>
    /// Generates a valid random ship placement for a given MapConfig.
    /// Uses retry-with-restart strategy — for the small ship counts we deal with,
    /// random placement (largest first) succeeds in well under MaxAttempts on every map size.
    /// </summary>
    public static class AutoPlacer
    {
        private const int MaxAttempts = 1000;
        private const int MaxRestarts = 50;
        private static readonly Random _rng = new Random();

        /// <summary>
        /// Places all ships from the config randomly on a new Board.
        /// Returns false if it fails within MaxRestarts × MaxAttempts (should be extremely rare).
        /// </summary>
        public static bool TryPlace(MapConfig config, out Board? board)
        {
            // Build a flat list of decks, largest first (greedy placement is far easier).
            var deckList = new List<int>();
            foreach (var group in config.Ships)
                for (int i = 0; i < group.Count; i++)
                    deckList.Add(group.Decks);
            deckList.Sort((a, b) => b.CompareTo(a));

            for (int restart = 0; restart < MaxRestarts; restart++)
            {
                board = new Board(config.BoardSize);
                if (TryPlaceAll(board, deckList))
                    return true;
            }

            board = null;
            return false;
        }

        private static bool TryPlaceAll(Board board, List<int> deckList)
        {
            foreach (var decks in deckList)
            {
                if (!TryPlaceSingle(board, decks))
                    return false;
            }
            return true;
        }

        private static bool TryPlaceSingle(Board board, int decks)
        {
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var orientation = _rng.Next(2) == 0
                    ? ShipOrientation.Horizontal
                    : ShipOrientation.Vertical;

                int maxX = orientation == ShipOrientation.Horizontal
                    ? board.Size - decks
                    : board.Size - 1;
                int maxY = orientation == ShipOrientation.Vertical
                    ? board.Size - decks
                    : board.Size - 1;

                if (maxX < 0 || maxY < 0) continue;

                int x = _rng.Next(0, maxX + 1);
                int y = _rng.Next(0, maxY + 1);

                var ship = new Ship(decks, orientation, new Cell(x, y));

                if (BoardValidator.IsValidPlacement(board, ship))
                {
                    board.AddShip(ship);
                    foreach (var cell in ship.GetCells())
                        board.SetCell(cell, CellState.Ship);
                    return true;
                }
            }
            return false;
        }
    }
}
