using FluentAssertions;
using Navy.Core.Engine;
using Navy.Core.Models;
using Xunit;

namespace Navy.Core.Tests.Engine
{
    /// <summary>
    /// Additional ShotResolver coverage to hit remaining branches.
    /// </summary>
    public sealed class ShotResolverExtraTests
    {
        [Fact]
        [Trait("FR", "FR-GP-03")]
        public void Resolve_Sunk_ExistingAdjacentCell_NotOverwritten()
        {
            // If an adjacent cell was already marked as Ship (another ship nearby),
            // it should be marked Adjacent only if it's Empty or Ship
            var board = new Board(10);
            var ship = new Ship(1, ShipOrientation.Horizontal, new Cell(5, 5));
            board.AddShip(ship);
            board.SetCell(new Cell(5, 5), CellState.Ship);
            // Pre-mark an adjacent cell as Hit (already shot)
            board.SetCell(new Cell(5, 4), CellState.Hit);

            ShotResolver.Resolve(board, new Cell(5, 5), out var adj);

            // The already-hit cell should NOT be overwritten to Adjacent
            board.GetCell(new Cell(5, 4)).Should().Be(CellState.Hit,
                "cells that were already Hit should not be overwritten by adjacent miss marking");
        }

        [Fact]
        [Trait("FR", "FR-GP-03")]
        public void Resolve_Sunk_AlreadyMissCell_NotOverwrittenToAdjacent()
        {
            var board = new Board(10);
            var ship = new Ship(1, ShipOrientation.Horizontal, new Cell(5, 5));
            board.AddShip(ship);
            board.SetCell(new Cell(5, 5), CellState.Ship);
            board.SetCell(new Cell(4, 5), CellState.Miss);

            ShotResolver.Resolve(board, new Cell(5, 5), out _);

            board.GetCell(new Cell(4, 5)).Should().Be(CellState.Miss,
                "Miss cells should not be overwritten to Adjacent");
        }

        [Fact]
        public void BoardValidator_GetAdjacentCells_CornerShip_ReturnsOnlyInBounds()
        {
            var board = new Board(10);
            var ship = new Ship(1, ShipOrientation.Horizontal, new Cell(0, 0));
            board.AddShip(ship);

            var adj = BoardValidator.GetAdjacentCells(board, ship);
            foreach (var cell in adj)
                board.IsInBounds(cell).Should().BeTrue();
        }

        [Fact]
        public void BoardValidator_GetAdjacentCells_MultiDeckShip_NoDuplicates()
        {
            var board = new Board(10);
            var ship = new Ship(3, ShipOrientation.Horizontal, new Cell(1, 1));
            board.AddShip(ship);

            var adj = BoardValidator.GetAdjacentCells(board, ship);
            // No cell should appear twice
            var seen = new System.Collections.Generic.HashSet<(int, int)>();
            foreach (var cell in adj)
            {
                var key = (cell.X, cell.Y);
                seen.Contains(key).Should().BeFalse($"cell ({cell.X},{cell.Y}) was duplicated");
                seen.Add(key);
            }
        }
    }
}
