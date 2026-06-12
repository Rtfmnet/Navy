// Navy.Data.Firebase.Dto

using System.Collections.Generic;

namespace Navy.Data.Firebase.Dto
{
    /// <summary>Mirrors /sessions/{code}/shots/{pushId} in RTDB.</summary>
    [System.Serializable]
    public class ShotDto
    {
        public string?        shooterUid;
        public string?        targetUid;
        public int            x;
        public int            y;
        public string?        result;            // "Miss" | "Hit" | "Sunk"
        public List<CellDto>? sunkShipCells;     // only on Sunk
        public List<CellDto>? adjacentMissCells; // only on Sunk
        public long           timestampMs;
    }

    [System.Serializable]
    public class CellDto
    {
        public int x;
        public int y;
    }
}
