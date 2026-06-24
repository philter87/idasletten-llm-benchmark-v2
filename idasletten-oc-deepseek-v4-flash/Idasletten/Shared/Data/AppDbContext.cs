using Idasletten.Shared.Entities;
using Idasletten.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared.Data;

public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentPlayer> TournamentPlayers => Set<TournamentPlayer>();
    public DbSet<TournamentTeam> TournamentTeams => Set<TournamentTeam>();
    public DbSet<TournamentMatch> TournamentMatches => Set<TournamentMatch>();
    public DbSet<TournamentTeamMatchResult> TournamentTeamMatchResults => Set<TournamentTeamMatchResult>();
    public DbSet<TournamentTeamPlayer> TournamentTeamPlayers => Set<TournamentTeamPlayer>();
    public DbSet<TournamentMatchTeam> TournamentMatchTeams => Set<TournamentMatchTeam>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Tournament>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.SeedTournament)
                .WithMany()
                .HasForeignKey(e => e.SeedTournamentId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.ParentTournament)
                .WithMany()
                .HasForeignKey(e => e.ParentTournamentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<TournamentPlayer>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.TournamentId });
            entity.HasOne(e => e.User)
                .WithMany(u => u.TournamentPlayers)
                .HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Tournament)
                .WithMany(t => t.Players)
                .HasForeignKey(e => e.TournamentId);
        });

        builder.Entity<TournamentTeam>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Tournament)
                .WithMany(t => t.Teams)
                .HasForeignKey(e => e.TournamentId);
        });

        builder.Entity<TournamentMatch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Tournament)
                .WithMany(t => t.Matches)
                .HasForeignKey(e => e.TournamentId);
        });

        builder.Entity<TournamentTeamMatchResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Match)
                .WithMany(m => m.Results)
                .HasForeignKey(e => e.MatchId);
            entity.HasOne(e => e.Team)
                .WithMany(t => t.Results)
                .HasForeignKey(e => e.TeamId);
        });

        builder.Entity<TournamentTeamPlayer>(entity =>
        {
            entity.HasKey(e => new { e.TeamId, e.UserId, e.TournamentId });
            entity.HasOne(e => e.Team)
                .WithMany(t => t.PlayerEntries)
                .HasForeignKey(e => e.TeamId);
            entity.HasOne(e => e.Player)
                .WithMany(p => p.TeamEntries)
                .HasForeignKey(e => new { e.UserId, e.TournamentId });
        });

        builder.Entity<TournamentMatchTeam>(entity =>
        {
            entity.HasKey(e => new { e.MatchId, e.TeamId });
            entity.HasOne(e => e.Match)
                .WithMany(m => m.TeamEntries)
                .HasForeignKey(e => e.MatchId);
            entity.HasOne(e => e.Team)
                .WithMany(t => t.MatchEntries)
                .HasForeignKey(e => e.TeamId);
        });

        builder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Initials).IsUnique();
        });
    }
}
