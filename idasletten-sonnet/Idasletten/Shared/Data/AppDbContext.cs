using Idasletten.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentPlayer> TournamentPlayers => Set<TournamentPlayer>();
    public DbSet<TournamentTeam> TournamentTeams => Set<TournamentTeam>();
    public DbSet<TournamentMatch> TournamentMatches => Set<TournamentMatch>();
    public DbSet<TournamentTeamMatchResult> TournamentTeamMatchResults => Set<TournamentTeamMatchResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
        });

        modelBuilder.Entity<Tournament>(e =>
        {
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
            e.HasOne(tp => tp.User)
             .WithMany(u => u.TournamentPlayers)
             .HasForeignKey(tp => tp.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(tp => tp.Tournament)
             .WithMany(t => t.Players)
             .HasForeignKey(tp => tp.TournamentId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(tp => new { tp.UserId, tp.TournamentId }).IsUnique();
        });

        modelBuilder.Entity<TournamentTeam>(e =>
        {
            e.HasOne(tt => tt.Tournament)
             .WithMany()
             .HasForeignKey(tt => tt.TournamentId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(tt => tt.Players)
             .WithMany()
             .UsingEntity("TournamentTeamPlayers");
        });

        modelBuilder.Entity<TournamentMatch>(e =>
        {
            e.HasOne(m => m.Tournament)
             .WithMany(t => t.Matches)
             .HasForeignKey(m => m.TournamentId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TournamentTeamMatchResult>(e =>
        {
            e.HasOne(r => r.Match)
             .WithMany(m => m.TeamResults)
             .HasForeignKey(r => r.MatchId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Team)
             .WithMany(t => t.MatchResults)
             .HasForeignKey(r => r.TeamId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
