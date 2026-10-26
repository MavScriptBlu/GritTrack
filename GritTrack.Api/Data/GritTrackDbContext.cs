using GritTrack.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GritTrack.Api.Data;

/// <summary>The EF Core door into SQL Server. Only the SQL repository uses it.</summary>
public class GritTrackDbContext : DbContext
{
    public GritTrackDbContext(DbContextOptions<GritTrackDbContext> options) : base(options)
    {
    }

    /// <summary>The Courses table.</summary>
    public DbSet<Course> Courses => Set<Course>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(course =>
        {
            course.HasKey(c => c.Id);

            // limits match the request rules, so the database never accepts what the API would refuse
            course.Property(c => c.Name).IsRequired().HasMaxLength(100);
            course.Property(c => c.CourseCode).IsRequired().HasMaxLength(10);
            course.Property(c => c.Instructor).HasMaxLength(100);
            course.Property(c => c.CurrentGrade).IsRequired().HasMaxLength(2);

            // two courses can never share a code (SQL Server compares case-insensitively by default)
            course.HasIndex(c => c.CourseCode).IsUnique();
        });
    }
}
