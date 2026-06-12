using System.Collections.Generic;
using FluentAssertions;
using Navy.Core.Engine;
using Navy.Core.Models;
using Xunit;

namespace Navy.Core.Tests.Engine
{
    public sealed class GameRulesTests
    {
        // ─── GetConfig ────────────────────────────────────────────────────────────

        [Theory]
        [InlineData(MapType.Small, 8, 6, 10)]
        [InlineData(MapType.Medium, 10, 10, 20)]
        [InlineData(MapType.Large, 12, 15, 30)]
        [Trait("FR", "FR-MP")]
        public void GetConfig_AllMapTypes_CorrectValues(MapType type, int boardSize, int totalShips, int totalCells)
        {
            var cfg = GameRules.GetConfig(type);
            cfg.BoardSize.Should().Be(boardSize);
            cfg.TotalShips.Should().Be(totalShips);
            cfg.TotalCells.Should().Be(totalCells);
        }

        // ─── ResolveMapConflict ───────────────────────────────────────────────────

        [Fact]
        [Trait("FR", "FR-MP")]
        public void ResolveMapConflict_BothSmall_ReturnsSmall()
        {
            GameRules.ResolveMapConflict(MapType.Small, MapType.Small).Should().Be(MapType.Small);
        }

        [Fact]
        [Trait("FR", "FR-MP")]
        public void ResolveMapConflict_BothMedium_ReturnsMedium()
        {
            GameRules.ResolveMapConflict(MapType.Medium, MapType.Medium).Should().Be(MapType.Medium);
        }

        [Fact]
        [Trait("FR", "FR-MP")]
        public void ResolveMapConflict_DifferentChoices_RandomlyPicksOne()
        {
            // Run many times — both outcomes must appear, and only those two
            var results = new HashSet<MapType>();
            for (int i = 0; i < 200; i++)
                results.Add(GameRules.ResolveMapConflict(MapType.Small, MapType.Medium));
            results.Should().Contain(MapType.Small);
            results.Should().Contain(MapType.Medium);
            results.Should().NotContain(MapType.Large);
        }

        // ─── PickFirstTurnUid ─────────────────────────────────────────────────────

        [Fact]
        [Trait("FR", "FR-GP-01")]
        public void PickFirstTurnUid_DistributionTest_BothPlayersPickedAtLeastOnce()
        {
            bool hostPicked = false, guestPicked = false;
            for (int i = 0; i < 200; i++)
            {
                var uid = GameRules.PickFirstTurnUid("host", "guest");
                if (uid == "host") hostPicked = true;
                if (uid == "guest") guestPicked = true;
            }
            hostPicked.Should().BeTrue("host should get first turn sometimes");
            guestPicked.Should().BeTrue("guest should get first turn sometimes");
        }

        [Fact]
        [Trait("FR", "FR-GP-01")]
        public void PickFirstTurnUid_OnlyReturnsOneOfTheTwoUids()
        {
            for (int i = 0; i < 50; i++)
            {
                var uid = GameRules.PickFirstTurnUid("Alice", "Bob");
                uid.Should().BeOneOf("Alice", "Bob");
            }
        }

        // ─── CheckWinCondition ────────────────────────────────────────────────────

        [Fact]
        [Trait("FR", "FR-GP-09")]
        public void CheckWinCondition_AllHostShipsSunk_GuestWins()
        {
            var state = MakeState();
            // Sink host's only ship
            var hostShip = new Ship(1, ShipOrientation.Horizontal, new Cell(0, 0));
            state.Host.Board.AddShip(hostShip);
            hostShip.TryHit(new Cell(0, 0));
            state.Guest.Board.AddShip(new Ship(1, ShipOrientation.Horizontal, new Cell(5, 5)));

            GameRules.CheckWinCondition(state).Should().Be("guest-uid");
        }

        [Fact]
        [Trait("FR", "FR-GP-09")]
        public void CheckWinCondition_AllGuestShipsSunk_HostWins()
        {
            var state = MakeState();
            state.Host.Board.AddShip(new Ship(1, ShipOrientation.Horizontal, new Cell(0, 0)));
            var guestShip = new Ship(1, ShipOrientation.Horizontal, new Cell(5, 5));
            state.Guest.Board.AddShip(guestShip);
            guestShip.TryHit(new Cell(5, 5));

            GameRules.CheckWinCondition(state).Should().Be("host-uid");
        }

        [Fact]
        [Trait("FR", "FR-GP-09")]
        public void CheckWinCondition_GameOngoing_ReturnsNull()
        {
            var state = MakeState();
            state.Host.Board.AddShip(new Ship(1, ShipOrientation.Horizontal, new Cell(0, 0)));
            state.Guest.Board.AddShip(new Ship(1, ShipOrientation.Horizontal, new Cell(5, 5)));
            GameRules.CheckWinCondition(state).Should().BeNull();
        }

        // ─── DetermineWinnerByHits ────────────────────────────────────────────────

        [Fact]
        [Trait("FR", "FR-GP-10")]
        public void DetermineWinnerByHits_HostMoreHits_HostWins()
        {
            var state = MakeState();
            state.Host.Hits = 5;
            state.Guest.Hits = 3;
            var (winner, isDraw) = GameRules.DetermineWinnerByHits(state);
            winner.Should().Be("host-uid");
            isDraw.Should().BeFalse();
        }

        [Fact]
        [Trait("FR", "FR-GP-10")]
        public void DetermineWinnerByHits_GuestMoreHits_GuestWins()
        {
            var state = MakeState();
            state.Host.Hits = 2;
            state.Guest.Hits = 7;
            var (winner, isDraw) = GameRules.DetermineWinnerByHits(state);
            winner.Should().Be("guest-uid");
            isDraw.Should().BeFalse();
        }

        [Fact]
        [Trait("FR", "FR-GP-10")]
        public void DetermineWinnerByHits_EqualHits_Draw()
        {
            var state = MakeState();
            state.Host.Hits = 4;
            state.Guest.Hits = 4;
            var (winner, isDraw) = GameRules.DetermineWinnerByHits(state);
            winner.Should().BeNull();
            isDraw.Should().BeTrue();
        }

        [Fact]
        [Trait("FR", "FR-GP-10")]
        public void DetermineWinnerByHits_BothZeroHits_Draw()
        {
            var state = MakeState();
            var (_, isDraw) = GameRules.DetermineWinnerByHits(state);
            isDraw.Should().BeTrue();
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private static GameState MakeState() => new GameState
        {
            Host = new PlayerState { Uid = "host-uid", Board = new Board(10) },
            Guest = new PlayerState { Uid = "guest-uid", Board = new Board(10) }
        };
    }
}
