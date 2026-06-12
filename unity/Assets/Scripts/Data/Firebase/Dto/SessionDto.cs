// Navy.Data.Firebase.Dto
// Serialization DTOs for Firebase Realtime Database

using System.Collections.Generic;

namespace Navy.Data.Firebase.Dto
{
    /// <summary>Mirrors /sessions/{code}/meta in RTDB.</summary>
    [System.Serializable]
    public class SessionDto
    {
        public string? hostUid;
        public string? guestUid;
        public string? mapType;          // "Small" | "Medium" | "Large" | null
        public string? hostMapChoice;
        public string? guestMapChoice;
        public string? phase;            // "Lobby" | "Setup" | "Playing" | "Finished"
        public string? currentTurnUid;
        public long    turnStartedAtMs;
        public string? winnerUid;
        public bool    isDraw;
        public long    createdAtMs;
    }
}
