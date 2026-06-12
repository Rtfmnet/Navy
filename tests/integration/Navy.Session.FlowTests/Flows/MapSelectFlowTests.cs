using System.Threading.Tasks;
using FluentAssertions;
using Navy.Core.Engine;
using Navy.Core.Models;
using Navy.Core.TestKit.Fakes;
using Xunit;

namespace Navy.Session.FlowTests.Flows
{
    public sealed class MapSelectFlowTests
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

        private static async Task<(FakeInMemorySessionService, FakeInMemorySessionService, SessionStore, FakeTimeProvider)>
            SetupConnectedPair()
        {
            var (host, guest, store, time) = MakePair();
            var code = await host.CreateSessionAsync("Host");
            await guest.JoinSessionAsync(code, "Guest");
            return (host, guest, store, time);
        }

        [Fact]
        [Trait("FR", "FR-MP")]
        public async Task BothChooseSame_Map_ResolvedCorrectly()
        {
            var (host, guest, store, _) = await SetupConnectedPair();
            await host.SubmitMapChoiceAsync(MapType.Medium);
            await guest.SubmitMapChoiceAsync(MapType.Medium);

            store.HostMapChoice.Should().Be("Medium");
            store.GuestMapChoice.Should().Be("Medium");

            // Host resolves
            var resolved = GameRules.ResolveMapConflict(MapType.Medium, MapType.Medium);
            await host.SetMapAndAdvanceToSetupAsync(resolved);

            store.MapType.Should().Be("Medium");
            store.Phase.Should().Be(GamePhase.Setup);
        }

        [Fact]
        [Trait("FR", "FR-MP")]
        public async Task DifferentChoices_ResolvedToOneOfTwo()
        {
            // Run several times — resolved map must be either Small or Large
            for (int i = 0; i < 20; i++)
            {
                var store = new SessionStore();
                var time = new FakeTimeProvider();
                var host = new FakeInMemorySessionService(store, time, isHost: true);
                var guest = new FakeInMemorySessionService(store, time, isHost: false);
                host.SetPeer(guest);
                guest.SetPeer(host);

                var code = await host.CreateSessionAsync("Host");
                await guest.JoinSessionAsync(code, "Guest");

                await host.SubmitMapChoiceAsync(MapType.Small);
                await guest.SubmitMapChoiceAsync(MapType.Large);

                var resolved = GameRules.ResolveMapConflict(MapType.Small, MapType.Large);
                resolved.Should().BeOneOf(MapType.Small, MapType.Large);
            }
        }

        [Fact]
        [Trait("FR", "FR-MP")]
        public async Task SubmitMapChoice_AdvancesToSetupPhase()
        {
            var (host, guest, store, _) = await SetupConnectedPair();
            await host.SubmitMapChoiceAsync(MapType.Small);
            await guest.SubmitMapChoiceAsync(MapType.Small);
            await host.SetMapAndAdvanceToSetupAsync(MapType.Small);

            store.Phase.Should().Be(GamePhase.Setup);
        }

        [Fact]
        [Trait("FR", "FR-MP")]
        public async Task MapChoices_StoredInPlayerSlots()
        {
            var (host, guest, store, _) = await SetupConnectedPair();
            await host.SubmitMapChoiceAsync(MapType.Large);
            await guest.SubmitMapChoiceAsync(MapType.Medium);

            store.Players["host-uid"].ChosenMapType.Should().Be("Large");
            store.Players["guest-uid"].ChosenMapType.Should().Be("Medium");
        }
    }
}
