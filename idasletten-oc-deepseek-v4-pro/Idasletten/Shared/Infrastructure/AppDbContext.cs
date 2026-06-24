using Idasletten.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared.Infrastructure;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentPlayer> TournamentPlayers => Set<TournamentPlayer>();
    public DbSet<TournamentTeam> TournamentTeams => Set<TournamentTeam>();
    public DbSet<TournamentTeamPlayer> TournamentTeamPlayers => Set<TournamentTeamPlayer>();
    public DbSet<TournamentMatch> TournamentMatches => Set<TournamentMatch>();
    public DbSet<TournamentTeamMatchResult> TournamentTeamMatchResults => Set<TournamentTeamMatchResult>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Username).HasMaxLength(20);
            e.Property(u => u.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<Tournament>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).HasMaxLength(200);
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
            e.Property(tt => tt.Name).HasMaxLength(100);
            e.HasOne(tt => tt.Tournament)
                .WithMany(t => t.Teams)
                .HasForeignKey(tt => tt.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TournamentTeamPlayer>(e =>
        {
            e.HasKey(ttp => new { ttp.TournamentTeamId, ttp.TournamentPlayerId });
            e.HasOne(ttp => ttp.Team)
                .WithMany(tt => tt.TeamPlayers)
                .HasForeignKey(ttp => ttp.TournamentTeamId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ttp => ttp.Player)
                .WithMany(tp => tp.TeamPlayers)
                .HasForeignKey(ttp => ttp.TournamentPlayerId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<TournamentMatch>(e =>
        {
            e.HasKey(tm => tm.Id);
            e.HasOne(tm => tm.Tournament)
                .WithMany(t => t.Matches)
                .HasForeignKey(tm => tm.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TournamentTeamMatchResult>(e =>
        {
            e.HasKey(ttmr => ttmr.Id);
            e.HasOne(ttmr => ttmr.Match)
                .WithMany(tm => tm.TeamResults)
                .HasForeignKey(ttmr => ttmr.MatchId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ttmr => ttmr.Team)
                .WithMany(tt => tt.MatchResults)
                .HasForeignKey(ttmr => ttmr.TeamId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
