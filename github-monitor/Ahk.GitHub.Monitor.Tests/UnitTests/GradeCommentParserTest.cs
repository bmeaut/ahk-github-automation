using System.Linq;
using Ahk.GitHub.Monitor.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ahk.GitHub.Monitor.Tests.UnitTests;

[TestClass]
public class GradeCommentParserTest
{
    [DataTestMethod]
    [DataRow("/ahk ok")]
    [DataRow("/ahk ok hello")]
    [DataRow("/ahk ok 1")]
    [DataRow("/ahk ok 1,2")]
    [DataRow("/ahk ok 1, 2")]
    [DataRow("/ahk ok 1, 2.5")]
    [DataRow("/ahk ok 1.33, 2.5, 44")]
    [DataRow("/ahk ok 1.33;2.5;44")]
    [DataRow("/ahk ok 1,33 2,5 44")]
    [DataRow("something\r\n\r\n/ahk ok")]
    [DataRow("something\r\n\r\n/ahk ok hello")]
    [DataRow("something\r\n\r\n/ahk ok 1")]
    [DataRow("something\r\n\r\n/ahk ok 1,2")]
    [DataRow("something\r\n\r\n/ahk ok 1, 2")]
    [DataRow("something\r\n\r\n/ahk ok 1, 2.5")]
    [DataRow("something\r\n\r\n/ahk ok 1.33, 2.5, 44")]
    [DataRow("something\r\n\r\n/ahk ok 1.33;2.5;44")]
    [DataRow("/ahk ok\r\n\r\nsomething")]
    [DataRow("/ahk ok hello\r\n\r\nsomething")]
    [DataRow("/ahk ok 1\r\n\r\nsomething")]
    [DataRow("/ahk ok 1,2\r\n\r\nsomething")]
    [DataRow("/ahk ok 1, 2\r\n\r\nsomething")]
    [DataRow("/ahk ok 1, 2.5\r\n\r\nsomething")]
    [DataRow("/ahk ok 1.33, 2.5, 44\r\n\r\nsomething")]
    [DataRow("/ahk ok 1.33;2.5;44\r\n\r\nsomething")]
    [DataRow("/ahk ok 1,33 2,5 44\r\n\r\nsomething")]
    public void IsGradeComment(string value) => Assert.IsTrue(new GradeCommentParser(value).IsMatch);

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("aaa")]
    [DataRow("@ok")]
    [DataRow("/ahkok")]
    [DataRow("/ahk okk")]
    [DataRow("ahk ok")]
    [DataRow("something\r\n\r\n/ahkok")]
    [DataRow("something\r\n\r\n/ahk okk")]
    [DataRow("something\r\n\r\nahk ok")]
    [DataRow("ahk ok\r\n\r\nsomething")]
    [DataRow("/ahkok\r\n\r\nsomething")]
    [DataRow("/ahk okk\r\n\r\nsomething")]
    public void IsNotGradeComment(string value) => Assert.IsFalse(new GradeCommentParser(value).IsMatch);

    [DataTestMethod]
    [DataRow("/ahk ok")]
    [DataRow("/ahk ok hello")]
    [DataRow("/ahk ok 1", 1)]
    [DataRow("/ahk ok 1,2", 1.2)]
    [DataRow("/ahk ok 1, 2", 1, 2)]
    [DataRow("/ahk ok 1,2 2", 1.2, 2)]
    [DataRow("/ahk ok 1, 2.5", 1, 2.5)]
    [DataRow("/ahk ok 1.33, 2.5, 44", 1.33, 2.5, 44)]
    [DataRow("/ahk ok 1.33;2.5;44", 1.33, 2.5, 44)]
    [DataRow("/ahk ok 1,33 2,5 44", 1.33, 2.5, 44)]
    [DataRow("something\r\n\r\n/ahk ok")]
    [DataRow("something\r\n\r\n/ahk ok hello")]
    [DataRow("something\r\n\r\n/ahk ok 1", 1)]
    [DataRow("something\r\n\r\n/ahk ok 1,2", 1.2)]
    [DataRow("something\r\n\r\n/ahk ok 1, 2", 1, 2)]
    [DataRow("something\r\n\r\n/ahk ok 1,2 2", 1.2, 2)]
    [DataRow("something\r\n\r\n/ahk ok 1, 2.5", 1, 2.5)]
    [DataRow("something\r\n\r\n/ahk ok 1.33, 2.5, 44", 1.33, 2.5, 44)]
    [DataRow("something\r\n\r\n/ahk ok 1.33;2.5;44", 1.33, 2.5, 44)]
    [DataRow("something\r\n\r\n/ahk ok 1,33 2,5 44", 1.33, 2.5, 44)]
    [DataRow("/ahk ok\r\n\r\nsomething")]
    [DataRow("/ahk ok hello\r\n\r\nsomething")]
    [DataRow("/ahk ok 1\r\n\r\nsomething", 1)]
    [DataRow("/ahk ok 1,2\r\n\r\nsomething", 1.2)]
    [DataRow("/ahk ok 1, 2\r\n\r\nsomething", 1, 2)]
    [DataRow("/ahk ok 1,2 2\r\n\r\nsomething", 1.2, 2)]
    [DataRow("/ahk ok 1, 2.5\r\n\r\nsomething", 1, 2.5)]
    [DataRow("/ahk ok 1.33, 2.5, 44\r\n\r\nsomething", 1.33, 2.5, 44)]
    [DataRow("/ahk ok 1.33;2.5;44\r\n\r\nsomething", 1.33, 2.5, 44)]
    [DataRow("/ahk ok 1,33 2,5 44\r\n\r\nsomething", 1.33, 2.5, 44)]
    public void GradesAreParsed(string value, params double[] expectedGrades)
    {
        var actualGrades = new GradeCommentParser(value);
        Assert.AreEqual(expectedGrades.Length > 0, actualGrades.HasGrades);
        CollectionAssert.AreEqual(expectedGrades, actualGrades.GradesWithOrder.Values.ToList());
    }
}
