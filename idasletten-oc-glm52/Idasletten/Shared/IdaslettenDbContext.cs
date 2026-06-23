using Idasletten.Features.Matches;
using Idasletten.Features.Players;
using Idasletten.Features.Teams;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Users;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared;

public class IdaslettenDbContext : DbContext
{
    public IdaslettenDbContext(DbContextOptions<IdaslettenDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentPlayer> TournamentPlayers => Set<TournamentPlayer>();
    public DbSet<TournamentTeam> TournamentTeams => Set<TournamentTeam>();
    public DbSet<TournamentMatch> TournamentMatches => Set<TournamentMatch>();
public DbSet<TournamentTeamMatchResult> TournamentTeamMatchResults => Set<TournamentTeamMatchResult>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Username).IsRequired();
            e.Property(u => u.Name).IsRequired();
        });

        b.Entity<Tournament>(e =>
        {
            e.HasOne(t => t.SeedTournament)
                .WithMany()
                .HasForeignKey(t => t.SeedTournamentId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.ParentTournament)
                .WithMany()
                .HasForeignKey(t => t.ParentTournamentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<TournamentPlayer>(e =>
        {
            e.HasOne(p => p.User).WithMany(u => u.TournamentPlayers).HasForeignKey(p => p.UserId);
            e.HasOne(p => p.Tournament).WithMany(t => t.Players).HasForeignKey(p => p.TournamentId);
            e.HasIndex(p => new { p.UserId, p.TournamentId }).IsUnique();
            e.HasMany(p => p.Teams).WithMany(t => t.Players)
                .UsingEntity<Dictionary<string, object>>(
                    "TournamentTeamPlayer",
                    j => j.HasOne<TournamentTeam>().WithMany().HasForeignKey("TeamsId"),
                    j => j.HasOne<TournamentPlayer>().WithMany().HasForeignKey("PlayersId"));
        });

        b.Entity<TournamentTeam>(e =>
        {
            e.HasOne(t => t.Tournament).WithMany(t => t.Teams).HasForeignKey(t => t.TournamentId);
        });

        b.Entity<TournamentMatch>(e =>
        {
            e.HasOne(m => m.Tournament).WithMany(t => t.Matches).HasForeignKey(m => m.TournamentId);
        });

        b.Entity<TournamentTeamMatchResult>(e =>
        {
            e.HasOne(r => r.Match).WithMany().HasForeignKey(r => r.MatchId);
            e.HasOne(r => r.Team).WithMany(t => t.Results).HasForeignKey(r => r.TeamId);
        });
    }
}