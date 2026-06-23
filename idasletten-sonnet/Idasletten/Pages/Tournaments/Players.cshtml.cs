using Idasletten.Features.Players.Commands.AddPlayer;
using Idasletten.Features.Players.Queries.GetTournamentPlayers;
using Idasletten.Features.Tournaments.Queries.GetAllTournaments;
using Idasletten.Features.Tournaments.Queries.GetTournament;
using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Pages.Tournaments;

public class PlayersPageModel(ISender sender, AppDbContext db) : PageModel
{
    public Tournament? Tournament { get; set; }
    public List<TournamentPlayer> Players { get; set; } = [];
    public List<TournamentPlayer> SeedTournamentPlayers { get; set; } = [];
    public List<Tournament> AllTournaments { get; set; } = [];
    public bool SeedTournamentSelected => Tournament?.SeedTournamentId.HasValue == true;
    public string? SeedTournamentName { get; set; }

    public async Task OnGetAsync(Guid tournamentId)
    {
        await LoadDataAsync(tournamentId);
    }

    private async Task LoadDataAsync(Guid tournamentId)
    {
        Tournament = await sender.Send(new GetTournamentQuery(tournamentId));
        if (Tournament is null) return;

        Players = await sender.Send(new GetTournamentPlayersQuery(tournamentId));
        AllTournaments = await sender.Send(new GetAllTournamentsQuery(IncludeChildren: true, IncludeArchived: true));

        if (Tournament.SeedTournamentId.HasValue)
        {
            SeedTournamentPlayers = await db.TournamentPlayers
                .Include(tp => tp.User)
                .Where(tp => tp.TournamentId == Tournament.SeedTournamentId.Value)
                .OrderByDescending(tp => tp.Score)
                .ToListAsync();
            SeedTournamentName = AllTournaments.FirstOrDefault(t => t.Id == Tournament.SeedTournamentId)?.Name;
        }
        else
        {
            SeedTournamentPlayers = AllTournaments
                .Where(t => t.Id != tournamentId)
                .SelectMany(t => new List<TournamentPlayer>())
                .ToList();
        }
    }

    public async Task<IActionResult> OnPostAddPlayerAsync(Guid tournamentId, string initials, string? playerName)
    {
        if (!string.IsNullOrWhiteSpace(initials))
            await sender.Send(new AddPlayerCommand(tournamentId, initials, playerName));
        return RedirectToPage(new { tournamentId });
    }

    public async Task<IActionResult> OnPostAddFromSeedAsync(Guid tournamentId, string initials)
    {
        if (!string.IsNullOrWhiteSpace(initials))
            await sender.Send(new AddPlayerCommand(tournamentId, initials));
        return RedirectToPage(new { tournamentId });
    }

    public async Task<IActionResult> OnPostSetSeedAsync(Guid tournamentId, Guid seedTournamentId)
    {
        var tournament = await db.Tournaments.FindAsync(tournamentId);
        if (tournament is not null)
        {
            tournament.SeedTournamentId = seedTournamentId;
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { tournamentId });
    }
}
