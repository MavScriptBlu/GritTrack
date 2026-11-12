using GritTrack.Api.Data;
using GritTrack.Api.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace GritTrack.Api.Tests.Sql;

// these check the migration files and the model without ever opening a connection,
// so they run anywhere. the "does it really create the table" version is in SqlServerIntegrationTests
[Collection("EnvironmentVariables")]
public class MigrationTests
{
    // a context that is pointed at a server that doesn't exist. nothing here connects to it
    private static GritTrackDbContext CreateOfflineContext()
    {
        var options = new DbContextOptionsBuilder<GritTrackDbContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=NeverConnected;User Id=sa;Password=not-used;Encrypt=true;TrustServerCertificate=true")
            .Options;

        return new GritTrackDbContext(options);
    }

    [Fact]
    public void ModelAndSnapshot_AreInSync_SoNoMigrationIsForgotten()
    {
        // fails when someone changes Course or the DbContext but forgets "dotnet ef migrations add"
        using var context = CreateOfflineContext();

        Assert.False(context.Database.HasPendingModelChanges(),
            "The model changed but there is no migration for it. Run: dotnet ef migrations add <Name>");
    }

    [Fact]
    public void InitialCreate_IsTheFirstMigration()
    {
        using var context = CreateOfflineContext();

        var migrationNames = context.Database.GetMigrations().ToList();

        Assert.NotEmpty(migrationNames);
        Assert.EndsWith("_InitialCreate", migrationNames[0]);
    }

    [Fact]
    public void Migrations_AreAllFoundInTheApiAssembly()
    {
        using var context = CreateOfflineContext();
        var migrationsAssembly = context.GetService<IMigrationsAssembly>();

        Assert.Same(typeof(GritTrackDbContext).Assembly, migrationsAssembly.Assembly);
        Assert.Contains(migrationsAssembly.Migrations, pair => pair.Value == typeof(InitialCreate));
    }

    [Fact]
    public void InitialCreate_Up_CreatesTheCoursesTableWithTheRightColumns()
    {
        var upOperations = new InitialCreate().UpOperations;

        var createTable = Assert.Single(upOperations.OfType<CreateTableOperation>());
        Assert.Equal("Courses", createTable.Name);

        var columnNames = createTable.Columns.Select(column => column.Name).ToList();
        Assert.Equal(
            new[] { "Id", "Name", "CourseCode", "Instructor", "Credits", "CurrentGrade", "GradePoints", "CreatedAt" },
            columnNames);
    }

    [Fact]
    public void InitialCreate_Up_UsesTheSameLimitsAsTheRequestRules()
    {
        var createTable = Assert.Single(new InitialCreate().UpOperations.OfType<CreateTableOperation>());
        AddColumnOperation Column(string name) => createTable.Columns.Single(column => column.Name == name);

        Assert.Equal(100, Column("Name").MaxLength);
        Assert.Equal(10, Column("CourseCode").MaxLength);
        Assert.Equal(100, Column("Instructor").MaxLength);
        Assert.Equal(2, Column("CurrentGrade").MaxLength);

        // Instructor is the only column allowed to be empty
        Assert.False(Column("Name").IsNullable);
        Assert.False(Column("CourseCode").IsNullable);
        Assert.False(Column("CurrentGrade").IsNullable);
        Assert.True(Column("Instructor").IsNullable);
    }

    [Fact]
    public void InitialCreate_Up_MakesIdTheIdentityPrimaryKey()
    {
        var createTable = Assert.Single(new InitialCreate().UpOperations.OfType<CreateTableOperation>());

        Assert.Equal("PK_Courses", createTable.PrimaryKey?.Name);
        Assert.Equal(new[] { "Id" }, createTable.PrimaryKey?.Columns);

        var idColumn = createTable.Columns.Single(column => column.Name == "Id");
        Assert.Contains(idColumn.GetAnnotations(), annotation => annotation.Name == "SqlServer:Identity");
    }

    [Fact]
    public void InitialCreate_Up_AddsAUniqueIndexOnCourseCode()
    {
        var index = Assert.Single(new InitialCreate().UpOperations.OfType<CreateIndexOperation>());

        Assert.Equal("IX_Courses_CourseCode", index.Name);
        Assert.Equal("Courses", index.Table);
        Assert.Equal(new[] { "CourseCode" }, index.Columns);
        Assert.True(index.IsUnique);
    }

    [Fact]
    public void InitialCreate_Down_DropsTheCoursesTable()
    {
        var drop = Assert.Single(new InitialCreate().DownOperations.OfType<DropTableOperation>());

        Assert.Equal("Courses", drop.Name);
    }

    [Fact]
    public void Model_CourseCodeIndexIsUnique_AndLimitsMatchTheMigration()
    {
        using var context = CreateOfflineContext();
        var courseType = context.Model.FindEntityType(typeof(GritTrack.Api.Models.Course))!;

        var codeIndex = Assert.Single(courseType.GetIndexes());
        Assert.True(codeIndex.IsUnique);
        Assert.Equal("CourseCode", Assert.Single(codeIndex.Properties).Name);

        Assert.Equal(100, courseType.FindProperty("Name")!.GetMaxLength());
        Assert.Equal(10, courseType.FindProperty("CourseCode")!.GetMaxLength());
        Assert.Equal(2, courseType.FindProperty("CurrentGrade")!.GetMaxLength());
    }

    [Fact]
    public void DesignTimeFactory_BuildsAContext_WhenThePasswordIsAvailable()
    {
        // this is what "dotnet ef" uses, so the migrations command line tools need it to work
        var previous = Environment.GetEnvironmentVariable("MSSQL_SA_PASSWORD");
        Environment.SetEnvironmentVariable("MSSQL_SA_PASSWORD", "test-only-Password-123");
        try
        {
            using var context = new GritTrackDbContextFactory().CreateDbContext(Array.Empty<string>());

            Assert.True(context.Database.IsSqlServer());
        }
        finally
        {
            Environment.SetEnvironmentVariable("MSSQL_SA_PASSWORD", previous);
        }
    }
}
