using Idasletten.Data;
using Idasletten.Shared.Domain;
using Idasletten.Shared.Graph;
using Idasletten.Shared.Scoring;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared;

/// <summary>
/// Small reusable helpers for the recurring "add a player by initials, creating the user if
/// needed" flow shared by the match and player slices and the seeder.
/// </summary>
public static class Provisioning
{
    /// <summary>
    /// Finds a user by username (initials, case-insensitive) or creates one. New users get their
    /// photo fetched from Graph (no-op when Graph is unconfigured).
    /// </summary>
    public static async Task<User> GetOrCreateUserAsync(
        AppDbContext db, IUserImageService images, string initials, string? name = null, CancellationToken ct = default)
    {
        var username = initials.Trim();
        var normalized = username.ToUpperInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.NormalizedUserName == normalized, ct);
        if (user is not null) return user;

        user = new User
        {
            Id = Guid.NewGuid(),
            UserName = username,
            NormalizedUserName = normalized,
            Name = string.IsNullOrWhiteSpace(name) ? username : name.Trim(),
            ImageUrl = await images.GetImageUrlAsync(null, ct)
        };
        db.Users.Add(user);
        return user;
    }

    /// <summary>Adds a user to a tournament as a player (idempotent), seeding the initial score.</summary>
    public static async Task<TournamentPlayer> AddPlayerAsync(
        AppDbContext db, ScoreService scores, Tournament tournament, User user, CancellationToken ct = default)
    {
        var existing = await db.TournamentPlayers
            .FirstOrDefaultAsync(p => p.TournamentId == tournament.Id && p.UserId == user.Id, ct);
        if (existing is not null) return existing;

        var player = new TournamentPlayer
        {
            TournamentId = tournament.Id,
            UserId = user.Id,
            Score = scores.CalculatorFor(tournament.ScoreSystem).InitialScore,
            Lives = 3
        };
        db.TournamentPlayers.Add(player);
        return player;
    }
}
