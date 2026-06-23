using Idasletten.Shared.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Data;

public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentPlayer> TournamentPlayers => Set<TournamentPlayer>();
    public DbSet<TournamentTeam> TournamentTeams => Set<TournamentTeam>();
    public DbSet<TournamentMatch> TournamentMatches => Set<TournamentMatch>();
    public DbSet<TournamentTeamMatchResult> TournamentTeamMatchResults => Set<TournamentTeamMatchResult>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<User>(e =>
        {
            // Username is usually 3 initials and must be unique. Identity already
            // provides a normalized unique index on UserName.
            e.Property(u => u.Name).HasMaxLength(200);
        });

        b.Entity<Tournament>(e =>
        {
            e.Property(t => t.Name).IsRequired().HasMaxLength(200);
            e.Property(t => t.ScoreSystem).HasConversion<string>();
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
            e.HasOne(p => p.Tournament)
                .WithMany(t => t.Players)
                .HasForeignKey(p => p.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.User)
                .WithMany(u => u.TournamentPlayers)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            // A user appears at most once per tournament.
            e.HasIndex(p => new { p.TournamentId, p.UserId }).IsUnique();
        });

        b.Entity<TournamentTeam>(e =>
        {
            e.HasOne(t => t.Tournament)
                .WithMany(t => t.Teams)
                .HasForeignKey(t => t.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
            // Many-to-many: a team has many players, a player may be on many teams
            // (teams can reshuffle between matches).
            e.HasMany(t => t.Players)
                .WithMany();
        });

        b.Entity<TournamentMatch>(e =>
        {
            e.HasOne(m => m.Tournament)
                .WithMany(t => t.Matches)
                .HasForeignKey(m => m.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(m => m.State).HasConversion<string>();
        });

        b.Entity<TournamentTeamMatchResult>(e =>
        {
            e.HasOne(r => r.Match)
                .WithMany(m => m.Results)
                .HasForeignKey(r => r.MatchId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Team)
                .WithMany()
                .HasForeignKey(r => r.TeamId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
