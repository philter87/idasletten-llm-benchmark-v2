using MediatR;

namespace Idasletten.Features.Tournaments;

public record TournamentCreated(Guid TournamentId, string Name) : INotification;
