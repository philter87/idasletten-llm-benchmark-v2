using Idasletten.Features.Players;
using Idasletten.Features.Players.Commands;
using Idasletten.Features.Tournaments;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches;

/// <summary>
/// Shared by the match commands: turns initials into tournament players and player sets into teams.
/// Teams are reused when the exact same players play together again, so "Team 3" keeps its identity
/// through a whole tournament.
/// </summary>
internal static class MatchTeams
{
    /// <summary>Resolves initials to tournament players, creating users and players when needed.</summary>
    public static async Task<List<Guid>> ResolvePlayerIdsAsync(
        AppDbContext db,
        ISender sender,
        Tournament tournament,
        IEnumerable<string> initials,
        CancellationToken cancellationToken)
    {
        var playerIds = new List<Guid>();

        foreach (var raw in initials)
        {
            var trimmed = (raw ?? string.Empty).Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            var playerId = await sender.Send(
                new AddPlayerToTournament(tournament.Id, trimmed), cancellationToken);

            if (!playerIds.Contains(playerId))
            {
                playerIds.Add(playerId);
            }
        }

        return playerIds;
    }

    /// <summary>Finds the team with exactly these players, or creates the next numbered team.</summary>
    public static async Task<TournamentTeam> GetOrCreateTeamAsync(
        AppDbContext db,
        Tournament tournament,
        IReadOnlyList<Guid> playerIds,
        CancellationToken cancellationToken)
    {
        if (playerIds.Count == 0)
        {
            throw new ArgumentException("A team needs at least one player.", nameof(playerIds));
        }

        var teams = await db.TournamentTeams
            .Include(t => t.Players)
            .Where(t => t.TournamentId == tournament.Id)
            .ToListAsync(cancellationToken);

        var wanted = playerIds.ToHashSet();
        var existing = teams.FirstOrDefault(t =>
            t.Players.Count == wanted.Count && t.Players.All(p => wanted.Contains(p.TournamentPlayerId)));

        if (existing is not null)
        {
            return existing;
        }

        var number = teams.Count == 0 ? 1 : teams.Max(t => t.Number) + 1;
        var team = new TournamentTeam
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            Number = number,
            Name = $"Team {number}",
            Players = playerIds.Select(id => new TournamentTeamPlayer { TournamentPlayerId = id }).ToList(),
        };

        db.TournamentTeams.Add(team);
        await db.SaveChangesAsync(cancellationToken);

        return team;
    }
}
