namespace GritTrack.Api.Configuration;

/// <summary>
/// Reads the repo's .env file (the same one docker compose uses) so the API and the
/// SQL container share one password. Real environment variables always win over the file.
/// </summary>
public static class DotEnvLoader
{
    /// <summary>Looks for .env in the start folder and then in each parent folder, and loads the first one it finds.</summary>
    public static void Load(string startFolder)
    {
        var folder = new DirectoryInfo(startFolder);
        while (folder is not null)
        {
            var filePath = Path.Combine(folder.FullName, ".env");
            if (File.Exists(filePath))
            {
                LoadFile(filePath);
                return;
            }

            folder = folder.Parent;
        }
    }

    /// <summary>Sets one environment variable for every NAME=value line, skipping blanks and # comments.</summary>
    private static void LoadFile(string filePath)
    {
        foreach (var rawLine in File.ReadAllLines(filePath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
                continue;

            var name = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim().Trim('"');

            // a variable that is already set in the real environment wins
            if (Environment.GetEnvironmentVariable(name) is null)
                Environment.SetEnvironmentVariable(name, value);
        }
    }
}
