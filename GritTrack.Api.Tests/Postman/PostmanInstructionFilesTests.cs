using System.Text.RegularExpressions;

namespace GritTrack.Api.Tests.Postman;

// .github/instructions has four files that tell Copilot how to write Postman scripts.
// they are only useful if they agree with themselves, so these tests check the front matter,
// the sections, and that the example scripts follow the rules the same file lays down
public class PostmanInstructionFilesTests
{
    private const string InstructionsFolder = ".github/instructions";

    // file name -> what its applyTo pattern has to be
    public static TheoryData<string, string> Files => new()
    {
        { "postman-folder-pre-request.instructions.md", "**/*-pre-f-pm-*" },
        { "postman-folder-post-response.instructions.md", "**/*-post-f-pm-*" },
        { "postman-http-request-pre-request.instructions.md", "**/*-pre-r-pm-*" },
        { "postman-http-request-post-response.instructions.md", "**/*-post-r-pm-*" }
    };

    private static string Read(string fileName) =>
        RepositoryFiles.ReadAllText($"{InstructionsFolder}/{fileName}").Replace("\r\n", "\n");

    private static bool IsPreRequest(string fileName) => fileName.Contains("-pre-request.");

    // the one ```javascript block in the file
    private static string ExampleScript(string text)
    {
        var match = Regex.Match(text, @"```javascript\n(.*?)\n```", RegexOptions.Singleline);
        Assert.True(match.Success, "no javascript example block found");
        return match.Groups[1].Value;
    }

    // ---- the folder ----

    [Fact]
    public void TheInstructionsFolder_HoldsExactlyTheFourPostmanFiles()
    {
        var found = Directory.GetFiles(RepositoryFiles.PathTo(InstructionsFolder), "postman-*.instructions.md")
            .Select(path => Path.GetFileName(path)!)
            .Order()
            .ToList();

        Assert.Equal(Files.Select(row => (string)row[0]).Order().ToList(), found);
    }

    [Fact]
    public void EveryApplyToPattern_IsDifferent_SoNoFileFightsAnotherForTheSameScripts()
    {
        var patterns = Files.Select(row => (string)row[1]).ToList();

        Assert.Equal(patterns.Count, patterns.Distinct().Count());
    }

    // ---- front matter ----

    [Theory]
    [MemberData(nameof(Files))]
    public void FrontMatter_IsOneBlockWithTheExpectedApplyToPattern(string fileName, string expectedPattern)
    {
        var text = Read(fileName);

        var match = Regex.Match(text, @"\A---\napplyTo: '([^']+)'\n---\n");
        Assert.True(match.Success, $"{fileName} doesn't start with a front matter block that has an applyTo line");
        Assert.Equal(expectedPattern, match.Groups[1].Value);
        // exactly two fence lines: the one that opens the block and the one that closes it
        Assert.Equal(2, Regex.Matches(text, @"^---$", RegexOptions.Multiline).Count);
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void ApplyToPattern_SaysTheSameThingAsTheFileName(string fileName, string expectedPattern)
    {
        // folder files end in -f-, request files in -r-, and pre/post has to line up with the name too
        var kindLetter = fileName.Contains("-folder-") ? "f" : "r";
        var phase = IsPreRequest(fileName) ? "pre" : "post";

        Assert.Equal($"**/*-{phase}-{kindLetter}-pm-*", expectedPattern);
    }

    // ---- sections ----

    [Theory]
    [MemberData(nameof(Files))]
    public void EveryFile_HasTheSameSections(string fileName, string _)
    {
        var text = Read(fileName);

        foreach (var section in new[]
                 {
                     "## Instructions", "**Context:**", "**Rules:**", "**Guidelines:**",
                     "**Output Format:**", "**Quality:**", "**Example Output:**", "**Note:**"
                 })
        {
            // a section heading starts its line. the quoted "> **Note:**" lines inside the rules start with ">" so they don't count
            var headingCount = Regex.Matches(text, "^" + Regex.Escape(section), RegexOptions.Multiline).Count;
            Assert.True(headingCount == 1, $"{fileName} has {section} {headingCount} times, expected once");
        }
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void CodeFences_AreBalanced_AndThereIsExactlyOneJavaScriptExample(string fileName, string _)
    {
        var text = Read(fileName);

        Assert.Equal(0, Regex.Matches(text, @"^```", RegexOptions.Multiline).Count % 2);
        Assert.Single(Regex.Matches(text, @"^```javascript$", RegexOptions.Multiline));
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void DocumentationLinks_PointAtPostmanDocs_AndAreNotListedTwice(string fileName, string _)
    {
        var text = Read(fileName);

        var links = Regex.Matches(text, @"https?://[^\s)\]]+").Select(match => match.Value).ToList();
        // the "refer to the official docs" bullet list is the one place a link must not repeat
        // (a rule's note can point at the same page the list does, that's fine)
        var listedLinks = Regex.Matches(text, @"^\s+- (https?://\S+)$", RegexOptions.Multiline).Select(match => match.Groups[1].Value).ToList();

        Assert.NotEmpty(links);
        Assert.All(links, link => Assert.StartsWith("https://learning.postman.com/docs/", link));
        Assert.NotEmpty(listedLinks);
        Assert.Equal(listedLinks.Count, listedLinks.Distinct().Count());
    }

    // ---- examples follow the file's own rules ----

    [Theory]
    [MemberData(nameof(Files))]
    public void Example_ParsesAsBalancedCode(string fileName, string _)
    {
        var script = ExampleScript(Read(fileName));

        Assert.Null(FindUnbalancedBracket(script));
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void Example_DoesNotUseTsCheck_WhenTheNoteSaysNotTo(string fileName, string _)
    {
        var text = Read(fileName);
        var script = ExampleScript(text);

        // every one of these files says "Do not add the @ts-check comment", so the example can't have it
        Assert.Contains("Do not add the @ts-check comment", text);
        Assert.DoesNotContain("@ts-check", script);
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void PreRequestExamples_NeverTouchTheResponseOrWriteTests(string fileName, string _)
    {
        if (!IsPreRequest(fileName))
            return;

        var script = ExampleScript(Read(fileName));

        Assert.DoesNotContain("pm.response", script);
        Assert.DoesNotContain("pm.test(", script);
        Assert.DoesNotContain("pm.expect(", script);
        Assert.Contains("pm.", script);
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void PostResponseExamples_UseTestsAndChaiAssertions(string fileName, string _)
    {
        if (IsPreRequest(fileName))
            return;

        var script = ExampleScript(Read(fileName));

        Assert.Contains("pm.test('", script);
        Assert.Contains("pm.expect(", script);
        Assert.Contains("pm.response", script);
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void PostResponseExamples_NeverUseAVariableThatWasNotDeclared(string fileName, string _)
    {
        if (IsPreRequest(fileName))
            return;

        var script = ExampleScript(Read(fileName));

        var declared = Regex.Matches(script, @"\bconst\s+(\w+)\s*=").Select(match => match.Groups[1].Value).ToHashSet();
        var used = Regex.Matches(script, @"\b(response\w*Json)\b").Select(match => match.Groups[1].Value).ToHashSet();

        // the old examples declared responseBodyJson but then used responseJson, which doesn't exist
        Assert.All(used, name => Assert.True(declared.Contains(name), $"{name} is used in the example but never declared"));
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void PostResponseExamples_NeverParseTheResponseOutsideATestCallback(string fileName, string _)
    {
        if (IsPreRequest(fileName))
            return;

        var text = Read(fileName);
        var script = ExampleScript(text);

        // the file's own Rule 3: no global variable that holds pm.response.json()
        Assert.Contains("Never declare a global variable referring to `pm.response.json()`", text);
        var topLevelParse = script.Split('\n')
            .Where(line => line.Length > 0 && !char.IsWhiteSpace(line[0]))
            .Where(line => line.Contains("pm.response.json()"));
        Assert.Empty(topLevelParse);
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void ExampleTests_HaveClearNames(string fileName, string _)
    {
        if (IsPreRequest(fileName))
            return;

        var script = ExampleScript(Read(fileName));
        var testNames = Regex.Matches(script, @"pm\.test\('([^']+)'").Select(match => match.Groups[1].Value).ToList();

        Assert.True(testNames.Count >= 3, "the example should show a few different kinds of checks");
        Assert.Equal(testNames.Count, testNames.Distinct().Count());
        Assert.All(testNames, name => Assert.True(name.Split(' ').Length >= 3, $"'{name}' isn't descriptive"));
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void ThePreRequestNotes_AreWordedTheSameWayInBothPreRequestFiles(string fileName, string _)
    {
        if (!IsPreRequest(fileName))
            return;

        var text = Read(fileName);

        Assert.Contains("response object (`pm.response`) is NOT available in pre-request scripts", text);
        Assert.Contains("Do not write test cases in pre-request scripts", text);
        // and the post response files must not claim the opposite
        Assert.DoesNotContain("pm.response`) is available", text);
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void ThePostResponseRules_AreWordedTheSameWayInBothPostResponseFiles(string fileName, string _)
    {
        if (IsPreRequest(fileName))
            return;

        var text = Read(fileName);

        Assert.Contains("response object (`pm.response`) is available in test scripts", text);
        Assert.DoesNotContain("is NOT available", text);
    }

    // walks the script once and returns a message for the first (, {, or [ that never closes or closes wrong.
    // quotes, backticks and comments are skipped so a bracket inside a string doesn't count
    private static string? FindUnbalancedBracket(string script)
    {
        var openers = new Stack<char>();
        var pairs = new Dictionary<char, char> { [')'] = '(', ['}'] = '{', [']'] = '[' };

        for (var position = 0; position < script.Length; position++)
        {
            var character = script[position];

            if (character == '/' && position + 1 < script.Length && script[position + 1] == '/')
            {
                while (position < script.Length && script[position] != '\n')
                    position++;
                continue;
            }

            if (character is '\'' or '"' or '`')
            {
                var quote = character;
                position++;
                while (position < script.Length && script[position] != quote)
                {
                    if (script[position] == '\\')
                        position++;
                    position++;
                }
                continue;
            }

            if (character is '(' or '{' or '[')
                openers.Push(character);
            else if (pairs.TryGetValue(character, out var expectedOpener))
            {
                if (openers.Count == 0 || openers.Pop() != expectedOpener)
                    return $"unexpected '{character}' at position {position}";
            }
        }

        return openers.Count == 0 ? null : $"'{openers.Peek()}' is never closed";
    }
}
