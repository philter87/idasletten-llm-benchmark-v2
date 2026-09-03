using Microsoft.EntityFrameworkCore;
using Idasletten.Models;

namespace Idasletten.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentPlayer> TournamentPlayers => Set<TournamentPlayer>();
    public DbSet<TournamentTeam> TournamentTeams => Set<TournamentTeam>();
    public DbSet<TeamPlayer> TeamPlayers => Set<TeamPlayer>();
    public DbSet<TournamentMatch> TournamentMatches => Set<TournamentMatch>();
    public DbSet<MatchTeam> MatchTeams => Set<MatchTeam>();
    public DbSet<TournamentTeamMatchResult> TournamentTeamMatchResults => Set<TournamentTeamMatchResult>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Tournament>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Name);
            e.HasOne(x => x.SeedTournament)
                .WithMany()
                .HasForeignKey(x => x.SeedTournamentId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ParentTournament)
                .WithMany(t => t.ChildTournaments)
                .HasForeignKey(x => x.ParentTournamentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<User>(e =>
        {
            e.Property(x => x.Username).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasMaxLength(320);
            e.Property(x => x.ImageUrl).HasMaxLength(2000);
        });

        b.Entity<TournamentPlayer>(e =>
        {
            e.HasIndex(x => new { x.TournamentId, x.UserId }).IsUnique();
            e.HasOne(x => x.User).WithMany(u => u.TournamentPlayers).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Tournament).WithMany(t => t.Players).HasForeignKey(x => x.TournamentId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Score).HasPrecision(18, 6);
            e.Property(x => x.ScoreDiff).HasPrecision(18, 6);
            e.Property(x => x.TrueSkillSigma).HasPrecision(18, 6);
        });

        b.Entity<TournamentTeam>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.Tournament).WithMany(t => t.Teams).HasForeignKey(x => x.TournamentId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<TeamPlayer>(e =>
        {
            e.HasKey(x => new { x.TeamId, x.TournamentPlayerId });
            e.HasOne(x => x.Team).WithMany(t => t.Players).HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Player).WithMany().HasForeignKey(x => x.TournamentPlayerId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<TournamentMatch>(e =>
        {
            e.HasIndex(x => new { x.TournamentId, x.Order }).IsUnique();
            e.HasOne(x => x.Tournament).WithMany(t => t.Matches).HasForeignKey(x => x.TournamentId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<MatchTeam>(e =>
        {
            e.HasKey(x => new { x.MatchId, x.TeamId });
            e.HasOne(x => x.Match).WithMany(m => m.TeamSlots).HasForeignKey(x => x.MatchId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Team).WithMany().HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<TournamentTeamMatchResult>(e =>
        {
            e.HasIndex(x => new { x.MatchId, x.TeamId }).IsUnique();
            e.HasOne(x => x.Match).WithMany(m => m.Results).HasForeignKey(x => x.MatchId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Team).WithMany(t => t.Results).HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.TournamentId);
        });
    }
}
