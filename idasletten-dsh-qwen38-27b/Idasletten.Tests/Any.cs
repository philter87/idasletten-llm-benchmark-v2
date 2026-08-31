using Idasletten.Models;

namespace Idasletten.Tests;

/// <summary>
/// Static factory of "any" domain objects for tests — just enough data to be
/// valid, callers override what a specific test needs.
/// </summary>
public static class Any
{
    public static string Initials = "THO";

    public static User User(string initials = "THO", string? name = null) => new()
    {
        Username = initials,
        Name = name ?? (initials switch
        {
            "THO" => "Thor Odinson",
            "LOV" => "Loki Laufeyson",
            "ODF" => "Odin Borson",
            "FRE" => "Freya Disdottir",
            _ => "Player " + initials
        }),
        Email = $"{initials.ToLowerInvariant()}@idasletten.dk"
    };

    public static Tournament Tournament(
        ScoreSystem scoreSystem = ScoreSystem.Elo,
        int teamSize = 1,
        int pointsToWin = 5,
        int? maxPlayerCount = null,
        bool isPublic = true,
        bool isArchived = false,
        Guid? parentTournamentId = null) => new()
    {
        Name = "Any Tournament",
        ScoreSystem = scoreSystem,
        TeamSize = teamSize,
        PointsToWin = pointsToWin,
        MaxPlayerCount = maxPlayerCount,
        IsPublic = isPublic,
        IsArchived = isArchived,
        RoundNumber = parentTournamentId is null ? 1 : 2,
        ParentTournamentId = parentTournamentId
    };

    public static TournamentPlayer Player(User? user = null, Tournament? tournament = null,
        double score = Elo.Default) => new()
    {
        UserId = user?.Id ?? Guid.NewGuid(),
        TournamentId = tournament?.Id ?? Guid.NewGuid(),
        Score = score,
        Lives = Lives.Default
    };

    public static class Elo { public const double Default = 1500; }
    public static class Lives { public const int Default = 3; }

    public static TournamentMatch Match(int order, MatchState state = MatchState.Done,
        Tournament? tournament = null) => new()
    {
        TournamentId = tournament?.Id ?? Guid.NewGuid(),
        Order = order,
        State = state
    };
}
