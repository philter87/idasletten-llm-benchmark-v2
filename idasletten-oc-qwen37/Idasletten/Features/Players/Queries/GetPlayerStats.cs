using Idasletten.Models;
using MediatR;

namespace Idasletten.Features.Players.Queries;

public record GetPlayerStatsQuery(Guid UserId) : IRequest<PlayerStatsDto?>;

public class PlayerStatsDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? ImageUrl { get; set; }
    public int TotalMatches { get; set; }
    public int TotalWins { get; set; }
    public int TotalLosses { get; set; }
    public int TotalPointsWon { get; set; }
    public int TotalPointsLost { get; set; }
    public List<TournamentStatsDto> Tournaments { get; set; } = new();
}

public class TournamentStatsDto
{
    public Guid TournamentId { get; set; }
    public string TournamentName { get; set; } = string.Empty;
    public int Matches { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double Score { get; set; }
}
