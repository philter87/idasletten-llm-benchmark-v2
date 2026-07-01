using MediatR;

namespace Idasletten.Features.Tournaments.Commands.CreateTournament;

public record TournamentCreated(Guid TournamentId) : INotification;
