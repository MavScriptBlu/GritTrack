using GritTrack.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GritTrack.Api.Tests.Sql;

// SQL Server containers say "ready" before they really are, so startup retries the migration.
// nothing here sleeps for real: the wait step is swapped for a counter
public class DatabaseMigrationRunnerTests
{
    // collects every log line so a test can look at what got warned
    private sealed class CapturingLogger : ILogger
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }

    // counts waits instead of sleeping
    private sealed class WaitCounter
    {
        public List<TimeSpan> Waits { get; } = new();

        public Task Wait(TimeSpan delay)
        {
            Waits.Add(delay);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task RunWithRetryAsync_RunsOnce_WhenTheFirstTrySucceeds()
    {
        var logger = new CapturingLogger();
        var waitCounter = new WaitCounter();
        var attemptCount = 0;

        await DatabaseMigrationRunner.RunWithRetryAsync(() =>
        {
            attemptCount++;
            return Task.CompletedTask;
        }, logger, wait: waitCounter.Wait);

        Assert.Equal(1, attemptCount);
        Assert.Empty(waitCounter.Waits);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task RunWithRetryAsync_KeepsTrying_UntilSqlServerIsReady()
    {
        var logger = new CapturingLogger();
        var waitCounter = new WaitCounter();
        var attemptCount = 0;

        await DatabaseMigrationRunner.RunWithRetryAsync(() =>
        {
            attemptCount++;
            if (attemptCount < 3)
                throw new InvalidOperationException("still starting up");
            return Task.CompletedTask;
        }, logger, wait: waitCounter.Wait);

        Assert.Equal(3, attemptCount);
        Assert.Equal(2, waitCounter.Waits.Count);
        Assert.Equal(2, logger.Entries.Count);
        Assert.All(logger.Entries, entry => Assert.Equal(LogLevel.Warning, entry.Level));
    }

    [Fact]
    public async Task RunWithRetryAsync_WaitsTheDefaultDelayBetweenTries()
    {
        var waitCounter = new WaitCounter();
        var attemptCount = 0;

        await DatabaseMigrationRunner.RunWithRetryAsync(() =>
        {
            attemptCount++;
            if (attemptCount == 1)
                throw new InvalidOperationException("nope");
            return Task.CompletedTask;
        }, new CapturingLogger(), wait: waitCounter.Wait);

        Assert.Equal(TimeSpan.FromSeconds(5), Assert.Single(waitCounter.Waits));
        Assert.Equal(DatabaseMigrationRunner.DefaultDelay, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RunWithRetryAsync_GivesUpAfterTheMaximumAttempts_AndThrowsTheRealError()
    {
        var waitCounter = new WaitCounter();
        var attemptCount = 0;
        var realError = new TimeoutException("the very last failure");

        var thrown = await Assert.ThrowsAsync<TimeoutException>(() =>
            DatabaseMigrationRunner.RunWithRetryAsync(() =>
            {
                attemptCount++;
                throw realError;
            }, new CapturingLogger(), wait: waitCounter.Wait));

        Assert.Same(realError, thrown);
        Assert.Equal(DatabaseMigrationRunner.DefaultMaximumAttempts, attemptCount);
        // no wait after the last try, nothing left to wait for
        Assert.Equal(DatabaseMigrationRunner.DefaultMaximumAttempts - 1, waitCounter.Waits.Count);
    }

    [Fact]
    public void DefaultMaximumAttempts_StaysAtSix()
    {
        // 6 tries with 5 seconds between them is about 25 seconds for a container to wake up
        Assert.Equal(6, DatabaseMigrationRunner.DefaultMaximumAttempts);
    }

    [Fact]
    public async Task RunWithRetryAsync_HonorsACustomMaximumNumberOfAttempts()
    {
        var attemptCount = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DatabaseMigrationRunner.RunWithRetryAsync(() =>
            {
                attemptCount++;
                throw new InvalidOperationException("always fails");
            }, new CapturingLogger(), maximumAttempts: 2, wait: new WaitCounter().Wait));

        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task RunWithRetryAsync_WarningSaysWhichAttemptItWas()
    {
        var logger = new CapturingLogger();
        var attemptCount = 0;

        await DatabaseMigrationRunner.RunWithRetryAsync(() =>
        {
            attemptCount++;
            if (attemptCount == 1)
                throw new InvalidOperationException("nope");
            return Task.CompletedTask;
        }, logger, maximumAttempts: 4, wait: new WaitCounter().Wait);

        var message = Assert.Single(logger.Entries).Message;
        Assert.Contains("attempt 1 of 4", message);
        Assert.Contains(nameof(InvalidOperationException), message);
    }

    [Fact]
    public async Task RunWithRetryAsync_LogsTheExceptionTypeButNeverItsMessage()
    {
        // a SQL error message can carry the connection string (and the password in it)
        var logger = new CapturingLogger();
        var attemptCount = 0;

        await DatabaseMigrationRunner.RunWithRetryAsync(() =>
        {
            attemptCount++;
            if (attemptCount == 1)
                throw new InvalidOperationException("Server=x;Password=super-secret-password");
            return Task.CompletedTask;
        }, logger, wait: new WaitCounter().Wait);

        var message = Assert.Single(logger.Entries).Message;
        Assert.DoesNotContain("super-secret-password", message);
        Assert.DoesNotContain("Password=", message);
    }

    [Fact]
    public async Task RunWithRetryAsync_TriesARealUnreachableServerEveryTime_ThenShowsTheSqlError()
    {
        // port 1 on this machine has nothing listening, so every try fails with a real SqlException
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<GritTrackDbContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=NeverThere;User Id=sa;Password=not-used;Encrypt=true;TrustServerCertificate=true;Connect Timeout=1")
            .Options;
        var logger = new CapturingLogger();
        var waitCounter = new WaitCounter();
        var attemptCount = 0;

        await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() =>
            DatabaseMigrationRunner.RunWithRetryAsync(async () =>
            {
                attemptCount++;
                await using var context = new GritTrackDbContext(options);
                await context.Database.MigrateAsync();
            }, logger, maximumAttempts: 3, wait: waitCounter.Wait));

        Assert.Equal(3, attemptCount);
        Assert.Equal(2, waitCounter.Waits.Count);
        Assert.All(logger.Entries, entry => Assert.Contains("SqlException", entry.Message));
    }

    [Fact]
    public async Task RunWithRetryAsync_WorksWithoutACustomWait_WhenTheFirstTrySucceeds()
    {
        // no wait passed in: the real Task.Delay default must be there but never get used on success
        await DatabaseMigrationRunner.RunWithRetryAsync(() => Task.CompletedTask, new CapturingLogger());
    }
}
