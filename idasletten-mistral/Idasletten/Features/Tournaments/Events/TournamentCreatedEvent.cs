using Idasletten.Shared.Data.Entities;
using MediatR;

namespace Idasletten.Features.Tournaments.Events;

public record TournamentCreatedEvent(Tournament Tournament) : INotification;
