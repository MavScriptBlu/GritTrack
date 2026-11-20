namespace GritTrack.Api.Tests.Postman;

// finds files in the repo no matter where the tests run from (bin folder, Visual Studio, CI)
internal static class RepositoryFiles
{
    public static string Root { get; } = FindRoot();

    private static string FindRoot()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, "GritTrack.slnx")))
            folder = folder.Parent;

        return folder?.FullName ?? throw new InvalidOperationException("Couldn't find GritTrack.slnx above the test folder.");
    }

    public static string PathTo(string relativePath) => Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));

    public static string ReadAllText(string relativePath) => File.ReadAllText(PathTo(relativePath));
}
