using FluentAssertions;
using Navy.Core.Engine;
using Navy.Core.Models;
using Navy.Core.TestKit.Builders;
using Xunit;

namespace Navy.Core.Tests.Engine
{
    public sealed class BuilderUsageTests
    {
        [Fact]
        public void BoardBuilder_CreatesShipsCorrectly()
        {
            var board = new BoardBuilder(MapType.Medium)
                .WithShip(decks: 4, x: 0, y: 0, ShipOrientation.Horizontal)
                .WithShip(decks: 3, x: 0, y: 2, ShipOrientation.Vertical)
                .Build();

            board.Ships.Should().HaveCount(2);
            board.Ships[0].Decks.Should().Be(4);
            board.Ships[1].Decks.Should().Be(3);
        }

        [Fact]
        public void ShipBuilder_FluentApi_CreatesShip()
        {
            var ship = new ShipBuilder()
                .WithDecks(3)
                .At(2, 4)
                .Vertical()
                .Build();

            ship.Decks.Should().Be(3);
            ship.Origin.X.Should().Be(2);
            ship.Origin.Y.Should().Be(4);
            ship.Orientation.Should().Be(ShipOrientation.Vertical);
        }

        [Fact]
        public void ShipBuilder_Horizontal_Default()
        {
            var ship = new ShipBuilder().WithDecks(2).At(0, 0).Horizontal().Build();
            ship.Orientation.Should().Be(ShipOrientation.Horizontal);
        }

        [Fact]
        public void GameStateBuilder_ProducesValidState()
        {
            var state = new GameStateBuilder()
                .WithMap(MapType.Small)
                .WithPhase(Navy.Core.Models.GamePhase.Playing)
                .WithCurrentTurn("host")
                .Build();

            state.Phase.Should().Be(Navy.Core.Models.GamePhase.Playing);
            state.CurrentTurnUid.Should().Be("host");
            state.Host.Should().NotBeNull();
            state.Guest.Should().NotBeNull();
        }

        [Fact]
        public void BoardBuilder_CustomSize_Works()
        {
            var board = new BoardBuilder(8)
                .WithShip(1, 0, 0)
                .Build();
            board.Size.Should().Be(8);
            board.Ships.Should().HaveCount(1);
        }
    }
}
