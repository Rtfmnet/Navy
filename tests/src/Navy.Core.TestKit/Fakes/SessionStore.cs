using System.Collections.Generic;
using Navy.Core.Models;

namespace Navy.Core.TestKit.Fakes
{
    /// <summary>
    /// Shared in-memory state backing store for FakeInMemorySessionService.
    /// Two service instances share one store to simulate host + guest.
    /// </summary>
    public sealed class SessionStore
    {
        // ─── Session meta ─────────────────────────────────────────────────────
        public string? SessionCode { get; set; }
        public string? HostUid { get; set; }
        public string? GuestUid { get; set; }
        public string? MapType { get; set; }          // resolved, after both chose
        public string? HostMapChoice { get; set; }
        public string? GuestMapChoice { get; set; }
        public GamePhase Phase { get; set; } = GamePhase.Lobby;
        public string? CurrentTurnUid { get; set; }
        public long TurnStartedAtMs { get; set; }
        public string? WinnerUid { get; set; }
        public bool IsDraw { get; set; }

        // ─── Players ──────────────────────────────────────────────────────────
        public Dictionary<string, PlayerSlot> Players { get; } = new Dictionary<string, PlayerSlot>();

        // ─── Shots ────────────────────────────────────────────────────────────
        public List<ShotRecord> Shots { get; } = new List<ShotRecord>();

        // ─── Pending shot (aim) ───────────────────────────────────────────────
        public PendingAim? PendingAim { get; set; }

        // ─── Surrender ────────────────────────────────────────────────────────
        public string? SurrenderUid { get; set; }

        // ─── Optimistic concurrency for turn transfer ─────────────────────────
        public int TurnVersion { get; set; }
    }

    public sealed class PlayerSlot
    {
        public string Uid { get; set; } = "";
        public string Nickname { get; set; } = "";
        public bool Connected { get; set; }
        public bool IsReady { get; set; }
        public bool BoardCommitted { get; set; }
        public int Hits { get; set; }
        public int Misses { get; set; }
        public int SunkShipsCount { get; set; }
        public string? ChosenMapType { get; set; }
    }

    public sealed class PendingAim
    {
        public string ShooterUid { get; set; } = "";
        public string TargetUid { get; set; } = "";
        public int X { get; set; }
        public int Y { get; set; }
    }
}
