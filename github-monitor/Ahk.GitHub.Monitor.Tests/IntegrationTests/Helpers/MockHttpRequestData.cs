using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Ahk.GitHub.Monitor.Tests.IntegrationTests;

public class MockHttpRequestData : HttpRequestData
{
    private readonly HttpResponseData _response;

    public MockHttpRequestData(FunctionContext functionContext, Stream body) : base(functionContext)
    {
        Body = body;
        Headers = new HttpHeadersCollection();
        _response = new MockHttpResponseData(functionContext);
    }

    public override Stream Body { get; }

    public override HttpHeadersCollection Headers { get; }

    public override IReadOnlyCollection<IHttpCookie> Cookies { get; } = new List<IHttpCookie>();

    public override Uri Url { get; } = new Uri("https://localhost");

    public override IEnumerable<ClaimsIdentity> Identities { get; } = new List<ClaimsIdentity>();

    public override string Method { get; } = "POST";

    public override HttpResponseData CreateResponse() => _response;
}
