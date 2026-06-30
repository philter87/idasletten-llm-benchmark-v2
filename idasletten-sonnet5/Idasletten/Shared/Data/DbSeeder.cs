using Idasletten.Features.Matches;
using Idasletten.Features.Matches.Commands.CreatePlannedMatch;
using Idasletten.Features.Matches.Commands.SaveMatch;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Tournaments.Commands.CreateTournament;
using Idasletten.Features.Users;
using Idasletten.Shared.Auth;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared.Data;

/// <summary>
/// Seeds a handful of users/tournaments/matches, used both for local development (so the app
/// isn't empty on first run) and by the test WebApplicationFactory. Runs through the same
/// MediatR commands the UI uses, so seeded data exercises real scoring/recalculation logic.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IdaslettenDbContext db, ISender sender, TestUserOptions testUserOptions, CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        if (testUserOptions is { Enabled: true, Email: not null })
        {
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                UserName = "test",
                NormalizedUserName = "TEST",
                Name = "Test Testesen",
                Email = testUserOptions.Email,
                NormalizedEmail = testUserOptions.Email.ToUpperInvariant(),
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N")
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        var eloTournamentId = await sender.Send(new CreateTournamentCommand(
            "Fredagsturnering", TeamSize: 2, PointsToWin: 5, ScoreSystem.Elo, MaxPlayerCount: null, IsPublic: true), cancellationToken);

        await SeedMatchesAsync(sender, eloTournamentId,
            ["test", "ODI", "THO", "LOK", "FRE", "BAL"], cancellationToken);

        var trueSkillTournamentId = await sender.Send(new CreateTournamentCommand(
            "Vinterserien", TeamSize: 2, PointsToWin: 5, ScoreSystem.TrueSkill, MaxPlayerCount: null, IsPublic: true), cancellationToken);

        await SeedMatchesAsync(sender, trueSkillTournamentId,
            ["ODI", "THO", "LOK", "FRE"], cancellationToken);

        await sender.Send(new CreateTournamentCommand(
            "Arkiveret turnering 2025", TeamSize: 2, PointsToWin: 5, ScoreSystem.WinCount, MaxPlayerCount: null, IsPublic: false), cancellationToken);
    }

    private static async Task SeedMatchesAsync(ISender sender, Guid tournamentId, string[] usernames, CancellationToken cancellationToken)
    {
        // A couple of completed matches (so scores/standings have real data)...
        for (var i = 0; i < 2; i++)
        {
            var matchId = await sender.Send(new CreatePlannedMatchCommand(tournamentId), cancellationToken);
            var teamA = usernames[(i * 2) % usernames.Length];
            var teamB = usernames[(i * 2 + 1) % usernames.Length];
            await sender.Send(new SaveMatchCommand(
                matchId, tournamentId,
                [new TeamInput([teamA], 5), new TeamInput([teamB], 3)],
                RecordResult: true), cancellationToken);
        }

        // ...and one still-planned match.
        var plannedMatchId = await sender.Send(new CreatePlannedMatchCommand(tournamentId), cancellationToken);
        await sender.Send(new SaveMatchCommand(
            plannedMatchId, tournamentId,
            [new TeamInput([usernames[0]], 0), new TeamInput([usernames[1]], 0)],
            RecordResult: false), cancellationToken);
    }
}
