using MediatR;

namespace Idasletten.Features.Players.Queries.GetSeedTournamentPlayers;

/// <summary>Players of a candidate seed tournament, ordered by their score there, with a flag for whether they're already in the target tournament.</summary>
public record GetSeedTournamentPlayersQuery(Guid SeedTournamentId, Guid TargetTournamentId) : IRequest<IReadOnlyList<SeedTournamentPlayerDto>>;

public record SeedTournamentPlayerDto(Guid UserId, string Username, string Name, double ScoreInSeedTournament, bool AlreadyAdded);
