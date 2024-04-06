using GradeManagement.Client.Pages;
using GradeManagement.Shared.Dtos;
using GradeManagement.Shared.Dtos.Response;

using System.Net.Http.Json;

namespace GradeManagement.Client.Services;

public class SubjectService(HttpClient Http)
{
    private Subject _currentSubject;

    public Subject CurrentSubject
    {
        get => _currentSubject;
        set
        {
            if (_currentSubject == value) return;
            _currentSubject = value;
            NotifyStateChanged();
        }
    }

    public event Action OnChange;

    private void NotifyStateChanged() => OnChange?.Invoke();

    public async Task<List<Subject>> LoadSubjects()
    {
        var subjects = await Http.GetFromJsonAsync<List<Subject>>(Endpoints.SUBJECTS) ?? [];
        if (_currentSubject is null && subjects.Count > 0)
        {
            _currentSubject = subjects[0];
        }

        return subjects;
    }

    public async Task<List<Course>> LoadCourses(bool loadAll = false)
    {
        return await Http.GetFromJsonAsync<List<Course>>(Endpoints.COURSES) ?? [];
    }

    public async Task<List<Semester>> LoadSemesters(bool loadAll = false)
    {
        return await Http.GetFromJsonAsync<List<Semester>>(Endpoints.SEMESTERS) ?? [];
    }

    public async Task<List<Language>> LoadLanguages(bool loadAll = false)
    {
        return await Http.GetFromJsonAsync<List<Language>>(Endpoints.LANGUAGES) ?? [];
    }

    public async Task<List<User>> LoadTeachers(bool loadAll = false)
    {
        return await Http.GetFromJsonAsync<List<User>>(Endpoints.TEACHERS) ?? [];
    }

    public async Task<List<Student>> LoadStudents(bool loadAll = false)
    {
        return await Http.GetFromJsonAsync<List<Student>>(Endpoints.STUDENTS) ?? [];
    }

    public async Task<List<Group>> LoadGroups(bool loadAll = false)
    {
        return await Http.GetFromJsonAsync<List<Group>>(Endpoints.GROUPS) ?? [];
    }

    public async Task<List<Exercise>> LoadExercises(bool loadAll = false)
    {
        return await Http.GetFromJsonAsync<List<Exercise>>(Endpoints.EXERCISES) ?? [];
    }
}
