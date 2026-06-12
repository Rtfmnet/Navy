using FluentAssertions;
using Navy.Core.Models;
using Xunit;

namespace Navy.Core.Tests.Models
{
    public sealed class ShipTests
    {
        [Fact]
        public void GetCells_Horizontal_ReturnsCorrectCells()
        {
            var ship = new Ship(3, ShipOrientation.Horizontal, new Cell(2, 4));
            var cells = ship.GetCells();
            cells.Should().HaveCount(3);
            cells[0].Should().BeEquivalentTo(new Cell(2, 4));
            cells[1].Should().BeEquivalentTo(new Cell(3, 4));
            cells[2].Should().BeEquivalentTo(new Cell(4, 4));
        }

        [Fact]
        public void GetCells_Vertical_ReturnsCorrectCells()
        {
            var ship = new Ship(3, ShipOrientation.Vertical, new Cell(1, 1));
            var cells = ship.GetCells();
            cells.Should().HaveCount(3);
            cells[0].Should().BeEquivalentTo(new Cell(1, 1));
            cells[1].Should().BeEquivalentTo(new Cell(1, 2));
            cells[2].Should().BeEquivalentTo(new Cell(1, 3));
        }

        [Fact]
        public void TryHit_HitCell_ReturnsTrue()
        {
            var ship = new Ship(2, ShipOrientation.Horizontal, new Cell(0, 0));
            ship.TryHit(new Cell(0, 0)).Should().BeTrue();
            ship.TryHit(new Cell(1, 0)).Should().BeTrue();
        }

        [Fact]
        public void TryHit_MissCell_ReturnsFalse()
        {
            var ship = new Ship(2, ShipOrientation.Horizontal, new Cell(0, 0));
            ship.TryHit(new Cell(5, 5)).Should().BeFalse();
        }

        [Fact]
        public void IsSunk_NotAllHit_ReturnsFalse()
        {
            var ship = new Ship(3, ShipOrientation.Horizontal, new Cell(0, 0));
            ship.TryHit(new Cell(0, 0));
            ship.TryHit(new Cell(1, 0));
            ship.IsSunk().Should().BeFalse();
        }

        [Fact]
        public void IsSunk_AllHit_ReturnsTrue()
        {
            var ship = new Ship(2, ShipOrientation.Horizontal, new Cell(0, 0));
            ship.TryHit(new Cell(0, 0));
            ship.TryHit(new Cell(1, 0));
            ship.IsSunk().Should().BeTrue();
        }

        [Fact]
        public void IsHitAt_HitCell_ReturnsTrue()
        {
            var ship = new Ship(2, ShipOrientation.Horizontal, new Cell(0, 0));
            ship.TryHit(new Cell(0, 0));
            ship.IsHitAt(new Cell(0, 0)).Should().BeTrue();
            ship.IsHitAt(new Cell(1, 0)).Should().BeFalse();
        }

        [Fact]
        public void SingleDeckShip_CanBeSunkInOneHit()
        {
            var ship = new Ship(1, ShipOrientation.Horizontal, new Cell(3, 3));
            ship.TryHit(new Cell(3, 3));
            ship.IsSunk().Should().BeTrue();
        }
    }
}
