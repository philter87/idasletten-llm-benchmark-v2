using Idasletten.Features.Matches;
using Idasletten.Features.Players;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared.Data;

public class ApplicationDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentPlayer> TournamentPlayers => Set<TournamentPlayer>();
    public DbSet<TournamentMatch> TournamentMatches => Set<TournamentMatch>();
    public DbSet<TournamentTeam> TournamentTeams => Set<TournamentTeam>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(e =>
        {
            e.Property(u => u.Username).HasColumnName("Initials").HasMaxLength(20).IsRequired();
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Name).HasMaxLength(200);
            e.Property(u => u.ImageUrl).HasMaxLength(1000);
        });

        builder.Entity<Tournament>(e =>
        {
            e.Property(t => t.Name).HasMaxLength(200).IsRequired();
            e.Property(t => t.TeamSize).HasDefaultValue(2);
            e.Property(t => t.PointsToWin).HasDefaultValue(5);
            e.Property(t => t.RoundNumber).HasDefaultValue(1);
            e.HasOne(t => t.SeedTournament)
                .WithMany()
                .HasForeignKey(t => t.SeedTournamentId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(t => t.ParentTournament)
                .WithMany()
                .HasForeignKey(t => t.ParentTournamentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<TournamentPlayer>(e =>
        {
            e.HasOne(tp => tp.User).WithMany().HasForeignKey(tp => tp.UserId);
            e.HasOne(tp => tp.Tournament).WithMany(t => t.Players).HasForeignKey(tp => tp.TournamentId);
            e.HasIndex(tp => new { tp.TournamentId, tp.UserId }).IsUnique();
        });

        builder.Entity<TournamentMatch>(e =>
        {
            e.HasOne(m => m.Tournament).WithMany(t => t.Matches).HasForeignKey(m => m.TournamentId);
        });

        builder.Entity<TournamentTeam>(e =>
        {
            e.Property(t => t.Name).HasMaxLength(100).IsRequired();
            e.HasOne(t => t.Match).WithMany(m => m.Teams).HasForeignKey(t => t.MatchId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.Tournament).WithMany().HasForeignKey(t => t.TournamentId);
            e.HasMany(t => t.Members)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "TournamentTeamMember",
                    j => j.HasOne<TournamentPlayer>().WithMany().HasForeignKey("TournamentPlayerId"),
                    j => j.HasOne<TournamentTeam>().WithMany().HasForeignKey("TournamentTeamId"),
                    j => j.HasKey("TournamentTeamId", "TournamentPlayerId"));
        });
    }
}
