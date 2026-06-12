// Navy.Data.Firebase.Dto

namespace Navy.Data.Firebase.Dto
{
    /// <summary>Mirrors /sessions/{code}/players/{uid} in RTDB.</summary>
    [System.Serializable]
    public class PlayerDto
    {
        public string? nickname;
        public bool    connected;
        public bool    isReady;
        public bool    boardCommitted;
        public int     hits;
        public int     misses;
        public int     sunkShipsCount;
        public string? chosenMapType;
    }
}
