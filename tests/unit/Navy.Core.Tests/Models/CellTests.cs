using System;
using FluentAssertions;
using Navy.Core.Models;
using Xunit;

namespace Navy.Core.Tests.Models
{
    public sealed class CellTests
    {
        [Fact]
        public void Equals_SameCells_AreEqual()
        {
            var a = new Cell(3, 7);
            var b = new Cell(3, 7);
            a.Equals(b).Should().BeTrue();
            (a == b).Should().BeFalse(); // reference equality, not overloaded ==
        }

        [Fact]
        public void Equals_DifferentX_NotEqual()
        {
            new Cell(1, 2).Equals(new Cell(2, 2)).Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentY_NotEqual()
        {
            new Cell(1, 2).Equals(new Cell(1, 3)).Should().BeFalse();
        }

        [Fact]
        public void Equals_Null_ReturnsFalse()
        {
            new Cell(0, 0).Equals(null).Should().BeFalse();
        }

        [Fact]
        public void Equals_OtherType_ReturnsFalse()
        {
            new Cell(1, 1).Equals("not a cell").Should().BeFalse();
        }

        [Fact]
        public void GetHashCode_SameCells_SameHash()
        {
            new Cell(5, 9).GetHashCode().Should().Be(new Cell(5, 9).GetHashCode());
        }

        [Fact]
        public void GetHashCode_DifferentCells_DifferentHash()
        {
            // Not guaranteed by contract but very likely
            new Cell(0, 0).GetHashCode().Should().NotBe(new Cell(1, 0).GetHashCode());
        }

        [Fact]
        public void ToString_FormatsCorrectly()
        {
            new Cell(3, 7).ToString().Should().Be("(3,7)");
        }
    }
}
