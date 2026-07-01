using MediatR;

namespace Idasletten.Features.Players.Queries.GetSeedableTournaments;

/// <summary>All tournaments that could be used as a seed source for the given tournament (i.e. every other tournament).</summary>
public record GetSeedableTournamentsQuery(Guid ExcludeTournamentId) : IRequest<IReadOnlyList<SeedableTournamentDto>>;

public record SeedableTournamentDto(Guid Id, string Name, int PlayerCount);
