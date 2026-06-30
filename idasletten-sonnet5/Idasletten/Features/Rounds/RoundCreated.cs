using MediatR;

namespace Idasletten.Features.Rounds;

public record RoundCreated(Guid TournamentId, Guid ParentTournamentId, int RoundNumber) : INotification;
