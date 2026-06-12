using System.Threading.Tasks;
using FluentAssertions;
using Navy.Core.Engine;
using Navy.Core.Models;
using Navy.Core.TestKit.Fakes;
using Xunit;

namespace Navy.Session.FlowTests.Flows
{
    public sealed class EndGameFlowTests
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

        private static async Task<(FakeInMemorySessionService host, FakeInMemorySessionService guest, SessionStore store, FakeTimeProvider time)>
            StartPlayingGame()
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
            await host.StartGameAsync(host.LocalUid);
            return (host, guest, store, time);
        }

        [Fact]
        [Trait("FR", "FR-EG-01")]
        public async Task FinishGame_WinnerSet_PhaseFinished()
        {
            var (host, guest, store, _) = await StartPlayingGame();

            await host.FinishGameAsync(host.LocalUid, false);

            store.Phase.Should().Be(GamePhase.Finished);
            store.WinnerUid.Should().Be(host.LocalUid);
            store.IsDraw.Should().BeFalse();
        }

        [Fact]
        [Trait("FR", "FR-EG-01")]
        public async Task FinishGame_Draw_SetsDraw()
        {
            var (host, guest, store, _) = await StartPlayingGame();

            await host.FinishGameAsync(null, true);

            store.Phase.Should().Be(GamePhase.Finished);
            store.WinnerUid.Should().BeNull();
            store.IsDraw.Should().BeTrue();
        }

        [Fact]
        [Trait("FR", "FR-EG-03")]
        public async Task Rematch_ResetsState_KeepsPlayers()
        {
            var (host, guest, store, time) = await StartPlayingGame();
            await host.FinishGameAsync(host.LocalUid, false);

            // Perform rematch reset
            FakeInMemorySessionService.ResetForRematch(store, time);

            store.Phase.Should().Be(GamePhase.Lobby);
            store.WinnerUid.Should().BeNull();
            store.IsDraw.Should().BeFalse();
            store.Shots.Should().BeEmpty();
            store.MapType.Should().BeNull();
            store.CurrentTurnUid.Should().BeNull();

            // Players still exist
            store.Players.Should().ContainKey(host.LocalUid);
            store.Players.Should().ContainKey(guest.LocalUid);

            // Stats reset
            store.Players[host.LocalUid].Hits.Should().Be(0);
            store.Players[host.LocalUid].BoardCommitted.Should().BeFalse();
        }

        [Fact]
        [Trait("FR", "FR-EG-01")]
        public async Task GameStateChanged_Fires_OnFinish()
        {
            var (host, guest, store, _) = await StartPlayingGame();

            bool fired = false;
            host.OnGameStateChanged += s =>
            {
                if (s.Phase == GamePhase.Finished) fired = true;
            };
            guest.OnGameStateChanged += s =>
            {
                if (s.Phase == GamePhase.Finished) fired = true;
            };

            await host.FinishGameAsync(guest.LocalUid, false);
            fired.Should().BeTrue();
        }
    }
}
