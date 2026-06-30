using Idasletten.Features.Matches;
using Idasletten.Features.TournamentPlayers;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared.Data;

public class IdaslettenDbContext(DbContextOptions<IdaslettenDbContext> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentPlayer> TournamentPlayers => Set<TournamentPlayer>();
    public DbSet<TournamentMatch> TournamentMatches => Set<TournamentMatch>();
    public DbSet<TournamentTeam> TournamentTeams => Set<TournamentTeam>();
    public DbSet<TournamentTeamPlayer> TournamentTeamPlayers => Set<TournamentTeamPlayer>();
    public DbSet<TournamentTeamMatchResult> TournamentTeamMatchResults => Set<TournamentTeamMatchResult>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Tournament>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).IsRequired();
        });

        builder.Entity<TournamentPlayer>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => new { p.TournamentId, p.UserId }).IsUnique();
        });

        builder.Entity<TournamentMatch>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasMany(m => m.Teams)
                .WithOne()
                .HasForeignKey(t => t.MatchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TournamentTeam>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasMany(t => t.Players)
                .WithOne()
                .HasForeignKey(tp => tp.TeamId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TournamentTeamPlayer>(e =>
        {
            e.HasKey(tp => new { tp.TeamId, tp.TournamentPlayerId });
        });

        builder.Entity<TournamentTeamMatchResult>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => new { r.MatchId, r.TeamId }).IsUnique();
        });
    }
}
