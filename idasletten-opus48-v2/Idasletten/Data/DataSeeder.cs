using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Players.Commands;
using Idasletten.Features.Tournaments.Commands;
using Idasletten.Shared.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Data;

/// <summary>
/// Seeds demo data for both local runs and tests so every page has something to show, and
/// ensures the configured test user exists. Idempotent: does nothing if tournaments already exist.
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var mediator = services.GetRequiredService<IMediator>();
        var config = services.GetRequiredService<IConfiguration>();

        await EnsureTestUserAsync(db, config);

        if (await db.Tournaments.AnyAsync()) return;

        // --- Public Elo tournament with a full set of results. ---
        var cupId = await mediator.Send(new CreateTournamentCommand(
            "Ragnarök Cup", TeamSize: 2, PointsToWin: 5, ScoreSystem.Elo,
            MaxPlayerCount: null, IsPublic: true));

        string[] warriors = { "THO", "ODI", "LOK", "FRE", "BAL", "TYR" };
        foreach (var w in warriors)
            await mediator.Send(new AddPlayerCommand(cupId, w, NameFor(w)));

        (string[] teamA, string[] teamB, int ga, int gb)[] cupMatches =
        {
            (new[] { "THO", "ODI" }, new[] { "LOK", "FRE" }, 5, 3),
            (new[] { "BAL", "TYR" }, new[] { "THO", "LOK" }, 2, 5),
            (new[] { "ODI", "FRE" }, new[] { "BAL", "THO" }, 5, 4),
            (new[] { "LOK", "TYR" }, new[] { "ODI", "BAL" }, 5, 1),
            (new[] { "THO", "FRE" }, new[] { "TYR", "ODI" }, 3, 5),
        };
        foreach (var m in cupMatches)
            await mediator.Send(new CreateOrUpdateMatchCommand(cupId, null, new()
            {
                new TeamInput(m.teamA.ToList(), m.ga),
                new TeamInput(m.teamB.ToList(), m.gb)
            }));

        // A couple of planned matches.
        await mediator.Send(new CreateOrUpdateMatchCommand(cupId, null, new()
        {
            new TeamInput(new() { "THO", "BAL" }, null),
            new TeamInput(new() { "LOK", "ODI" }, null)
        }));
        await mediator.Send(new CreateOrUpdateMatchCommand(cupId, null, new()
        {
            new TeamInput(new() { "FRE", "TYR" }, null),
            new TeamInput(new() { "ODI", "THO" }, null)
        }));

        // --- A TrueSkill tournament. ---
        var tsId = await mediator.Send(new CreateTournamentCommand(
            "Bifrost Skirmish", TeamSize: 2, PointsToWin: 10, ScoreSystem.TrueSkill,
            MaxPlayerCount: 8, IsPublic: true));
        foreach (var w in new[] { "THO", "ODI", "LOK", "FRE" })
            await mediator.Send(new AddPlayerCommand(tsId, w, NameFor(w)));
        await mediator.Send(new CreateOrUpdateMatchCommand(tsId, null, new()
        {
            new TeamInput(new() { "THO", "LOK" }, 10),
            new TeamInput(new() { "ODI", "FRE" }, 7)
        }));

        // --- An archived (historical) Lives tournament. ---
        var oldId = await mediator.Send(new CreateTournamentCommand(
            "Fimbulwinter Classic", TeamSize: 2, PointsToWin: 5, ScoreSystem.Lives,
            MaxPlayerCount: null, IsPublic: false));
        foreach (var w in new[] { "BAL", "TYR", "THO", "ODI" })
            await mediator.Send(new AddPlayerCommand(oldId, w, NameFor(w)));
        await mediator.Send(new CreateOrUpdateMatchCommand(oldId, null, new()
        {
            new TeamInput(new() { "BAL", "TYR" }, 5),
            new TeamInput(new() { "THO", "ODI" }, 2)
        }));
        var archived = await db.Tournaments.FirstAsync(t => t.Id == oldId);
        archived.IsArchived = true;
        await db.SaveChangesAsync();
    }

    private static async Task EnsureTestUserAsync(AppDbContext db, IConfiguration config)
    {
        var email = config["TestUser:Email"];
        if (string.IsNullOrWhiteSpace(email)) return;

        var normalizedEmail = email.ToUpperInvariant();
        if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail)) return;

        const string username = "TST";
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            Name = "Test User",
            Email = email,
            NormalizedEmail = normalizedEmail,
            EmailConfirmed = true
        });
        await db.SaveChangesAsync();
    }

    private static string NameFor(string initials) => initials switch
    {
        "THO" => "Thor",
        "ODI" => "Odin",
        "LOK" => "Loki",
        "FRE" => "Freya",
        "BAL" => "Balder",
        "TYR" => "Tyr",
        _ => initials
    };
}
