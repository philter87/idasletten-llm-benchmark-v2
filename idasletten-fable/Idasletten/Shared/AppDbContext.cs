using Idasletten.Features.Matches;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Users;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared;

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
        modelBuilder.Entity<User>(b =>
        {
            b.HasIndex(u => u.NormalizedUserName).IsUnique();
        });

        modelBuilder.Entity<Tournament>(b =>
        {
            b.HasMany(t => t.Players).WithOne().HasForeignKey(p => p.TournamentId);
            b.HasMany(t => t.Teams).WithOne().HasForeignKey(t => t.TournamentId);
        });

        modelBuilder.Entity<TournamentPlayer>(b =>
        {
            b.HasIndex(p => new { p.TournamentId, p.UserId }).IsUnique();
            b.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId);
        });

        modelBuilder.Entity<TournamentTeam>(b =>
        {
            b.HasMany(t => t.Players).WithMany();
        });

        modelBuilder.Entity<TournamentMatch>(b =>
        {
            b.HasMany(m => m.Results).WithOne().HasForeignKey(r => r.MatchId);
            b.HasIndex(m => m.TournamentId);
        });

        modelBuilder.Entity<TournamentTeamMatchResult>(b =>
        {
            b.HasOne(r => r.Team).WithMany().HasForeignKey(r => r.TeamId);
        });
    }
}
