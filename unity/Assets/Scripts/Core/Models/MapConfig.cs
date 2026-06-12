// Navy.Core.Models
// Pure C# - NO UnityEngine dependency

using System.Collections.Generic;

namespace Navy.Core.Models
{
    public sealed class MapConfig
    {
        public MapType Type { get; set; }
        public int BoardSize { get; set; }            // 8 / 10 / 12
        public IReadOnlyList<ShipGroup> Ships { get; set; } = null!;
        public int TotalShips { get; set; }           // 6 / 10 / 15
        public int TotalCells { get; set; }           // 10 / 20 / 30
    }

    public sealed class ShipGroup
    {
        public int Decks { get; set; }   // 1..5
        public int Count { get; set; }
    }
}
