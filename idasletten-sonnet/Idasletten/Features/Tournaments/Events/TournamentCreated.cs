using MediatR;

namespace Idasletten.Features.Tournaments.Events;

public record TournamentCreated(Guid TournamentId, string Name) : INotification;
