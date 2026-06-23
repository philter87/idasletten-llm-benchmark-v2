using Idasletten.Features.Matches.Events;
using Idasletten.Features.Players.Commands.AddPlayer;
using Idasletten.Features.Scoring;
using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands.CreateMatch;

public class CreateMatchHandler(AppDbContext db, ScoreCalculatorFactory calculatorFactory, ISender sender, IPublisher publisher)
    : IRequestHandler<CreateMatchCommand, Guid>
{
    public async Task<Guid> Handle(CreateMatchCommand request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments.FindAsync([request.TournamentId], cancellationToken)
            ?? throw new InvalidOperationException("Tournament not found");

        // Ensure all players exist
        foreach (var initials in request.Team1.PlayerInitials.Concat(request.Team2.PlayerInitials))
            await sender.Send(new AddPlayerCommand(request.TournamentId, initials), cancellationToken);

        // Resolve player entities
        var allInitials = request.Team1.PlayerInitials.Select(i => i.ToUpper())
            .Concat(request.Team2.PlayerInitials.Select(i => i.ToUpper()))
            .ToList();

        var users = await db.Users.Where(u => allInitials.Contains(u.Username)).ToListAsync(cancellationToken);
        var players = await db.TournamentPlayers
            .Where(tp => tp.TournamentId == request.TournamentId && users.Select(u => u.Id).Contains(tp.UserId))
            .ToListAsync(cancellationToken);

        var team1UserIds = request.Team1.PlayerInitials.Select(i => i.ToUpper())
            .Select(i => users.First(u => u.Username == i).Id).ToList();
        var team2UserIds = request.Team2.PlayerInitials.Select(i => i.ToUpper())
            .Select(i => users.First(u => u.Username == i).Id).ToList();

        var team1Players = players.Where(p => team1UserIds.Contains(p.UserId)).ToList();
        var team2Players = players.Where(p => team2UserIds.Contains(p.UserId)).ToList();

        // Create or update teams
        var matchOrder = await db.TournamentMatches.Where(m => m.TournamentId == request.TournamentId).CountAsync(cancellationToken) + 1;

        TournamentMatch match;
        if (request.ExistingMatchId.HasValue)
        {
            match = await db.TournamentMatches
                .Include(m => m.TeamResults).ThenInclude(r => r.Team).ThenInclude(t => t.Players)
                .FirstOrDefaultAsync(m => m.Id == request.ExistingMatchId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Match not found");
            db.TournamentTeamMatchResults.RemoveRange(match.TeamResults);
        }
        else
        {
            match = new TournamentMatch
            {
                Id = Guid.NewGuid(),
                TournamentId = request.TournamentId,
                Order = matchOrder,
            };
            db.TournamentMatches.Add(match);
        }

        match.State = MatchState.Done;
        match.PlayedAt = DateTime.UtcNow;

        // Create teams and results
        var team1 = new TournamentTeam
        {
            Id = Guid.NewGuid(),
            TournamentId = request.TournamentId,
            Number = 1,
            Name = $"Team 1",
        };
        team1.Players = team1Players;

        var team2 = new TournamentTeam
        {
            Id = Guid.NewGuid(),
            TournamentId = request.TournamentId,
            Number = 2,
            Name = $"Team 2",
        };
        team2.Players = team2Players;

        db.TournamentTeams.AddRange(team1, team2);

        await db.SaveChangesAsync(cancellationToken);

        var result1 = new TournamentTeamMatchResult
        {
            Id = Guid.NewGuid(),
            MatchId = match.Id,
            TournamentId = request.TournamentId,
            TeamId = team1.Id,
            GoalsWon = request.Team1.Goals,
            GoalsLost = request.Team2.Goals,
        };

        var result2 = new TournamentTeamMatchResult
        {
            Id = Guid.NewGuid(),
            MatchId = match.Id,
            TournamentId = request.TournamentId,
            TeamId = team2.Id,
            GoalsWon = request.Team2.Goals,
            GoalsLost = request.Team1.Goals,
        };

        db.TournamentTeamMatchResults.AddRange(result1, result2);

        // Update scores
        var calculator = calculatorFactory.GetCalculator(tournament.ScoreSystem);
        calculator.UpdateScores(team1Players, team2Players, request.Team1.Goals, request.Team2.Goals, tournament);

        await db.SaveChangesAsync(cancellationToken);
        await publisher.Publish(new MatchResultRecorded(match.Id, request.TournamentId), cancellationToken);

        return match.Id;
    }
}
