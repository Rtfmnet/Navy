using FluentAssertions;
using Navy.Core.Engine;
using Navy.Core.Models;
using Xunit;

namespace Navy.Core.Tests.Models
{
    public sealed class MapConfigTests
    {
        [Fact]
        [Trait("FR", "FR-MP")]
        public void Small_TotalShips6_TotalCells10_BoardSize8()
        {
            var cfg = GameRules.GetConfig(MapType.Small);
            cfg.TotalShips.Should().Be(6);
            cfg.TotalCells.Should().Be(10);
            cfg.BoardSize.Should().Be(8);
        }

        [Fact]
        [Trait("FR", "FR-MP")]
        public void Small_ShipGroups_Match_Spec()
        {
            // Small: 3×1, 2×2, 1×3
            var cfg = GameRules.GetConfig(MapType.Small);
            int sumCells = 0;
            int sumShips = 0;
            foreach (var g in cfg.Ships) { sumCells += g.Decks * g.Count; sumShips += g.Count; }
            sumShips.Should().Be(6);
            sumCells.Should().Be(10);
        }

        [Fact]
        [Trait("FR", "FR-MP")]
        public void Medium_TotalShips10_TotalCells20_BoardSize10()
        {
            var cfg = GameRules.GetConfig(MapType.Medium);
            cfg.TotalShips.Should().Be(10);
            cfg.TotalCells.Should().Be(20);
            cfg.BoardSize.Should().Be(10);
        }

        [Fact]
        [Trait("FR", "FR-MP")]
        public void Large_TotalShips15_TotalCells30_BoardSize12()
        {
            var cfg = GameRules.GetConfig(MapType.Large);
            cfg.TotalShips.Should().Be(15);
            cfg.TotalCells.Should().Be(30);
            cfg.BoardSize.Should().Be(12);
        }

        [Fact]
        [Trait("FR", "FR-MP")]
        public void Large_ContainsFiveDeckShip()
        {
            var cfg = GameRules.GetConfig(MapType.Large);
            bool has5Deck = false;
            foreach (var g in cfg.Ships) if (g.Decks == 5) has5Deck = true;
            has5Deck.Should().BeTrue("Large map must have a 5-deck ship per spec");
        }

        [Fact]
        [Trait("FR", "FR-MP")]
        public void Small_HasNo4DeckShip()
        {
            var cfg = GameRules.GetConfig(MapType.Small);
            foreach (var g in cfg.Ships)
                g.Decks.Should().BeLessThan(4, "Small map must not have 4+ deck ships per spec");
        }
    }
}
