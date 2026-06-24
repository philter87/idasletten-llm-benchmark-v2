using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries;

public record GetTournamentByIdQuery(Guid Id) : IRequest<Tournament?>;

public class GetTournamentByIdHandler : IRequestHandler<GetTournamentByIdQuery, Tournament?>
{
    private readonly AppDbContext _db;

    public GetTournamentByIdHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Tournament?> Handle(GetTournamentByIdQuery request, CancellationToken cancellationToken)
    {
        return await _db.Tournaments
            .Include(t => t.Players)
                .ThenInclude(tp => tp.User)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
    }
}
