using System.Collections.Generic;
using System.IO;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Ahk.GitHub.Monitor.Tests.IntegrationTests;

public class MockHttpResponseData : HttpResponseData
{
    public MockHttpResponseData(FunctionContext functionContext) : base(functionContext)
    {
        Headers = new HttpHeadersCollection();
        Body = new MemoryStream();
    }

    public override HttpStatusCode StatusCode { get; set; }

    public override HttpHeadersCollection Headers { get; set; }

    public override Stream Body { get; set; }
    public override HttpCookies Cookies { get; }
}
