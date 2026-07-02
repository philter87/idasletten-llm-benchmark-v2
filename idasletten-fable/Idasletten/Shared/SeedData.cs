using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Tournaments.Commands;
using Idasletten.Features.Users.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared;

/// <summary>
/// Seeds the test user (when TestUser__Email/TestUser__Password are set) and demo
/// data. Runs both at local startup and from the test WebApplicationFactory.
/// </summary>
public static class SeedData
{
    public static async Task EnsureSeeded(IServiceProvider services, bool seedDemoData)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await SeedTestUser(mediator, configuration);

        if (seedDemoData && !await db.Tournaments.AnyAsync())
            await SeedDemoData(mediator);
    }

    private static async Task SeedTestUser(IMediator mediator, IConfiguration configuration)
    {
        var email = configuration["TestUser:Email"];
        var password = configuration["TestUser:Password"];
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            return;

        var initials = new string(email.TakeWhile(c => c != '@').Take(3).ToArray()).ToUpperInvariant();
        await mediator.Send(new CreateUserCommand(initials, "Test User", email));
    }

    private static async Task SeedDemoData(IMediator mediator)
    {
        // A finished, archived tournament — usable as seed source for new ones.
        var autumn = await mediator.Send(new CreateTournamentCommand(
            "Valhal Høst 2025", TeamSize: 2, PointsToWin: 5, ScoreSystem.WinCount, IsPublic: true));

        var gods = new (string Initials, string Name)[]
        {
            ("THO", "Thor Odinson"), ("ODI", "Odin Alfader"), ("LOK", "Loke Laufeyson"),
            ("FRE", "Freja Vanadis"), ("BAL", "Balder den Gode"), ("TYR", "Tyr den Enarmede"),
            ("HEI", "Heimdal Vogter"), ("SIF", "Sif Gyldenhår")
        };
        foreach (var (initials, name) in gods)
            await mediator.Send(new AddPlayerToTournamentCommand(autumn.Id, initials, name));

        await mediator.Send(new RecordMatchResultCommand(autumn.Id,
            [new TeamResultInput(["THO", "LOK"], 5), new TeamResultInput(["ODI", "FRE"], 3)]));
        await mediator.Send(new RecordMatchResultCommand(autumn.Id,
            [new TeamResultInput(["BAL", "TYR"], 2), new TeamResultInput(["HEI", "SIF"], 5)]));
        await mediator.Send(new RecordMatchResultCommand(autumn.Id,
            [new TeamResultInput(["THO", "LOK"], 5), new TeamResultInput(["HEI", "SIF"], 4)]));

        await ArchiveTournament(mediator, autumn.Id);

        // The current public tournament, seeded from the autumn one.
        var spring = await mediator.Send(new CreateTournamentCommand(
            "Ragnarok Forår 2026", TeamSize: 2, PointsToWin: 5, ScoreSystem.Elo, IsPublic: true));
        foreach (var (initials, name) in gods)
            await mediator.Send(new AddPlayerToTournamentCommand(spring.Id, initials, name));

        await mediator.Send(new SetSeedTournamentCommand(spring.Id, autumn.Id));

        await mediator.Send(new RecordMatchResultCommand(spring.Id,
            [new TeamResultInput(["THO", "SIF"], 5), new TeamResultInput(["LOK", "BAL"], 2)]));
        await mediator.Send(new RecordMatchResultCommand(spring.Id,
            [new TeamResultInput(["ODI", "TYR"], 5), new TeamResultInput(["FRE", "HEI"], 4)]));
        await mediator.Send(new RecordMatchResultCommand(spring.Id,
            [new TeamResultInput(["THO", "SIF"], 3), new TeamResultInput(["ODI", "TYR"], 5)]));

        await mediator.Send(new PlanSeveralMatchesCommand(
            spring.Id, GamesPerPlayer: 2, FixedTeams: false, SeedingType.Fair));

        // A private singles tournament with the Lives system.
        var einherjer = await mediator.Send(new CreateTournamentCommand(
            "Einherjernes Kamp", TeamSize: 1, PointsToWin: 10, ScoreSystem.Lives, IsPublic: false));
        foreach (var (initials, name) in gods.Take(4))
            await mediator.Send(new AddPlayerToTournamentCommand(einherjer.Id, initials, name));
        await mediator.Send(new RecordMatchResultCommand(einherjer.Id,
            [new TeamResultInput(["THO"], 10), new TeamResultInput(["LOK"], 7)]));
    }

    private static async Task ArchiveTournament(IMediator mediator, Guid tournamentId)
    {
        await mediator.Send(new ArchiveTournamentCommand(tournamentId, true));
    }
}
