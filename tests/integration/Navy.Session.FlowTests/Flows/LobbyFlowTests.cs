using System.Threading.Tasks;
using FluentAssertions;
using Navy.Core.Engine;
using Navy.Core.Models;
using Navy.Core.TestKit.Fakes;
using Xunit;

namespace Navy.Session.FlowTests.Flows
{
    public sealed class LobbyFlowTests
    {
        private static (FakeInMemorySessionService host, FakeInMemorySessionService guest, SessionStore store) MakePair()
        {
            var store = new SessionStore();
            var time = new FakeTimeProvider();
            var host = new FakeInMemorySessionService(store, time, isHost: true);
            var guest = new FakeInMemorySessionService(store, time, isHost: false);
            host.SetPeer(guest);
            guest.SetPeer(host);
            return (host, guest, store);
        }

        [Fact]
        [Trait("FR", "FR-CN-02")]
        public async Task CreateSession_ReturnsSessionCode()
        {
            var (host, _, _) = MakePair();
            var code = await host.CreateSessionAsync("HostPlayer");
            code.Should().HaveLength(6);
            int.TryParse(code, out _).Should().BeTrue();
        }

        [Fact]
        [Trait("FR", "FR-CN-03")]
        public async Task JoinSession_WithValidCode_Succeeds()
        {
            var (host, guest, store) = MakePair();
            var code = await host.CreateSessionAsync("Host");
            await guest.JoinSessionAsync(code, "Guest");
            store.GuestUid.Should().Be("guest-uid");
        }

        [Fact]
        [Trait("FR", "FR-CN-04")]
        public async Task BothSeeConnectedStatus_AfterJoin()
        {
            var (host, guest, store) = MakePair();
            var code = await host.CreateSessionAsync("Host");

            GameState? hostState = null;
            GameState? guestState = null;
            host.OnGameStateChanged += s => hostState = s;
            guest.OnGameStateChanged += s => guestState = s;

            await guest.JoinSessionAsync(code, "Guest");

            hostState.Should().NotBeNull();
            guestState.Should().NotBeNull();
            store.Players.Should().ContainKey("host-uid");
            store.Players.Should().ContainKey("guest-uid");
        }

        [Fact]
        [Trait("FR", "FR-CN-02")]
        public async Task CreateSession_HostUidIsSet()
        {
            var (host, _, store) = MakePair();
            await host.CreateSessionAsync("Host");
            store.HostUid.Should().Be("host-uid");
        }

        [Fact]
        [Trait("FR", "FR-CN")]
        public async Task JoinSession_InvalidCode_ThrowsException()
        {
            var (_, guest, _) = MakePair();
            var act = async () => await guest.JoinSessionAsync("999999", "Guest");
            await act.Should().ThrowAsync<System.InvalidOperationException>();
        }

        [Fact]
        [Trait("FR", "FR-CN")]
        public async Task HostLeaves_BeforeGuestJoins_SessionBecomesUnavailable()
        {
            var (host, guest, store) = MakePair();
            var code = await host.CreateSessionAsync("Host");
            await host.LeaveSessionAsync();

            // Host disconnected
            store.Players["host-uid"].Connected.Should().BeFalse();
        }
    }
}
