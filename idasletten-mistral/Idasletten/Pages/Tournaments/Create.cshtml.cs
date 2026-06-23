using Idasletten.Features.Tournaments;
using Idasletten.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IMediator _mediator;

    public CreateModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    [BindProperty]
    public CreateTournamentCommand Command { get; set; } = new();

    public ICollection<Tournament> AvailableTournaments { get; set; } = new List<Tournament>();
    public string ErrorMessage { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        AvailableTournaments = await _mediator.Send(new GetTournamentsForSeedingQuery());
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var tournamentId = await _mediator.Send(Command);
            return RedirectToPage("/Tournaments/Detail", new { tournamentId });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            AvailableTournaments = await _mediator.Send(new GetTournamentsForSeedingQuery());
            return Page();
        }
    }
}

public class GetTournamentsForSeedingQuery : IRequest<ICollection<Tournament>>
{
}

public class GetTournamentsForSeedingHandler : IRequestHandler<GetTournamentsForSeedingQuery, ICollection<Tournament>>
{
    private readonly AppDbContext _context;

    public GetTournamentsForSeedingHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<Tournament>> Handle(GetTournamentsForSeedingQuery request, CancellationToken cancellationToken)
    {
        return await _context.Tournaments
            .Where(t => !t.ParentTournamentId.HasValue && !t.IsArchived) // Can only seed from tournaments without parents
            .Include(t => t.Players)
            .OrderByDescending(t => t.Name)
            .ToListAsync(cancellationToken);
    }
}
