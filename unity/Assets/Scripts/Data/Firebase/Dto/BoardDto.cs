// Navy.Data.Firebase.Dto
// NOTE: Per tech.md §5.6, the full board is intentionally NOT stored in RTDB
// (peer-authoritative model — each client keeps its own ships locally).
// This DTO is reserved for future use (e.g., a server-authoritative migration)
// and is referenced from the architecture document's project structure.

using System.Collections.Generic;

namespace Navy.Data.Firebase.Dto
{
    /// <summary>
    /// Reserved DTO for serialising a full Board to RTDB.
    /// Currently unused — boards are local in peer-authoritative MVP.
    /// </summary>
    [System.Serializable]
    public class BoardDto
    {
        public int             size;
        public List<ShipDto>?  ships;

        [System.Serializable]
        public class ShipDto
        {
            public int     decks;
            public string? orientation;   // "Horizontal" | "Vertical"
            public int     originX;
            public int     originY;
        }
    }
}
