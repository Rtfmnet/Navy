using System.Collections.Generic;
using Navy.Core.Models;

namespace Navy.Core.TestKit.Builders
{
    /// <summary>
    /// Fluent builder for GameState.
    /// </summary>
    public sealed class GameStateBuilder
    {
        private string _sessionId = "123456";
        private MapType _mapType = MapType.Medium;
        private GamePhase _phase = GamePhase.Playing;
        private string _hostUid = "host";
        private string _guestUid = "guest";
        private string _hostNick = "HostPlayer";
        private string _guestNick = "GuestPlayer";
        private Board? _hostBoard;
        private Board? _guestBoard;
        private string _currentTurnUid = "host";

        public GameStateBuilder WithSession(string id) { _sessionId = id; return this; }
        public GameStateBuilder WithMap(MapType t) { _mapType = t; return this; }
        public GameStateBuilder WithPhase(GamePhase p) { _phase = p; return this; }
        public GameStateBuilder WithHostUid(string uid) { _hostUid = uid; return this; }
        public GameStateBuilder WithGuestUid(string uid) { _guestUid = uid; return this; }
        public GameStateBuilder WithHostBoard(Board b) { _hostBoard = b; return this; }
        public GameStateBuilder WithGuestBoard(Board b) { _guestBoard = b; return this; }
        public GameStateBuilder WithCurrentTurn(string uid) { _currentTurnUid = uid; return this; }

        public GameState Build()
        {
            int size = _mapType switch
            {
                MapType.Small => 8,
                MapType.Large => 12,
                _ => 10
            };

            return new GameState
            {
                SessionId = _sessionId,
                MapType = _mapType,
                Phase = _phase,
                CurrentTurnUid = _currentTurnUid,
                History = new List<ShotRecord>(),
                Host = new PlayerState
                {
                    Uid = _hostUid,
                    Nickname = _hostNick,
                    Board = _hostBoard ?? new Board(size),
                    IsReady = true,
                    BoardCommitted = true
                },
                Guest = new PlayerState
                {
                    Uid = _guestUid,
                    Nickname = _guestNick,
                    Board = _guestBoard ?? new Board(size),
                    IsReady = true,
                    BoardCommitted = true
                }
            };
        }
    }
}
