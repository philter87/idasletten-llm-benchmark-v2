using Idasletten.Shared.Data.Enums;

namespace Idasletten.Shared.Data.Entities;

public class Tournament
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string Name { get; set; } = default!;
    
    /// <summary>
    /// Players per team. Default is 2.
    /// </summary>
    public int TeamSize { get; set; } = 2;
    
    /// <summary>
    /// Points (goals) needed to win a match. Default is 5.
    /// </summary>
    public int PointsToWin { get; set; } = 5;
    
    /// <summary>
    /// Scoring system used for this tournament. Default is TrueSkill.
    /// </summary>
    public ScoreSystem ScoreSystem { get; set; } = ScoreSystem.TrueSkill;
    
    /// <summary>
    /// Optional. Empty means there is no limit to the number of players.
    /// </summary>
    public int? MaxPlayerCount { get; set; }
    
    public bool IsArchived { get; set; } = false;
    public bool IsPublic { get; set; } = true;
    
    /// <summary>
    /// Source tournament used to seed/plan matches in this tournament.
    /// </summary>
    public Guid? SeedTournamentId { get; set; }
    
    /// <summary>
    /// This tournament is a later round that continues from a previous tournament.
    /// The parent's results are used to create this tournament's players.
    /// A tournament may be seeded only if it has no parent.
    /// </summary>
    public Guid? ParentTournamentId { get; set; }
    
    /// <summary>
    /// Only relevant when ParentTournamentId is set. Default is 1 and this is auto-incremented.
    /// </summary>
    public int? RoundNumber { get; set; } = 1;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public Tournament? SeedTournament { get; set; }
    public Tournament? ParentTournament { get; set; }
    public ICollection<Tournament> ChildTournaments { get; set; } = new List<Tournament>();
    public ICollection<Tournament> SeededTournaments { get; set; } = new List<Tournament>();
    public ICollection<TournamentPlayer> TournamentPlayers { get; set; } = new List<TournamentPlayer>();
    public ICollection<TournamentMatch> Matches { get; set; } = new List<TournamentMatch>();
    public ICollection<TournamentTeam> Teams { get; set; } = new List<TournamentTeam>();
}
