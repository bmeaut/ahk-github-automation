using Ahk.GradeManagement.Client.Services;

using Ahk.GradeManagement.Client.Network;

namespace Ahk.GradeManagement.Client.Network;

public class ExceptionMessageHandler(CrudSnackbarService _crudSnackbarService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Attempt to send the request and get the response
            var response = await base.SendAsync(request, cancellationToken);

            // You might want to throw an exception for non-success status codes depending on your case.
            if (!response.IsSuccessStatusCode)
            {
                // Log response status code etc.

                // Optionally read content for more details
                var content = await response.Content.ReadAsStringAsync();

                // Throw or handle based on status code/content etc.
                //throw new CustomApiException(response.StatusCode, content);
                _crudSnackbarService.ShowError($"Error: {response.StatusCode} - {content}");
            }

            return response;
        }
        catch (ApiException ex)
        {
            // Handle API exceptions

            // Log exception etc.

            // Optionally read content for more details
            _crudSnackbarService.ShowError($"Error: {ex.StatusCode} - {ex.Response}");
        }
        catch (Exception ex)
        {
            // Handle other exceptions
            _crudSnackbarService.ShowError($"Error: {ex.Message}");
        }

        return null;
    }
}
