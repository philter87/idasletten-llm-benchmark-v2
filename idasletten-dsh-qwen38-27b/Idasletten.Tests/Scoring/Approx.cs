using Idasletten.Models;
using Idasletten.Scoring;

namespace Idasletten.Tests.Scoring;

internal static class Approx
{
    public static void Equal(double expected, double actual, double tolerance = 0.001)
        => Assert.True(Math.Abs(expected - actual) <= tolerance, $"expected ~{expected}, got {actual}");
}
