using Idasletten.Features.Tournaments;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class DetailsModel(ISender sender) : PageModel
{
    public TournamentDetail? Tournament { get; private set; }
    [BindProperty] public string Initials { get; set; } = "";
    [BindProperty] public string? PlayerName { get; set; }
    public async Task<IActionResult> OnGetAsync(Guid tournamentId)
    {
        Tournament = await sender.Send(new GetTournamentQuery(tournamentId));
        return Tournament is null ? NotFound() : Page();
    }
    public async Task<IActionResult> OnPostAddPlayerAsync(Guid tournamentId)
    {
        if (string.IsNullOrWhiteSpace(Initials))
            return RedirectToPage(new { tournamentId });
        await sender.Send(new AddPlayerCommand(tournamentId, Initials, PlayerName));
        return RedirectToPage(new { tournamentId });
    }
}
