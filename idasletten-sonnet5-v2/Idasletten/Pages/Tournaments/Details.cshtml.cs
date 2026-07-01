using Idasletten.Features.Players.Commands.AddPlayerToTournament;
using Idasletten.Features.Tournaments.Queries.GetTournamentDetail;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class DetailsModel(ISender sender) : PageModel
{
    public TournamentDetailResult Tournament { get; private set; } = null!;

    [BindProperty]
    public string AddPlayerUsername { get; set; } = string.Empty;

    [BindProperty]
    public string? AddPlayerName { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var result = await sender.Send(new GetTournamentDetailQuery(id));
        if (result is null)
        {
            return NotFound();
        }

        Tournament = result;
        return Page();
    }

    public async Task<IActionResult> OnPostAddPlayerAsync(Guid id)
    {
        if (!string.IsNullOrWhiteSpace(AddPlayerUsername))
        {
            try
            {
                await sender.Send(new AddPlayerToTournamentCommand(id, AddPlayerUsername.Trim(), AddPlayerName?.Trim()));
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
        }

        return RedirectToPage(new { id });
    }
}
