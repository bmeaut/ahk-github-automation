using Ahk.GitHub.Monitor.EventHandlers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ahk.GitHub.Monitor.Tests.UnitTests
{
    [TestClass]
    public class ConfigYamlParserTest
    {
        [DataTestMethod]
        [DataRow("enabled")]
        [DataRow("enabled: true")]
        [DataRow("enabled: yes")]
        [DataRow("enabled: 1")]
        [DataRow("enabled: true\r")]
        [DataRow("enabled: true\n")]
        [DataRow("enabled: true\r\n")]
        [DataRow("enabled: true\r\naaa: 1")]
        [DataRow("aaa: 1\r\nenabled: true")]
        public void ConfigYamlIsEnabled(string value)
        {
            Assert.IsTrue(ConfigYamlParser.IsEnabled(value));
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("aaa")]
        [DataRow("enabl")]
        [DataRow("enabled: false")]
        [DataRow("enabled: no")]
        [DataRow("enabled: 0")]
        [DataRow("enabled: maybe")]
        [DataRow("enabled hello")]
        public void ConfigYamlIsDisabled(string value)
        {
            Assert.IsFalse(ConfigYamlParser.IsEnabled(value));
        }
    }
}
