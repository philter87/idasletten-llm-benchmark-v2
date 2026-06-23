using Idasletten.Features.Tournaments;
using Idasletten.Features.Users;
using Idasletten.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Data.Common;

namespace Idasletten.Tests.TestInfrastructure;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing database context registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            // Build the service provider
            var sp = services.BuildServiceProvider();

            // Create a scope to get the context
            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<AppDbContext>();

                // Ensure the database is created
                db.Database.EnsureCreated();

                // Seed test data
                SeedTestData(db, scopedServices).Wait();
            }
        });
    }

    private async Task SeedTestData(AppDbContext db, IServiceProvider serviceProvider)
    {
        // Clear existing data
        db.TournamentTeamMatchResults.RemoveRange(db.TournamentTeamMatchResults);
        db.TournamentMatches.RemoveRange(db.TournamentMatches);
        db.TournamentTeams.RemoveRange(db.TournamentTeams);
        db.TournamentPlayers.RemoveRange(db.TournamentPlayers);
        db.Tournaments.RemoveRange(db.Tournaments);
        db.Users.RemoveRange(db.Users);

        await db.SaveChangesAsync();

        // Seed test users
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        
        var testUsers = new List<User>
        {
            new User { UserName = "JDO", Name = "Jens Dobbeltoft", Email = "jdo@mjolner.com", EmailConfirmed = true },
            new User { UserName = "PCH", Name = "Peter Christensen", Email = "pch@mjolner.com", EmailConfirmed = true },
            new User { UserName = "MAD", Name = "Mads Andersen", Email = "mad@mjolner.com", EmailConfirmed = true },
            new User { UserName = "LAS", Name = "Lars Petersen", Email = "las@mjolner.com", EmailConfirmed = true },
            new User { UserName = "KRI", Name = "Kristian Jensen", Email = "kri@mjolner.com", EmailConfirmed = true },
            new User { UserName = "SOR", Name = "Soren Rasmussen", Email = "sor@mjolner.com", EmailConfirmed = true }
        };

        foreach (var user in testUsers)
        {
            if (!await db.Users.AnyAsync(u => u.UserName == user.UserName))
            {
                var result = await userManager.CreateAsync(user, "Test123!");
                if (!result.Succeeded)
                {
                    throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        // Seed test tournaments
        var tournaments = new List<Tournament>
        {
            new Tournament
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
                Name = "Forårsturnering 2024",
                TeamSize = 2,
                PointsToWin = 5,
                ScoreSystem = ScoreSystem.Elo,
                IsPublic = true,
                IsArchived = false
            },
            new Tournament
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
                Name = "Efterårsturnering 2023",
                TeamSize = 2,
                PointsToWin = 5,
                ScoreSystem = ScoreSystem.WinCount,
                IsPublic = true,
                IsArchived = true
            },
            new Tournament
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440002"),
                Name = "Privat turnering",
                TeamSize = 2,
                PointsToWin = 5,
                ScoreSystem = ScoreSystem.Lives,
                IsPublic = false,
                IsArchived = false
            }
        };

        foreach (var tournament in tournaments)
        {
            if (!await db.Tournaments.AnyAsync(t => t.Id == tournament.Id))
            {
                db.Tournaments.Add(tournament);
            }
        }

        await db.SaveChangesAsync();

        // Add tournament players
        foreach (var tournament in tournaments)
        {
            var users = await db.Users.ToListAsync();
            foreach (var user in users.Take(4)) // Add first 4 users to each tournament
            {
                if (!await db.TournamentPlayers.AnyAsync(tp => tp.TournamentId == tournament.Id && tp.UserId == user.Id))
                {
                    var tournamentPlayer = new TournamentPlayer
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        TournamentId = tournament.Id,
                        Score = tournament.ScoreSystem == ScoreSystem.Elo ? 1200 : 
                               tournament.ScoreSystem == ScoreSystem.TrueSkill ? 25.0 : 
                               tournament.ScoreSystem == ScoreSystem.Lives ? 0 : 0,
                        WinCount = 0,
                        MatchCount = 0,
                        LoseCount = 0,
                        Lives = tournament.ScoreSystem == ScoreSystem.Lives ? 3 : 3,
                        PointsWon = 0,
                        PointsLost = 0,
                        ScoreDiff = 0
                    };
                    db.TournamentPlayers.Add(tournamentPlayer);
                }
            }
        }

        await db.SaveChangesAsync();

        // Seed some matches
        var springTournament = await db.Tournaments.FirstOrDefaultAsync(t => t.Name == "Forårsturnering 2024");
        if (springTournament != null)
        {
            var players = await db.TournamentPlayers
                .Where(tp => tp.TournamentId == springTournament.Id)
                .Include(tp => tp.User)
                .ToListAsync();

            if (players.Count >= 4)
            {
                // Create a match between first 2 vs next 2 players
                var match = new TournamentMatch
                {
                    Id = Guid.NewGuid(),
                    TournamentId = springTournament.Id,
                    Order = 1,
                    State = MatchState.Done
                };

                // Team 1
                var team1 = new TournamentTeam
                {
                    Id = Guid.NewGuid(),
                    Name = "Hold 1",
                    Number = 1,
                    TournamentId = springTournament.Id
                };

                // Add players to team 1
                var player1 = players[0];
                var player2 = players[1];
                team1.Players.Add(player1);
                team1.Players.Add(player2);
                player1.Teams.Add(team1);
                player2.Teams.Add(team1);

                // Team 2
                var team2 = new TournamentTeam
                {
                    Id = Guid.NewGuid(),
                    Name = "Hold 2",
                    Number = 2,
                    TournamentId = springTournament.Id
                };

                // Add players to team 2
                var player3 = players[2];
                var player4 = players[3];
                team2.Players.Add(player3);
                team2.Players.Add(player4);
                player3.Teams.Add(team2);
                player4.Teams.Add(team2);

                // Add teams to match
                match.Teams.Add(team1);
                match.Teams.Add(team2);
                team1.Matches.Add(match);
                team2.Matches.Add(match);

                // Add results
                var result1 = new TournamentTeamMatchResult
                {
                    Id = Guid.NewGuid(),
                    MatchId = match.Id,
                    TournamentId = springTournament.Id,
                    TeamId = team1.Id,
                    GoalsWon = 5,
                    GoalsLost = 3
                };

                var result2 = new TournamentTeamMatchResult
                {
                    Id = Guid.NewGuid(),
                    MatchId = match.Id,
                    TournamentId = springTournament.Id,
                    TeamId = team2.Id,
                    GoalsWon = 3,
                    GoalsLost = 5
                };

                match.Results.Add(result1);
                match.Results.Add(result2);

                db.TournamentMatches.Add(match);
                db.TournamentTeams.Add(team1);
                db.TournamentTeams.Add(team2);

                // Update player stats for this match
                player1.MatchCount = 1;
                player1.WinCount = 1;
                player1.PointsWon = 5;
                player1.PointsLost = 3;
                
                player2.MatchCount = 1;
                player2.WinCount = 1;
                player2.PointsWon = 5;
                player2.PointsLost = 3;

                player3.MatchCount = 1;
                player3.LoseCount = 1;
                player3.PointsWon = 3;
                player3.PointsLost = 5;

                player4.MatchCount = 1;
                player4.LoseCount = 1;
                player4.PointsWon = 3;
                player4.PointsLost = 5;

                // Update scores based on tournament scoring system
                if (springTournament.ScoreSystem == ScoreSystem.Elo)
                {
                    player1.Score = 1220; // Won, so increased
                    player2.Score = 1220;
                    player3.Score = 1180; // Lost, so decreased
                    player4.Score = 1180;
                    player1.ScoreDiff = 20;
                    player2.ScoreDiff = 20;
                    player3.ScoreDiff = -20;
                    player4.ScoreDiff = -20;
                }
                else if (springTournament.ScoreSystem == ScoreSystem.WinCount)
                {
                    player1.Score = 1; // 1 win
                    player2.Score = 1;
                    player3.Score = 0; // 0 wins
                    player4.Score = 0;
                }
            }
        }

        await db.SaveChangesAsync();
    }
}
