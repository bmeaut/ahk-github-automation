using GradeManagement.Client.Pages;
using GradeManagement.Shared.Dtos;

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
        return await Http.GetFromJsonAsync<List<Subject>>(Endpoints.SUBJECTS) ?? [];
    }

    public async Task<List<Course>> LoadCourses(bool loadAll = false)
    {
        return
        [
            new Course
            {
                Id = 1,
                Name = "Course 1",
                MoodleCourseId = "1234",
                Semester = new Semester { Id = 1, Name = "Semester 1" },
                Subject = new Subject { Id = 1, Name = "Subject 1" },
                Language = new Language { Id = 1, Name = "Language 1" }
            },
        ];
        return await Http.GetFromJsonAsync<List<Course>>(Endpoints.COURSES) ?? [];
    }

    public async Task<List<Semester>> LoadSemesters(bool loadAll = false)
    {
        return
        [
            new Semester { Id = 1, Name = "Semester 1", },
        ];
        return await Http.GetFromJsonAsync<List<Semester>>(Endpoints.SEMESTERS) ?? [];
    }

    public async Task<List<Language>> LoadLanguages(bool loadAll = false)
    {
        return
        [
            new Language { Id = 1, Name = "Language 1", },
        ];
        return await Http.GetFromJsonAsync<List<Language>>(Endpoints.LANGUAGES) ?? [];
    }

    public async Task<List<Teacher>> LoadTeachers(bool loadAll = false)
    {
        return
        [
            new Teacher
            {
                Id = 1,
                Name = "Teacher 1",
                NeptunCode = "ABC123",
                GithubId = "github",
                BmeEmail = "email@edu.bme.hu",
            },
        ];
        return await Http.GetFromJsonAsync<List<Teacher>>(Endpoints.TEACHERS) ?? [];
    }

    public async Task<List<Student>> LoadStudents(bool loadAll = false)
    {
        return
        [
            new Student { Id = 1, Name = "Student 1", NeptunCode = "ABC123" },
        ];
        return await Http.GetFromJsonAsync<List<Student>>(Endpoints.STUDENTS) ?? [];
    }

    public async Task<List<Group>> LoadGroups(bool loadAll = false)
    {
        return
        [
            new() { Id = 1, Name = "Group 1" },
            new() { Id = 2, Name = "Group 2" },
            new() { Id = 3, Name = "Group 3" },
            new() { Id = 4, Name = "Group 4" }
        ];
        return await Http.GetFromJsonAsync<List<Group>>(Endpoints.GROUPS) ?? [];
    }

    public async Task<List<Exercise>> LoadExercises(bool loadAll = false)
    {
        return
        [
            new() { Id = 1, Name = "Exercise 1" },
            new() { Id = 2, Name = "Exercise 2" },
            new() { Id = 3, Name = "Exercise 3" },
            new() { Id = 4, Name = "Exercise 4" }
        ];
        return await Http.GetFromJsonAsync<List<Exercise>>(Endpoints.EXERCISES) ?? [];
    }
}
