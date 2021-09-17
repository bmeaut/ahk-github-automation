using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Ahk.GradeManagement.Tests.UnitTests
{
    [TestClass]
    public class HmacSha256ValidatorTest
    {
        private const string Secret = "Wcks02cnncc67c33";
        private const string HttpVerb = "POST";
        private const string HttpUrl = "https://my.url.com/address";
        private static DateTime Date = new DateTime(2021, 9, 1, 13, 34, 56, 0, DateTimeKind.Utc);

        [DataTestMethod]
        [DataRow("aaaaaa\r\nbbbbbbb\r\ncccccccccc\r\n", "SGAhL9hfzLqi30G1uqtQyErRC4oKBlxT9NImaJ/V9CQ=")]
        [DataRow("qqqq\r\nsdfsdfsdfsdf\r\nwwwwwwwwwwwww\r\n", "K7lZXguubpUONKhHh40lAzxt2vPyZnm6LkjLhrYPwAo=")]
        [DataRow("aaaaaaqqqqqqqqqqqqqqq", "cN9KEIb9uO7VskC9mmZ7wWkzqOXirFXcjqB3i4cK0mA=")]
        public void SignatureIsValid(string payload, string expectedSignature)
        {
            Assert.IsTrue(HmacSha256Validator.IsSignatureValid(HttpVerb, HttpUrl, Date, payload, expectedSignature, Secret));
        }

        [DataTestMethod]
        [DataRow("aaaaaa\r\nbbbbbbb\r\ncccccccccc\r\n", "SGAhL9hfzLqi30G1uqtQyErRC4oKBlxT9NImaJ/V9CQ=")]
        [DataRow("qqqq\r\nsdfsdfsdfsdf\r\nwwwwwwwwwwwww\r\n", "K7lZXguubpUONKhHh40lAzxt2vPyZnm6LkjLhrYPwAo=")]
        [DataRow("aaaaaaqqqqqqqqqqqqqqq", "cN9KEIb9uO7VskC9mmZ7wWkzqOXirFXcjqB3i4cK0mA=")]
        public void SignatureNotValidIfHttpVerbIsDifferent(string payload, string expectedSignature)
        {
            Assert.IsFalse(HmacSha256Validator.IsSignatureValid("WRONGVERB", HttpUrl, Date, payload, expectedSignature, Secret));
        }

        [DataTestMethod]
        [DataRow("aaaaaa\r\nbbbbbbb\r\ncccccccccc\r\n", "SGAhL9hfzLqi30G1uqtQyErRC4oKBlxT9NImaJ/V9CQ=")]
        [DataRow("qqqq\r\nsdfsdfsdfsdf\r\nwwwwwwwwwwwww\r\n", "K7lZXguubpUONKhHh40lAzxt2vPyZnm6LkjLhrYPwAo=")]
        [DataRow("aaaaaaqqqqqqqqqqqqqqq", "cN9KEIb9uO7VskC9mmZ7wWkzqOXirFXcjqB3i4cK0mA=")]
        public void SignatureNotValidIfHttpUrlIsDifferent(string payload, string expectedSignature)
        {
            Assert.IsFalse(HmacSha256Validator.IsSignatureValid(HttpVerb, HttpUrl + "wrongurl", Date, payload, expectedSignature, Secret));
        }


        [DataTestMethod]
        [DataRow("aaaaaa\r\nbbbbbbb\r\ncccccccccc\r\n", "SGAhL9hfzLqi30G1uqtQyErRC4oKBlxT9NImaJ/V9CQ=")]
        [DataRow("qqqq\r\nsdfsdfsdfsdf\r\nwwwwwwwwwwwww\r\n", "K7lZXguubpUONKhHh40lAzxt2vPyZnm6LkjLhrYPwAo=")]
        [DataRow("aaaaaaqqqqqqqqqqqqqqq", "cN9KEIb9uO7VskC9mmZ7wWkzqOXirFXcjqB3i4cK0mA=")]
        public void SignatureNotValidIfDateIsDifferent(string payload, string expectedSignature)
        {
            Assert.IsFalse(HmacSha256Validator.IsSignatureValid(HttpVerb, HttpUrl, Date.AddSeconds(1), payload, expectedSignature, Secret));
        }


        [DataTestMethod]
        [DataRow("aaaaaa\r\nbbbbbbb\r\ncccccccccc\r\n", "SGAhL9hfzLqi30G1uqtQyErRC4oKBlxT9NImaJ/V9CQ=")]
        [DataRow("qqqq\r\nsdfsdfsdfsdf\r\nwwwwwwwwwwwww\r\n", "K7lZXguubpUONKhHh40lAzxt2vPyZnm6LkjLhrYPwAo=")]
        [DataRow("aaaaaaqqqqqqqqqqqqqqq", "cN9KEIb9uO7VskC9mmZ7wWkzqOXirFXcjqB3i4cK0mA=")]
        public void SignatureNotValidIfPayloadIsDifferent(string payload, string expectedSignature)
        {
            Assert.IsFalse(HmacSha256Validator.IsSignatureValid(HttpVerb, HttpUrl, Date, payload + "a", expectedSignature, Secret));
        }
    }
}
