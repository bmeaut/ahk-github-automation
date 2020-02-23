using Microsoft.AspNetCore.Http;
using System.IO;

namespace Ahk.GitHub.Monitor
{
    /// <summary>
    /// Helps with reading the request payload both as byte array and as string.
    /// </summary>
    /// <remarks>
    /// The HttpRequest content is not guaranteed to be readable twice, but we do need to read twice.
    /// Keep a buffered copy as an in-memory stream.
    /// </remarks>
    public class PayloadReader
    {
        private readonly MemoryStream content = new MemoryStream();

        public PayloadReader(HttpRequest request)
            => request.Body.CopyTo(content);

        public byte[] ReadAsByteArray()
            => content.ToArray();

        public string ReadAsString()
        {
            content.Seek(0, SeekOrigin.Begin);
            return new StreamReader(content).ReadToEnd();
        }
    }
}
