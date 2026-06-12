using System;
using FluentAssertions;
using Navy.Core.Engine;
using Navy.Core.Models;
using Xunit;

namespace Navy.Core.Tests.Engine
{
    public sealed class AutoPlacerTests
    {
        [Fact]
        [Trait("FR", "FR-SP-03")]
        public void TryPlace_Small_ProducesValidBoard()
        {
            var config = GameRules.GetConfig(MapType.Small);
            var success = AutoPlacer.TryPlace(config, out var board);
            success.Should().BeTrue();
            board.Should().NotBeNull();
            BoardValidator.IsFullyValid(board!, config).Should().BeTrue();
        }

        [Fact]
        [Trait("FR", "FR-SP-03")]
        public void TryPlace_Medium_ProducesValidBoard()
        {
            var config = GameRules.GetConfig(MapType.Medium);
            var success = AutoPlacer.TryPlace(config, out var board);
            success.Should().BeTrue();
            board.Should().NotBeNull();
            BoardValidator.IsFullyValid(board!, config).Should().BeTrue();
        }

        [Fact]
        [Trait("FR", "FR-SP-03")]
        public void TryPlace_Large_ProducesValidBoard()
        {
            var config = GameRules.GetConfig(MapType.Large);
            var success = AutoPlacer.TryPlace(config, out var board);
            success.Should().BeTrue();
            board.Should().NotBeNull();
            BoardValidator.IsFullyValid(board!, config).Should().BeTrue();
        }

        [Fact]
        [Trait("FR", "FR-SP-03")]
        public void TryPlace_Small_MultipleCallsAllValid()
        {
            var config = GameRules.GetConfig(MapType.Small);
            for (int i = 0; i < 20; i++)
            {
                AutoPlacer.TryPlace(config, out var board);
                board.Should().NotBeNull();
                BoardValidator.IsFullyValid(board!, config).Should().BeTrue($"Run {i} should produce valid board");
            }
        }

        [Fact]
        [Trait("FR", "FR-SP-03")]
        public void TryPlace_Large_MultipleCallsAllValid()
        {
            var config = GameRules.GetConfig(MapType.Large);
            for (int i = 0; i < 10; i++)
            {
                AutoPlacer.TryPlace(config, out var board);
                BoardValidator.IsFullyValid(board!, config).Should().BeTrue($"Run {i}");
            }
        }

        [Fact]
        [Trait("FR", "FR-SP-03")]
        public void TryPlace_CorrectShipCount()
        {
            var config = GameRules.GetConfig(MapType.Medium);
            AutoPlacer.TryPlace(config, out var board);
            board!.Ships.Count.Should().Be(config.TotalShips);
        }

        [Fact]
        [Trait("FR", "FR-SP-03")]
        public void TryPlace_AllShipsInBounds()
        {
            var config = GameRules.GetConfig(MapType.Large);
            AutoPlacer.TryPlace(config, out var board);
            foreach (var ship in board!.Ships)
                foreach (var cell in ship.GetCells())
                    board.IsInBounds(cell).Should().BeTrue();
        }
    }
}
