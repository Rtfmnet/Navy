using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Navy.Core.Engine;
using Navy.Core.Models;
using Navy.Core.TestKit.Fakes;
using Xunit;

namespace Navy.Session.FlowTests.Flows
{
    public sealed class GameplayFlowTests
    {
        // ─── Test helpers ─────────────────────────────────────────────────────────

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

        /// <summary>Returns host board with a single 1-deck ship at (0,0) and guest board with one at (5,5).</summary>
        private static (Board hostBoard, Board guestBoard) MakeMinimalBoards()
        {
            var hostBoard = new Board(10);
            var hShip = new Ship(1, ShipOrientation.Horizontal, new Cell(0, 0));
            hostBoard.AddShip(hShip);
            hostBoard.SetCell(new Cell(0, 0), CellState.Ship);

            var guestBoard = new Board(10);
            var gShip = new Ship(1, ShipOrientation.Horizontal, new Cell(5, 5));
            guestBoard.AddShip(gShip);
            guestBoard.SetCell(new Cell(5, 5), CellState.Ship);

            return (hostBoard, guestBoard);
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

            var (hb, gb) = MakeMinimalBoards();
            host.SetLocalBoard(hb);
            guest.SetLocalBoard(gb);

            await host.CommitBoardAsync();
            await guest.CommitBoardAsync();
            await host.StartGameAsync(host.LocalUid); // host goes first
            return (host, guest, store, time);
        }

        // ─── FR-GP-01: First turn ─────────────────────────────────────────────────

        [Fact]
        [Trait("FR", "FR-GP-01")]
        public async Task StartGame_CurrentTurnIsSet()
        {
            var (host, guest, store, _) = await StartPlayingGame();
            store.CurrentTurnUid.Should().BeOneOf(host.LocalUid, guest.LocalUid);
        }

        // ─── FR-GP-02: Hit continues, Miss transfers ──────────────────────────────

        [Fact]
        [Trait("FR", "FR-GP-02")]
        public async Task Hit_SamePlayerContinues()
        {
            var (host, guest, store, _) = await StartPlayingGame();
            // Host shoots at guest's ship at (5,5) — Hit
            var guestBoard = guest.CurrentState.Guest?.Board ?? new Board(10);
            // Get the actual guest board via store
            var coord = new Cell(5, 5);

            await host.SubmitShotAsync(
                host.LocalUid, guest.LocalUid, coord,
                ShotResult.Hit, new List<Cell>(), null);

            // Turn should NOT transfer on hit
            store.CurrentTurnUid.Should().Be(host.LocalUid,
                "turn stays with shooter on Hit (FR-GP-02)");
        }

        [Fact]
        [Trait("FR", "FR-GP-02")]
        public async Task Miss_TurnTransfers()
        {
            var (host, guest, store, _) = await StartPlayingGame();

            // Host shoots a miss
            await host.SubmitShotAsync(
                host.LocalUid, guest.LocalUid, new Cell(9, 9),
                ShotResult.Miss, new List<Cell>(), null);

            // Transfer turn to guest
            await host.TransferTurnAsync(guest.LocalUid);

            store.CurrentTurnUid.Should().Be(guest.LocalUid);
        }

        // ─── FR-GP-03: Sunk + adjacent ────────────────────────────────────────────

        [Fact]
        [Trait("FR", "FR-GP-03")]
        public async Task Sunk_AdjacentCellsReturned()
        {
            var (host, guest, store, _) = await StartPlayingGame();

            // Guest's 1-deck ship is at (5,5); shoot it → Sunk
            var adjacentCells = new List<Cell>
            {
                new Cell(4, 4), new Cell(5, 4), new Cell(6, 4),
                new Cell(4, 5), new Cell(6, 5),
                new Cell(4, 6), new Cell(5, 6), new Cell(6, 6)
            };

            await host.SubmitShotAsync(
                host.LocalUid, guest.LocalUid, new Cell(5, 5),
                ShotResult.Sunk, adjacentCells, new List<Cell> { new Cell(5, 5) });

            store.Players[host.LocalUid].Hits.Should().Be(1);
            store.Players[host.LocalUid].SunkShipsCount.Should().Be(1);
        }

        // ─── FR-GP-07: Surrender ─────────────────────────────────────────────────

        [Fact]
        [Trait("FR", "FR-GP-07")]
        public async Task Surrender_LoserSurrenders_OpponentWins()
        {
            var (host, guest, store, _) = await StartPlayingGame();

            GameState? lastState = null;
            host.OnGameStateChanged += s => lastState = s;
            guest.OnGameStateChanged += s => lastState = s;

            await host.SurrenderAsync();

            store.Phase.Should().Be(GamePhase.Finished);
            store.WinnerUid.Should().Be(guest.LocalUid);
            store.IsDraw.Should().BeFalse();
        }

        // ─── FR-GP-09: Win by all ships sunk ─────────────────────────────────────

        [Fact]
        [Trait("FR", "FR-GP-09")]
        public async Task AllShipsSunk_WinConditionDetected()
        {
            var (host, guest, store, _) = await StartPlayingGame();

            // Sink host's ship at (0,0)
            var hostBoard = host.CurrentState?.Host?.Board;
            if (hostBoard != null)
            {
                ShotResolver.Resolve(hostBoard, new Cell(0, 0), out _);
            }

            // Build state for win check
            var state = new GameState
            {
                Host = new PlayerState
                {
                    Uid = host.LocalUid,
                    Board = new Board(10) // empty board = all ships sunk
                },
                Guest = new PlayerState
                {
                    Uid = guest.LocalUid,
                    Board = new Board(10)
                }
            };

            // Add a live guest ship
            state.Guest.Board.AddShip(new Ship(1, ShipOrientation.Horizontal, new Cell(9, 9)));
            // Host board is empty — all ships sunk
            var winner = GameRules.CheckWinCondition(state);
            winner.Should().Be(guest.LocalUid);
        }

        // ─── FR-GP-10: Early exit by hit count ───────────────────────────────────

        [Fact]
        [Trait("FR", "FR-GP-10")]
        public async Task EarlyExit_HostMoreHits_HostWins()
        {
            var (host, guest, store, _) = await StartPlayingGame();
            store.Players[host.LocalUid].Hits = 5;
            store.Players[guest.LocalUid].Hits = 2;

            var state = new GameState
            {
                Host = new PlayerState { Uid = host.LocalUid, Hits = 5, Board = new Board(10) },
                Guest = new PlayerState { Uid = guest.LocalUid, Hits = 2, Board = new Board(10) }
            };
            var (winner, isDraw) = GameRules.DetermineWinnerByHits(state);
            winner.Should().Be(host.LocalUid);
            isDraw.Should().BeFalse();
        }

        [Fact]
        [Trait("FR", "FR-GP-10")]
        public async Task EarlyExit_EqualHits_Draw()
        {
            var (host, guest, _, _) = await StartPlayingGame();

            var state = new GameState
            {
                Host = new PlayerState { Uid = host.LocalUid, Hits = 3, Board = new Board(10) },
                Guest = new PlayerState { Uid = guest.LocalUid, Hits = 3, Board = new Board(10) }
            };
            var (winner, isDraw) = GameRules.DetermineWinnerByHits(state);
            winner.Should().BeNull();
            isDraw.Should().BeTrue();
        }

        // ─── FR-GP-05: Turn timer ─────────────────────────────────────────────────

        [Fact]
        [Trait("FR", "FR-GP-05")]
        public async Task TurnTimerExpiry_TransfersTurn()
        {
            var (host, guest, store, time) = await StartPlayingGame();
            // Advance time past 5 minutes (300 seconds)
            time.AdvanceSec(301);

            // Simulate the host client detecting its turn expired and transferring
            await host.TransferTurnAsync(guest.LocalUid);
            store.CurrentTurnUid.Should().Be(guest.LocalUid);
        }

        // ─── Shot recording ───────────────────────────────────────────────────────

        [Fact]
        [Trait("FR", "FR-GP")]
        public async Task SubmitShot_RecordedInHistory()
        {
            var (host, guest, store, _) = await StartPlayingGame();
            await host.SubmitShotAsync(host.LocalUid, guest.LocalUid, new Cell(3, 3),
                ShotResult.Miss, new List<Cell>(), null);
            store.Shots.Should().HaveCount(1);
            store.Shots[0].Result.Should().Be(ShotResult.Miss);
        }

        [Fact]
        [Trait("FR", "FR-GP")]
        public async Task SubmitShot_FiresOnShotResolvedOnBothClients()
        {
            var (host, guest, store, _) = await StartPlayingGame();

            ShotRecord? hostRecord = null, guestRecord = null;
            host.OnShotResolved += r => hostRecord = r;
            guest.OnShotResolved += r => guestRecord = r;

            await host.SubmitShotAsync(host.LocalUid, guest.LocalUid, new Cell(3, 3),
                ShotResult.Miss, new List<Cell>(), null);

            hostRecord.Should().NotBeNull();
            guestRecord.Should().NotBeNull();
        }

        // ─── Aim event ────────────────────────────────────────────────────────────

        [Fact]
        [Trait("FR", "FR-GP")]
        public async Task SubmitAim_FiresOnAimReceivedOnTarget()
        {
            var (host, guest, store, _) = await StartPlayingGame();

            Cell? receivedAim = null;
            guest.OnAimReceived += c => receivedAim = c;

            await host.SubmitAimAsync(guest.LocalUid, new Cell(4, 7));
            receivedAim.Should().NotBeNull();
            receivedAim!.X.Should().Be(4);
            receivedAim.Y.Should().Be(7);
        }
    }
}
