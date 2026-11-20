using GritTrack.Api.Configuration;

namespace GritTrack.Api.Tests.Sql;

// the .env file is how the API and the SQL container share one password,
// so the loader has to find it, read it right, and never beat a real environment variable.
// these tests touch process wide environment variables, so they run one at a time
[Collection("EnvironmentVariables")]
public class DotEnvLoaderTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "grittrack-dotenv-" + Guid.NewGuid().ToString("N"));
    private readonly List<string> _variableNamesToClean = new();

    public DotEnvLoaderTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        foreach (var name in _variableNamesToClean)
            Environment.SetEnvironmentVariable(name, null);

        Directory.Delete(_folder, recursive: true);
    }

    // a unique name per test so nothing leaks between tests (or into the real .env values)
    private string NewVariableName(string label)
    {
        var name = "GRITTRACK_TEST_" + label + "_" + Guid.NewGuid().ToString("N");
        _variableNamesToClean.Add(name);
        return name;
    }

    [Fact]
    public void Load_SetsVariablesFromTheEnvFile()
    {
        var name = NewVariableName("PLAIN");
        File.WriteAllText(Path.Combine(_folder, ".env"), $"{name}=hello");

        DotEnvLoader.Load(_folder);

        Assert.Equal("hello", Environment.GetEnvironmentVariable(name));
    }

    [Fact]
    public void Load_SkipsBlankLinesComments_AndLinesWithoutAnEqualsSign()
    {
        var name = NewVariableName("SKIP");
        var notAVariable = NewVariableName("NOEQUALS");
        File.WriteAllText(Path.Combine(_folder, ".env"),
            string.Join(Environment.NewLine, "", "# a comment", notAVariable, $"{name}=kept"));

        DotEnvLoader.Load(_folder);

        Assert.Equal("kept", Environment.GetEnvironmentVariable(name));
        Assert.Null(Environment.GetEnvironmentVariable(notAVariable));
    }

    [Fact]
    public void Load_TrimsSpacesAndStripsQuotesAroundTheValue()
    {
        var name = NewVariableName("QUOTED");
        File.WriteAllText(Path.Combine(_folder, ".env"), $"  {name} =  \"p@ss word\"  ");

        DotEnvLoader.Load(_folder);

        Assert.Equal("p@ss word", Environment.GetEnvironmentVariable(name));
    }

    [Fact]
    public void Load_KeepsEqualsSignsThatAreInsideTheValue()
    {
        var name = NewVariableName("EQUALS");
        File.WriteAllText(Path.Combine(_folder, ".env"), $"{name}=abc=def==");

        DotEnvLoader.Load(_folder);

        Assert.Equal("abc=def==", Environment.GetEnvironmentVariable(name));
    }

    [Fact]
    public void Load_NeverOverwritesARealEnvironmentVariable()
    {
        var name = NewVariableName("WINS");
        Environment.SetEnvironmentVariable(name, "from the real environment");
        File.WriteAllText(Path.Combine(_folder, ".env"), $"{name}=from the file");

        DotEnvLoader.Load(_folder);

        Assert.Equal("from the real environment", Environment.GetEnvironmentVariable(name));
    }

    [Fact]
    public void Load_FindsTheEnvFileInAParentFolder()
    {
        var name = NewVariableName("PARENT");
        File.WriteAllText(Path.Combine(_folder, ".env"), $"{name}=found it");
        var deepFolder = Path.Combine(_folder, "GritTrack.Api", "bin", "Debug");
        Directory.CreateDirectory(deepFolder);

        DotEnvLoader.Load(deepFolder);

        Assert.Equal("found it", Environment.GetEnvironmentVariable(name));
    }

    [Fact]
    public void Load_UsesTheClosestEnvFile_AndStopsLooking()
    {
        var name = NewVariableName("CLOSEST");
        File.WriteAllText(Path.Combine(_folder, ".env"), $"{name}=far away");
        var childFolder = Path.Combine(_folder, "child");
        Directory.CreateDirectory(childFolder);
        File.WriteAllText(Path.Combine(childFolder, ".env"), $"{name}=close by");

        DotEnvLoader.Load(childFolder);

        Assert.Equal("close by", Environment.GetEnvironmentVariable(name));
    }

    [Fact]
    public void Load_DoesNothing_WhenThereIsNoEnvFileInThisFolder()
    {
        var name = NewVariableName("NOFILE");
        var emptyFolder = Path.Combine(_folder, "empty");
        Directory.CreateDirectory(emptyFolder);

        // there may be a real .env further up the tree on this machine, so just make sure it doesn't throw
        // and doesn't invent our made up variable
        var exception = Record.Exception(() => DotEnvLoader.Load(emptyFolder));

        Assert.Null(exception);
        Assert.Null(Environment.GetEnvironmentVariable(name));
    }
}
