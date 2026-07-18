using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared;

public class IdaslettenDbContext(DbContextOptions<IdaslettenDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentPlayer> TournamentPlayers => Set<TournamentPlayer>();
    public DbSet<TournamentTeam> TournamentTeams => Set<TournamentTeam>();
    public DbSet<TournamentMatch> TournamentMatches => Set<TournamentMatch>();
    public DbSet<TournamentTeamMatchResult> TournamentTeamMatchResults => Set<TournamentTeamMatchResult>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<User>().HasIndex(x => x.Username).IsUnique();
        builder.Entity<TournamentPlayer>().HasIndex(x => new { x.UserId, x.TournamentId }).IsUnique();
        builder.Entity<TournamentTeam>().HasIndex(x => new { x.TournamentId, x.Number }).IsUnique();
        builder.Entity<TournamentTeamPlayer>().HasKey(x => new { x.TeamId, x.TournamentPlayerId });
        builder.Entity<TournamentMatchTeam>().HasKey(x => new { x.MatchId, x.TeamId });
        builder.Entity<TournamentMatchTeam>()
            .HasOne(x => x.Match).WithMany(x => x.Teams).HasForeignKey(x => x.MatchId);
        builder.Entity<TournamentMatchTeam>()
            .HasOne(x => x.Team).WithMany().HasForeignKey(x => x.TeamId);
        builder.Entity<TournamentTeamMatchResult>()
            .HasIndex(x => new { x.MatchId, x.TeamId }).IsUnique();
        builder.Entity<Tournament>().Property(x => x.ScoreSystem).HasConversion<string>();
        builder.Entity<TournamentMatch>().Property(x => x.State).HasConversion<string>();
    }
}
