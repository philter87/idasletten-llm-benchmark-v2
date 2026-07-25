using Idasletten.Features.Matches;
using Idasletten.Features.Players;

namespace Idasletten.Features.Tournaments;

public class Tournament
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    /// <summary>Players per team.</summary>
    public int TeamSize { get; set; } = 2;

    /// <summary>Points (goals) needed to win a match.</summary>
    public int PointsToWin { get; set; } = 5;

    public ScoreSystem ScoreSystem { get; set; } = ScoreSystem.Elo;

    /// <summary>Null means an unlimited number of players.</summary>
    public int? MaxPlayerCount { get; set; }

    public bool IsArchived { get; set; }

    public bool IsPublic { get; set; }

    /// <summary>Tournament whose results are used to seed/plan the matches of this tournament.</summary>
    public Guid? SeedTournamentId { get; set; }
    public Tournament? SeedTournament { get; set; }

    /// <summary>Set when this tournament is a later round continuing from a previous tournament.</summary>
    public Guid? ParentTournamentId { get; set; }
    public Tournament? ParentTournament { get; set; }

    /// <summary>Round 1 unless this tournament continues from a parent, then parent round + 1.</summary>
    public int? RoundNumber { get; set; } = 1;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public List<TournamentPlayer> Players { get; set; } = [];
    public List<TournamentTeam> Teams { get; set; } = [];
    public List<TournamentMatch> Matches { get; set; } = [];
    public List<Tournament> Rounds { get; set; } = [];

    /// <summary>A tournament may only be seeded when it has no parent (rounds inherit their players).</summary>
    public bool CanBeSeeded => ParentTournamentId is null;

    public bool HasRoomForMorePlayers(int currentPlayerCount) =>
        MaxPlayerCount is null || currentPlayerCount < MaxPlayerCount;
}
