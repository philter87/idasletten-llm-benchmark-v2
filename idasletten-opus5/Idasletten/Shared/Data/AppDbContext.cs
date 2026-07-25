using Idasletten.Features.Matches;
using Idasletten.Features.Players;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentPlayer> TournamentPlayers => Set<TournamentPlayer>();
    public DbSet<TournamentTeam> TournamentTeams => Set<TournamentTeam>();
    public DbSet<TournamentTeamPlayer> TournamentTeamPlayers => Set<TournamentTeamPlayer>();
    public DbSet<TournamentMatch> TournamentMatches => Set<TournamentMatch>();
    public DbSet<TournamentTeamMatchResult> TournamentTeamMatchResults => Set<TournamentTeamMatchResult>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(user =>
        {
            user.Property(u => u.Name).HasMaxLength(200);
            user.Property(u => u.ImageUrl);
        });

        builder.Entity<Tournament>(tournament =>
        {
            tournament.Property(t => t.Name).HasMaxLength(200).IsRequired();

            tournament.HasOne(t => t.SeedTournament)
                .WithMany()
                .HasForeignKey(t => t.SeedTournamentId)
                .OnDelete(DeleteBehavior.Restrict);

            tournament.HasOne(t => t.ParentTournament)
                .WithMany(t => t.Rounds)
                .HasForeignKey(t => t.ParentTournamentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TournamentPlayer>(player =>
        {
            player.HasOne(p => p.Tournament)
                .WithMany(t => t.Players)
                .HasForeignKey(p => p.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            player.HasOne(p => p.User)
                .WithMany(u => u.TournamentPlayers)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // A user can only join a tournament once.
            player.HasIndex(p => new { p.TournamentId, p.UserId }).IsUnique();
        });

        builder.Entity<TournamentTeam>(team =>
        {
            team.Property(t => t.Name).HasMaxLength(100).IsRequired();

            team.HasOne(t => t.Tournament)
                .WithMany(t => t.Teams)
                .HasForeignKey(t => t.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            team.HasIndex(t => new { t.TournamentId, t.Number }).IsUnique();
        });

        builder.Entity<TournamentTeamPlayer>(teamPlayer =>
        {
            teamPlayer.HasKey(tp => new { tp.TeamId, tp.TournamentPlayerId });

            teamPlayer.HasOne(tp => tp.Team)
                .WithMany(t => t.Players)
                .HasForeignKey(tp => tp.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            teamPlayer.HasOne(tp => tp.TournamentPlayer)
                .WithMany(p => p.TeamMemberships)
                .HasForeignKey(tp => tp.TournamentPlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TournamentMatch>(match =>
        {
            match.HasOne(m => m.Tournament)
                .WithMany(t => t.Matches)
                .HasForeignKey(m => m.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            match.HasIndex(m => new { m.TournamentId, m.Order });
        });

        builder.Entity<TournamentTeamMatchResult>(result =>
        {
            result.HasOne(r => r.Match)
                .WithMany(m => m.Results)
                .HasForeignKey(r => r.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            result.HasOne(r => r.Team)
                .WithMany()
                .HasForeignKey(r => r.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            result.HasOne(r => r.Tournament)
                .WithMany()
                .HasForeignKey(r => r.TournamentId)
                .OnDelete(DeleteBehavior.Restrict);

            result.HasIndex(r => new { r.MatchId, r.TeamId }).IsUnique();
        });
    }
}
