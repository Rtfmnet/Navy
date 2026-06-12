// Navy.Core.Models
// Pure C# - NO UnityEngine dependency

namespace Navy.Core.Models
{
    public sealed class PlayerState
    {
        public string Uid { get; set; } = "";
        public string Nickname { get; set; } = "";
        public Board Board { get; set; } = null!;    // own board with ships
        public bool IsReady { get; set; }
        public MapType? ChosenMapType { get; set; }  // null until chosen (FR-MP)
        public int Hits { get; set; }
        public int Misses { get; set; }
        public int SunkShips { get; set; }
        public bool BoardCommitted { get; set; }

        /// <summary>Accuracy as a percentage (0–100). Returns 0 if no shots taken.</summary>
        public float Accuracy
        {
            get
            {
                int total = Hits + Misses;
                return total == 0 ? 0f : (Hits / (float)total) * 100f;
            }
        }
    }
}
