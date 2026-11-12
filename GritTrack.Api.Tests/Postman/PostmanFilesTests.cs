using System.Text.Json;
using System.Text.RegularExpressions;

namespace GritTrack.Api.Tests.Postman;

// the Postman collection and the two environments get run by hand and in demos, so a typo in them
// only shows up when something goes red at the worst moment. these checks catch the usual slips:
// broken JSON, variables nobody defined, a request name that promises a status the script never checks
public class PostmanFilesTests
{
    private const string CollectionPath = "GritTrack.Api/postman/GritTrack.postman_collection.json";
    private const string AuthOnPath = "GritTrack.Api/postman/GritTrack-AuthOn.postman_environment.json";
    private const string AuthOffPath = "GritTrack.Api/postman/GritTrack-AuthOff.postman_environment.json";

    private static readonly string[] AllowedMethods = { "GET", "POST", "PUT", "DELETE" };

    // one request from the collection, with the folder it lives in
    private sealed record RequestInfo(string Folder, string Name, string Method, string Url, JsonElement Json)
    {
        public string FullName => $"{Folder} / {Name}";
        public string TestScript => ScriptFor("test");
        public string PreRequestScript => ScriptFor("prerequest");
        public bool HasBody => Json.GetProperty("request").TryGetProperty("body", out _);

        private string ScriptFor(string listen)
        {
            if (!Json.TryGetProperty("event", out var events))
                return string.Empty;

            return string.Join("\n", events.EnumerateArray()
                .Where(eventElement => eventElement.GetProperty("listen").GetString() == listen)
                .SelectMany(eventElement => eventElement.GetProperty("script").GetProperty("exec").EnumerateArray())
                .Select(line => line.GetString()));
        }
    }

    private static JsonDocument Load(string relativePath) => JsonDocument.Parse(RepositoryFiles.ReadAllText(relativePath));

    private static List<RequestInfo> ReadRequests()
    {
        using var document = Load(CollectionPath);
        var requests = new List<RequestInfo>();
        Walk(document.RootElement.GetProperty("item"), "(root)", requests);
        return requests.Select(request => request with { Json = request.Json.Clone() }).ToList();
    }

    private static void Walk(JsonElement items, string folder, List<RequestInfo> requests)
    {
        foreach (var item in items.EnumerateArray())
        {
            var name = item.GetProperty("name").GetString()!;
            if (item.TryGetProperty("item", out var children))
            {
                Walk(children, name, requests);
                continue;
            }

            var request = item.GetProperty("request");
            var url = request.GetProperty("url").ValueKind == JsonValueKind.String
                ? request.GetProperty("url").GetString()!
                : request.GetProperty("url").GetProperty("raw").GetString()!;
            requests.Add(new RequestInfo(folder, name, request.GetProperty("method").GetString()!, url, item));
        }
    }

    // pulls every environment variable out of one environment file
    private static Dictionary<string, JsonElement> ReadEnvironmentValues(string relativePath)
    {
        using var document = Load(relativePath);
        return document.RootElement.GetProperty("values").EnumerateArray()
            .ToDictionary(value => value.GetProperty("key").GetString()!, value => value.Clone());
    }

    private static HashSet<string> ReadCollectionVariableNames()
    {
        using var document = Load(CollectionPath);
        return document.RootElement.GetProperty("variable").EnumerateArray()
            .Select(variable => variable.GetProperty("key").GetString()!)
            .ToHashSet();
    }

    // every status code number (401, 404...) inside the text
    private static HashSet<int> StatusCodesIn(string text) =>
        Regex.Matches(text, @"\b([1-5]\d\d)\b").Select(match => int.Parse(match.Groups[1].Value)).ToHashSet();

    // ---- the files are real, readable JSON ----

    [Theory]
    [InlineData(CollectionPath)]
    [InlineData(AuthOnPath)]
    [InlineData(AuthOffPath)]
    public void PostmanFile_IsValidJson(string relativePath)
    {
        var exception = Record.Exception(() => Load(relativePath).Dispose());

        Assert.Null(exception);
    }

    // ---- collection shape ----

    [Fact]
    public void Collection_UsesThePostmanV21Schema_AndHasANameAndId()
    {
        using var document = Load(CollectionPath);
        var info = document.RootElement.GetProperty("info");

        Assert.Equal("https://schema.getpostman.com/json/collection/v2.1.0/collection.json", info.GetProperty("schema").GetString());
        Assert.False(string.IsNullOrWhiteSpace(info.GetProperty("name").GetString()));
        Assert.True(Guid.TryParse(info.GetProperty("_postman_id").GetString(), out _));
    }

    [Fact]
    public void Collection_HasFoldersAndEveryFolderHasRequests()
    {
        using var document = Load(CollectionPath);
        var folders = document.RootElement.GetProperty("item").EnumerateArray().ToList();

        Assert.NotEmpty(folders);
        Assert.All(folders, folder =>
        {
            Assert.True(folder.TryGetProperty("item", out var children), $"{folder.GetProperty("name")} has no item list");
            Assert.NotEmpty(children.EnumerateArray());
        });
    }

    [Fact]
    public void EveryRequest_UsesAKnownMethod_AndAUrlThatStartsWithTheBaseUrl()
    {
        var requests = ReadRequests();

        Assert.NotEmpty(requests);
        Assert.All(requests, request =>
        {
            Assert.True(AllowedMethods.Contains(request.Method), $"{request.FullName} uses {request.Method}");
            Assert.StartsWith("{{baseUrl}}/api/", request.Url);
        });
    }

    [Fact]
    public void RequestNames_AreUniqueInsideEachFolder()
    {
        var duplicates = ReadRequests()
            .GroupBy(request => (request.Folder, request.Name))
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key.Folder} / {group.Key.Name}")
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void EveryRequest_HasATestScriptWithAtLeastOneNamedTest()
    {
        var withoutTests = ReadRequests()
            .Where(request => !Regex.IsMatch(request.TestScript, @"pm\.test\(\s*[^,\s]"))
            .Select(request => request.FullName)
            .ToList();

        Assert.Empty(withoutTests);
    }

    [Fact]
    public void RequestsWithABody_SendJson_AndTheBodyIsValidJson()
    {
        var problems = new List<string>();
        foreach (var request in ReadRequests().Where(request => request.HasBody))
        {
            var body = request.Json.GetProperty("request").GetProperty("body");
            if (body.GetProperty("mode").GetString() != "raw")
            {
                problems.Add($"{request.FullName}: body mode isn't raw");
                continue;
            }

            // swap {{variables}} for plain text first, so the body can be checked as JSON
            var raw = Regex.Replace(body.GetProperty("raw").GetString()!, @"\{\{[^}]+\}\}", "placeholder");
            try
            {
                JsonDocument.Parse(raw).Dispose();
            }
            catch (JsonException)
            {
                problems.Add($"{request.FullName}: body isn't valid JSON");
            }

            var headers = request.Json.GetProperty("request").GetProperty("header").EnumerateArray();
            if (!headers.Any(header => header.GetProperty("key").GetString() == "Content-Type"
                                       && header.GetProperty("value").GetString() == "application/json"))
                problems.Add($"{request.FullName}: has a body but no Content-Type: application/json header");
        }

        Assert.Empty(problems);
    }

    // ---- no contradictions: names promise what the scripts check ----

    [Fact]
    public void StatusCodesInARequestName_AreTheSameOnesItsTestScriptChecks()
    {
        var problems = new List<string>();
        foreach (var request in ReadRequests())
        {
            var codesInName = StatusCodesIn(request.Name);
            if (codesInName.Count == 0)
                continue;

            // only look at what goes inside have.status( ... ), so other numbers in a script don't count
            var codesInScript = Regex.Matches(request.TestScript, @"have\.status\(([^)]*)\)")
                .SelectMany(match => StatusCodesIn(match.Groups[1].Value))
                .ToHashSet();

            if (!codesInName.SetEquals(codesInScript))
                problems.Add($"{request.FullName}: name says [{string.Join(", ", codesInName.Order())}] but the script checks [{string.Join(", ", codesInScript.Order())}]");
        }

        Assert.Empty(problems);
    }

    [Fact]
    public void RequestsThatSayWhenAuthIsOff_ReallyBranchOnTheAuthSwitch()
    {
        var problems = ReadRequests()
            .Where(request => request.Name.Contains("auth is off", StringComparison.OrdinalIgnoreCase))
            .Where(request => !request.TestScript.Contains("authEnabled"))
            .Select(request => request.FullName)
            .ToList();

        Assert.Empty(problems);
    }

    [Fact]
    public void RequestsThatDontMentionAuthOff_NeverBranchOnTheAuthSwitchInTheirStatusCheck()
    {
        // a request like "Create course A (201)" must give the same answer in both modes
        var problems = ReadRequests()
            .Where(request => !request.Name.Contains("auth is off", StringComparison.OrdinalIgnoreCase))
            .Where(request => Regex.IsMatch(request.TestScript, @"have\.status\([^)]*authOn"))
            .Select(request => request.FullName)
            .ToList();

        Assert.Empty(problems);
    }

    [Fact]
    public void TheNoTokenRequests_AreNamedSoTheCollectionScriptLeavesThemAlone()
    {
        // the collection level script skips any request whose name includes "(no token)"
        using var document = Load(CollectionPath);
        var collectionScript = string.Join("\n", document.RootElement.GetProperty("event").EnumerateArray()
            .Where(eventElement => eventElement.GetProperty("listen").GetString() == "prerequest")
            .SelectMany(eventElement => eventElement.GetProperty("script").GetProperty("exec").EnumerateArray())
            .Select(line => line.GetString()));

        Assert.Contains("(no token)", collectionScript);
        Assert.Contains(ReadRequests(), request => request.Name.Contains("(no token)"));
    }

    // ---- no undefined variables ----

    [Fact]
    public void EveryVariableUsedInTheCollection_IsDefinedSomewhere()
    {
        // defined = a collection variable, or a value in BOTH environments (so either one can run it)
        var defined = ReadCollectionVariableNames();
        var inBothEnvironments = ReadEnvironmentValues(AuthOnPath).Keys.Intersect(ReadEnvironmentValues(AuthOffPath).Keys);
        defined.UnionWith(inBothEnvironments);

        var collectionText = RepositoryFiles.ReadAllText(CollectionPath);
        var used = Regex.Matches(collectionText, @"\{\{([^}]+)\}\}")
            .Select(match => match.Groups[1].Value.Trim())
            .Where(name => !name.StartsWith('$'))   // {{$guid}} style dynamic variables are built in
            .Distinct()
            .ToList();

        Assert.NotEmpty(used);
        Assert.All(used, name => Assert.True(defined.Contains(name), $"{{{name}}} is used but never defined"));
    }

    [Fact]
    public void EveryVariableTheScriptsReadWithPm_IsDefinedOrSetByAScript()
    {
        var defined = ReadCollectionVariableNames();
        defined.UnionWith(ReadEnvironmentValues(AuthOnPath).Keys);

        var allScripts = string.Join("\n", ReadRequests().Select(request => request.TestScript + "\n" + request.PreRequestScript));
        using var document = Load(CollectionPath);
        allScripts += "\n" + document.RootElement.GetRawText();

        var setByScripts = Regex.Matches(allScripts, @"pm\.(?:environment|variables|collectionVariables)\.set\(\\?['""]([A-Za-z0-9_]+)")
            .Select(match => match.Groups[1].Value)
            .ToHashSet();
        var read = Regex.Matches(allScripts, @"pm\.(?:environment|variables|collectionVariables)\.(?:get|unset)\(\\?['""]([A-Za-z0-9_]+)")
            .Select(match => match.Groups[1].Value)
            .Distinct()
            .ToList();

        Assert.NotEmpty(read);
        Assert.All(read, name => Assert.True(defined.Contains(name) || setByScripts.Contains(name), $"{name} is read by a script but never defined or set"));
    }

    [Fact]
    public void ThePathVariablesForCourseIds_AreFilledInByAnEarlierRequestBeforeTheyAreUsed()
    {
        // {{courseIdA}} and {{courseIdB}} start empty, so a create request has to set them first
        var requests = ReadRequests();
        foreach (var variable in new[] { "courseIdA", "courseIdB" })
        {
            var firstSetter = requests.FindIndex(request => request.TestScript.Contains($"'{variable}'") && request.TestScript.Contains(".set("));
            var firstUser = requests.FindIndex(request => request.Url.Contains("{{" + variable + "}}"));

            Assert.True(firstSetter >= 0, $"no request sets {variable}");
            Assert.True(firstSetter < firstUser, $"{variable} is used before anything sets it");
        }
    }

    // ---- the two environments ----

    [Fact]
    public void BothEnvironments_HaveTheSameVariables()
    {
        var authOn = ReadEnvironmentValues(AuthOnPath).Keys.Order().ToList();
        var authOff = ReadEnvironmentValues(AuthOffPath).Keys.Order().ToList();

        Assert.Equal(authOn, authOff);
    }

    [Fact]
    public void TheAuthSwitch_IsTheOnlyThingThatDiffersBetweenTheEnvironments()
    {
        var authOn = ReadEnvironmentValues(AuthOnPath);
        var authOff = ReadEnvironmentValues(AuthOffPath);

        var differing = authOn.Keys
            .Where(key => authOn[key].GetProperty("value").GetString() != authOff[key].GetProperty("value").GetString())
            .ToList();

        Assert.Equal(new[] { "authEnabled" }, differing);
    }

    [Fact]
    public void AuthEnabled_MatchesTheNameOfEachEnvironment()
    {
        using var onDocument = Load(AuthOnPath);
        using var offDocument = Load(AuthOffPath);

        Assert.Contains("ON", onDocument.RootElement.GetProperty("name").GetString());
        Assert.Contains("OFF", offDocument.RootElement.GetProperty("name").GetString());
        Assert.Equal("true", ReadEnvironmentValues(AuthOnPath)["authEnabled"].GetProperty("value").GetString());
        Assert.Equal("false", ReadEnvironmentValues(AuthOffPath)["authEnabled"].GetProperty("value").GetString());
    }

    [Fact]
    public void TheCollectionDescription_NamesTheSameEnvironmentsThatExist()
    {
        using var collection = Load(CollectionPath);
        using var onDocument = Load(AuthOnPath);
        using var offDocument = Load(AuthOffPath);
        var description = collection.RootElement.GetProperty("info").GetProperty("description").GetString();

        Assert.Contains(onDocument.RootElement.GetProperty("name").GetString()!, description);
        Assert.Contains(offDocument.RootElement.GetProperty("name").GetString()!, description);
    }

    [Theory]
    [InlineData(AuthOnPath)]
    [InlineData(AuthOffPath)]
    public void Environment_KeepsSecretsOutOfGit(string relativePath)
    {
        var values = ReadEnvironmentValues(relativePath);

        foreach (var secretName in new[] { "demoPassword", "token" })
        {
            Assert.Equal("secret", values[secretName].GetProperty("type").GetString());
            Assert.Equal(string.Empty, values[secretName].GetProperty("value").GetString());
        }
    }

    [Theory]
    [InlineData(AuthOnPath)]
    [InlineData(AuthOffPath)]
    public void Environment_HasTheStandardPostmanFields_AndEveryValueIsSwitchedOn(string relativePath)
    {
        using var document = Load(relativePath);

        Assert.Equal("environment", document.RootElement.GetProperty("_postman_variable_scope").GetString());
        Assert.True(Guid.TryParse(document.RootElement.GetProperty("id").GetString(), out _));
        Assert.All(document.RootElement.GetProperty("values").EnumerateArray(),
            value => Assert.True(value.GetProperty("enabled").GetBoolean(), $"{value.GetProperty("key")} is turned off"));
    }

    [Fact]
    public void TheTwoEnvironments_HaveDifferentIds()
    {
        using var onDocument = Load(AuthOnPath);
        using var offDocument = Load(AuthOffPath);

        Assert.NotEqual(
            onDocument.RootElement.GetProperty("id").GetString(),
            offDocument.RootElement.GetProperty("id").GetString());
    }

    [Fact]
    public void BothEnvironments_AimAtTheSameBaseUrl_AsTheCollectionDefault()
    {
        var onBaseUrl = ReadEnvironmentValues(AuthOnPath)["baseUrl"].GetProperty("value").GetString();
        var offBaseUrl = ReadEnvironmentValues(AuthOffPath)["baseUrl"].GetProperty("value").GetString();

        using var collection = Load(CollectionPath);
        var collectionBaseUrl = collection.RootElement.GetProperty("variable").EnumerateArray()
            .Single(variable => variable.GetProperty("key").GetString() == "baseUrl")
            .GetProperty("value").GetString();

        Assert.Equal(onBaseUrl, offBaseUrl);
        Assert.Equal(onBaseUrl, collectionBaseUrl);
    }

    // ---- nothing secret sits in the collection ----

    [Fact]
    public void Collection_HasNoRealLookingTokenOrPassword()
    {
        var collectionText = RepositoryFiles.ReadAllText(CollectionPath);

        // a real JWT starts with eyJ. the one fake token in there is "not.a.real.token"
        Assert.DoesNotMatch(@"eyJ[A-Za-z0-9_-]{10,}", collectionText);
        Assert.DoesNotMatch(@"""password""\s*:\s*""(?!\{\{|this-is-not-the-password)", collectionText.Replace("\\\"", "\""));
    }
}
