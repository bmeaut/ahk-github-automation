using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Ahk.GradeManagement.Client.Network;

public class AuthorizationMessageHandler(IAccessTokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var accessTokenResult = await tokenProvider.RequestAccessToken();

        if (accessTokenResult.TryGetToken(out var accessToken))
        {
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken.Value);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
