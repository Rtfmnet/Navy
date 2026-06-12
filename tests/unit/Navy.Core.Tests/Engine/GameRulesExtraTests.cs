using FluentAssertions;
using Navy.Core.Engine;
using Navy.Core.Models;
using Xunit;

namespace Navy.Core.Tests.Engine
{
    /// <summary>
    /// Additional coverage tests to ensure all GameRules constants and paths are hit.
    /// </summary>
    public sealed class GameRulesExtraTests
    {
        [Fact]
        public void TurnTimerSeconds_Is300()
        {
            GameRules.TurnTimerSeconds.Should().Be(300);
        }

        [Fact]
        public void TurnWarningYellowSec_Is60()
        {
            GameRules.TurnWarningYellowSec.Should().Be(60);
        }

        [Fact]
        public void TurnWarningRedSec_Is30()
        {
            GameRules.TurnWarningRedSec.Should().Be(30);
        }

        [Fact]
        public void TurnWarningBlinkSec_Is10()
        {
            GameRules.TurnWarningBlinkSec.Should().Be(10);
        }

        [Fact]
        public void GetConfig_InvalidMapType_ThrowsArgumentOutOfRange()
        {
            var act = () => GameRules.GetConfig((MapType)99);
            act.Should().Throw<System.ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ResolveMapConflict_BothLarge_ReturnsLarge()
        {
            GameRules.ResolveMapConflict(MapType.Large, MapType.Large).Should().Be(MapType.Large);
        }

        [Fact]
        [Trait("FR", "FR-GP-09")]
        public void CheckWinCondition_EmptyBoards_HostBoardEmpty_GuestWins()
        {
            // Host board has no ships at all — guest wins
            var state = new GameState
            {
                Host = new PlayerState { Uid = "h", Board = new Board(10) },
                Guest = new PlayerState { Uid = "g", Board = new Board(10) }
            };
            state.Guest.Board.AddShip(new Ship(1, ShipOrientation.Horizontal, new Cell(0, 0)));
            // Host has no ships → AliveShipsCount = 0 → Guest wins
            GameRules.CheckWinCondition(state).Should().Be("g");
        }
    }
}
