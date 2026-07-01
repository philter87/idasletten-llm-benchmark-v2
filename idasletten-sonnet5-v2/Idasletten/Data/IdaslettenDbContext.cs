using Idasletten.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Data;

public class IdaslettenDbContext(DbContextOptions<IdaslettenDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentPlayer> TournamentPlayers => Set<TournamentPlayer>();
    public DbSet<TournamentMatch> TournamentMatches => Set<TournamentMatch>();
    public DbSet<TournamentTeam> TournamentTeams => Set<TournamentTeam>();
    public DbSet<TournamentTeamPlayer> TournamentTeamPlayers => Set<TournamentTeamPlayer>();
    public DbSet<TournamentTeamMatchResult> TournamentTeamMatchResults => Set<TournamentTeamMatchResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.NormalizedUserName).IsUnique();
            entity.Property(u => u.UserName).HasMaxLength(16);
        });

        modelBuilder.Entity<Tournament>(entity =>
        {
            entity.Property(t => t.ScoreSystem).HasConversion<string>();
        });

        modelBuilder.Entity<TournamentPlayer>(entity =>
        {
            entity.HasOne(p => p.User)
                .WithMany(u => u.TournamentPlayers)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Tournament)
                .WithMany(t => t.Players)
                .HasForeignKey(p => p.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(p => new { p.TournamentId, p.UserId }).IsUnique();
        });

        modelBuilder.Entity<TournamentMatch>(entity =>
        {
            entity.Property(m => m.State).HasConversion<string>();

            entity.HasOne(m => m.Tournament)
                .WithMany(t => t.Matches)
                .HasForeignKey(m => m.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TournamentTeam>(entity =>
        {
            entity.HasOne(t => t.Match)
                .WithMany(m => m.Teams)
                .HasForeignKey(t => t.MatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TournamentTeamPlayer>(entity =>
        {
            entity.HasKey(tp => new { tp.TeamId, tp.TournamentPlayerId });

            entity.HasOne(tp => tp.Team)
                .WithMany(t => t.TeamPlayers)
                .HasForeignKey(tp => tp.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(tp => tp.TournamentPlayer)
                .WithMany()
                .HasForeignKey(tp => tp.TournamentPlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TournamentTeamMatchResult>(entity =>
        {
            entity.HasOne(r => r.Match)
                .WithMany(m => m.Results)
                .HasForeignKey(r => r.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Team)
                .WithMany()
                .HasForeignKey(r => r.TeamId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
