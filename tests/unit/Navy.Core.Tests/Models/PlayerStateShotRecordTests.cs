using FluentAssertions;
using Navy.Core.Engine;
using Navy.Core.Models;
using Xunit;

namespace Navy.Core.Tests.Models
{
    /// <summary>
    /// Additional coverage for PlayerState and ShotRecord to hit remaining paths.
    /// </summary>
    public sealed class PlayerStateShotRecordTests
    {
        [Fact]
        public void PlayerState_Accuracy_ZeroTotal_Returns0()
        {
            var ps = new PlayerState { Hits = 0, Misses = 0 };
            ps.Accuracy.Should().Be(0f);
        }

        [Fact]
        public void PlayerState_Accuracy_AllHits_Returns100()
        {
            var ps = new PlayerState { Hits = 10, Misses = 0 };
            ps.Accuracy.Should().BeApproximately(100f, 0.01f);
        }

        [Fact]
        public void PlayerState_Accuracy_HalfHits_Returns50()
        {
            var ps = new PlayerState { Hits = 5, Misses = 5 };
            ps.Accuracy.Should().BeApproximately(50f, 0.01f);
        }

        [Fact]
        public void ShotRecord_CanBeConstructed()
        {
            var record = new ShotRecord
            {
                ShooterUid = "host",
                TargetUid = "guest",
                Coordinate = new Cell(3, 4),
                Result = ShotResult.Sunk,
                TimestampMs = 12345L,
                SunkShipCells = new System.Collections.Generic.List<Cell> { new Cell(3, 4) },
                AdjacentMissCells = new System.Collections.Generic.List<Cell> { new Cell(4, 4) }
            };
            record.ShooterUid.Should().Be("host");
            record.Result.Should().Be(ShotResult.Sunk);
            record.SunkShipCells!.Should().HaveCount(1);
            record.AdjacentMissCells!.Should().HaveCount(1);
        }

        [Fact]
        public void PlayerState_AllProperties_Settable()
        {
            var board = new Board(10);
            var ps = new PlayerState
            {
                Uid = "abc",
                Nickname = "Player",
                Board = board,
                IsReady = true,
                ChosenMapType = MapType.Large,
                Hits = 3,
                Misses = 2,
                SunkShips = 1,
                BoardCommitted = true
            };
            ps.Uid.Should().Be("abc");
            ps.ChosenMapType.Should().Be(MapType.Large);
            ps.SunkShips.Should().Be(1);
        }

        [Fact]
        public void GameState_AllProperties_Settable()
        {
            var state = new GameState
            {
                SessionId = "456",
                MapType = MapType.Medium,
                Phase = GamePhase.Playing,
                CurrentTurnUid = "host",
                TurnStartedAtMs = 100L,
                WinnerUid = null,
                IsDraw = false
            };
            state.SessionId.Should().Be("456");
            state.Phase.Should().Be(GamePhase.Playing);
        }
    }
}
