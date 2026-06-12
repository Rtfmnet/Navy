// Navy.Core.Models
// Pure C# - NO UnityEngine dependency

namespace Navy.Core.Models
{
    /// <summary>
    /// Represents a single cell coordinate on the board.
    /// </summary>
    public sealed class Cell
    {
        public int X { get; }
        public int Y { get; }

        public Cell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Cell other)
                return X == other.X && Y == other.Y;
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override string ToString() => $"({X},{Y})";
    }
}
