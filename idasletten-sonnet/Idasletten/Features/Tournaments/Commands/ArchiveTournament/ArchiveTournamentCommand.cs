using MediatR;

namespace Idasletten.Features.Tournaments.Commands.ArchiveTournament;

public record ArchiveTournamentCommand(Guid TournamentId) : IRequest;
