using System.Collections.Generic;
using FluentAssertions;
using Navy.Core.Engine;
using Navy.Core.Models;
using Xunit;

namespace Navy.Core.Tests.Engine
{
    public sealed class ShotResolverTests
    {
        private static Board MakeBoardWithShip(int decks, int x, int y, ShipOrientation orientation = ShipOrientation.Horizontal)
        {
            var board = new Board(10);
            var ship = new Ship(decks, orientation, new Cell(x, y));
            board.AddShip(ship);
            foreach (var cell in ship.GetCells())
                board.SetCell(cell, CellState.Ship);
            return board;
        }

        [Fact]
        [Trait("FR", "FR-GP-02")]
        public void Resolve_Miss_ReturnsMiss_AndSetsCell()
        {
            var board = MakeBoardWithShip(2, 5, 5);
            var result = ShotResolver.Resolve(board, new Cell(0, 0), out _);
            result.Should().Be(ShotResult.Miss);
            board.GetCell(0, 0).Should().Be(CellState.Miss);
        }

        [Fact]
        [Trait("FR", "FR-GP-02")]
        public void Resolve_Hit_ReturnsHit_AndSetsCell()
        {
            var board = MakeBoardWithShip(2, 0, 0);
            var result = ShotResolver.Resolve(board, new Cell(0, 0), out _);
            result.Should().Be(ShotResult.Hit);
            board.GetCell(0, 0).Should().Be(CellState.Hit);
        }

        [Fact]
        [Trait("FR", "FR-GP-02")]
        [Trait("FR", "FR-GP-03")]
        public void Resolve_Sunk_ReturnsSunk_AndMarksAdjacentCells()
        {
            var board = MakeBoardWithShip(1, 5, 5);
            var result = ShotResolver.Resolve(board, new Cell(5, 5), out var adj);
            result.Should().Be(ShotResult.Sunk);
            board.GetCell(5, 5).Should().Be(CellState.Hit);
            adj.Should().NotBeEmpty("adjacent cells should be returned on Sunk");
            // All adjacent cells in bounds should now be Adjacent
            foreach (var cell in adj)
                board.GetCell(cell).Should().Be(CellState.Adjacent);
        }

        [Fact]
        [Trait("FR", "FR-GP-03")]
        public void Resolve_Sunk_AdjacentCellsExcludeOutOfBounds()
        {
            // Ship in corner — fewer adjacents
            var board = MakeBoardWithShip(1, 0, 0);
            ShotResolver.Resolve(board, new Cell(0, 0), out var adj);
            foreach (var cell in adj)
                board.IsInBounds(cell).Should().BeTrue();
        }

        [Fact]
        [Trait("FR", "FR-GP-04")]
        public void Resolve_Sunk_MultiDeckShip_AdjacentNotRevealingWholeShape()
        {
            // The resolver should mark adjacents but NOT the ship cells themselves as misses
            var board = MakeBoardWithShip(3, 0, 0, ShipOrientation.Horizontal);
            // Hit all three cells
            ShotResolver.Resolve(board, new Cell(0, 0), out _);
            ShotResolver.Resolve(board, new Cell(1, 0), out _);
            ShotResolver.Resolve(board, new Cell(2, 0), out var adj);
            // The hit cells should still be Hit (not Adjacent)
            board.GetCell(0, 0).Should().Be(CellState.Hit);
            board.GetCell(1, 0).Should().Be(CellState.Hit);
            board.GetCell(2, 0).Should().Be(CellState.Hit);
        }

        [Fact]
        [Trait("FR", "FR-GP")]
        public void IsCellAlreadyShot_HitCell_ReturnsTrue()
        {
            var board = MakeBoardWithShip(1, 5, 5);
            ShotResolver.Resolve(board, new Cell(5, 5), out _);
            ShotResolver.IsCellAlreadyShot(board, new Cell(5, 5)).Should().BeTrue();
        }

        [Fact]
        [Trait("FR", "FR-GP")]
        public void IsCellAlreadyShot_MissCell_ReturnsTrue()
        {
            var board = MakeBoardWithShip(1, 5, 5);
            ShotResolver.Resolve(board, new Cell(0, 0), out _);
            ShotResolver.IsCellAlreadyShot(board, new Cell(0, 0)).Should().BeTrue();
        }

        [Fact]
        [Trait("FR", "FR-GP")]
        public void IsCellAlreadyShot_AdjacentCell_ReturnsTrue()
        {
            var board = MakeBoardWithShip(1, 5, 5);
            ShotResolver.Resolve(board, new Cell(5, 5), out _);
            // (6, 6) should be adjacent
            ShotResolver.IsCellAlreadyShot(board, new Cell(6, 6)).Should().BeTrue();
        }

        [Fact]
        [Trait("FR", "FR-GP")]
        public void IsCellAlreadyShot_UnShotCell_ReturnsFalse()
        {
            var board = new Board(10);
            ShotResolver.IsCellAlreadyShot(board, new Cell(0, 0)).Should().BeFalse();
        }

        [Fact]
        [Trait("FR", "FR-GP-02")]
        public void Resolve_HitIsNotSunk_ReturnHit_NotSunk()
        {
            var board = MakeBoardWithShip(3, 0, 0);
            var result = ShotResolver.Resolve(board, new Cell(0, 0), out var adj);
            result.Should().Be(ShotResult.Hit);
            adj.Should().BeEmpty("no adjacent cells until Sunk");
        }
    }
}
