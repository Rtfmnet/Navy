// Navy.Core.Models
// Pure C# - NO UnityEngine dependency

using System.Collections.Generic;

namespace Navy.Core.Models
{
    public sealed class ShotRecord
    {
        public string ShooterUid { get; set; } = "";
        public string TargetUid { get; set; } = "";
        public Cell Coordinate { get; set; } = null!;
        public ShotResult Result { get; set; }
        public long TimestampMs { get; set; }

        /// <summary>Filled only when Result == Sunk.</summary>
        public IReadOnlyList<Cell>? SunkShipCells { get; set; }

        /// <summary>Filled only when Result == Sunk: cells auto-marked as misses (FR-GP-03).</summary>
        public IReadOnlyList<Cell>? AdjacentMissCells { get; set; }
    }
}
