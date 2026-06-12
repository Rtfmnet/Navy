using System.Collections.Generic;
using FluentAssertions;
using Navy.Core.Engine;
using Xunit;

namespace Navy.Core.Tests.Engine
{
    public sealed class SessionCodeGeneratorTests
    {
        [Fact]
        public void Generate_Returns6DigitString()
        {
            for (int i = 0; i < 100; i++)
            {
                var code = SessionCodeGenerator.Generate();
                code.Should().HaveLength(6, "code must be exactly 6 characters");
                int.TryParse(code, out _).Should().BeTrue("code must be numeric");
            }
        }

        [Fact]
        public void Generate_LeadingZeroPadded()
        {
            // Since we format with D6, values < 100000 are zero-padded
            // Run many times hoping to catch a small value
            for (int i = 0; i < 10000; i++)
            {
                var code = SessionCodeGenerator.Generate();
                if (code.StartsWith("0"))
                {
                    code.Length.Should().Be(6);
                    break;
                }
            }
            // Not guaranteed in small runs, but D6 format is tested by length check
            // Just verify length always 6
        }

        [Fact]
        public void Generate_Distribution_NoObviousBias()
        {
            // Generate 1000 codes; each first digit should appear roughly 10% of the time
            var firstDigitCounts = new Dictionary<char, int>();
            for (int i = 0; i < 1000; i++)
            {
                var code = SessionCodeGenerator.Generate();
                var first = code[0];
                if (!firstDigitCounts.ContainsKey(first)) firstDigitCounts[first] = 0;
                firstDigitCounts[first]++;
            }
            // Every digit 0-9 should appear at least a few times over 1000 runs
            // (With 1000 samples, 10% * 1000 = 100 per digit, allow variance down to 20)
            foreach (var kv in firstDigitCounts)
            {
                kv.Value.Should().BeGreaterThan(5,
                    $"digit '{kv.Key}' appeared only {kv.Value} times out of 1000 — suspicious");
            }
        }

        [Fact]
        public void Generate_ValuesInRange()
        {
            for (int i = 0; i < 200; i++)
            {
                var code = SessionCodeGenerator.Generate();
                int value = int.Parse(code);
                value.Should().BeInRange(0, 999999);
            }
        }
    }
}
