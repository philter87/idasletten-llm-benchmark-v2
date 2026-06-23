using Idasletten.Features.Matches.Entities;
using Idasletten.Features.Tournaments.Entities;
using Idasletten.Features.Users.Entities;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentPlayer> TournamentPlayers => Set<TournamentPlayer>();
    public DbSet<TournamentTeam> TournamentTeams => Set<TournamentTeam>();
    public DbSet<TournamentTeamPlayer> TournamentTeamPlayers => Set<TournamentTeamPlayer>();
    public DbSet<TournamentMatch> TournamentMatches => Set<TournamentMatch>();
    public DbSet<TournamentTeamMatchResult> TournamentTeamMatchResults => Set<TournamentTeamMatchResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Username).IsRequired().HasMaxLength(50);
            e.Property(u => u.Name).IsRequired().HasMaxLength(200);
            e.Property(u => u.Email).HasMaxLength(200);
            e.Property(u => u.ImageUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<Tournament>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).IsRequired().HasMaxLength(200);
            e.Property(t => t.ScoreSystem).HasConversion<string>();

            e.HasOne(t => t.SeedTournament)
                .WithMany()
                .HasForeignKey(t => t.SeedTournamentId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(t => t.ParentTournament)
                .WithMany(t => t.ChildTournaments)
                .HasForeignKey(t => t.ParentTournamentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TournamentPlayer>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => new { p.TournamentId, p.UserId }).IsUnique();

            e.HasOne(p => p.User)
                .WithMany(u => u.TournamentPlayers)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.Tournament)
                .WithMany(t => t.Players)
                .HasForeignKey(p => p.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TournamentTeam>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).IsRequired().HasMaxLength(100);

            e.HasOne(t => t.Tournament)
                .WithMany(t => t.Teams)
                .HasForeignKey(t => t.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TournamentTeamPlayer>(e =>
        {
            e.HasKey(tp => tp.Id);
            e.HasIndex(tp => new { tp.TournamentTeamId, tp.TournamentPlayerId }).IsUnique();

            e.HasOne(tp => tp.Team)
                .WithMany(t => t.TeamPlayers)
                .HasForeignKey(tp => tp.TournamentTeamId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(tp => tp.Player)
                .WithMany(p => p.TeamPlayers)
                .HasForeignKey(tp => tp.TournamentPlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TournamentMatch>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.State).HasConversion<string>();

            e.HasOne(m => m.Tournament)
                .WithMany(t => t.Matches)
                .HasForeignKey(m => m.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TournamentTeamMatchResult>(e =>
        {
            e.HasKey(r => r.Id);

            e.HasOne(r => r.Match)
                .WithMany(m => m.TeamResults)
                .HasForeignKey(r => r.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Team)
                .WithMany(t => t.MatchResults)
                .HasForeignKey(r => r.TeamId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
