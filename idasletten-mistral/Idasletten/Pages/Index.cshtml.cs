using Idasletten.Features.Tournaments;
using Idasletten.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages;

public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public IndexModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public ICollection<Tournament> PublicTournaments { get; set; } = new List<Tournament>();

    public async Task OnGetAsync()
    {
        PublicTournaments = await _mediator.Send(new GetPublicTournamentsQuery());
    }
}

public class GetPublicTournamentsQuery : IRequest<ICollection<Tournament>>
{
}

public class GetPublicTournamentsHandler : IRequestHandler<GetPublicTournamentsQuery, ICollection<Tournament>>
{
    private readonly AppDbContext _context;

    public GetPublicTournamentsHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<Tournament>> Handle(GetPublicTournamentsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Tournaments
            .Where(t => t.IsPublic && !t.IsArchived)
            .Include(t => t.Players)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }
}
