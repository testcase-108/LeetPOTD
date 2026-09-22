using LeetPOTD.Models;
using Microsoft.EntityFrameworkCore;

namespace LeetPOTD.Data;

public sealed class LeetPotdDbContext(DbContextOptions<LeetPotdDbContext> options) : DbContext(options)
{
    public DbSet<PotdRun> PotdRuns => Set<PotdRun>();
    public DbSet<SubmissionLog> SubmissionLogs => Set<SubmissionLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PotdRun>(entity =>
        {
            entity.HasKey(run => run.Id);
            entity.Property(run => run.Status).HasConversion<string>().HasMaxLength(64);
            entity.Property(run => run.ProblemTitle).HasMaxLength(256);
            entity.Property(run => run.ProblemSlug).HasMaxLength(256);
            entity.Property(run => run.ProblemUrl).HasMaxLength(512);
            entity.Property(run => run.SolutionPath).HasMaxLength(1024);
            entity.HasMany(run => run.Logs)
                .WithOne()
                .HasForeignKey(log => log.PotdRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SubmissionLog>(entity =>
        {
            entity.HasKey(log => log.Id);
            entity.Property(log => log.Status).HasConversion<string>().HasMaxLength(64);
            entity.Property(log => log.Step).HasMaxLength(128);
            entity.Property(log => log.SubmissionId).HasMaxLength(128);
        });
    }
}
