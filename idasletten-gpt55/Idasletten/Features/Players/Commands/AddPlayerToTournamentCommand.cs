using Idasletten.Features.Tournaments.Events;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Commands;

public record AddPlayerToTournamentCommand(Guid TournamentId, string Initials, string? Name = null) : IRequest<Guid>;

public class AddPlayerToTournamentHandler(IdaslettenDbContext db, IPublisher publisher) : IRequestHandler<AddPlayerToTournamentCommand, Guid>
{
    public async Task<Guid> Handle(AddPlayerToTournamentCommand request, CancellationToken cancellationToken)
    {
        var initials = Normalize(request.Initials);
        if (string.IsNullOrWhiteSpace(initials)) throw new InvalidOperationException("Initials are required.");
        var tournament = await db.Tournaments.Include(t => t.Players).SingleAsync(t => t.Id == request.TournamentId, cancellationToken);
        if (tournament.MaxPlayerCount.HasValue && tournament.Players.Count >= tournament.MaxPlayerCount.Value) throw new InvalidOperationException("The tournament is full.");

        var user = await db.Users.SingleOrDefaultAsync(u => u.NormalizedUserName == initials, cancellationToken);
        if (user is null)
        {
            user = new AppUser { UserName = initials, NormalizedUserName = initials, Name = string.IsNullOrWhiteSpace(request.Name) ? initials : request.Name.Trim() };
            db.Users.Add(user);
        }

        var player = await db.TournamentPlayers.SingleOrDefaultAsync(p => p.TournamentId == request.TournamentId && p.UserId == user.Id, cancellationToken);
        if (player is null)
        {
            player = new TournamentPlayer { TournamentId = request.TournamentId, User = user, Lives = tournament.ScoreSystem == ScoreSystem.Lives ? 3 : 0, Score = tournament.ScoreSystem is ScoreSystem.Elo or ScoreSystem.TrueSkill ? 1000 : 0 };
            db.TournamentPlayers.Add(player);
        }

        await db.SaveChangesAsync(cancellationToken);
        await publisher.Publish(new TournamentPlayerAdded(request.TournamentId, user.Id), cancellationToken);
        return player.Id;
    }

    public static string Normalize(string initials) => new string(initials.Trim().Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
}
