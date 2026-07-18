namespace Idasletten.Shared;

public enum ScoreSystem { Elo, TrueSkill, Lives, WinCount }
public enum MatchState { Planned, Done, Cancelled }
public enum SeedingType { Random, Equality, Fair }

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public string? ImageUrl { get; set; }
    public ICollection<TournamentPlayer> TournamentPlayers { get; set; } = [];
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
    public bool IsPublic { get; set; }
    public Guid? SeedTournamentId { get; set; }
    public Guid? ParentTournamentId { get; set; }
    public int? RoundNumber { get; set; }
    public ICollection<TournamentPlayer> Players { get; set; } = [];
    public ICollection<TournamentTeam> Teams { get; set; } = [];
    public ICollection<TournamentMatch> Matches { get; set; } = [];
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
    public int? Lives { get; set; }
    public int PointsWon { get; set; }
    public int PointsLost { get; set; }
    public double ScoreDiff { get; set; }
    public User User { get; set; } = null!;
    public Tournament Tournament { get; set; } = null!;
    public ICollection<TournamentTeamPlayer> Teams { get; set; } = [];
}

public class TournamentTeam
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TournamentId { get; set; }
    public string Name { get; set; } = "";
    public int Number { get; set; }
    public Tournament Tournament { get; set; } = null!;
    public ICollection<TournamentTeamPlayer> Players { get; set; } = [];
}

public class TournamentTeamPlayer
{
    public Guid TeamId { get; set; }
    public Guid TournamentPlayerId { get; set; }
    public TournamentTeam Team { get; set; } = null!;
    public TournamentPlayer TournamentPlayer { get; set; } = null!;
}

public class TournamentMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Order { get; set; }
    public Guid TournamentId { get; set; }
    public MatchState State { get; set; } = MatchState.Planned;
    public DateTimeOffset? PlayedAt { get; set; }
    public Tournament Tournament { get; set; } = null!;
    public ICollection<TournamentMatchTeam> Teams { get; set; } = [];
    public ICollection<TournamentTeamMatchResult> Results { get; set; } = [];
}

public class TournamentMatchTeam
{
    public Guid MatchId { get; set; }
    public Guid TeamId { get; set; }
    public TournamentMatch Match { get; set; } = null!;
    public TournamentTeam Team { get; set; } = null!;
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
