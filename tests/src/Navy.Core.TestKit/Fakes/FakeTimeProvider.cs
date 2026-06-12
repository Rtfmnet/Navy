using Navy.Core.Contracts;

namespace Navy.Core.TestKit.Fakes
{
    /// <summary>
    /// Controllable time source for tests. Advance virtual time without sleeping.
    /// </summary>
    public sealed class FakeTimeProvider : ITimeProvider
    {
        private long _nowMs;

        public FakeTimeProvider(long startMs = 0)
        {
            _nowMs = startMs;
        }

        public long NowMs => _nowMs;

        /// <summary>Advance virtual time by the given number of milliseconds.</summary>
        public void AdvanceMs(long ms) => _nowMs += ms;

        /// <summary>Advance virtual time by the given number of seconds.</summary>
        public void AdvanceSec(long seconds) => AdvanceMs(seconds * 1000L);
    }
}
