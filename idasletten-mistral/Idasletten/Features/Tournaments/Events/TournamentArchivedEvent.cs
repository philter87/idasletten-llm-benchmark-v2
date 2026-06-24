using Idasletten.Shared.Data.Entities;
using MediatR;

namespace Idasletten.Features.Tournaments.Events;

public record TournamentArchivedEvent(Tournament Tournament, bool IsArchived) : INotification;
