using Idasletten.Models;
using MediatR;

namespace Idasletten.Features.Matches.Commands;

public record CompleteMatchCommand(Guid MatchId, List<TeamResultDto> TeamResults) : IRequest;
