using Idasletten.Features.Matches.Queries;
using Idasletten.Features.Players.Commands;
using Idasletten.Features.Players.Queries;
using Idasletten.Features.Tournaments.Commands;
using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class DetailModel(ISender sender) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid TournamentId { get; set; }

    public TournamentDetail Tournament { get; private set; } = null!;

    public IReadOnlyList<ScoreboardRow> Scoreboard { get; private set; } = [];

    public MatchOverview Matches { get; private set; } = new([], []);

    public async Task<IActionResult> OnGetAsync()
    {
        return await LoadAsync() ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostAddPlayerAsync(string initials, string? name)
    {
        try
        {
            await sender.Send(new AddPlayerToTournament(TournamentId, initials, name));
            TempData["Message"] = $"{initials.ToUpperInvariant()} er med i turneringen.";
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToPage(new { tournamentId = TournamentId });
    }

    public async Task<IActionResult> OnPostArchiveAsync(bool archived)
    {
        await sender.Send(new SetTournamentArchived(TournamentId, archived));
        return RedirectToPage(new { tournamentId = TournamentId });
    }

    private async Task<bool> LoadAsync()
    {
        var tournament = await sender.Send(new GetTournament(TournamentId));
        if (tournament is null)
        {
            return false;
        }

        Tournament = tournament;
        Scoreboard = await sender.Send(new GetScoreboard(TournamentId));
        Matches = await sender.Send(new GetMatches(TournamentId, PlannedLimit: 5, PlayedLimit: 5));

        return true;
    }
}
