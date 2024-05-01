using GradeManagement.Client.Network;
using GradeManagement.Client.Pages;

using System.Net.Http.Json;

namespace GradeManagement.Client.Services;

public class ExerciseService(HttpClient Http, CrudSnackbarService SnackbarService)
{
    private readonly string _baseUrl = Endpoints.EXERCISES;

    public async Task CreateExercise(Exercise exercise)
    {
        await Http.PostAsJsonAsync(Endpoints.EXERCISES, exercise);
    }

    public async Task UpdateExercise(Exercise exercise)
    {
        var response = await Http.PutAsJsonAsync($"{_baseUrl}/{exercise.Id}", exercise);
        if (!response.IsSuccessStatusCode)
        {
            await Console.Error.WriteLineAsync("Error");
            SnackbarService.ShowEditError();
        }
        else
        {
            SnackbarService.ShowEditSuccess();
        }
    }

    public async Task DeleteExercise(long id)
    {
        var response = await Http.DeleteAsync($"{_baseUrl}/{id}");
        if (!response.IsSuccessStatusCode)
        {
            await Console.Error.WriteLineAsync("Error");
            SnackbarService.ShowDeleteError();
        }
        else
        {
            SnackbarService.ShowDeleteSuccess();
        }
    }
}
