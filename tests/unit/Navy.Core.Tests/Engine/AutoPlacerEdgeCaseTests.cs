using FluentAssertions;
using Navy.Core.Engine;
using Navy.Core.Models;
using Xunit;
using System.Collections.Generic;

namespace Navy.Core.Tests.Engine
{
    public sealed class AutoPlacerEdgeCaseTests
    {
        [Fact]
        [Trait("FR", "FR-SP-03")]
        public void TryPlace_ImpossibleConfig_ReturnsFalse()
        {
            // 2x2 board with a 3-deck ship — cannot possibly fit
            var config = new MapConfig
            {
                Type = MapType.Small,
                BoardSize = 2,
                Ships = new List<ShipGroup> { new ShipGroup { Decks = 3, Count = 1 } },
                TotalShips = 1,
                TotalCells = 3
            };
            var success = AutoPlacer.TryPlace(config, out var board);
            success.Should().BeFalse("a 3-deck ship cannot fit in a 2x2 board");
            board.Should().BeNull();
        }

        [Fact]
        [Trait("FR", "FR-SP-03")]
        public void TryPlace_MinimalBoard_SmallShip_Succeeds()
        {
            var config = new MapConfig
            {
                Type = MapType.Small,
                BoardSize = 3,
                Ships = new List<ShipGroup> { new ShipGroup { Decks = 1, Count = 1 } },
                TotalShips = 1,
                TotalCells = 1
            };
            var success = AutoPlacer.TryPlace(config, out var board);
            success.Should().BeTrue();
            board.Should().NotBeNull();
            board!.Ships.Should().HaveCount(1);
        }
    }
}
