using Idasletten.Data;
using Idasletten.Models;
using Idasletten.Shared.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public class CompleteMatchHandler : IRequestHandler<CompleteMatchCommand>
{
    private readonly AppDbContext _db;

    public CompleteMatchHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(CompleteMatchCommand request, CancellationToken cancellationToken)
    {
        var match = await _db.TournamentMatches
            .Include(m => m.Tournament)
            .Include(m => m.TeamResults)
                .ThenInclude(r => r.Team)
                    .ThenInclude(t => t.Players)
            .FirstOrDefaultAsync(m => m.Id == request.MatchId, cancellationToken);

        if (match == null)
            throw new InvalidOperationException("Match not found");

        foreach (var resultDto in request.TeamResults)
        {
            var team = match.TeamResults.FirstOrDefault(r =>
                r.Team.Players.All(p => resultDto.PlayerInitials.Contains(p.User.Username)));

            if (team != null)
            {
                team.GoalsWon = resultDto.GoalsWon;
                team.GoalsLost = request.TeamResults.Where(t => t != resultDto).Sum(t => t.GoalsWon);
            }
        }

        match.State = MatchState.Done;

        var calculator = ScoringCalculatorFactory.GetCalculator(match.Tournament.ScoreSystem);
        calculator.CalculateScores(match.Tournament, match);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
