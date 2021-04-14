using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ahk.GitHub.Monitor.Tests.UnitTests
{
    [TestClass]
    public class PayloadSignatureValidatorTest
    {
        private const string Secret = "Wcks02cnncc67c33";

        [DataTestMethod]
        [DataRow("aaaaaa\r\nbbbbbbb\r\ncccccccccc\r\n", "sha1=d9d56ec2e6dd4835db81ef5a4199f75e95f67db9")]
        [DataRow("qqqq\r\nsdfsdfsdfsdf\r\nwwwwwwwwwwwww\r\n", "sha1=1cc31ba7358a85c512dff4e2a37ff5aa9be65e9f")]
        [DataRow("aaaaaaqqqqqqqqqqqqqqq", "sha1=db2c8c2df329e4bd9f9cb59d15f977b6f6c43d6f")]
        public void SignatureIsValid(string payload, string expectedSignature)
        {
            Assert.IsTrue(PayloadValidator.IsSignatureValid(getPayloadBytes(payload), expectedSignature, Secret));
        }

        [DataTestMethod]
        [DataRow("aaaaaa\r\nbbbbbbb\r\ncccccccccc\r\n", "sha1=dummy")]
        [DataRow("aaaaaa\r\nbbbbbbb\r\ncccccccccc\r\n", "sha1=aaaaaaaa")]
        [DataRow("aaaaaa\r\nbbbbbbb\r\ncccccccccc\r\n", "dummy")]
        [DataRow("aaaaaa\r\nbbbbbbb\r\ncccccccccc\r\n", "")]
        public void SignatureIsNotValid(string payload, string expectedSignature)
        {
            Assert.IsFalse(PayloadValidator.IsSignatureValid(getPayloadBytes(payload), expectedSignature, Secret));
        }

        private byte[] getPayloadBytes(string payload)
        {
            using var s = new MemoryStream();
            using var w = new StreamWriter(s);
            w.Write(payload);
            w.Flush();
            return s.ToArray();
        }
    }
}
