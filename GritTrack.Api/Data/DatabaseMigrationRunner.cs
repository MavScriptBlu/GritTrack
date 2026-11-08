namespace GritTrack.Api.Data;

/// <summary>
/// Runs the "apply migrations" step and keeps trying while SQL Server is still waking up.
/// Pulled out of Program.cs so the retry rules can be tested without a real database.
/// </summary>
public static class DatabaseMigrationRunner
{
    /// <summary>How many times to try before giving up.</summary>
    public const int DefaultMaximumAttempts = 6;

    /// <summary>How long to wait between tries.</summary>
    public static readonly TimeSpan DefaultDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Runs migrate. If it throws, logs a warning, waits, and tries again. The last attempt does not
    /// catch anything, so the real error reaches the caller.
    /// </summary>
    /// <param name="migrate">The work to try, normally context.Database.MigrateAsync.</param>
    /// <param name="logger">Where the "not ready yet" warnings go.</param>
    /// <param name="maximumAttempts">Total tries, including the first one.</param>
    /// <param name="wait">How to wait between tries. Tests swap this out so they don't sleep for real.</param>
    public static async Task RunWithRetryAsync(
        Func<Task> migrate,
        ILogger logger,
        int maximumAttempts = DefaultMaximumAttempts,
        Func<TimeSpan, Task>? wait = null)
    {
        wait ??= delay => Task.Delay(delay);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await migrate();
                return;
            }
            catch (Exception exception) when (attempt < maximumAttempts)
            {
                // only the exception type goes in the log, never its message (it can hold the connection string)
                logger.LogWarning("Database not ready yet (attempt {Attempt} of {Maximum}): {Reason}",
                    attempt, maximumAttempts, exception.GetType().Name);
                await wait(DefaultDelay);
            }
        }
    }
}
