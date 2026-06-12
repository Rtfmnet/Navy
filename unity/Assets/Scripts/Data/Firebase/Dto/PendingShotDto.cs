// Navy.Data.Firebase.Dto
// Pending-shot envelope: shooter writes target cell, target client resolves and produces ShotDto.

namespace Navy.Data.Firebase.Dto
{
    /// <summary>Mirrors /sessions/{code}/pendingShot in RTDB.</summary>
    [System.Serializable]
    public class PendingShotDto
    {
        public string? shooterUid;
        public string? targetUid;
        public int     x;
        public int     y;
    }
}
