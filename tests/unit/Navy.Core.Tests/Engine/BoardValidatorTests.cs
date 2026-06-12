using FluentAssertions;
using Navy.Core.Engine;
using Navy.Core.Models;
using Xunit;

namespace Navy.Core.Tests.Engine
{
    public sealed class BoardValidatorTests
    {
        private static Board EmptyBoard(int size = 10) => new Board(size);

        // ─── Single ship placement validation ────────────────────────────────────

        [Fact]
        [Trait("FR", "FR-SP-02")]
        public void ShipInBounds_IsValid()
        {
            var board = EmptyBoard(10);
            var ship = new Ship(3, ShipOrientation.Horizontal, new Cell(0, 0));
            BoardValidator.IsValidPlacement(board, ship).Should().BeTrue();
        }

        [Fact]
        [Trait("FR", "FR-SP-02")]
        public void ShipOutOfBoundsRight_IsInvalid()
        {
            var board = EmptyBoard(10);
            // Ship of 3 at x=8 would occupy 8,9,10 — out of bounds
            var ship = new Ship(3, ShipOrientation.Horizontal, new Cell(8, 0));
            BoardValidator.IsValidPlacement(board, ship).Should().BeFalse();
        }

        [Fact]
        [Trait("FR", "FR-SP-02")]
        public void ShipOutOfBoundsBottom_IsInvalid()
        {
            var board = EmptyBoard(10);
            var ship = new Ship(3, ShipOrientation.Vertical, new Cell(0, 9));
            BoardValidator.IsValidPlacement(board, ship).Should().BeFalse();
        }

        [Fact]
        [Trait("FR", "FR-SP-02")]
        public void ShipAtNegativeOrigin_IsInvalid()
        {
            var board = EmptyBoard(10);
            var ship = new Ship(2, ShipOrientation.Horizontal, new Cell(-1, 0));
            BoardValidator.IsValidPlacement(board, ship).Should().BeFalse();
        }

        [Fact]
        [Trait("FR", "FR-SP-02")]
        public void OverlappingShips_IsInvalid()
        {
            var board = EmptyBoard(10);
            var ship1 = new Ship(3, ShipOrientation.Horizontal, new Cell(0, 0));
            board.AddShip(ship1);
            foreach (var c in ship1.GetCells()) board.SetCell(c, CellState.Ship);

            var ship2 = new Ship(2, ShipOrientation.Vertical, new Cell(1, 0));
            BoardValidator.IsValidPlacement(board, ship2).Should().BeFalse();
        }

        [Fact]
        [Trait("FR", "FR-SP-02")]
        public void CardinallyTouchingShips_IsInvalid()
        {
            var board = EmptyBoard(10);
            var ship1 = new Ship(2, ShipOrientation.Horizontal, new Cell(0, 0));
            board.AddShip(ship1);
            foreach (var c in ship1.GetCells()) board.SetCell(c, CellState.Ship);

            // Ship2 directly below ship1
            var ship2 = new Ship(2, ShipOrientation.Horizontal, new Cell(0, 1));
            BoardValidator.IsValidPlacement(board, ship2).Should().BeFalse();
        }

        [Fact]
        [Trait("FR", "FR-SP-02")]
        public void DiagonallyTouchingShips_IsInvalid()
        {
            var board = EmptyBoard(10);
            var ship1 = new Ship(1, ShipOrientation.Horizontal, new Cell(3, 3));
            board.AddShip(ship1);
            foreach (var c in ship1.GetCells()) board.SetCell(c, CellState.Ship);

            // Ship2 diagonally adjacent
            var ship2 = new Ship(1, ShipOrientation.Horizontal, new Cell(4, 4));
            BoardValidator.IsValidPlacement(board, ship2).Should().BeFalse();
        }

        [Fact]
        [Trait("FR", "FR-SP-02")]
        public void ShipsWithGap_IsValid()
        {
            var board = EmptyBoard(10);
            var ship1 = new Ship(2, ShipOrientation.Horizontal, new Cell(0, 0));
            board.AddShip(ship1);
            foreach (var c in ship1.GetCells()) board.SetCell(c, CellState.Ship);

            // 2 cells apart — valid
            var ship2 = new Ship(2, ShipOrientation.Horizontal, new Cell(0, 2));
            BoardValidator.IsValidPlacement(board, ship2).Should().BeTrue();
        }

        // ─── Full board validation ────────────────────────────────────────────────

        [Fact]
        [Trait("FR", "FR-SP")]
        public void FullValidation_ValidSmallBoard_IsValid()
        {
            var config = Navy.Core.Engine.GameRules.GetConfig(MapType.Small);
            AutoPlacer.TryPlace(config, out var board);
            board.Should().NotBeNull();
            BoardValidator.IsFullyValid(board!, config).Should().BeTrue();
        }

        [Fact]
        [Trait("FR", "FR-SP")]
        public void FullValidation_WrongShipCount_IsInvalid()
        {
            var config = Navy.Core.Engine.GameRules.GetConfig(MapType.Small);
            // Build a board with only 1 ship (wrong count)
            var board = new Board(config.BoardSize);
            var ship = new Ship(3, ShipOrientation.Horizontal, new Cell(0, 0));
            board.AddShip(ship);
            BoardValidator.IsFullyValid(board, config).Should().BeFalse();
        }

        [Fact]
        [Trait("FR", "FR-SP")]
        public void FullValidation_WrongDeckCount_IsInvalid()
        {
            // Create a board where all ships are 1-deck but config requires some 3-deck
            var config = Navy.Core.Engine.GameRules.GetConfig(MapType.Small);
            var board = new Board(config.BoardSize);
            // Add 6 one-deck ships in valid positions
            for (int i = 0; i < 6; i++)
                board.AddShip(new Ship(1, ShipOrientation.Horizontal, new Cell(0, i * 2)));
            BoardValidator.IsFullyValid(board, config).Should().BeFalse();
        }
    }
}
