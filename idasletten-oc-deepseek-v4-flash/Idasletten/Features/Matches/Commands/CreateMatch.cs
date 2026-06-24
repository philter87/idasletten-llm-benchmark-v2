using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public record CreateMatchCommand(
    Guid TournamentId,
    List<List<string>> TeamPlayerUsernames,
    int? GoalsWonTeam1 = null,
    int? GoalsWonTeam2 = null
) : IRequest<Guid>;

public class CreateMatchHandler : IRequestHandler<CreateMatchCommand, Guid>
{
    private readonly AppDbContext _db;

    public CreateMatchHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(CreateMatchCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _db.Tournaments.FindAsync(new object[] { request.TournamentId }, cancellationToken);
        if (tournament == null)
            throw new InvalidOperationException("Tournament not found.");

        var maxOrder = await _db.TournamentMatches
            .Where(m => m.TournamentId == request.TournamentId)
            .MaxAsync(m => (int?)m.Order, cancellationToken) ?? 0;

        var match = new TournamentMatch
        {
            TournamentId = request.TournamentId,
            Order = maxOrder + 1,
            State = MatchState.Planned
        };
        _db.TournamentMatches.Add(match);

        for (int i = 0; i < request.TeamPlayerUsernames.Count; i++)
        {
            var team = new TournamentTeam
            {
                TournamentId = request.TournamentId,
                Number = i + 1,
                Name = $"Team {i + 1}"
            };
            _db.TournamentTeams.Add(team);

            foreach (var username in request.TeamPlayerUsernames[i])
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Initials == username, cancellationToken);
                TournamentPlayer player;
                if (user == null)
                {
                    user = new User { Id = Guid.NewGuid(), UserName = username, Initials = username, Name = username };
                    _db.Users.Add(user);
                    player = new TournamentPlayer
                    {
                        UserId = user.Id,
                        TournamentId = request.TournamentId,
                        Score = 1000,
                        Lives = 3
                    };
                    _db.TournamentPlayers.Add(player);
                }
                else
                {
                    player = await _db.TournamentPlayers
                        .FirstOrDefaultAsync(tp => tp.UserId == user.Id && tp.TournamentId == request.TournamentId, cancellationToken);
                    if (player == null)
                    {
                        player = new TournamentPlayer
                        {
                            UserId = user.Id,
                            TournamentId = request.TournamentId,
                            Score = 1000,
                            Lives = 3
                        };
                        _db.TournamentPlayers.Add(player);
                    }
                }

                _db.TournamentTeamPlayers.Add(new TournamentTeamPlayer
                {
                    TeamId = team.Id,
                    UserId = user.Id,
                    TournamentId = request.TournamentId
                });
            }

            _db.TournamentMatchTeams.Add(new TournamentMatchTeam
            {
                MatchId = match.Id,
                TeamId = team.Id
            });

            if (request.GoalsWonTeam1.HasValue && request.GoalsWonTeam2.HasValue)
            {
                var isTeam1 = i == 0;
                var result = new TournamentTeamMatchResult
                {
                    MatchId = match.Id,
                    TournamentId = request.TournamentId,
                    TeamId = team.Id,
                    GoalsWon = isTeam1 ? request.GoalsWonTeam1.Value : request.GoalsWonTeam2.Value,
                    GoalsLost = isTeam1 ? request.GoalsWonTeam2.Value : request.GoalsWonTeam1.Value
                };
                _db.TournamentTeamMatchResults.Add(result);
                match.State = MatchState.Done;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return match.Id;
    }
}
