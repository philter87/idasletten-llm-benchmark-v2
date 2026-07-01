using MediatR;

namespace Idasletten.Features.Tournaments.Commands.SetSeedTournament;

public record SeedTournamentSet(Guid TournamentId, Guid SeedTournamentId) : INotification;
