using Idasletten.Features.Tournaments;
using Idasletten.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public IndexModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public ICollection<Tournament> Tournaments { get; set; } = new List<Tournament>();
    public string SearchTerm { get; set; } = string.Empty;

    public async Task OnGetAsync(string? search)
    {
        SearchTerm = search ?? string.Empty;
        Tournaments = await _mediator.Send(new GetAllTournamentsQuery(SearchTerm));
    }
}

public class GetAllTournamentsQuery : IRequest<ICollection<Tournament>>
{
    public string SearchTerm { get; }
    
    public GetAllTournamentsQuery(string searchTerm)
    {
        SearchTerm = searchTerm;
    }
}

public class GetAllTournamentsHandler : IRequestHandler<GetAllTournamentsQuery, ICollection<Tournament>>
{
    private readonly AppDbContext _context;
    private readonly IPublisher _publisher;

    public GetAllTournamentsHandler(AppDbContext context, IPublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    public async Task<ICollection<Tournament>> Handle(GetAllTournamentsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Tournaments
            .Include(t => t.Players)
            .OrderByDescending(t => t.IsArchived)
            .ThenBy(t => t.Name);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(t => t.Name.Contains(request.SearchTerm));
        }

        var result = await query.ToListAsync(cancellationToken);
        
        // Publish event for analytics/tracking
        await _publisher.Publish(new TournamentsListed(request.SearchTerm), cancellationToken);
        
        return result;
    }
}

public record TournamentsListed(string SearchTerm) : INotification;
