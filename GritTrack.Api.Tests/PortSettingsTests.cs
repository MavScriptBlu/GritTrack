using System.Text.RegularExpressions;

namespace GritTrack.Api.Tests;

// the API port lives in four places (launchSettings, the app's ApiSettings, the Postman files).
// if they drift apart the app silently can't find the API, so check they all agree
public class PortSettingsTests
{
    // Windows hands ports from 49152 up to random stuff and sometimes reserves them,
    // which is what broke 49227 (socket error 10013). stay below that
    private const int FirstDynamicPort = 49152;

    private static string FindRepositoryRoot()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, "GritTrack.slnx")))
            folder = folder.Parent;

        return folder?.FullName ?? throw new InvalidOperationException("Couldn't find GritTrack.slnx above the test folder.");
    }

    private static int ReadFirstPort(string relativePath, string pattern)
    {
        var text = File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath));
        var match = Regex.Match(text, pattern);
        Assert.True(match.Success, $"No port found in {relativePath}");
        return int.Parse(match.Groups[1].Value);
    }

    private static int LaunchSettingsHttpsPort() =>
        ReadFirstPort("GritTrack.Api/Properties/launchSettings.json", @"https://localhost:(\d+)");

    [Fact]
    public void AppApiPort_MatchesLaunchSettingsHttpsPort()
    {
        var appPort = ReadFirstPort("GritTrack/Services/Api/ApiSettings.cs", @"ApiPort\s*=\s*(\d+)");

        Assert.Equal(LaunchSettingsHttpsPort(), appPort);
    }

    [Theory]
    [InlineData("GritTrack.Api/postman/GritTrack-AuthOn.postman_environment.json")]
    [InlineData("GritTrack.Api/postman/GritTrack-AuthOff.postman_environment.json")]
    [InlineData("GritTrack.Api/postman/GritTrack.postman_collection.json")]
    public void PostmanBaseUrl_MatchesLaunchSettingsHttpsPort(string relativePath)
    {
        var postmanPort = ReadFirstPort(relativePath, @"https://localhost:(\d+)");

        Assert.Equal(LaunchSettingsHttpsPort(), postmanPort);
    }

    [Fact]
    public void ApiPorts_StayOutOfTheWindowsDynamicRange()
    {
        var httpsPort = LaunchSettingsHttpsPort();
        var httpPort = ReadFirstPort("GritTrack.Api/Properties/launchSettings.json", @"http://localhost:(\d+)");

        Assert.True(httpsPort < FirstDynamicPort, $"https port {httpsPort} is in the dynamic range");
        Assert.True(httpPort < FirstDynamicPort, $"http port {httpPort} is in the dynamic range");
    }
}
