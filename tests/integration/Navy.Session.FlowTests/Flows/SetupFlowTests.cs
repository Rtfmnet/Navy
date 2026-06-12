using System.Threading.Tasks;
using FluentAssertions;
using Navy.Core.Engine;
using Navy.Core.Models;
using Navy.Core.TestKit.Fakes;
using Xunit;

namespace Navy.Session.FlowTests.Flows
{
    public sealed class SetupFlowTests
    {
        private static async Task<(FakeInMemorySessionService host, FakeInMemorySessionService guest, SessionStore store, FakeTimeProvider time)>
            SetupInSetupPhase()
        {
            var store = new SessionStore();
            var time = new FakeTimeProvider();
            var host = new FakeInMemorySessionService(store, time, isHost: true);
            var guest = new FakeInMemorySessionService(store, time, isHost: false);
            host.SetPeer(guest);
            guest.SetPeer(host);

            var code = await host.CreateSessionAsync("Host");
            await guest.JoinSessionAsync(code, "Guest");
            await host.SubmitMapChoiceAsync(MapType.Medium);
            await guest.SubmitMapChoiceAsync(MapType.Medium);
            await host.SetMapAndAdvanceToSetupAsync(MapType.Medium);
            return (host, guest, store, time);
        }

        private static Board MakeValidMediumBoard()
        {
            var config = GameRules.GetConfig(MapType.Medium);
            AutoPlacer.TryPlace(config, out var board);
            return board!;
        }

        [Fact]
        [Trait("FR", "FR-SP-05")]
        public async Task BothCommitted_CanStartGame()
        {
            var (host, guest, store, _) = await SetupInSetupPhase();
            host.SetLocalBoard(MakeValidMediumBoard());
            guest.SetLocalBoard(MakeValidMediumBoard());

            await host.CommitBoardAsync();
            await guest.CommitBoardAsync();

            store.Players["host-uid"].BoardCommitted.Should().BeTrue();
            store.Players["guest-uid"].BoardCommitted.Should().BeTrue();
        }

        [Fact]
        [Trait("FR", "FR-SP-05")]
        public async Task OnlyOneCommitted_GameDoesNotStart()
        {
            var (host, _, store, _) = await SetupInSetupPhase();
            host.SetLocalBoard(MakeValidMediumBoard());
            await host.CommitBoardAsync();

            store.Players["host-uid"].BoardCommitted.Should().BeTrue();
            // Guest not committed
            store.Players.TryGetValue("guest-uid", out var gs);
            (gs?.BoardCommitted ?? false).Should().BeFalse();
            store.Phase.Should().Be(GamePhase.Setup, "game should not start until both commit");
        }

        [Fact]
        [Trait("FR", "FR-SP-05")]
        public async Task BothCommitted_StartGame_AdvancesToPlayingPhase()
        {
            var (host, guest, store, _) = await SetupInSetupPhase();
            host.SetLocalBoard(MakeValidMediumBoard());
            guest.SetLocalBoard(MakeValidMediumBoard());

            await host.CommitBoardAsync();
            await guest.CommitBoardAsync();

            var firstTurn = GameRules.PickFirstTurnUid(host.LocalUid, guest.LocalUid);
            await host.StartGameAsync(firstTurn);

            store.Phase.Should().Be(GamePhase.Playing);
            store.CurrentTurnUid.Should().BeOneOf(host.LocalUid, guest.LocalUid);
        }

        [Fact]
        [Trait("FR", "FR-SP-03")]
        public async Task AutoPlacer_UsedByHost_ProducesValidBoard()
        {
            var (host, _, _, _) = await SetupInSetupPhase();
            var config = GameRules.GetConfig(MapType.Medium);
            AutoPlacer.TryPlace(config, out var board);
            board.Should().NotBeNull();
            BoardValidator.IsFullyValid(board!, config).Should().BeTrue();
            host.SetLocalBoard(board!);
            await host.CommitBoardAsync();
        }
    }
}
