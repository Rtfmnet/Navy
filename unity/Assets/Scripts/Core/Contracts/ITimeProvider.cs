// Navy.Core.Contracts
// Pure C# - NO UnityEngine dependency

namespace Navy.Core.Contracts
{
    /// <summary>
    /// Abstraction over system time, useful for testing and avoiding direct DateTime usage.
    /// </summary>
    public interface ITimeProvider
    {
        /// <summary>Current UTC time as Unix milliseconds.</summary>
        long NowMs { get; }
    }
}
