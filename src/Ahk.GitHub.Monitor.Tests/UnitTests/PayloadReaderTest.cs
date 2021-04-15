using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ahk.GitHub.Monitor.Tests.UnitTests
{
    [TestClass]
    public class PayloadReaderTest
    {
        [TestMethod]
        public async Task PayloadReaderAllowsMultiRead()
        {
            const string expectedContent = "Hello World";

            var req = new DefaultHttpRequest(new DefaultHttpContext());
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            writer.Write(expectedContent);
            writer.Flush();
            stream.Position = 0;
            req.Body = stream;

            var reader = new PayloadReader(req);
            CollectionAssert.AreEqual(stream.ToArray(), await reader.ReadAsByteArray());
            Assert.AreEqual(expectedContent, await reader.ReadAsString());
        }
    }
}
