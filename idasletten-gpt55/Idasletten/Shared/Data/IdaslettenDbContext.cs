using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared.Data;

public class IdaslettenDbContext(DbContextOptions<IdaslettenDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentPlayer> TournamentPlayers => Set<TournamentPlayer>();
    public DbSet<TournamentTeam> TournamentTeams => Set<TournamentTeam>();
    public DbSet<TournamentTeamPlayer> TournamentTeamPlayers => Set<TournamentTeamPlayer>();
    public DbSet<TournamentMatch> TournamentMatches => Set<TournamentMatch>();
    public DbSet<TournamentTeamMatchResult> TournamentTeamMatchResults => Set<TournamentTeamMatchResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>().HasIndex(user => user.NormalizedUserName).IsUnique();
        modelBuilder.Entity<Tournament>().HasOne(tournament => tournament.SeedTournament).WithMany().HasForeignKey(tournament => tournament.SeedTournamentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Tournament>().HasOne(tournament => tournament.ParentTournament).WithMany().HasForeignKey(tournament => tournament.ParentTournamentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TournamentPlayer>().HasIndex(player => new { player.TournamentId, player.UserId }).IsUnique();
        modelBuilder.Entity<TournamentPlayer>().HasOne(player => player.Tournament).WithMany(tournament => tournament.Players).HasForeignKey(player => player.TournamentId);
        modelBuilder.Entity<TournamentPlayer>().HasOne(player => player.User).WithMany().HasForeignKey(player => player.UserId);
        modelBuilder.Entity<TournamentTeam>().HasOne(team => team.Match).WithMany(match => match.Teams).HasForeignKey(team => team.MatchId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TournamentTeamPlayer>().HasKey(teamPlayer => new { teamPlayer.TournamentTeamId, teamPlayer.TournamentPlayerId });
        modelBuilder.Entity<TournamentTeamPlayer>().HasOne(teamPlayer => teamPlayer.Team).WithMany(team => team.Players).HasForeignKey(teamPlayer => teamPlayer.TournamentTeamId);
        modelBuilder.Entity<TournamentTeamPlayer>().HasOne(teamPlayer => teamPlayer.Player).WithMany(player => player.Teams).HasForeignKey(teamPlayer => teamPlayer.TournamentPlayerId);
        modelBuilder.Entity<TournamentTeamMatchResult>().HasOne(result => result.Match).WithMany(match => match.Results).HasForeignKey(result => result.MatchId);
        modelBuilder.Entity<TournamentTeamMatchResult>().HasOne(result => result.Team).WithOne(team => team.Result).HasForeignKey<TournamentTeamMatchResult>(result => result.TeamId).OnDelete(DeleteBehavior.Cascade);
    }
}
