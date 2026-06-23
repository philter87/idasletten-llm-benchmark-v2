using Idasletten.Features.Tournaments;
using Idasletten.Features.Users;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared;

public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Tournaments
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentPlayer> TournamentPlayers => Set<TournamentPlayer>();
    public DbSet<TournamentTeam> TournamentTeams => Set<TournamentTeam>();
    public DbSet<TournamentMatch> TournamentMatches => Set<TournamentMatch>();
    public DbSet<TournamentTeamMatchResult> TournamentTeamMatchResults => Set<TournamentTeamMatchResult>();

    // Users
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Identity to use our custom User class
        builder.Entity<User>(b =>
        {
            b.HasKey(u => u.Id);
            b.Property(u => u.Username).HasMaxLength(256);
            b.Property(u => u.NormalizedUserName).HasMaxLength(256);
            b.Property(u => u.Email).HasMaxLength(256);
            b.Property(u => u.NormalizedEmail).HasMaxLength(256);
            b.Property(u => u.Name).HasMaxLength(256);
            b.Property(u => u.ImageUrl).HasMaxLength(500);
        });

        // Tournament configuration
        builder.Entity<Tournament>(b =>
        {
            b.HasKey(t => t.Id);
            b.Property(t => t.Name).HasMaxLength(256).IsRequired();
            b.Property(t => t.TeamSize).HasDefaultValue(2);
            b.Property(t => t.PointsToWin).HasDefaultValue(5);
            b.Property(t => t.ScoreSystem).HasDefaultValue(ScoreSystem.Elo);
            b.Property(t => t.IsPublic).HasDefaultValue(true);
            b.Property(t => t.RoundNumber).HasDefaultValue(1);

            // Self-referencing for parent/child
            b.HasOne(t => t.ParentTournament)
                .WithMany(t => t.ChildTournaments)
                .HasForeignKey(t => t.ParentTournamentId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(t => t.SeedTournament)
                .WithMany()
                .HasForeignKey(t => t.SeedTournamentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // TournamentPlayer configuration
        builder.Entity<TournamentPlayer>(b =>
        {
            b.HasKey(tp => tp.Id);
            b.HasOne(tp => tp.User)
                .WithMany(u => u.TournamentPlayers)
                .HasForeignKey(tp => tp.UserId);

            b.HasOne(tp => tp.Tournament)
                .WithMany(t => t.Players)
                .HasForeignKey(tp => tp.TournamentId);

            b.Property(tp => tp.Score).HasDefaultValue(0);
            b.Property(tp => tp.WinCount).HasDefaultValue(0);
            b.Property(tp => tp.MatchCount).HasDefaultValue(0);
            b.Property(tp => tp.LoseCount).HasDefaultValue(0);
            b.Property(tp => tp.Lives).HasDefaultValue(3);
            b.Property(tp => tp.PointsWon).HasDefaultValue(0);
            b.Property(tp => tp.PointsLost).HasDefaultValue(0);
            b.Property(tp => tp.ScoreDiff).HasDefaultValue(0);
        });

        // TournamentTeam configuration
        builder.Entity<TournamentTeam>(b =>
        {
            b.HasKey(tt => tt.Id);
            b.Property(tt => tt.Name).HasMaxLength(256);
            b.HasOne(tt => tt.Tournament)
                .WithMany(t => t.Teams)
                .HasForeignKey(tt => tt.TournamentId);
        });

        // Many-to-many: TournamentTeam <-> TournamentPlayer
        builder.Entity<TournamentTeam>()
            .HasMany(tt => tt.Players)
            .WithMany(tp => tp.Teams)
            .UsingEntity<Dictionary<string, object>>(
                "TournamentTeamPlayer",
                j => j.HasOne<TournamentPlayer>().WithMany().HasForeignKey("PlayerId"),
                j => j.HasOne<TournamentTeam>().WithMany().HasForeignKey("TeamId")
            );

        // TournamentMatch configuration
        builder.Entity<TournamentMatch>(b =>
        {
            b.HasKey(tm => tm.Id);
            b.Property(tm => tm.Order);
            b.Property(tm => tm.State).HasDefaultValue(MatchState.Planned);
            b.HasOne(tm => tm.Tournament)
                .WithMany(t => t.Matches)
                .HasForeignKey(tm => tm.TournamentId);
        });

        // Many-to-many: TournamentMatch <-> TournamentTeam
        builder.Entity<TournamentMatch>()
            .HasMany(tm => tm.Teams)
            .WithMany(tt => tt.Matches)
            .UsingEntity<Dictionary<string, object>>(
                "TournamentMatchTeam",
                j => j.HasOne<TournamentTeam>().WithMany().HasForeignKey("TeamId"),
                j => j.HasOne<TournamentMatch>().WithMany().HasForeignKey("MatchId")
            );

        // TournamentTeamMatchResult configuration
        builder.Entity<TournamentTeamMatchResult>(b =>
        {
            b.HasKey(ttmr => ttmr.Id);
            b.HasOne(ttmr => ttmr.Match)
                .WithMany(m => m.Results)
                .HasForeignKey(ttmr => ttmr.MatchId);
            b.HasOne(ttmr => ttmr.Tournament)
                .WithMany()
                .HasForeignKey(ttmr => ttmr.TournamentId);
            b.HasOne(ttmr => ttmr.Team)
                .WithMany()
                .HasForeignKey(ttmr => ttmr.TeamId);
        });
    }
}
