using System.ComponentModel.DataAnnotations;

namespace Idasletten.Shared.Data;

public enum ScoreSystem
{
    Elo = 0,
    TrueSkill = 1,
    Lives = 2,
    WinCount = 3
}

public enum MatchState
{
    Planned = 0,
    Done = 1,
    Cancelled = 2
}

public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(16)] public string UserName { get; set; } = "";
    [MaxLength(16)] public string NormalizedUserName { get; set; } = "";
    [MaxLength(200)] public string Name { get; set; } = "";
    [MaxLength(320)] public string? Email { get; set; }
    [MaxLength(1000)] public string? ImageUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class Tournament
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public int TeamSize { get; set; } = 2;
    public int PointsToWin { get; set; } = 5;
    public ScoreSystem ScoreSystem { get; set; } = ScoreSystem.Elo;
    public int? MaxPlayerCount { get; set; }
    public bool IsArchived { get; set; }
    public bool IsPublic { get; set; } = true;
    public Guid? SeedTournamentId { get; set; }
    public Guid? ParentTournamentId { get; set; }
    public int? RoundNumber { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Tournament? SeedTournament { get; set; }
    public Tournament? ParentTournament { get; set; }
    public List<TournamentPlayer> Players { get; set; } = [];
    public List<TournamentMatch> Matches { get; set; } = [];
}

public class TournamentPlayer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid TournamentId { get; set; }
    public double Score { get; set; } = 1000;
    public int WinCount { get; set; }
    public int MatchCount { get; set; }
    public int LoseCount { get; set; }
    public int Lives { get; set; } = 3;
    public int PointsWon { get; set; }
    public int PointsLost { get; set; }
    public double ScoreDiff { get; set; }
    public AppUser User { get; set; } = null!;
    public Tournament Tournament { get; set; } = null!;
    public List<TournamentTeamPlayer> Teams { get; set; } = [];
}

public class TournamentTeam
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TournamentId { get; set; }
    public Guid? MatchId { get; set; }
    public string Name { get; set; } = "";
    public int Number { get; set; }
    public TournamentMatch? Match { get; set; }
    public List<TournamentTeamPlayer> Players { get; set; } = [];
    public TournamentTeamMatchResult? Result { get; set; }
}

public class TournamentTeamPlayer
{
    public Guid TournamentTeamId { get; set; }
    public Guid TournamentPlayerId { get; set; }
    public TournamentTeam Team { get; set; } = null!;
    public TournamentPlayer Player { get; set; } = null!;
}

public class TournamentMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Order { get; set; }
    public Guid TournamentId { get; set; }
    public MatchState State { get; set; } = MatchState.Planned;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public Tournament Tournament { get; set; } = null!;
    public List<TournamentTeam> Teams { get; set; } = [];
    public List<TournamentTeamMatchResult> Results { get; set; } = [];
}

public class TournamentTeamMatchResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MatchId { get; set; }
    public Guid TournamentId { get; set; }
    public Guid TeamId { get; set; }
    public int GoalsWon { get; set; }
    public int GoalsLost { get; set; }
    public TournamentMatch Match { get; set; } = null!;
    public TournamentTeam Team { get; set; } = null!;
}
