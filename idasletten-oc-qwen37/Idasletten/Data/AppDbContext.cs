using Idasletten.Models;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentPlayer> TournamentPlayers => Set<TournamentPlayer>();
    public DbSet<TournamentTeam> TournamentTeams => Set<TournamentTeam>();
    public DbSet<TournamentMatch> TournamentMatches => Set<TournamentMatch>();
    public DbSet<TournamentTeamMatchResult> TournamentTeamMatchResults => Set<TournamentTeamMatchResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
        });

        modelBuilder.Entity<Tournament>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.SeedTournament)
                .WithMany()
                .HasForeignKey(e => e.SeedTournamentId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.ParentTournament)
                .WithMany(e => e.ChildTournaments)
                .HasForeignKey(e => e.ParentTournamentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TournamentPlayer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany(u => u.TournamentPlayers)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Tournament)
                .WithMany(t => t.Players)
                .HasForeignKey(e => e.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.UserId, e.TournamentId }).IsUnique();
        });

        modelBuilder.Entity<TournamentTeam>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Tournament)
                .WithMany(t => t.Teams)
                .HasForeignKey(e => e.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Players)
                .WithMany(p => p.Teams);
        });

        modelBuilder.Entity<TournamentMatch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Tournament)
                .WithMany(t => t.Matches)
                .HasForeignKey(e => e.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TournamentTeamMatchResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Match)
                .WithMany(m => m.TeamResults)
                .HasForeignKey(e => e.MatchId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Tournament)
                .WithMany()
                .HasForeignKey(e => e.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Team)
                .WithMany(t => t.MatchResults)
                .HasForeignKey(e => e.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
