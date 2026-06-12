// Navy.Core.Engine
// Pure C# - NO UnityEngine dependency

using System;

namespace Navy.Core.Engine
{
    /// <summary>
    /// Generates a 6-digit numeric session code.
    /// </summary>
    public static class SessionCodeGenerator
    {
        private static readonly Random _rng = new Random();

        /// <summary>Returns a zero-padded 6-digit code, e.g. "048291".</summary>
        public static string Generate()
        {
            int value = _rng.Next(0, 1_000_000);
            return value.ToString("D6");
        }
    }
}
