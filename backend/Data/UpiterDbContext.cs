using Microsoft.EntityFrameworkCore;
using Upiter.Api.Models;

namespace Upiter.Api.Data;

public class UpiterDbContext : DbContext
{
    public UpiterDbContext(DbContextOptions<UpiterDbContext> options) : base(options)
    {
    }
    public DbSet<Target> Targets => Set<Target>();
    public DbSet<CheckResult> CheckResults => Set<CheckResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Target>()
            .HasMany(t => t.CheckResults)
            .WithOne(cr => cr.Target!)
            .HasForeignKey(cr => cr.TargetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}


