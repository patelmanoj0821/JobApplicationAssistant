using Microsoft.EntityFrameworkCore;
using JobApplicationAssistant.Api.Models;

namespace JobApplicationAssistant.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<ApplicationLog> ApplicationLogs => Set<ApplicationLog>();
    public DbSet<SearchCriteria> SearchCriteria => Set<SearchCriteria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<JobApplication>()
            .HasOne(a => a.UserProfile)
            .WithMany(u => u.JobApplications)
            .HasForeignKey(a => a.UserProfileId);

        modelBuilder.Entity<ApplicationLog>()
            .HasOne(l => l.JobApplication)
            .WithMany(a => a.Logs)
            .HasForeignKey(l => l.JobApplicationId);
            
        modelBuilder.Entity<JobApplication>()
            .Property(e => e.State)
            .HasConversion<string>();
    }
}
