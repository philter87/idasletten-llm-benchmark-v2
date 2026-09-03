using MediatR;

namespace Idasletten.Features.Tournaments.Events;

public sealed record TournamentCreated(Guid TournamentId) : INotification;
