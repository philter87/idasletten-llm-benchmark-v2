using Idasletten.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Tournament> Tournaments { get; set; }
    public DbSet<TournamentPlayer> TournamentPlayers { get; set; }
    public DbSet<TournamentTeam> TournamentTeams { get; set; }
    public DbSet<TournamentTeamPlayer> TournamentTeamPlayers { get; set; }
    public DbSet<TournamentMatch> TournamentMatches { get; set; }
    public DbSet<TournamentTeamMatchResult> TournamentTeamMatchResults { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Username).IsRequired().HasMaxLength(20);
            e.Property(u => u.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<Tournament>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).IsRequired().HasMaxLength(200);
            e.HasOne(t => t.SeedTournament)
                .WithMany()
                .HasForeignKey(t => t.SeedTournamentId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(t => t.ParentTournament)
                .WithMany()
                .HasForeignKey(t => t.ParentTournamentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TournamentPlayer>(e =>
        {
            e.HasKey(tp => tp.Id);
            e.HasIndex(tp => new { tp.UserId, tp.TournamentId }).IsUnique();
            e.HasOne(tp => tp.User)
                .WithMany(u => u.TournamentPlayers)
                .HasForeignKey(tp => tp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(tp => tp.Tournament)
                .WithMany(t => t.Players)
                .HasForeignKey(tp => tp.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TournamentTeam>(e =>
        {
            e.HasKey(tt => tt.Id);
            e.Property(tt => tt.Name).IsRequired().HasMaxLength(100);
            e.HasOne(tt => tt.Tournament)
                .WithMany(t => t.Teams)
                .HasForeignKey(tt => tt.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TournamentTeamPlayer>(e =>
        {
            e.HasKey(ttp => new { ttp.TournamentTeamId, ttp.TournamentPlayerId });
            e.HasOne(ttp => ttp.Team)
                .WithMany(t => t.TeamPlayers)
                .HasForeignKey(ttp => ttp.TournamentTeamId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ttp => ttp.Player)
                .WithMany(p => p.TeamPlayers)
                .HasForeignKey(ttp => ttp.TournamentPlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TournamentMatch>(e =>
        {
            e.HasKey(m => m.Id);
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
