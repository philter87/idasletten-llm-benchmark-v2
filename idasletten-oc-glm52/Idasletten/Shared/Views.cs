using Idasletten.Features.Users;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Players;
using Idasletten.Features.Matches;
using Idasletten.Features.Teams;

namespace Idasletten.Shared;

public class PlayerView
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = "";
    public string Name { get; set; } = "";
    public string? ImageUrl { get; set; }
    public double Score { get; set; }
    public int WinCount { get; set; }
    public int LoseCount { get; set; }
    public int MatchCount { get; set; }
    public int Lives { get; set; }
    public int PointsWon { get; set; }
    public int PointsLost { get; set; }
    public double ScoreDiff { get; set; }
}

public class MatchSummaryView
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public MatchState State { get; set; }
    public string Display { get; set; } = "";
}

public class TournamentView
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int TeamSize { get; set; }
    public int PointsToWin { get; set; }
    public ScoreSystem ScoreSystem { get; set; }
    public int? MaxPlayerCount { get; set; }
    public bool IsArchived { get; set; }
    public bool IsPublic { get; set; }
    public Guid? SeedTournamentId { get; set; }
    public Guid? ParentTournamentId { get; set; }
    public int? RoundNumber { get; set; }
    public int PlayerCount { get; set; }
}