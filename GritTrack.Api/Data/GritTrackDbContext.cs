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
        modelBuilder.Entity<Course>(builder =>
        {
            builder.HasKey(course => course.Id);

            // limits match the request rules, so the database never accepts what the API would refuse
            builder.Property(course => course.Name).IsRequired().HasMaxLength(100);
            builder.Property(course => course.CourseCode).IsRequired().HasMaxLength(10);
            builder.Property(course => course.Instructor).HasMaxLength(100);
            builder.Property(course => course.CurrentGrade).IsRequired().HasMaxLength(2);

            // two courses can never share a code (SQL Server compares case-insensitively by default)
            builder.HasIndex(course => course.CourseCode).IsUnique();
        });
    }
}
