using Ahk.GradeManagement.Client.Services;
using Ahk.GradeManagement.Shared.Constants;

using System.Globalization;

namespace Ahk.GradeManagement.Client.Network;

public class SubjectHeaderHandler(SelectedSubjectService selectedSubjectService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var subjectId = selectedSubjectService.CurrentSubject?.Id;
        if (subjectId is not null)
        {
            request.Headers.Add(Headers.XSubjectId, subjectId.Value.ToString(CultureInfo.InvariantCulture));
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
