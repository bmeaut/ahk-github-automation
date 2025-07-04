using GradeManagement.Client.Services;

using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace GradeManagement.Client.Network;

public class SubjectHeaderHandler(SelectedSubjectService SelectedSubjectService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("SubjectHeaderHandler");
        var subjectId = SelectedSubjectService.CurrentSubject?.Id;
        if (subjectId is not null)
        {
            request.Headers.Add("X-Subject-Id-Value", subjectId.ToString());
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
