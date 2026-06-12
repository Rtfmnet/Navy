using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Navy.Core.Contracts;
using Navy.Core.Engine;
using Navy.Core.Models;

namespace Navy.Core.TestKit.Fakes
{
    /// <summary>
    /// Full in-memory implementation of ISessionService for Phase 1 tests.
    /// Two instances (host and guest) share a single SessionStore to simulate RTDB.
    /// </summary>
    public sealed class FakeInMemorySessionService : ISessionService
    {
        private readonly SessionStore _store;
        private readonly FakeTimeProvider _time;
        private readonly bool _isHost;

        private string _localUid = "";
        private string _localNick = "";
        private Board? _localBoard;

        // Peer reference — needed to fire events on the other client
        private FakeInMemorySessionService? _peer;

        public FakeInMemorySessionService(SessionStore store, FakeTimeProvider time, bool isHost)
        {
            _store = store;
            _time = time;
            _isHost = isHost;
        }

        /// <summary>Wire up the peer so events are forwarded.</summary>
        public void SetPeer(FakeInMemorySessionService peer) => _peer = peer;

        // ─── ISessionService ──────────────────────────────────────────────────

        public string LocalUid => _localUid;
        public bool IsHost => _isHost;

        public GameState CurrentState
        {
            get
            {
                if (_store.SessionCode == null) return null!;
                return BuildGameState();
            }
        }

        // ─── Events ───────────────────────────────────────────────────────────

        public event Action<GameState>? OnGameStateChanged;
        public event Action<bool>? OnOpponentConnectionChanged;
        public event Action<ShotRecord>? OnShotResolved;
        public event Action<Cell>? OnAimReceived;

        // Explicit interface implementations
        event Action<GameState> ISessionService.OnGameStateChanged
        {
            add => OnGameStateChanged += value;
            remove => OnGameStateChanged -= value;
        }
        event Action<bool> ISessionService.OnOpponentConnectionChanged
        {
            add => OnOpponentConnectionChanged += value;
            remove => OnOpponentConnectionChanged -= value;
        }
        event Action<ShotRecord> ISessionService.OnShotResolved
        {
            add => OnShotResolved += value;
            remove => OnShotResolved -= value;
        }
        event Action<Cell> ISessionService.OnAimReceived
        {
            add => OnAimReceived += value;
            remove => OnAimReceived -= value;
        }

        // ─── Auth ─────────────────────────────────────────────────────────────

        public UniTask<string> SignInAnonymouslyAsync(CancellationToken ct = default)
        {
            _localUid = _isHost ? "host-uid" : "guest-uid";
            return UniTask.FromResult(_localUid);
        }

        // ─── Session lifecycle ────────────────────────────────────────────────

        public UniTask<string> CreateSessionAsync(string nickname, CancellationToken ct = default)
        {
            _localNick = nickname;
            _localUid = "host-uid";
            string code = SessionCodeGenerator.Generate();
            _store.SessionCode = code;
            _store.HostUid = _localUid;
            _store.Phase = GamePhase.Lobby;
            _store.Players[_localUid] = new PlayerSlot
            {
                Uid = _localUid,
                Nickname = nickname,
                Connected = true
            };
            return UniTask.FromResult(code);
        }

        public UniTask JoinSessionAsync(string sessionCode, string nickname, CancellationToken ct = default)
        {
            if (_store.SessionCode != sessionCode)
                throw new InvalidOperationException("Session not found.");
            if (_store.GuestUid != null)
                throw new InvalidOperationException("Session already full.");

            _localNick = nickname;
            _localUid = "guest-uid";
            _store.GuestUid = _localUid;
            _store.Players[_localUid] = new PlayerSlot
            {
                Uid = _localUid,
                Nickname = nickname,
                Connected = true
            };

            // Notify both sides
            NotifyStateChanged();
            _peer?.NotifyStateChanged();

            return UniTask.CompletedTask;
        }

        public UniTask LeaveSessionAsync(CancellationToken ct = default)
        {
            if (_store.Players.TryGetValue(_localUid, out var slot))
                slot.Connected = false;

            _peer?.OnOpponentConnectionChanged?.Invoke(false);
            _peer?.NotifyStateChanged();
            return UniTask.CompletedTask;
        }

        // ─── Setup phase ──────────────────────────────────────────────────────

        public UniTask SubmitMapChoiceAsync(MapType choice, CancellationToken ct = default)
        {
            string choiceStr = choice.ToString();
            if (_isHost)
                _store.HostMapChoice = choiceStr;
            else
                _store.GuestMapChoice = choiceStr;

            if (_store.Players.TryGetValue(_localUid, out var slot))
                slot.ChosenMapType = choiceStr;

            NotifyStateChanged();
            _peer?.NotifyStateChanged();
            return UniTask.CompletedTask;
        }

        public UniTask CommitBoardAsync(CancellationToken ct = default)
        {
            if (_store.Players.TryGetValue(_localUid, out var slot))
            {
                slot.BoardCommitted = true;
                slot.IsReady = true;
            }
            NotifyStateChanged();
            _peer?.NotifyStateChanged();
            return UniTask.CompletedTask;
        }

        public UniTask SetMapAndAdvanceToSetupAsync(MapType resolved, CancellationToken ct = default)
        {
            _store.MapType = resolved.ToString();
            _store.Phase = GamePhase.Setup;
            NotifyStateChanged();
            _peer?.NotifyStateChanged();
            return UniTask.CompletedTask;
        }

        public UniTask StartGameAsync(string firstTurnUid, CancellationToken ct = default)
        {
            _store.Phase = GamePhase.Playing;
            _store.CurrentTurnUid = firstTurnUid;
            _store.TurnStartedAtMs = _time.NowMs;
            _store.TurnVersion = 0;
            NotifyStateChanged();
            _peer?.NotifyStateChanged();
            return UniTask.CompletedTask;
        }

        public UniTask FinishGameAsync(string? winnerUid, bool isDraw, CancellationToken ct = default)
        {
            _store.Phase = GamePhase.Finished;
            _store.WinnerUid = winnerUid;
            _store.IsDraw = isDraw;
            NotifyStateChanged();
            _peer?.NotifyStateChanged();
            return UniTask.CompletedTask;
        }

        // ─── Local board ──────────────────────────────────────────────────────

        public void SetLocalBoard(Board board)
        {
            _localBoard = board;
        }

        // ─── Gameplay ─────────────────────────────────────────────────────────

        public UniTask SubmitAimAsync(string targetUid, Cell coord, CancellationToken ct = default)
        {
            _store.PendingAim = new PendingAim
            {
                ShooterUid = _localUid,
                TargetUid = targetUid,
                X = coord.X,
                Y = coord.Y
            };

            // Fire OnAimReceived on the target client (the peer)
            _peer?.OnAimReceived?.Invoke(coord);
            return UniTask.CompletedTask;
        }

        public UniTask SubmitShotAsync(
            string shooterUid, string targetUid, Cell coord,
            ShotResult result,
            IReadOnlyList<Cell>? adjacentMissCells,
            IReadOnlyList<Cell>? sunkShipCells,
            CancellationToken ct = default)
        {
            // Update stats
            if (_store.Players.TryGetValue(shooterUid, out var shooter))
            {
                if (result == ShotResult.Miss)
                    shooter.Misses++;
                else
                    shooter.Hits++;

                if (result == ShotResult.Sunk)
                    shooter.SunkShipsCount++;
            }

            var record = new ShotRecord
            {
                ShooterUid = shooterUid,
                TargetUid = targetUid,
                Coordinate = coord,
                Result = result,
                TimestampMs = _time.NowMs,
                AdjacentMissCells = adjacentMissCells ?? new List<Cell>(),
                SunkShipCells = sunkShipCells ?? new List<Cell>()
            };
            _store.Shots.Add(record);

            // Notify both clients
            OnShotResolved?.Invoke(record);
            _peer?.OnShotResolved?.Invoke(record);

            NotifyStateChanged();
            _peer?.NotifyStateChanged();
            return UniTask.CompletedTask;
        }

        public UniTask TransferTurnAsync(string nextTurnUid, CancellationToken ct = default)
        {
            // Optimistic concurrency: only the current turn holder can transfer
            if (_store.CurrentTurnUid != _localUid)
                throw new InvalidOperationException("Turn already transferred (race condition).");

            _store.CurrentTurnUid = nextTurnUid;
            _store.TurnStartedAtMs = _time.NowMs;
            _store.TurnVersion++;

            NotifyStateChanged();
            _peer?.NotifyStateChanged();
            return UniTask.CompletedTask;
        }

        public UniTask SurrenderAsync(CancellationToken ct = default)
        {
            _store.SurrenderUid = _localUid;
            string? winnerUid = _localUid == _store.HostUid ? _store.GuestUid : _store.HostUid;
            _store.Phase = GamePhase.Finished;
            _store.WinnerUid = winnerUid;
            _store.IsDraw = false;

            NotifyStateChanged();
            _peer?.NotifyStateChanged();
            return UniTask.CompletedTask;
        }

        // ─── Manual test control ──────────────────────────────────────────────

        /// <summary>Simulate this UID disconnecting (e.g. app background).</summary>
        public void SimulateDisconnect(string uid)
        {
            if (_store.Players.TryGetValue(uid, out var slot))
                slot.Connected = false;

            // Notify the other side
            if (uid == _localUid)
                _peer?.OnOpponentConnectionChanged?.Invoke(false);
            else
                OnOpponentConnectionChanged?.Invoke(false);
        }

        /// <summary>Simulate reconnect for a uid.</summary>
        public void SimulateReconnect(string uid)
        {
            if (_store.Players.TryGetValue(uid, out var slot))
                slot.Connected = true;

            if (uid == _localUid)
                _peer?.OnOpponentConnectionChanged?.Invoke(true);
            else
                OnOpponentConnectionChanged?.Invoke(true);
        }

        /// <summary>Reset store for rematch (keeps UIDs and nicknames).</summary>
        public static void ResetForRematch(SessionStore store, FakeTimeProvider time)
        {
            store.Phase = GamePhase.Lobby;
            store.MapType = null;
            store.HostMapChoice = null;
            store.GuestMapChoice = null;
            store.CurrentTurnUid = null;
            store.WinnerUid = null;
            store.IsDraw = false;
            store.TurnStartedAtMs = 0;
            store.TurnVersion = 0;
            store.Shots.Clear();
            store.PendingAim = null;
            store.SurrenderUid = null;

            foreach (var slot in store.Players.Values)
            {
                slot.BoardCommitted = false;
                slot.IsReady = false;
                slot.Hits = 0;
                slot.Misses = 0;
                slot.SunkShipsCount = 0;
                slot.ChosenMapType = null;
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private void NotifyStateChanged()
        {
            OnGameStateChanged?.Invoke(BuildGameState());
        }

        private GameState BuildGameState()
        {
            string? hostUid = _store.HostUid;
            string? guestUid = _store.GuestUid;

            PlayerState? host = null;
            if (hostUid != null && _store.Players.TryGetValue(hostUid, out var hs))
            {
                host = new PlayerState
                {
                    Uid = hostUid,
                    Nickname = hs.Nickname,
                    Board = (_isHost ? _localBoard : _peer?._localBoard) ?? new Board(10),
                    IsReady = hs.IsReady,
                    BoardCommitted = hs.BoardCommitted,
                    Hits = hs.Hits,
                    Misses = hs.Misses,
                    SunkShips = hs.SunkShipsCount,
                    ChosenMapType = hs.ChosenMapType != null
                        ? (MapType?)Enum.Parse(typeof(MapType), hs.ChosenMapType)
                        : null
                };
            }

            PlayerState? guest = null;
            if (guestUid != null && _store.Players.TryGetValue(guestUid, out var gs))
            {
                guest = new PlayerState
                {
                    Uid = guestUid,
                    Nickname = gs.Nickname,
                    Board = (!_isHost ? _localBoard : _peer?._localBoard) ?? new Board(10),
                    IsReady = gs.IsReady,
                    BoardCommitted = gs.BoardCommitted,
                    Hits = gs.Hits,
                    Misses = gs.Misses,
                    SunkShips = gs.SunkShipsCount,
                    ChosenMapType = gs.ChosenMapType != null
                        ? (MapType?)Enum.Parse(typeof(MapType), gs.ChosenMapType)
                        : null
                };
            }

            MapType mapType = MapType.Medium;
            if (_store.MapType != null)
                mapType = (MapType)Enum.Parse(typeof(MapType), _store.MapType);

            return new GameState
            {
                SessionId = _store.SessionCode ?? "",
                MapType = mapType,
                Phase = _store.Phase,
                Host = host!,
                Guest = guest!,
                CurrentTurnUid = _store.CurrentTurnUid,
                TurnStartedAtMs = _store.TurnStartedAtMs,
                History = new System.Collections.Generic.List<ShotRecord>(_store.Shots),
                WinnerUid = _store.WinnerUid,
                IsDraw = _store.IsDraw
            };
        }
    }
}
