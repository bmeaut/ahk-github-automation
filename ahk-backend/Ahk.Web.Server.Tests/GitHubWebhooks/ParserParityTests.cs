using Ahk.Web.Services.GitHubWebhooks;
using Xunit;

namespace Ahk.Web.Server.Tests.GitHubWebhooks;

/// <summary>
/// The <c>.github/ahk-monitor.yml</c> opt-in gate. Cases ported verbatim from
/// <c>github-monitor/.../UnitTests/ConfigYamlParserTest.cs</c> — existing course templates carry these exact
/// spellings, so the accepted set is a compatibility contract, not a style choice.
/// </summary>
public class ConfigYamlParserTests
{
    [Theory]
    [InlineData("enabled")]
    [InlineData("enabled: true")]
    [InlineData("enabled: yes")]
    [InlineData("enabled: 1")]
    [InlineData("enabled: true\r")]
    [InlineData("enabled: true\n")]
    [InlineData("enabled: true\r\n")]
    [InlineData("enabled: true\r\naaa: 1")]
    [InlineData("aaa: 1\r\nenabled: true")]
    public void ConfigYamlIsEnabled(string value) => Assert.True(ConfigYamlParser.IsEnabled(value));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("aaa")]
    [InlineData("enabl")]
    [InlineData("enabled: false")]
    [InlineData("enabled: no")]
    [InlineData("enabled: 0")]
    [InlineData("enabled: maybe")]
    [InlineData("enabled hello")]
    public void ConfigYamlIsDisabled(string? value) => Assert.False(ConfigYamlParser.IsEnabled(value));
}

/// <summary>
/// The <c>/ahk ok</c> chatops grammar. Cases ported verbatim from
/// <c>github-monitor/.../UnitTests/GradeCommentParserTest.cs</c>: teachers have this syntax in their fingers
/// and any narrowing of it silently stops grading a pull request.
/// </summary>
public class GradeCommentParserTests
{
    [Theory]
    [InlineData("/ahk ok")]
    [InlineData("/ahk ok hello")]
    [InlineData("/ahk ok 1")]
    [InlineData("/ahk ok 1,2")]
    [InlineData("/ahk ok 1, 2")]
    [InlineData("/ahk ok 1, 2.5")]
    [InlineData("/ahk ok 1.33, 2.5, 44")]
    [InlineData("/ahk ok 1.33;2.5;44")]
    [InlineData("/ahk ok 1,33 2,5 44")]
    [InlineData("something\r\n\r\n/ahk ok")]
    [InlineData("something\r\n\r\n/ahk ok 1.33, 2.5, 44")]
    [InlineData("/ahk ok\r\n\r\nsomething")]
    [InlineData("/ahk ok 1.33, 2.5, 44\r\n\r\nsomething")]
    [InlineData("/AHK OK 5")]
    public void IsGradeComment(string value) => Assert.True(new GradeCommentParser(value).IsMatch);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("aaa")]
    [InlineData("@ok")]
    [InlineData("/ahkok")]
    [InlineData("/ahk okk")]
    [InlineData("ahk ok")]
    [InlineData("something\r\n\r\n/ahkok")]
    [InlineData("something\r\n\r\n/ahk okk")]
    [InlineData("ahk ok\r\n\r\nsomething")]
    public void IsNotGradeComment(string? value) => Assert.False(new GradeCommentParser(value).IsMatch);

    [Theory]
    [InlineData("/ahk ok", new double[0])]
    [InlineData("/ahk ok hello", new double[0])]
    [InlineData("/ahk ok 1", new[] { 1d })]
    [InlineData("/ahk ok 1,2", new[] { 1.2 })]
    [InlineData("/ahk ok 1, 2", new[] { 1d, 2d })]
    [InlineData("/ahk ok 1,2 2", new[] { 1.2, 2d })]
    [InlineData("/ahk ok 1, 2.5", new[] { 1d, 2.5 })]
    [InlineData("/ahk ok 1.33, 2.5, 44", new[] { 1.33, 2.5, 44d })]
    [InlineData("/ahk ok 1.33;2.5;44", new[] { 1.33, 2.5, 44d })]
    [InlineData("/ahk ok 1,33 2,5 44", new[] { 1.33, 2.5, 44d })]
    [InlineData("something\r\n\r\n/ahk ok 1, 2.5", new[] { 1d, 2.5 })]
    [InlineData("/ahk ok 1.33, 2.5, 44\r\n\r\nsomething", new[] { 1.33, 2.5, 44d })]
    public void GradesAreParsed(string value, double[] expectedGrades)
    {
        var parsed = new GradeCommentParser(value);

        Assert.Equal(expectedGrades.Length > 0, parsed.HasGrades);
        Assert.Equal(expectedGrades, parsed.Grades);
    }

    /// <summary>
    /// The parse loop deliberately does not stop at the first match, so a comment that corrects itself grades
    /// with the correction. Looks like a bug, is behaviour — pinned so nobody "fixes" it.
    /// </summary>
    [Fact]
    public void LastMatchingLineWins()
    {
        var parsed = new GradeCommentParser("/ahk ok 1 2\r\nsorry, I meant:\r\n/ahk ok 3 4");

        Assert.True(parsed.IsMatch);
        Assert.Equal(new[] { 3d, 4d }, parsed.Grades);
    }
}
