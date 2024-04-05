using MudBlazor;

namespace GradeManagement.Client.Services;

public class CrudSnackbarService(ISnackbar snackbar)
{
    public void ShowMessage(string message, Severity severity = Severity.Normal)
    {
        snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomCenter;
        snackbar.Add(message, severity);
    }

    public void ShowSuccess(string message)
    {
        ShowMessage(message, Severity.Success);
    }

    public void ShowError(string message)
    {
        ShowMessage(message, Severity.Error);
    }

    public void ShowEditSuccess(string message = "Edit successful")
    {
        ShowSuccess(message);
    }

    public void ShowEditError(string message = "Edit failed")
    {
        ShowError(message);
    }

    public void ShowAddSuccess(string message = "Add successful")
    {
        ShowSuccess(message);
    }

    public void ShowAddError(string message = "Add failed")
    {
        ShowError(message);
    }

    public void ShowDeleteSuccess(string message = "Delete successful")
    {
        ShowSuccess(message);
    }

    public void ShowDeleteError(string message = "Delete failed")
    {
        ShowError(message);
    }
}
