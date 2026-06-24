using Idasletten.Shared.Data.Enums;
using MediatR;

namespace Idasletten.Features.Matches.Commands;

public record CreateMatchCommand(
    Guid TournamentId,
    List<string> Team1Players,
    List<string> Team2Players,
    List<int> Team1Goals,
    List<int> Team2Goals,
    MatchState State = MatchState.Planned) : IRequest<Guid>;
