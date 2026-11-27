using GritTrack.Api.Data;
using GritTrack.Api.Models;
using GritTrack.Api.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GritTrack.Api.Tests.Sql;

// the real thing: a real SQL Server (LocalDB), the real migrations, the real SqlCourseRepository.
// skipped on machines without LocalDB. every test makes its own course codes so they never trip over each other
public class SqlServerIntegrationTests : IClassFixture<LocalDbFixture>
{
    private readonly LocalDbFixture _database;

    public SqlServerIntegrationTests(LocalDbFixture database)
    {
        _database = database;
    }

    // 9 characters, so it fits the 10 character CourseCode column
    private static string NewCourseCode() => "T" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    private static Course NewCourse(string courseCode, string name = "Test Course") => new()
    {
        Name = name,
        CourseCode = courseCode,
        Instructor = "Staff",
        Credits = 3,
        CurrentGrade = "B+",
        GradePoints = 3.3,
        CreatedAt = DateTime.UtcNow
    };

    // ---- migrations against a real server ----

    [LocalDbFact]
    public async Task Migrate_CreatesTheCoursesTable_AndRecordsInitialCreate()
    {
        await using var context = _database.CreateContext();

        var appliedMigrations = (await context.Database.GetAppliedMigrationsAsync()).ToList();
        var pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).ToList();

        Assert.Contains(appliedMigrations, name => name.EndsWith("_InitialCreate"));
        Assert.Empty(pendingMigrations);
        Assert.Equal(0, await context.Courses.CountAsync(course => course.Id < 0));
    }

    [LocalDbFact]
    public async Task Migrate_CanRunAgain_WithoutHarmOrDuplicates()
    {
        // startup runs the migration every single time the API boots, so a second run has to be a no-op
        await using var context = _database.CreateContext();

        await context.Database.MigrateAsync();
        await context.Database.MigrateAsync();

        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        Assert.Equal(appliedMigrations.Count(), appliedMigrations.Distinct().Count());
    }

    [LocalDbFact]
    public async Task StartupRunner_AppliesMigrationsToABrandNewDatabase()
    {
        // the same call Program.cs makes on startup, aimed at an empty database
        var scratchDatabaseName = "GritTrackTests_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<GritTrackDbContext>()
            .UseSqlServer(LocalDb.ConnectionStringFor(scratchDatabaseName))
            .Options;

        await using var context = new GritTrackDbContext(options);
        try
        {
            await DatabaseMigrationRunner.RunWithRetryAsync(
                () => context.Database.MigrateAsync(),
                Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance,
                wait: _ => Task.CompletedTask);

            Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }

    [LocalDbFact]
    public async Task CourseCodeIndex_StopsTwoCoursesWithTheSameCode()
    {
        var courseCode = NewCourseCode();
        await using (var first = _database.CreateContext())
        {
            first.Courses.Add(NewCourse(courseCode));
            await first.SaveChangesAsync();
        }

        await using var second = _database.CreateContext();
        second.Courses.Add(NewCourse(courseCode));

        // SQL Server itself says no, not just the service layer
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
        Assert.IsType<SqlException>(exception.InnerException);
    }

    [LocalDbFact]
    public async Task ColumnLimits_AreEnforcedByTheDatabase()
    {
        await using var context = _database.CreateContext();
        context.Courses.Add(NewCourse("TOO-LONG-CODE-1234"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    // ---- SqlCourseRepository ----

    [LocalDbFact]
    public async Task AddAsync_GivesTheCourseAnIdFromSqlServer()
    {
        await using var context = _database.CreateContext();
        var repository = new SqlCourseRepository(context);

        var saved = await repository.AddAsync(NewCourse(NewCourseCode()), CancellationToken.None);

        Assert.True(saved.Id > 0);
    }

    [LocalDbFact]
    public async Task AddAsync_IdsKeepGoingUp()
    {
        await using var context = _database.CreateContext();
        var repository = new SqlCourseRepository(context);

        var first = await repository.AddAsync(NewCourse(NewCourseCode()), CancellationToken.None);
        var second = await repository.AddAsync(NewCourse(NewCourseCode()), CancellationToken.None);

        Assert.True(second.Id > first.Id);
    }

    [LocalDbFact]
    public async Task GetByIdAsync_FindsASavedCourse_AndAllItsFields()
    {
        var courseCode = NewCourseCode();
        int savedId;
        await using (var writeContext = _database.CreateContext())
        {
            var saved = await new SqlCourseRepository(writeContext).AddAsync(NewCourse(courseCode, "Databases"), CancellationToken.None);
            savedId = saved.Id;
        }

        await using var readContext = _database.CreateContext();
        var found = await new SqlCourseRepository(readContext).GetByIdAsync(savedId, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal("Databases", found.Name);
        Assert.Equal(courseCode, found.CourseCode);
        Assert.Equal("Staff", found.Instructor);
        Assert.Equal(3, found.Credits);
        Assert.Equal("B+", found.CurrentGrade);
        Assert.Equal(3.3, found.GradePoints);
    }

    [LocalDbFact]
    public async Task GetByIdAsync_ReturnsNull_ForAnIdThatDoesNotExist()
    {
        await using var context = _database.CreateContext();

        var found = await new SqlCourseRepository(context).GetByIdAsync(int.MaxValue, CancellationToken.None);

        Assert.Null(found);
    }

    [LocalDbFact]
    public async Task GetByCourseCodeAsync_IgnoresCapitalLetters_BecauseOfTheColumnCollation()
    {
        var courseCode = NewCourseCode();
        await using (var writeContext = _database.CreateContext())
            await new SqlCourseRepository(writeContext).AddAsync(NewCourse(courseCode), CancellationToken.None);

        await using var readContext = _database.CreateContext();
        var found = await new SqlCourseRepository(readContext).GetByCourseCodeAsync(courseCode.ToLowerInvariant(), CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(courseCode, found.CourseCode);
    }

    [LocalDbFact]
    public async Task GetByCourseCodeAsync_TrimsSpacesAroundTheCode()
    {
        var courseCode = NewCourseCode();
        await using (var writeContext = _database.CreateContext())
            await new SqlCourseRepository(writeContext).AddAsync(NewCourse(courseCode), CancellationToken.None);

        await using var readContext = _database.CreateContext();
        var found = await new SqlCourseRepository(readContext).GetByCourseCodeAsync($"  {courseCode}  ", CancellationToken.None);

        Assert.NotNull(found);
    }

    [LocalDbFact]
    public async Task GetByCourseCodeAsync_ReturnsNull_WhenNobodyHasThatCode()
    {
        await using var context = _database.CreateContext();

        var found = await new SqlCourseRepository(context).GetByCourseCodeAsync("NOPE-NOPE", CancellationToken.None);

        Assert.Null(found);
    }

    [LocalDbFact]
    public async Task GetAllAsync_ReturnsCoursesInIdOrder_AndDoesNotTrackThem()
    {
        var firstCode = NewCourseCode();
        var secondCode = NewCourseCode();
        await using (var writeContext = _database.CreateContext())
        {
            var repository = new SqlCourseRepository(writeContext);
            await repository.AddAsync(NewCourse(firstCode), CancellationToken.None);
            await repository.AddAsync(NewCourse(secondCode), CancellationToken.None);
        }

        await using var readContext = _database.CreateContext();
        var courses = await new SqlCourseRepository(readContext).GetAllAsync(CancellationToken.None);

        var ourCourses = courses.Where(course => course.CourseCode == firstCode || course.CourseCode == secondCode).ToList();
        Assert.Equal(new[] { firstCode, secondCode }, ourCourses.Select(course => course.CourseCode));
        Assert.Equal(courses.Select(course => course.Id).OrderBy(id => id), courses.Select(course => course.Id));
        Assert.Empty(readContext.ChangeTracker.Entries());
    }

    [LocalDbFact]
    public async Task UpdateAsync_SavesChangesMadeToAnotherCopyOfTheCourse()
    {
        var courseCode = NewCourseCode();
        int savedId;
        await using (var writeContext = _database.CreateContext())
        {
            var saved = await new SqlCourseRepository(writeContext).AddAsync(NewCourse(courseCode), CancellationToken.None);
            savedId = saved.Id;
        }

        // a detached copy, like the one the service builds from a request
        var edited = NewCourse(courseCode, "Renamed Course");
        edited.Id = savedId;
        edited.CurrentGrade = "A";
        edited.GradePoints = 4.0;

        await using (var updateContext = _database.CreateContext())
            Assert.True(await new SqlCourseRepository(updateContext).UpdateAsync(edited, CancellationToken.None));

        await using var readContext = _database.CreateContext();
        var found = await new SqlCourseRepository(readContext).GetByIdAsync(savedId, CancellationToken.None);
        Assert.Equal("Renamed Course", found!.Name);
        Assert.Equal("A", found.CurrentGrade);
        Assert.Equal(4.0, found.GradePoints);
    }

    [LocalDbFact]
    public async Task UpdateAsync_SavesChangesMadeToTheTrackedCourse()
    {
        // the service loads a course, edits it in place, then calls UpdateAsync with that same object
        await using var context = _database.CreateContext();
        var repository = new SqlCourseRepository(context);
        var course = await repository.AddAsync(NewCourse(NewCourseCode()), CancellationToken.None);

        course.Name = "Edited In Place";
        Assert.True(await repository.UpdateAsync(course, CancellationToken.None));

        await using var readContext = _database.CreateContext();
        var found = await new SqlCourseRepository(readContext).GetByIdAsync(course.Id, CancellationToken.None);
        Assert.Equal("Edited In Place", found!.Name);
    }

    [LocalDbFact]
    public async Task UpdateAsync_ReturnsFalse_ForAnIdThatDoesNotExist()
    {
        await using var context = _database.CreateContext();
        var ghost = NewCourse(NewCourseCode());
        ghost.Id = int.MaxValue;

        Assert.False(await new SqlCourseRepository(context).UpdateAsync(ghost, CancellationToken.None));
    }

    [LocalDbFact]
    public async Task DeleteAsync_RemovesTheCourse()
    {
        int savedId;
        await using (var writeContext = _database.CreateContext())
        {
            var saved = await new SqlCourseRepository(writeContext).AddAsync(NewCourse(NewCourseCode()), CancellationToken.None);
            savedId = saved.Id;
        }

        await using (var deleteContext = _database.CreateContext())
            Assert.True(await new SqlCourseRepository(deleteContext).DeleteAsync(savedId, CancellationToken.None));

        await using var readContext = _database.CreateContext();
        Assert.Null(await new SqlCourseRepository(readContext).GetByIdAsync(savedId, CancellationToken.None));
    }

    [LocalDbFact]
    public async Task DeleteAsync_ReturnsFalse_ForAnIdThatDoesNotExist()
    {
        await using var context = _database.CreateContext();

        Assert.False(await new SqlCourseRepository(context).DeleteAsync(int.MaxValue, CancellationToken.None));
    }
}
