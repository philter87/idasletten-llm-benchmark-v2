using MediatR;

namespace Idasletten.Features.Tournaments.Commands;

public record ArchiveTournamentCommand(Guid TournamentId, bool IsArchived) : IRequest<Unit>;
