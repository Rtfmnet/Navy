// Navy.Core.Models
// Pure C# - NO UnityEngine dependency

using System.Collections.Generic;

namespace Navy.Core.Models
{
    public sealed class GameState
    {
        public string SessionId { get; set; } = "";
        public MapType MapType { get; set; }
        public GamePhase Phase { get; set; }
        public PlayerState Host { get; set; } = null!;
        public PlayerState Guest { get; set; } = null!;
        public string? CurrentTurnUid { get; set; }   // UID of the player whose turn it is
        public long TurnStartedAtMs { get; set; }    // Firebase ServerValue.TIMESTAMP
        public List<ShotRecord> History { get; set; } = new List<ShotRecord>();
        public string? WinnerUid { get; set; }        // null until finished
        public bool IsDraw { get; set; }

        public PlayerState? GetPlayer(string uid)
        {
            if (Host?.Uid == uid) return Host;
            if (Guest?.Uid == uid) return Guest;
            return null;
        }

        public PlayerState? GetOpponent(string uid)
        {
            if (Host?.Uid == uid) return Guest;
            if (Guest?.Uid == uid) return Host;
            return null;
        }
    }
}
