using Idasletten.Shared.Data.Entities;
using Idasletten.Shared.Data.Enums;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    // DbSets
    public DbSet<Tournament> Tournaments { get; set; } = default!;
    public DbSet<TournamentPlayer> TournamentPlayers { get; set; } = default!;
    public DbSet<TournamentTeam> TournamentTeams { get; set; } = default!;
    public DbSet<TournamentMatch> TournamentMatches { get; set; } = default!;
    public DbSet<TournamentTeamMatchResult> TournamentTeamMatchResults { get; set; } = default!;
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Configure Tournament
        builder.Entity<Tournament>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(255);
            entity.Property(t => t.TeamSize).HasDefaultValue(2);
            entity.Property(t => t.PointsToWin).HasDefaultValue(5);
            entity.Property(t => t.ScoreSystem).HasDefaultValue(ScoreSystem.TrueSkill);
            entity.Property(t => t.IsArchived).HasDefaultValue(false);
            entity.Property(t => t.IsPublic).HasDefaultValue(true);
            entity.Property(t => t.RoundNumber).HasDefaultValue(1);
            
            // Self-referencing for Parent/Child
            entity.HasOne(t => t.ParentTournament)
                .WithMany(t => t.ChildTournaments)
                .HasForeignKey(t => t.ParentTournamentId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Self-referencing for Seed
            entity.HasOne(t => t.SeedTournament)
                .WithMany(t => t.SeededTournaments)
                .HasForeignKey(t => t.SeedTournamentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // Configure TournamentPlayer
        builder.Entity<TournamentPlayer>(entity =>
        {
            entity.HasKey(tp => tp.Id);
            entity.HasOne(tp => tp.User)
                .WithMany(u => u.TournamentPlayers)
                .HasForeignKey(tp => tp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(tp => tp.Tournament)
                .WithMany(t => t.TournamentPlayers)
                .HasForeignKey(tp => tp.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(tp => tp.Score).HasDefaultValue(0.0);
            entity.Property(tp => tp.WinCount).HasDefaultValue(0);
            entity.Property(tp => tp.MatchCount).HasDefaultValue(0);
            entity.Property(tp => tp.LoseCount).HasDefaultValue(0);
            entity.Property(tp => tp.Lives).HasDefaultValue(3);
            entity.Property(tp => tp.PointsWon).HasDefaultValue(0);
            entity.Property(tp => tp.PointsLost).HasDefaultValue(0);
            entity.Property(tp => tp.ScoreDiff).HasDefaultValue(0.0);
        });
        
        // Configure TournamentTeam
        builder.Entity<TournamentTeam>(entity =>
        {
            entity.HasKey(tt => tt.Id);
            entity.HasOne(tt => tt.Tournament)
                .WithMany(t => t.Teams)
                .HasForeignKey(tt => tt.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(tt => tt.Name).IsRequired().HasMaxLength(255);
        });
        
        // Configure TournamentMatch
        builder.Entity<TournamentMatch>(entity =>
        {
            entity.HasKey(tm => tm.Id);
            entity.HasOne(tm => tm.Tournament)
                .WithMany(t => t.Matches)
                .HasForeignKey(tm => tm.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(tm => tm.State).HasDefaultValue(MatchState.Planned);
        });
        
        // Configure TournamentTeamMatchResult
        builder.Entity<TournamentTeamMatchResult>(entity =>
        {
            entity.HasKey(ttmr => ttmr.Id);
            entity.HasOne(ttmr => ttmr.Match)
                .WithMany(m => m.Results)
                .HasForeignKey(ttmr => ttmr.MatchId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ttmr => ttmr.Tournament)
                .WithMany()
                .HasForeignKey(ttmr => ttmr.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ttmr => ttmr.Team)
                .WithMany(tt => tt.MatchResults)
                .HasForeignKey(ttmr => ttmr.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(ttmr => ttmr.GoalsWon).HasDefaultValue(0);
            entity.Property(ttmr => ttmr.GoalsLost).HasDefaultValue(0);
        });
        
        // Many-to-many between TournamentPlayer and TournamentTeam
        builder.Entity<TournamentPlayer>()
            .HasMany(tp => tp.Teams)
            .WithMany(tt => tt.Players)
            .UsingEntity<Dictionary<string, object>>(
                "TournamentPlayerTeam",
                j => j.HasOne<TournamentTeam>().WithMany().HasForeignKey("TeamId"),
                j => j.HasOne<TournamentPlayer>().WithMany().HasForeignKey("TournamentPlayerId"));
        
        // Many-to-many between TournamentMatch and TournamentTeam
        builder.Entity<TournamentMatch>()
            .HasMany(m => m.Teams)
            .WithMany(tt => tt.Matches)
            .UsingEntity<Dictionary<string, object>>(
                "TournamentMatchTeam",
                j => j.HasOne<TournamentTeam>().WithMany().HasForeignKey("TournamentTeamId"),
                j => j.HasOne<TournamentMatch>().WithMany().HasForeignKey("TournamentMatchId"));
    }
}
