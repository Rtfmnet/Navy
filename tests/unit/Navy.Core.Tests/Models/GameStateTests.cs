using FluentAssertions;
using Navy.Core.Models;
using Xunit;

namespace Navy.Core.Tests.Models
{
    public sealed class GameStateTests
    {
        private GameState MakeState()
        {
            return new GameState
            {
                SessionId = "123456",
                Host = new PlayerState { Uid = "host-uid", Nickname = "Host", Board = new Board(10) },
                Guest = new PlayerState { Uid = "guest-uid", Nickname = "Guest", Board = new Board(10) }
            };
        }

        [Fact]
        public void GetPlayer_HostUid_ReturnsHost()
        {
            var state = MakeState();
            state.GetPlayer("host-uid").Should().BeSameAs(state.Host);
        }

        [Fact]
        public void GetPlayer_GuestUid_ReturnsGuest()
        {
            var state = MakeState();
            state.GetPlayer("guest-uid").Should().BeSameAs(state.Guest);
        }

        [Fact]
        public void GetPlayer_UnknownUid_ReturnsNull()
        {
            var state = MakeState();
            state.GetPlayer("unknown").Should().BeNull();
        }

        [Fact]
        public void GetOpponent_HostUid_ReturnsGuest()
        {
            var state = MakeState();
            state.GetOpponent("host-uid").Should().BeSameAs(state.Guest);
        }

        [Fact]
        public void GetOpponent_GuestUid_ReturnsHost()
        {
            var state = MakeState();
            state.GetOpponent("guest-uid").Should().BeSameAs(state.Host);
        }

        [Fact]
        public void GetOpponent_UnknownUid_ReturnsNull()
        {
            var state = MakeState();
            state.GetOpponent("nobody").Should().BeNull();
        }
    }
}
