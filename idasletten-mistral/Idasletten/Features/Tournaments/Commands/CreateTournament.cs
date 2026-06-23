using Idasletten.Shared;
using MediatR;

namespace Idasletten.Features.Tournaments.Commands;

public class CreateTournamentCommand : IRequest<Guid>
{
    public string Name { get; set; } = string.Empty;
    public int TeamSize { get; set; } = 2;
    public int PointsToWin { get; set; } = 5;
    public ScoreSystem ScoreSystem { get; set; } = ScoreSystem.Elo;
    public int? MaxPlayerCount { get; set; }
    public bool IsPublic { get; set; } = true;
    public Guid? SeedTournamentId { get; set; }
}

public class CreateTournamentHandler : IRequestHandler<CreateTournamentCommand, Guid>
{
    private readonly AppDbContext _context;
    private readonly IPublisher _publisher;

    public CreateTournamentHandler(AppDbContext context, IPublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(CreateTournamentCommand request, CancellationToken cancellationToken)
    {
        // Validate name is not empty
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Turneringsnavn er påkrævet");
        }

        // Check if tournament name already exists
        if (await _context.Tournaments.AnyAsync(t => t.Name == request.Name, cancellationToken))
        {
            throw new ArgumentException("En turnering med dette navn findes allerede");
        }

        // Validate seed tournament if provided
        if (request.SeedTournamentId.HasValue)
        {
            var seedTournament = await _context.Tournaments
                .FindAsync(request.SeedTournamentId.Value);
            
            if (seedTournament == null)
            {
                throw new ArgumentException("Seed turnering findes ikke");
            }

            // Cannot seed from a tournament that has a parent
            if (seedTournament.ParentTournamentId.HasValue)
            {
                throw new ArgumentException("Kan ikke seede fra en turnering som allerede har en forælder");
            }
        }

        // Create the tournament
        var tournament = new Tournament
        {
            Name = request.Name,
            TeamSize = request.TeamSize,
            PointsToWin = request.PointsToWin,
            ScoreSystem = request.ScoreSystem,
            MaxPlayerCount = request.MaxPlayerCount,
            IsPublic = request.IsPublic,
            SeedTournamentId = request.SeedTournamentId,
            IsArchived = false
        };

        _context.Tournaments.Add(tournament);
        await _context.SaveChangesAsync(cancellationToken);

        // Publish event
        await _publisher.Publish(new TournamentCreated(tournament.Id), cancellationToken);

        return tournament.Id;
    }
}

public record TournamentCreated(Guid TournamentId) : INotification;
