using MediatR;

namespace Idasletten.Features.Matches.Queries;

public record GetMatchesForTournamentQuery(Guid TournamentId) : IRequest<MatchesResult>;

public class MatchesResult
{
    public List<MatchViewModel> Planned { get; set; } = [];
    public List<MatchViewModel> Completed { get; set; } = [];
}

public class MatchViewModel
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public string State { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? PlayedAt { get; set; }
    public List<TeamViewModel> Teams { get; set; } = [];
}

public class TeamViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int Number { get; set; }
    public int GoalsWon { get; set; }
    public int GoalsLost { get; set; }
    public List<string> PlayerInitials { get; set; } = [];
}

public record GetMatchByIdQuery(Guid TournamentId, Guid MatchId) : IRequest<MatchViewModel?>;
