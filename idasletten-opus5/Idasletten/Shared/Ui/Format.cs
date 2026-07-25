using System.Globalization;
using Idasletten.Features.Tournaments;

namespace Idasletten.Shared.Ui;

/// <summary>Small formatting helpers so the Razor pages stay free of logic.</summary>
public static class Format
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    /// <summary>A score rendered with the number of decimals the score system needs.</summary>
    public static string Score(double score, ScoreSystem system) =>
        score.ToString("F" + system.Decimals(), Culture);

    /// <summary>The change since the last match, rendered as +12 or -12.</summary>
    public static string Delta(double delta, ScoreSystem system)
    {
        var rounded = Math.Round(delta, system.Decimals());
        if (Math.Abs(rounded) < 0.0001)
        {
            return "-";
        }

        var sign = rounded > 0 ? "+" : string.Empty;
        return sign + rounded.ToString("F" + system.Decimals(), Culture);
    }

    public static string DeltaClass(double delta) => delta switch
    {
        > 0.0001 => "delta-up",
        < -0.0001 => "delta-down",
        _ => "delta-none",
    };

    public static string Lives(int lives) =>
        lives <= 0 ? "-" : string.Concat(Enumerable.Repeat("♥", lives));

    /// <summary>"i dag 14:32", "i går 09:12" or a date for older matches.</summary>
    public static string When(DateTime? utc)
    {
        if (utc is null)
        {
            return "-";
        }

        var local = DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc).ToLocalTime();
        var days = (DateTime.Now.Date - local.Date).Days;

        return days switch
        {
            0 => $"i dag {local:HH:mm}",
            1 => $"i går {local:HH:mm}",
            < 7 => $"{days} dage siden",
            _ => local.ToString("d. MMM yyyy", new CultureInfo("da-DK")),
        };
    }

    public static string RankClass(int rank) => rank switch
    {
        1 => "rank rank-1",
        2 => "rank rank-2",
        3 => "rank rank-3",
        _ => "rank",
    };
}
