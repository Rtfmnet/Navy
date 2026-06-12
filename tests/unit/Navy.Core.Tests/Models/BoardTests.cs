using FluentAssertions;
using Navy.Core.Models;
using Xunit;

namespace Navy.Core.Tests.Models
{
    public sealed class BoardTests
    {
        [Fact]
        public void AddShip_ThenGetShipAt_ReturnsShip()
        {
            var board = new Board(10);
            var ship = new Ship(2, ShipOrientation.Horizontal, new Cell(0, 0));
            board.AddShip(ship);
            board.GetShipAt(new Cell(0, 0)).Should().BeSameAs(ship);
            board.GetShipAt(new Cell(1, 0)).Should().BeSameAs(ship);
        }

        [Fact]
        public void GetShipAt_NoShip_ReturnsNull()
        {
            var board = new Board(10);
            board.GetShipAt(new Cell(5, 5)).Should().BeNull();
        }

        [Fact]
        public void IsInBounds_InsideBounds_ReturnsTrue()
        {
            var board = new Board(8);
            board.IsInBounds(new Cell(0, 0)).Should().BeTrue();
            board.IsInBounds(new Cell(7, 7)).Should().BeTrue();
        }

        [Fact]
        public void IsInBounds_OutsideBounds_ReturnsFalse()
        {
            var board = new Board(8);
            board.IsInBounds(new Cell(-1, 0)).Should().BeFalse();
            board.IsInBounds(new Cell(8, 0)).Should().BeFalse();
            board.IsInBounds(new Cell(0, -1)).Should().BeFalse();
            board.IsInBounds(new Cell(0, 8)).Should().BeFalse();
        }

        [Fact]
        public void SetCell_ThenGetCell_ReturnsCorrectState()
        {
            var board = new Board(10);
            board.SetCell(3, 4, CellState.Hit);
            board.GetCell(3, 4).Should().Be(CellState.Hit);
            board.GetCell(new Cell(3, 4)).Should().Be(CellState.Hit);
        }

        [Fact]
        public void AliveShipsCount_BeforeSinking_CountsAllShips()
        {
            var board = new Board(10);
            board.AddShip(new Ship(2, ShipOrientation.Horizontal, new Cell(0, 0)));
            board.AddShip(new Ship(1, ShipOrientation.Horizontal, new Cell(4, 4)));
            board.AliveShipsCount().Should().Be(2);
        }

        [Fact]
        public void SunkShipsCount_AfterSinking_CountsCorrectly()
        {
            var board = new Board(10);
            var ship = new Ship(1, ShipOrientation.Horizontal, new Cell(0, 0));
            board.AddShip(ship);
            board.AddShip(new Ship(2, ShipOrientation.Horizontal, new Cell(4, 4)));
            ship.TryHit(new Cell(0, 0));
            board.SunkShipsCount().Should().Be(1);
            board.AliveShipsCount().Should().Be(1);
        }

        [Fact]
        public void RemoveAllShips_ClearsAllShips()
        {
            var board = new Board(10);
            board.AddShip(new Ship(2, ShipOrientation.Horizontal, new Cell(0, 0)));
            board.RemoveAllShips();
            board.Ships.Should().BeEmpty();
        }

        [Fact]
        public void NewBoard_DefaultCellState_IsEmpty()
        {
            var board = new Board(10);
            board.GetCell(5, 5).Should().Be(CellState.Empty);
        }
    }
}
