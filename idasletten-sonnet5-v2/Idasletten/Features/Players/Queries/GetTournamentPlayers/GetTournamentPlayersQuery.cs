using MediatR;

namespace Idasletten.Features.Players.Queries.GetTournamentPlayers;

public record GetTournamentPlayersQuery(Guid TournamentId) : IRequest<IReadOnlyList<TournamentPlayerDto>>;

public record TournamentPlayerDto(
    Guid TournamentPlayerId,
    Guid UserId,
    string Username,
    string Name,
    string? ImageUrl,
    double Score,
    int WinCount,
    int LoseCount,
    int MatchCount);
