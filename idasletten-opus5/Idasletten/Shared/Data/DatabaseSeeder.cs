using Idasletten.Features.Matches;
using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Players.Commands;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Tournaments.Commands;
using Idasletten.Features.Users.Commands;
using Idasletten.Shared.Auth;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Idasletten.Shared.Data;

/// <summary>
/// Fills an empty database with a believable tournament history. It runs both locally (where the
/// database only lives in memory) and in the tests, so both get the same starting point.
/// Everything goes through the normal commands, which means the seeded scores are calculated by the
/// real scoring code instead of being written by hand.
/// </summary>
public class DatabaseSeeder(
    AppDbContext db,
    ISender sender,
    IOptions<TestUserOptions> testUserOptions,
    ILogger<DatabaseSeeder> logger)
{
    private static readonly (string Initials, string Name)[] Vikings =
    [
        ("THO", "Thor Odinson"),
        ("ODI", "Odin Alfader"),
        ("LOK", "Loke Laufeyson"),
        ("FRJ", "Frøya Njordsdatter"),
        ("TYR", "Tyr Hymirson"),
        ("BAL", "Balder Odinson"),
        ("SIF", "Sif Thorsdatter"),
        ("HEI", "Heimdal Nifurson"),
        ("BRA", "Brage Odinson"),
        ("IDU", "Iduna Bragesdatter"),
        ("NJO", "Njord Vaneson"),
        ("FRE", "Frej Njordson"),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Tournaments.AnyAsync(cancellationToken))
        {
            return;
        }

        logger.LogInformation("Seeding Idasletten with vikings and tournaments");

        foreach (var (initials, name) in Vikings)
        {
            await sender.Send(new GetOrCreateUser(initials, name), cancellationToken);
        }

        await SeedTestUserAsync(cancellationToken);

        var ragnarok = await SeedRagnarokCupAsync(cancellationToken);
        await SeedRoundTwoAsync(ragnarok, cancellationToken);
        await SeedValhalAsync(cancellationToken);
        await SeedBifrostAsync(cancellationToken);
        await SeedMidgardAsync(cancellationToken);
    }

    private async Task SeedTestUserAsync(CancellationToken cancellationToken)
    {
        var testUser = testUserOptions.Value;
        if (!testUser.IsEnabled)
        {
            return;
        }

        await sender.Send(
            new GetOrCreateUser(testUser.Initials, testUser.Name, testUser.Email), cancellationToken);
    }

    /// <summary>The flagship tournament shown on the front page - Elo, doubles, first to 10.</summary>
    private async Task<Guid> SeedRagnarokCupAsync(CancellationToken cancellationToken)
    {
        var tournamentId = await sender.Send(
            new CreateTournament(
                "Ragnarok Cup 2026",
                TeamSize: 2,
                PointsToWin: 10,
                ScoreSystem: ScoreSystem.Elo,
                MaxPlayerCount: 16,
                IsPublic: true),
            cancellationToken);

        await AddPlayersAsync(tournamentId, ["THO", "ODI", "LOK", "FRJ", "TYR", "BAL", "SIF", "HEI"],
            cancellationToken);

        var testUser = testUserOptions.Value;
        if (testUser.IsEnabled)
        {
            await sender.Send(
                new AddPlayerToTournament(tournamentId, testUser.Initials, testUser.Name),
                cancellationToken);
        }

        await PlayAsync(tournamentId, ["THO", "ODI"], 10, ["LOK", "FRJ"], 7, cancellationToken);
        await PlayAsync(tournamentId, ["TYR", "BAL"], 10, ["SIF", "HEI"], 4, cancellationToken);
        await PlayAsync(tournamentId, ["THO", "LOK"], 8, ["ODI", "TYR"], 10, cancellationToken);
        await PlayAsync(tournamentId, ["FRJ", "SIF"], 10, ["BAL", "HEI"], 9, cancellationToken);
        await PlayAsync(tournamentId, ["THO", "TYR"], 10, ["ODI", "BAL"], 6, cancellationToken);
        await PlayAsync(tournamentId, ["LOK", "SIF"], 10, ["FRJ", "HEI"], 8, cancellationToken);

        // A couple of games waiting to be played, planned with the fair seeding.
        await sender.Send(
            new PlanMatches(tournamentId, GamesPerPlayer: 1, FixedTeams: false,
                Seeding: SeedingType.Fair, RandomSeed: 42),
            cancellationToken);

        return tournamentId;
    }

    /// <summary>Round two of the Ragnarok Cup - the four best players continue with scores reset.</summary>
    private async Task SeedRoundTwoAsync(Guid parentTournamentId, CancellationToken cancellationToken)
    {
        var roundTwoId = await sender.Send(
            new CreateTournament(
                "Ragnarok Cup 2026 - runde 2",
                TeamSize: 2,
                PointsToWin: 10,
                ScoreSystem: ScoreSystem.Elo,
                IsPublic: true,
                ParentTournamentId: parentTournamentId,
                AdvancingPlayerCount: 4),
            cancellationToken);

        await sender.Send(
            new PlanMatches(roundTwoId, GamesPerPlayer: 2, FixedTeams: false,
                Seeding: SeedingType.Equality, RandomSeed: 7),
            cancellationToken);
    }

    /// <summary>A TrueSkill tournament so the scoreboard shows the skill numbers as well.</summary>
    private async Task SeedValhalAsync(CancellationToken cancellationToken)
    {
        var tournamentId = await sender.Send(
            new CreateTournament(
                "Valhal Vinterturnering",
                TeamSize: 2,
                PointsToWin: 5,
                ScoreSystem: ScoreSystem.TrueSkill,
                IsPublic: true),
            cancellationToken);

        await AddPlayersAsync(tournamentId, ["ODI", "FRJ", "BRA", "IDU", "NJO", "FRE"], cancellationToken);

        await PlayAsync(tournamentId, ["ODI", "BRA"], 5, ["IDU", "NJO"], 3, cancellationToken);
        await PlayAsync(tournamentId, ["FRJ", "FRE"], 5, ["ODI", "IDU"], 2, cancellationToken);
        await PlayAsync(tournamentId, ["BRA", "NJO"], 4, ["FRJ", "FRE"], 5, cancellationToken);
        await PlayAsync(tournamentId, ["ODI", "FRE"], 5, ["BRA", "IDU"], 1, cancellationToken);

        await sender.Send(
            new PlanMatches(tournamentId, GamesPerPlayer: 1, FixedTeams: true,
                Seeding: SeedingType.Equality, RandomSeed: 3),
            cancellationToken);
    }

    /// <summary>Singles, three lives each - the last viking standing wins.</summary>
    private async Task SeedBifrostAsync(CancellationToken cancellationToken)
    {
        var tournamentId = await sender.Send(
            new CreateTournament(
                "Bifrost Blitz",
                TeamSize: 1,
                PointsToWin: 5,
                ScoreSystem: ScoreSystem.Lives,
                IsPublic: true),
            cancellationToken);

        await AddPlayersAsync(tournamentId, ["THO", "LOK", "TYR", "HEI", "BAL", "SIF"], cancellationToken);

        await PlayAsync(tournamentId, ["THO"], 5, ["LOK"], 3, cancellationToken);
        await PlayAsync(tournamentId, ["TYR"], 5, ["HEI"], 1, cancellationToken);
        await PlayAsync(tournamentId, ["BAL"], 2, ["SIF"], 5, cancellationToken);
        await PlayAsync(tournamentId, ["THO"], 5, ["TYR"], 4, cancellationToken);
        await PlayAsync(tournamentId, ["SIF"], 5, ["LOK"], 2, cancellationToken);
        await PlayAsync(tournamentId, ["HEI"], 5, ["BAL"], 3, cancellationToken);
        await PlayAsync(tournamentId, ["LOK"], 1, ["THO"], 5, cancellationToken);
    }

    /// <summary>Last year's tournament: archived, private and scored on wins only.</summary>
    private async Task SeedMidgardAsync(CancellationToken cancellationToken)
    {
        var tournamentId = await sender.Send(
            new CreateTournament(
                "Midgard Mesterskab 2025",
                TeamSize: 2,
                PointsToWin: 10,
                ScoreSystem: ScoreSystem.WinCount,
                IsPublic: false),
            cancellationToken);

        await AddPlayersAsync(tournamentId, ["THO", "ODI", "LOK", "TYR", "BRA", "NJO"], cancellationToken);

        await PlayAsync(tournamentId, ["THO", "BRA"], 10, ["ODI", "NJO"], 6, cancellationToken);
        await PlayAsync(tournamentId, ["LOK", "TYR"], 7, ["THO", "ODI"], 10, cancellationToken);
        await PlayAsync(tournamentId, ["BRA", "NJO"], 10, ["LOK", "ODI"], 8, cancellationToken);
        await PlayAsync(tournamentId, ["THO", "TYR"], 10, ["BRA", "LOK"], 5, cancellationToken);

        await sender.Send(new SetTournamentArchived(tournamentId, true), cancellationToken);
    }

    private async Task AddPlayersAsync(
        Guid tournamentId, IEnumerable<string> initials, CancellationToken cancellationToken)
    {
        foreach (var player in initials)
        {
            await sender.Send(new AddPlayerToTournament(tournamentId, player), cancellationToken);
        }
    }

    private Task PlayAsync(
        Guid tournamentId,
        string[] homeTeam, int homeGoals,
        string[] awayTeam, int awayGoals,
        CancellationToken cancellationToken) =>
        sender.Send(
            new SaveMatch(
                tournamentId,
                Guid.NewGuid(),
                [new MatchTeamInput(homeTeam, homeGoals), new MatchTeamInput(awayTeam, awayGoals)]),
            cancellationToken);
}
