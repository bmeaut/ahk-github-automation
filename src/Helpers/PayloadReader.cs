using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

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
        private MemoryStream content;
        private HttpRequest request;

        public PayloadReader(HttpRequest request)
            => this.request = request;

        private async Task assureInitialized()
        {
            if (content == null)
            {
                content = new MemoryStream();
                await request.Body.CopyToAsync(content);
            }
        }

        public async Task<byte[]> ReadAsByteArray()
        {
            await assureInitialized();
            return content.ToArray();
        }

        public async Task<string> ReadAsString()
        {
            await assureInitialized();
            content.Seek(0, SeekOrigin.Begin);
            return new StreamReader(content).ReadToEnd();
        }
    }
}
