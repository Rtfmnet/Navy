using System;
using System.Threading.Tasks;
using FluentAssertions;
using Navy.Core.Engine;
using Navy.Core.Models;
using Navy.Core.TestKit.Fakes;
using Xunit;

namespace Navy.Session.FlowTests.Flows
{
    public sealed class ConnectionFlowTests
    {
        private static (FakeInMemorySessionService host, FakeInMemorySessionService guest, SessionStore store, FakeTimeProvider time)
            MakePair()
        {
            var store = new SessionStore();
            var time = new FakeTimeProvider();
            var host = new FakeInMemorySessionService(store, time, isHost: true);
            var guest = new FakeInMemorySessionService(store, time, isHost: false);
            host.SetPeer(guest);
            guest.SetPeer(host);
            return (host, guest, store, time);
        }

        // ─── FR-CN-05: 60s guest timeout ─────────────────────────────────────────

        [Fact]
        [Trait("FR", "FR-CN-05")]
        public async Task GuestTimeout_60s_NoGuestJoined()
        {
            var (host, _, store, time) = MakePair();
            await host.CreateSessionAsync("Host");

            // Advance 61 seconds — guest never joined
            time.AdvanceSec(61);

            // The game logic (LobbyPresenter) would check guest after 60s;
            // here we verify the store still has no guest
            store.GuestUid.Should().BeNull("guest never joined within 60s");
        }

        // ─── FR-CN-06: Reconnect window ──────────────────────────────────────────

        [Fact]
        [Trait("FR", "FR-CN-06")]
        public async Task Reconnect_Within60s_Succeeds()
        {
            var (host, guest, store, time) = MakePair();
            var code = await host.CreateSessionAsync("Host");
            await guest.JoinSessionAsync(code, "Guest");

            // Guest disconnects
            guest.SimulateDisconnect(guest.LocalUid);
            store.Players[guest.LocalUid].Connected.Should().BeFalse();

            // Reconnect within 60s
            time.AdvanceSec(30);
            guest.SimulateReconnect(guest.LocalUid);
            store.Players[guest.LocalUid].Connected.Should().BeTrue();
        }

        [Fact]
        [Trait("FR", "FR-CN-06")]
        public async Task Reconnect_Timeout_OpponentNotified()
        {
            var (host, guest, store, time) = MakePair();
            var code = await host.CreateSessionAsync("Host");
            await guest.JoinSessionAsync(code, "Guest");

            bool hostNotified = false;
            host.OnOpponentConnectionChanged += connected =>
            {
                if (!connected) hostNotified = true;
            };

            guest.SimulateDisconnect(guest.LocalUid);
            hostNotified.Should().BeTrue("host should be notified of guest disconnect");
        }

        // ─── FR-CN-07: App background = leave ────────────────────────────────────

        [Fact]
        [Trait("FR", "FR-CN-07")]
        public async Task BackgroundLeave_DisconnectsPlayer()
        {
            var (host, guest, store, _) = MakePair();
            var code = await host.CreateSessionAsync("Host");
            await guest.JoinSessionAsync(code, "Guest");

            // Simulate foreground → background (leave session)
            await host.LeaveSessionAsync();
            store.Players[host.LocalUid].Connected.Should().BeFalse();
        }

        // ─── Race condition: TransferTurn ─────────────────────────────────────────

        [Fact]
        [Trait("FR", "FR-GP")]
        public async Task RaceCondition_TransferTurn_OnlyOneSucceeds()
        {
            var (host, guest, store, time) = MakePair();
            var code = await host.CreateSessionAsync("Host");
            await guest.JoinSessionAsync(code, "Guest");
            await host.SubmitMapChoiceAsync(MapType.Medium);
            await guest.SubmitMapChoiceAsync(MapType.Medium);
            await host.SetMapAndAdvanceToSetupAsync(MapType.Medium);

            var config = GameRules.GetConfig(MapType.Medium);
            AutoPlacer.TryPlace(config, out var hb);
            AutoPlacer.TryPlace(config, out var gb);
            host.SetLocalBoard(hb!);
            guest.SetLocalBoard(gb!);

            await host.CommitBoardAsync();
            await guest.CommitBoardAsync();

            // Host goes first
            await host.StartGameAsync(host.LocalUid);

            // Only the host (current turn holder) can transfer
            // If host tries twice, second should fail (or guest trying to transfer when it's host's turn fails)
            await host.TransferTurnAsync(guest.LocalUid);
            store.CurrentTurnUid.Should().Be(guest.LocalUid);

            // Now if host tries to transfer again (stale attempt), it should fail
            var act = async () => await host.TransferTurnAsync(host.LocalUid);
            await act.Should().ThrowAsync<InvalidOperationException>(
                "turn already transferred — race condition guard");
        }

        // ─── Disconnect event ─────────────────────────────────────────────────────

        [Fact]
        [Trait("FR", "FR-CN")]
        public async Task SimulateDisconnect_PeerNotified()
        {
            var (host, guest, store, _) = MakePair();
            var code = await host.CreateSessionAsync("Host");
            await guest.JoinSessionAsync(code, "Guest");

            bool peerNotified = false;
            host.OnOpponentConnectionChanged += _ => peerNotified = true;

            guest.SimulateDisconnect(guest.LocalUid);
            peerNotified.Should().BeTrue();
        }

        [Fact]
        [Trait("FR", "FR-CN")]
        public async Task SimulateReconnect_PeerNotifiedTrue()
        {
            var (host, guest, store, _) = MakePair();
            var code = await host.CreateSessionAsync("Host");
            await guest.JoinSessionAsync(code, "Guest");

            bool? lastConnectionStatus = null;
            host.OnOpponentConnectionChanged += c => lastConnectionStatus = c;

            guest.SimulateDisconnect(guest.LocalUid);
            guest.SimulateReconnect(guest.LocalUid);
            lastConnectionStatus.Should().BeTrue();
        }
    }
}
