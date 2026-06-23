using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Players.Commands;
using Idasletten.Features.Tournaments.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared.Data;

public static class SeedData
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdaslettenDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        if (await db.Tournaments.AnyAsync(cancellationToken))
        {
            await EnsureTestUserAsync(db, cancellationToken);
            return;
        }

        await EnsureTestUserAsync(db, cancellationToken);
        var summer = await mediator.Send(new CreateTournamentCommand("Ragnarok Friday", 2, 5, ScoreSystem.Elo, null, true), cancellationToken);
        var winter = await mediator.Send(new CreateTournamentCommand("Valhal Winter League", 2, 5, ScoreSystem.Lives, 12, true), cancellationToken);
        foreach (var initials in new[] { "ODN", "THR", "LOK", "FRG", "TYR", "BAL", "SIG", "EIR" }) await mediator.Send(new AddPlayerToTournamentCommand(summer, initials, NameFromInitials(initials)), cancellationToken);
        foreach (var initials in new[] { "ASK", "EMB", "FRE", "HEL", "VID", "ULL" }) await mediator.Send(new AddPlayerToTournamentCommand(winter, initials, NameFromInitials(initials)), cancellationToken);
        await mediator.Send(new RecordMatchCommand(summer, null, ["ODN", "THR"], ["LOK", "FRG"], 5, 3), cancellationToken);
        await mediator.Send(new RecordMatchCommand(summer, null, ["TYR", "BAL"], ["SIG", "EIR"], 4, 5), cancellationToken);
        await mediator.Send(new CreatePlannedMatchCommand(summer, ["ODN", "LOK"], ["THR", "FRG"]), cancellationToken);
        await mediator.Send(new CreatePlannedMatchCommand(summer, ["TYR", "SIG"], ["BAL", "EIR"]), cancellationToken);
    }

    private static async Task EnsureTestUserAsync(IdaslettenDbContext db, CancellationToken cancellationToken)
    {
        var email = Environment.GetEnvironmentVariable("TestUser__Email");
        if (string.IsNullOrWhiteSpace(email) || await db.Users.AnyAsync(user => user.NormalizedUserName == "TEST", cancellationToken)) return;
        db.Users.Add(new AppUser { UserName = "TEST", NormalizedUserName = "TEST", Name = "Test User", Email = email });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string NameFromInitials(string initials) => initials switch
    {
        "ODN" => "Odin", "THR" => "Thor", "LOK" => "Loki", "FRG" => "Frigg", "TYR" => "Tyr", "BAL" => "Balder", "SIG" => "Sigrun", "EIR" => "Eir", _ => initials
    };
}
